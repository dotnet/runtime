' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Reflection
Imports System.Security

Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.CompilerServices

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)> _
    Class IOUtils
        ' Prevent creation.
        Private Sub New()
        End Sub

        Friend Shared Function FindFirstFile(assem As Assembly, pathName As String, attributes As FileAttributes) As String
            Dim dir As DirectoryInfo
            Dim dirName As String = Nothing
            Dim fileName As String = "*.*"
            Dim files() As FileSystemInfo
            Dim oAssemblyData As AssemblyData
            Const DiskNotReadyError As Integer = &H80070015

            If pathName.Length > 0 AndAlso pathName.Chars(pathName.Length - 1) = Path.DirectorySeparatorChar Then
                dirName = Path.GetFullPath(pathName)
            Else
                If pathName.Length <> 0 Then
                    fileName = Path.GetFileName(pathName)
                    dirName = Path.GetDirectoryName(pathName)

                    If (fileName Is Nothing) OrElse (fileName.Length = 0) OrElse (fileName = ".") Then
                        fileName = "*.*"
                    End If
                End If

                If (dirName Is Nothing) OrElse (dirName.Length = 0) Then
                    If Path.IsPathRooted(pathName) Then
                        dirName = Path.GetPathRoot(pathName)
                    Else
                        dirName = Environment.CurrentDirectory
                        If dirName.Chars(dirName.Length - 1) <> Path.DirectorySeparatorChar Then
                            dirName = dirName & Path.DirectorySeparatorChar
                        End If
                    End If
                Else
                    If dirName.Chars(dirName.Length - 1) <> Path.DirectorySeparatorChar Then
                        dirName &= Path.DirectorySeparatorChar
                    End If
                End If

                If fileName = ".." Then
                    dirName &= "..\"
                    fileName = "*.*"
                End If
            End If

            Try
                dir = Directory.GetParent(dirName & fileName)
                files = dir.GetFileSystemInfos(fileName)
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
            oAssemblyData.m_DirAttributes = attributes

            If (files Is Nothing) OrElse (files.Length = 0) Then
                Return ""
            End If

            Return FindFileFilter(oAssemblyData)
        End Function

        Friend Shared Function FindNextFile(assem As Assembly) As String
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

        Private Shared Function FindFileFilter(oAssemblyData As AssemblyData) As String
            Dim index As Integer = oAssemblyData.m_DirNextFileIndex
            Dim files() As FileSystemInfo = oAssemblyData.m_DirFiles

            Do While True
                If index > files.GetUpperBound(0) Then
                    oAssemblyData.m_DirFiles = Nothing
                    oAssemblyData.m_DirNextFileIndex = 0
                    Return Nothing
                End If

                Dim file As FileSystemInfo = files(Index)

                If ((file.Attributes And (FileAttributes.Directory Or FileAttributes.System Or FileAttributes.Hidden)) = 0) OrElse
           ((file.Attributes And oAssemblyData.m_DirAttributes) <> 0) Then
                    oAssemblyData.m_DirNextFileIndex = index + 1
                    Return files(index).Name
                End If

                index += 1
            Loop
            Return Nothing
        End Function

    End Class

End Namespace
