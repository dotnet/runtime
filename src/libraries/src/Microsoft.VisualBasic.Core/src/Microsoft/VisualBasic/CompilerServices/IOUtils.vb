' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Security
Imports System.IO

Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.CompilerServices

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)> _
    Class IOUtils
        ' Prevent creation.
        Private Sub New()
        End Sub

        Friend Shared Function FindFirstFile(ByVal assem As System.Reflection.Assembly, ByVal PathName As String, ByVal Attributes As IO.FileAttributes) As String
            Dim Dir As DirectoryInfo
            Dim DirName As String = Nothing
            Dim FileName As String
            Dim files() As FileSystemInfo
            Dim oAssemblyData As AssemblyData
            Const DiskNotReadyError As Integer = &H80070015

            If PathName.Length > 0 AndAlso PathName.Chars(PathName.Length - 1) = Path.DirectorySeparatorChar Then
                DirName = Path.GetFullPath(PathName)
                FileName = "*.*"
            Else
                If PathName.Length = 0 Then
                    FileName = "*.*"
                Else
                    FileName = Path.GetFileName(PathName)
                    DirName = Path.GetDirectoryName(PathName)

                    If (FileName Is Nothing) OrElse (FileName.Length = 0) OrElse (FileName = ".") Then
                        FileName = "*.*"
                    End If
                End If

                If (DirName Is Nothing) OrElse (DirName.Length = 0) Then
                    If Path.IsPathRooted(PathName) Then
                        DirName = Path.GetPathRoot(PathName)
                    Else
                        DirName = Environment.CurrentDirectory
                        If DirName.Chars(DirName.Length - 1) <> Path.DirectorySeparatorChar Then
                            DirName = DirName & Path.DirectorySeparatorChar
                        End If
                    End If
                Else
                    If DirName.Chars(DirName.Length - 1) <> Path.DirectorySeparatorChar Then
                        DirName = DirName & Path.DirectorySeparatorChar
                    End If
                End If

                If FileName = ".." Then
                    DirName = DirName & "..\"
                    FileName = "*.*"
                End If
            End If

            Try
                Dir = Directory.GetParent(DirName & FileName)
                files = Dir.GetFileSystemInfos(FileName)
            Catch ex As SecurityException
                Throw ex
            Catch IOex As IOException When _
                      (System.Runtime.InteropServices.Marshal.GetHRForException(IOex) = DiskNotReadyError)
                Throw VbMakeException(vbErrors.BadFileNameOrNumber)
            Catch ex As StackOverflowException
                Throw ex
            Catch ex As OutOfMemoryException
                Throw ex
            Catch ex As System.Threading.ThreadAbortException
                Throw ex
            Catch
                Return ""
            End Try

            oAssemblyData = ProjectData.GetProjectData().GetAssemblyData(assem)
            oAssemblyData.m_DirFiles = files
            oAssemblyData.m_DirNextFileIndex = 0
            oAssemblyData.m_DirAttributes = Attributes

            If (files Is Nothing) OrElse (files.Length = 0) Then
                Return ""
            End If

            Return FindFileFilter(oAssemblyData)
        End Function

        Friend Shared Function FindNextFile(ByVal assem As System.Reflection.Assembly) As String
            Dim oAssemblyData As AssemblyData

            oAssemblyData = ProjectData.GetProjectData().GetAssemblyData(assem)

            If oAssemblyData.m_DirFiles Is Nothing Then
                Throw New ArgumentException(GetResourceString(SR.DIR_IllegalCall))
            End If

            If oAssemblyData.m_DirNextFileIndex > oAssemblyData.m_DirFiles.GetUpperBound(0) Then
                'Prevent hitting the security check in this scenario
                oAssemblyData.m_DirFiles = Nothing
                oAssemblyData.m_DirNextFileIndex = 0
                Return Nothing
            End If

            Return FindFileFilter(oAssemblyData)
        End Function

        Private Shared Function FindFileFilter(ByVal oAssemblyData As AssemblyData) As String
            Dim Index As Integer
            Dim files() As FileSystemInfo
            Dim file As FileSystemInfo

            files = oAssemblyData.m_DirFiles
            Index = oAssemblyData.m_DirNextFileIndex

            Do While True
                If Index > files.GetUpperBound(0) Then
                    oAssemblyData.m_DirFiles = Nothing
                    oAssemblyData.m_DirNextFileIndex = 0
                    Return Nothing
                End If

                file = files(Index)

                If ((file.Attributes And (FileAttributes.Directory Or FileAttributes.System Or FileAttributes.Hidden)) = 0) OrElse
           ((file.Attributes And oAssemblyData.m_DirAttributes) <> 0) Then
                    oAssemblyData.m_DirNextFileIndex = Index + 1
                    Return files(Index).Name
                End If

                Index += 1
            Loop
            Return Nothing
        End Function

    End Class

End Namespace
