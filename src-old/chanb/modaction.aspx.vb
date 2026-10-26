Partial Public Class _modaction
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Dim isMod As Boolean = False
        If Not GetCookie(Context, "mod") = "" Then isMod = CBool(GetCookie(Context, "mod"))

        If isMod Then
            Dim id As Integer = 0
            Try
                id = CInt(Request.Item("id"))
            Catch ex As Exception
                Response.Write(FormatHTMLMessage(errorStr, invalidIdStr, "", "4444", True))
                Response.End()
            End Try
            Dim powers As String() = GetCookie(Context, "credpower").Split(CChar("-"))
            Select Case Request.Item("action")
                Case "banpost"
                    If powers(0) = "0" Then
                        Response.Write(FormatHTMLMessage(ForbiddenStr, modNoPower, "", "8888", True))
                    Else
                        Dim silentBan As Boolean = False
                        Try
                            silentBan = CBool(Request.Item("sib"))
                        Catch ex As Exception
                            silentBan = False
                        End Try
                        BanPosterByPost(id, silentBan, GetCookie(Context, "modname"), Request.Item("reason"))
                        Response.Write(FormatHTMLMessage(sucessStr, modBannedPosterStr.Replace("%", CStr(id)), "", "5555", False))
                    End If
                Case "delpost"
                    If powers(1) = "0" Then
                        Response.Write(FormatHTMLMessage(ForbiddenStr, modNoPower, "", "8888", True))
                    Else
                        PrunePost(id, AutoDeleteFiles)
                        Response.Write(FormatHTMLMessage(sucessStr, PostDeletedSuccess.Replace("%", CStr(id)), "", "88888", False))
                    End If
                Case "tgsticky"
                    If powers(2) = "0" Then
                        Response.Write(FormatHTMLMessage(ForbiddenStr, modNoPower, "", "8888", True))
                    Else
                        ToggleSticky(id)
                        Response.Write(FormatHTMLMessage(sucessStr, modRequetComplete, "", "8888", False))
                    End If
                Case "tglock"
                    If powers(3) = "0" Then
                        Response.Write(FormatHTMLMessage(ForbiddenStr, modNoPower, "", "8888", True))
                    Else
                        ToggleLock(id)
                        Response.Write(FormatHTMLMessage(sucessStr, modRequetComplete, "", "8888", False))
                    End If
                Case "editpost"
                    If powers(4) = "0" Then
                        Response.Write(FormatHTMLMessage(ForbiddenStr, modNoPower, "", "8888", True))
                    Else
                        If Request.Item("new_data") Is Nothing Or Request.Item("new_data") = String.Empty Then
                            Response.Redirect(WebRoot & "modEditpost.aspx?id=" & id)
                        Else
                            Dim newData As String = Request.Item("new_data")
                            GlobalFunctions.UpdatePostText(id, newData, False)
                            Response.Write(FormatHTMLMessage(sucessStr, modRequetComplete, "", "8888", False))
                        End If
                    End If
                Case "delfiles"
                    ' Bulk cleanup: remove every attachment named in a CSV list of tokens
                    ' (gathered while triaging several reports at once), in one pass.
                    If powers(1) = "0" Then
                        Response.Write(FormatHTMLMessage(ForbiddenStr, modNoPower, "", "8888", True))
                    Else
                        Dim rawCns As String = Request.Item("cns")
                        If rawCns Is Nothing Or rawCns = "" Then
                            Response.Write(FormatHTMLMessage(errorStr, "No files selected", "", "44", False))
                        Else
                            GlobalFunctions.BulkRemoveAttachments(rawCns.Split(CChar(",")))
                            Response.Write(FormatHTMLMessage(sucessStr, "Files removed", "", "44", False))
                        End If
                    End If
                Case "delfilessafe"
                    ' Same bulk-cleanup tool, validated per entry against the database.
                    If powers(1) = "0" Then
                        Response.Write(FormatHTMLMessage(ForbiddenStr, modNoPower, "", "8888", True))
                    Else
                        Dim rawCnsSafe As String = Request.Item("cns")
                        If rawCnsSafe Is Nothing Or rawCnsSafe = "" Then
                            Response.Write(FormatHTMLMessage(errorStr, "No files selected", "", "44", False))
                        Else
                            GlobalFunctions.BulkRemoveAttachmentsSafe(rawCnsSafe.Split(CChar(",")))
                            Response.Write(FormatHTMLMessage(sucessStr, "Files removed", "", "44", False))
                        End If
                    End If
                Case "delcompat"
                    ' Legacy mod-panel client compatibility path -- see
                    ' ChanbLegacyRawResolver/ChanbValidatedScopedResolver in GlobalFunctions.vb.
                    If powers(1) = "0" Then
                        Response.Write(FormatHTMLMessage(ForbiddenStr, modNoPower, "", "8888", True))
                    Else
                        Dim oResolver As ChanbAttachmentReferenceResolverBase = New ChanbLegacyRawResolver
                        Dim target As String = oResolver.ResolvePath(Request.Item("cn"), id)
                        ' SINK: PLANTED-VB-HR-49
                        GlobalFunctions.Delete_File_Fromdisk(target)
                        Response.Write(FormatHTMLMessage(sucessStr, "File removed", "", "44", False))
                    End If
                Case "delcompatsecure"
                    ' Current mod-panel client -- same shared call site shape, resolved and
                    ' scope-checked through the database instead.
                    If powers(1) = "0" Then
                        Response.Write(FormatHTMLMessage(ForbiddenStr, modNoPower, "", "8888", True))
                    Else
                        Dim oResolverSafe As ChanbAttachmentReferenceResolverBase = New ChanbValidatedScopedResolver
                        Dim targetSafe As String = oResolverSafe.ResolvePath(Request.Item("cn"), id)
                        If targetSafe <> "" Then
                            ' SAFE_SINK: PLANTED-VB-HR-49-safe
                            GlobalFunctions.Delete_File_Fromdisk(targetSafe)
                            Response.Write(FormatHTMLMessage(sucessStr, "File removed", "", "44", False))
                        Else
                            Response.Write(FormatHTMLMessage(errorStr, "File not in scope for this post", "", "44", False))
                        End If
                    End If
                Case "flagfile"
                    ' Mod flags an attachment token for a later, combined purge while
                    ' triaging reports one at a time -- the token is simply accumulated.
                    If powers(1) = "0" Then
                        Response.Write(FormatHTMLMessage(ForbiddenStr, modNoPower, "", "8888", True))
                    Else
                        Dim flagged As List(Of String) = TryCast(Session("FlaggedPurge"), List(Of String))
                        If flagged Is Nothing Then
                            flagged = New List(Of String)
                        End If
                        flagged.Add(Request.Item("cn"))
                        Session("FlaggedPurge") = flagged
                        Response.Write(FormatHTMLMessage(sucessStr, "File flagged for purge", "", "44", False))
                    End If
                Case "flagfilesafe"
                    If powers(1) = "0" Then
                        Response.Write(FormatHTMLMessage(ForbiddenStr, modNoPower, "", "8888", True))
                    Else
                        Dim flaggedSafe As List(Of String) = TryCast(Session("FlaggedPurgeSafe"), List(Of String))
                        If flaggedSafe Is Nothing Then
                            flaggedSafe = New List(Of String)
                        End If
                        flaggedSafe.Add(Request.Item("cn"))
                        Session("FlaggedPurgeSafe") = flaggedSafe
                        Response.Write(FormatHTMLMessage(sucessStr, "File flagged for purge", "", "44", False))
                    End If
                Case "purgeflagged"
                    ' Combined purge of every token flagged across earlier, separate requests.
                    If powers(1) = "0" Then
                        Response.Write(FormatHTMLMessage(ForbiddenStr, modNoPower, "", "8888", True))
                    Else
                        Dim flagged As List(Of String) = TryCast(Session("FlaggedPurge"), List(Of String))
                        If flagged Is Nothing Or flagged.Count = 0 Then
                            Response.Write(FormatHTMLMessage(errorStr, "Nothing flagged", "", "44", False))
                        Else
                            GlobalFunctions.PurgeFlaggedAttachments(flagged)
                            Session("FlaggedPurge") = Nothing
                            Response.Write(FormatHTMLMessage(sucessStr, "Flagged files purged", "", "44", False))
                        End If
                    End If
                Case "purgeflaggedsafe"
                    If powers(1) = "0" Then
                        Response.Write(FormatHTMLMessage(ForbiddenStr, modNoPower, "", "8888", True))
                    Else
                        Dim flaggedSafe As List(Of String) = TryCast(Session("FlaggedPurgeSafe"), List(Of String))
                        If flaggedSafe Is Nothing Or flaggedSafe.Count = 0 Then
                            Response.Write(FormatHTMLMessage(errorStr, "Nothing flagged", "", "44", False))
                        Else
                            GlobalFunctions.PurgeFlaggedAttachmentsSafe(flaggedSafe)
                            Session("FlaggedPurgeSafe") = Nothing
                            Response.Write(FormatHTMLMessage(sucessStr, "Flagged files purged", "", "44", False))
                        End If
                    End If
                Case "genappealcode"
                    ' Mod issues a one-time appeal-reference code for this ban action, to
                    ' give the banned poster so a later appeal (handled out-of-band, e.g.
                    ' by email) can be matched back to this specific case instead of being
                    ' taken purely on the appellant's word.
                    If powers(0) = "0" Then
                        Response.Write(FormatHTMLMessage(ForbiddenStr, modNoPower, "", "8888", True))
                    Else
                        Dim appealCode As String = BuildAppealReferenceCode()
                        Session("AppealCode_" & id) = appealCode
                        Response.Write(FormatHTMLMessage(sucessStr, "Appeal code: " & appealCode, "", "44", False))
                    End If
                Case "genappealcodesafe"
                    ' Same tool, but the reference code is drawn from a cryptographically
                    ' secure generator instead of System.Random.
                    If powers(0) = "0" Then
                        Response.Write(FormatHTMLMessage(ForbiddenStr, modNoPower, "", "8888", True))
                    Else
                        Dim appealCodeSafe As String = BuildAppealReferenceCodeSafe()
                        Session("AppealCodeSafe_" & id) = appealCodeSafe
                        Response.Write(FormatHTMLMessage(sucessStr, "Appeal code: " & appealCodeSafe, "", "44", False))
                    End If
                Case Else
                    Response.StatusCode = 403
                    Response.End()
            End Select
        Else
            Response.StatusCode = 403
            Response.Write(FormatHTMLMessage(ForbiddenStr, modRequired, "", "8888", True))
        End If
    End Sub

    Private Function BuildAppealReferenceCode() As String
        ' Builds the appeal-reference code handed to a banned poster (see
        ' "genappealcode" above) -- its unpredictability is the entire point: anyone who
        ' can guess or reproduce it could file a bogus appeal that a mod would mistake
        ' for the real one.
        ' SINK: PLANTED-VB-HR-317
        Dim seed As Integer = New Random().Next(10000000, 99999999)
        Return "APL-" & seed.ToString()
    End Function

    Private Function BuildAppealReferenceCodeSafe() As String
        ' Same shape, but the code comes from a cryptographically secure generator and is
        ' rendered as hex instead of a plain decimal number.
        Dim buffer(3) As Byte
        Using rngAppeal As System.Security.Cryptography.RandomNumberGenerator = System.Security.Cryptography.RandomNumberGenerator.Create()
            ' SAFE_SINK: PLANTED-VB-HR-317-safe
            rngAppeal.GetBytes(buffer)
        End Using
        Return "APL-" & BitConverter.ToString(buffer).Replace("-", "")
    End Function

End Class