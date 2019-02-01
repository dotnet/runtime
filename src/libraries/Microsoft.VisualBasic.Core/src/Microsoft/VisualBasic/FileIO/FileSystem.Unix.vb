' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.
Option Explicit On
Option Strict On

Imports System

Namespace Microsoft.VisualBasic.FileIO
    Partial Public Class FileSystem
        Private Shared Sub ShellCopyOrMove(operation As CopyOrMove, directory As FileOrDirectory, sourceDirectoryFullPath As String, targetDirectoryFullPath As String, showUI As UIOptionInternal, onUserCancel As UICancelOption)
            Throw New PlatformNotSupportedException(SR.NoShellCopyOrMove)
        End Sub

        Private Shared Sub ShellDelete(directoryFullPath As String, showUI As UIOptionInternal, recycle As RecycleOption, onUserCancel As UICancelOption, directory As FileOrDirectory)
            Throw New PlatformNotSupportedException(SR.NoShellDelete)
        End Sub

        ''' <summary>
        ''' Stub to prevent compile error, this will not get called on non Windows OS's
        ''' </summary>
        ''' <param name="sourceFileFullPath"></param>
        ''' <param name="destinationFileFullPath"></param>
        Private Shared Sub WinNTCopyOrMove(sourceFileFullPath As String, destinationFileFullPath As String)
            Throw New PlatformNotSupportedException()
        End Sub
    End Class

End Namespace
