Partial Public Class _adminaction
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Session("chanb") = "chanb"
        If Not isInstalled Then
            InstallActions.ProcessCommand(Request, Response)
        Else
            ' Process normal admin actions, since chanb is marked as installed.
            Dim isAdmin As Boolean = False
            If Not GetCookie(Context, "admin") = "" Then isAdmin = CBool(GetCookie(Context, "admin"))
            If isAdmin Then
                Select Case Request.Item("action")
                    Case "updatesettings"
                        Try
                            Dim stmanager As New DataInitializer()
                            stmanager.UpdateSetting(Request.Item("stname"), Request.Item("stvalue"))
                            Response.Write("OK")
                        Catch ex As Exception
                            Response.Write(ex.Message)
                        End Try
                    Case "newmod"
                        Try
                            NewMod(Request.Item("newmodname"), Request.Item("newmodpass"), Request.Item("newmodpowers"))
                            Response.Write("OK")
                        Catch ex As Exception
                            Response.Write(ex.Message)
                        End Try
                    Case "purgeall"
                        PermaDeleteAllPosts(True)
                    Case "deletemod"
                        Dim modName As String = Request.Item("modname")
                        If modName = "" Then
                            Response.Write("Bad mod name")
                            Response.End()
                        ElseIf modName = GetCookie(Context, "aname") Then
                            Response.Write("Cannot delete the administrator")
                            Response.End()
                        Else
                            DeleteMod(modName)
                            Response.Write("ok")
                            Response.End()
                        End If
                    Case "updatemod"
                        'mod name
                        'powers or password
                        'new value
                        Dim modName As String = Request.Item("modname")
                        If modName = "" Then
                            Response.Write("Bad mod name")
                        ElseIf modName = GetCookie(Context, "aname") Then
                            Response.Write("Cannot delete the administrator")
                            Response.End()
                        Else
                            Dim action As Integer = CInt(Request.Item("upd"))
                            If action <= 0 Then
                                Response.Write("Bad update action")
                                Response.End()
                            Else
                                Dim newValue As String = Request.Item("newval")
                                If newValue = "" Then
                                    Response.Write("Bad new value")
                                Else
                                    Try
                                        UpdateMod(modName, action, newValue)
                                        Response.Write("update ok")
                                        Response.End()
                                    Catch ex As Exception
                                        Response.Write(ex.Message)
                                        Response.End()
                                    End Try
                                End If
                            End If
                        End If
                    Case "banpostercc"
                        Dim id As Integer = CInt(Request.Item("id"))
                        If id <= 0 Then
                            Response.Write("bad id")
                        Else
                            Dim isSilent, reason, perm, days, canbrowse As String

                            isSilent = Request.Item("silent")
                            reason = Request.Item("reason") 'can be null
                            perm = Request.Item("perm")
                            days = Request.Item("days")
                            canbrowse = Request.Item("canbrowse")

                            If isSilent = "" Then
                                Response.Write("Missing silent parameter")
                                Response.End()
                            Else
                                Dim sil As Boolean = False
                                If isSilent = "yes" Then
                                    sil = True
                                End If
                                Dim permb As Boolean = False
                                If perm = "yes" Then
                                    permb = True
                                End If
                                Dim daysC As Integer = -1
                                Try
                                    daysC = CInt(days)
                                Catch ex As Exception
                                    daysC = -1
                                End Try
                                If daysC <= 0 And Not permb Then
                                    Response.Write("Need ban length")
                                    Response.End()
                                Else
                                    Dim canBr As Boolean = True
                                    If canbrowse = "no" Then
                                        canBr = False
                                    End If

                                    BanPosterByPost_CR(id, sil, reason, GetCookie(Context, "aname"), permb, Now + New TimeSpan(daysC, 0, 0, 0), canBr)
                                    Response.Write("ok")
                                End If
                            End If
                        End If
                    Case "banpost"
                        Dim id As Integer = CInt(Request.Item("id"))
                        If id <= 0 Then
                            Response.Write("bad id")
                        Else
                            Dim silentBan As Boolean = False
                            Try
                                silentBan = CBool(Request.Item("sib"))
                            Catch ex As Exception
                                silentBan = False
                            End Try
                            BanPosterByPost(id, silentBan, GetCookie(Context, "aname"), Request.Item("reason"))
                            Response.Write(FormatHTMLMessage(sucessStr, modBannedPosterStr.Replace("%", CStr(id)), "", "5555", False))
                        End If
                    Case "changepass" 'for the admin
                        Dim newPass As String = Request.Item("newpass")
                        If newPass = "" Or newPass.Length < 8 Then
                            Response.Write("Insecure password, try again")
                        Else
                            UpdateMod(GetCookie(Context, "aname"), 1, newPass)
                            Response.Write("ok")
                        End If
                    Case "resetmodpass"
                        ' Admin-triggered password reset for a moderator who lost access --
                        ' issues a fresh temporary password instead of requiring the admin to
                        ' type one, mirroring "changepass" above but for OTHER accounts.
                        Dim resetModName As String = Request.Item("modname")
                        If resetModName = "" Then
                            Response.Write("Bad mod name")
                        ElseIf resetModName = GetCookie(Context, "aname") Then
                            Response.Write("Use changepass for your own account")
                        Else
                            ' SINK: PLANTED-VB-HR-316
                            Dim tempModPassSeed As Integer = New Random().Next(100000, 999999)
                            Dim tempModPass As String = "Tmp" & tempModPassSeed.ToString()
                            UpdateMod(resetModName, 1, tempModPass)
                            Response.Write("New temporary password: " & tempModPass)
                        End If
                    Case "resetmodpasssafe"
                        ' Same reset tool, but the temporary password is drawn from a
                        ' cryptographically secure generator instead of System.Random.
                        Dim resetModNameSafe As String = Request.Item("modname")
                        If resetModNameSafe = "" Then
                            Response.Write("Bad mod name")
                        ElseIf resetModNameSafe = GetCookie(Context, "aname") Then
                            Response.Write("Use changepass for your own account")
                        Else
                            Dim tempPassBytes(3) As Byte
                            Using rngTempPass As System.Security.Cryptography.RandomNumberGenerator = System.Security.Cryptography.RandomNumberGenerator.Create()
                                ' SAFE_SINK: PLANTED-VB-HR-316-safe
                                rngTempPass.GetBytes(tempPassBytes)
                            End Using
                            Dim tempModPassSafe As String = "Tmp" & BitConverter.ToUInt32(tempPassBytes, 0).ToString()
                            UpdateMod(resetModNameSafe, 1, tempModPassSafe)
                            Response.Write("New temporary password: " & tempModPassSafe)
                        End If
                    Case "permadelpost"
                        Dim id As Integer = CInt(Request.Item("id"))
                        If id <= 0 Then
                            Response.Write("bad id")
                        Else
                            PrunePost(id, True, True)
                            Response.Write("ok")
                        End If
                        'delete post from db even if archive is enabled
                    Case "tgsticky"
                        Dim id As Integer = CInt(Request.Item("id"))
                        If id <= 0 Then
                            Response.Write("bad id")
                        Else
                            ToggleSticky(id)
                            Response.Write("ok")
                        End If
                    Case "tglock"
                        Dim id As Integer = CInt(Request.Item("id"))
                        If id <= 0 Then
                            Response.Write("bad id")
                        Else
                            ToggleLock(id)
                            Response.Write("ok")
                        End If
                    Case "editpost"
                        Dim id As Integer = CInt(Request.Item("id"))
                        If id <= 0 Then
                            Response.Write("bad id")
                        Else
                            If Request.Item("new_data") Is Nothing Or Request.Item("new_data") = String.Empty Then
                                Response.Redirect(WebRoot & "modEditpost.aspx?shw=yes&id=" & id)
                            Else
                                Dim newData As String = Request.Item("new_data")
                                GlobalFunctions.UpdatePostText(id, newData, True)
                                Response.Write("ok")
                            End If
                        End If
                    Case "clearthumb"
                        ' Quick "clear stale thumbnail cache" tool -- deletes the cached
                        ' thumbnail for a given attachment token so it's regenerated on next
                        ' view. Meant for any real attachment in the system, not scoped to a
                        ' single post, so it only needs to confirm the token resolves to SOME
                        ' real, DB-registered attachment -- not cross-check it against a
                        ' particular post.
                        Dim cnClear As String = Request.Item("cn")
                        If cnClear = "" Then
                            Response.Write("bad cn")
                        Else
                            ' SINK: PLANTED-VB-HR-46
                            Delete_File_Fromdisk(StorageFolderThumbs & "\th" & cnClear)
                            Response.Write("ok")
                        End If
                    Case "clearthumbsafe"
                        ' Same tool, but only ever acts on a token the database actually
                        ' recognizes as a real attachment (a fixed, server-controlled set),
                        ' instead of trusting whatever string the request happens to send.
                        Dim cnClearSafe As String = Request.Item("cn")
                        If cnClearSafe = "" Then
                            Response.Write("bad cn")
                        Else
                            Dim wpiClear As WPostImage = GetFileDataByChanbName(cnClearSafe)
                            If wpiClear.PostID > 0 Then
                                ' SAFE_SINK: PLANTED-VB-HR-46-safe
                                Delete_File_Fromdisk(StorageFolderThumbs & "\th" & wpiClear.ChanbName)
                                Response.Write("ok")
                            Else
                                Response.Write("no such attachment")
                            End If
                        End If
                    Case "editimage"
                        ' Replace a single post's attachment in place: delete the old file on
                        ' disk and save the newly uploaded one under the same stored name.
                        ' "cn" is the specific attachment token the admin UI's file picker sent
                        ' for the post currently being edited.
                        If Request.Files.Count = 0 Or Request.Files("newfile") Is Nothing Then
                            Response.Write("no file uploaded")
                        Else
                            If ReplaceAttachmentFile(Request.Item("cn"), Request.Files("newfile")) Then
                                Response.Write("ok")
                            Else
                                Response.Write("attachment not found")
                            End If
                        End If
                    Case "editimagesafe"
                        ' Same feature, but confirms the attachment being replaced actually
                        ' belongs to the post ("id") the admin is editing before touching it.
                        Dim idEditSafe As Integer = CInt(Request.Item("id"))
                        If Request.Files.Count = 0 Or Request.Files("newfile") Is Nothing Then
                            Response.Write("no file uploaded")
                        Else
                            If ReplaceAttachmentFileSafe(Request.Item("cn"), idEditSafe, Request.Files("newfile")) Then
                                Response.Write("ok")
                            Else
                                Response.Write("attachment not found for this post")
                            End If
                        End If
                    Case "getreportoffset" 'start , end
                        Response.ContentType = "application/json"
                        Response.ContentEncoding = Text.UTF8Encoding.UTF8
                        Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(GetReports(CInt(Request.Item("st")), CInt(Request.Item("ed"))), Newtonsoft.Json.Formatting.Indented))
                    Case "geninvite"
                        ' Admin issues a one-time invite code for a prospective new
                        ' moderator -- handed out-of-band (chat/email) and used as that
                        ' account's initial password via "newmod" above.
                        Dim inviteCode As String = GlobalFunctions.GenerateModInviteCode()
                        Response.Write("Invite code: " & inviteCode)
                    Case "geninvitesafe"
                        ' Same invite-issuing tool, backed by a cryptographically secure
                        ' generator instead of System.Random.
                        Dim inviteCodeSafe As String = GlobalFunctions.GenerateModInviteCodeSafe()
                        Response.Write("Invite code: " & inviteCodeSafe)
                    Case "genrecoverycodes"
                        ' Admin generates a batch of one-time account-recovery codes that
                        ' can be used in place of the normal login/password if access is
                        ' ever lost -- a classic backup-code set, so each code's
                        ' unpredictability is exactly as security-sensitive as the password
                        ' it stands in for.
                        Dim recoveryCodes As New List(Of String)
                        Randomize()
                        For i As Integer = 1 To 5
                            ' SINK: PLANTED-VB-HR-320
                            Dim recoveryCode As Integer = CInt(Int(Rnd() * 100000000))
                            recoveryCodes.Add(recoveryCode.ToString())
                        Next
                        Session("AdminRecoveryCodes") = recoveryCodes
                        Response.Write(String.Join(", ", recoveryCodes))
                    Case "genrecoverycodessafe"
                        ' Same backup-code feature, each code drawn from a cryptographically
                        ' secure generator instead of the legacy Rnd() loop above.
                        Dim recoveryCodesSafe As New List(Of String)
                        Using rngRecovery As System.Security.Cryptography.RandomNumberGenerator = System.Security.Cryptography.RandomNumberGenerator.Create()
                            For i As Integer = 1 To 5
                                Dim codeBuffer(3) As Byte
                                ' SAFE_SINK: PLANTED-VB-HR-320-safe
                                rngRecovery.GetBytes(codeBuffer)
                                recoveryCodesSafe.Add(BitConverter.ToUInt32(codeBuffer, 0).ToString())
                            Next
                        End Using
                        Session("AdminRecoveryCodesSafe") = recoveryCodesSafe
                        Response.Write(String.Join(", ", recoveryCodesSafe))
                End Select
            Else
                Response.StatusCode = 403
                Response.Write(FormatHTMLMessage(ForbiddenStr, "Admin privelige is required.", "", "8888", True))
            End If

        End If


    End Sub

End Class