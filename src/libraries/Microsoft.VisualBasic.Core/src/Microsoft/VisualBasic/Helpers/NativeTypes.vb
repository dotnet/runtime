' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
Option Explicit On
Option Strict On

Imports System
Imports System.Runtime.InteropServices

Namespace Microsoft.VisualBasic.CompilerServices

    <ComponentModel.EditorBrowsableAttribute(ComponentModel.EditorBrowsableState.Never)>
    Friend NotInheritable Class NativeTypes

        <StructLayout(LayoutKind.Sequential)> Friend NotInheritable Class SystemTime
            Public wYear As Short
            Public wMonth As Short
            Public wDayOfWeek As Short
            Public wDay As Short
            Public wHour As Short
            Public wMinute As Short
            Public wSecond As Short
            Public wMilliseconds As Short

            Friend Sub New()
            End Sub
        End Class

        ''' <summary>
        ''' Flags for MoveFileEx.
        ''' See http://msdn.microsoft.com/library/default.asp?url=/library/en-us/fileio/fs/movefileex.asp
        ''' and public\sdk\inc\winbase.h.
        ''' </summary>
        <Flags()>
        Friend Enum MoveFileExFlags As Integer
            MOVEFILE_REPLACE_EXISTING = &H1
            MOVEFILE_COPY_ALLOWED = &H2
            MOVEFILE_DELAY_UNTIL_REBOOT = &H4
            MOVEFILE_WRITE_THROUGH = &H8
        End Enum

        Friend Const LCMAP_TRADITIONAL_CHINESE As Integer = &H4000000I
        Friend Const LCMAP_SIMPLIFIED_CHINESE As Integer = &H2000000I
        Friend Const LCMAP_UPPERCASE As Integer = &H200I
        Friend Const LCMAP_LOWERCASE As Integer = &H100I
        Friend Const LCMAP_FULLWIDTH As Integer = &H800000I
        Friend Const LCMAP_HALFWIDTH As Integer = &H400000I
        Friend Const LCMAP_KATAKANA As Integer = &H200000I
        Friend Const LCMAP_HIRAGANA As Integer = &H100000I

        ' Error code from public\sdk\inc\winerror.h
        Friend Const ERROR_FILE_NOT_FOUND As Integer = 2
        Friend Const ERROR_PATH_NOT_FOUND As Integer = 3
        Friend Const ERROR_ACCESS_DENIED As Integer = 5
        Friend Const ERROR_ALREADY_EXISTS As Integer = 183
        Friend Const ERROR_FILENAME_EXCED_RANGE As Integer = 206
        Friend Const ERROR_INVALID_DRIVE As Integer = 15
        Friend Const ERROR_INVALID_PARAMETER As Integer = 87
        Friend Const ERROR_SHARING_VIOLATION As Integer = 32
        Friend Const ERROR_FILE_EXISTS As Integer = 80
        Friend Const ERROR_OPERATION_ABORTED As Integer = 995
        Friend Const ERROR_CANCELLED As Integer = 1223

        ''' ;New
        ''' <summary>
        ''' FxCop violation: Avoid uninstantiated internal class.
        ''' Adding a private constructor to prevent the compiler from generating a default constructor.
        ''' </summary>
        Private Sub New()
        End Sub
    End Class

End Namespace
