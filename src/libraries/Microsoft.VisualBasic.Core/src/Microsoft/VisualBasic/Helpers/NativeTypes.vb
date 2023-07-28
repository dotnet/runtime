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
        Friend Const ERROR_NOT_SAME_DEVICE As Integer = 17
        Friend Const ERROR_WRITE_FAULT As Integer = 29
        Friend Const ERROR_READ_FAULT As Integer = 30
        Friend Const ERROR_GEN_FAILURE As Integer = 31
        Friend Const ERROR_BAD_PATHNAME As Integer = 161
        Friend Const ERROR_FILE_TOO_LARGE As Integer = 223

        ' Error code from SHFileOperationW function (shellapi.h)
        Friend Const DE_SAMEFILE As Integer = 113
        Friend Const DE_MANYSRC1DEST As Integer = 114
        Friend Const DE_DIFFDIR As Integer = 115
        Friend Const DE_ROOTDIR As Integer = 116
        Friend Const DE_OPCANCELLED As Integer = 117
        Friend Const DE_DESTSUBTREE As Integer = 118
        Friend Const DE_ACCESSDENIEDSRC As Integer = 120
        Friend Const DE_PATHTOODEEP As Integer = 121
        Friend Const DE_MANYDEST As Integer = 122
        Friend Const DE_INVALIDFILES As Integer = 124
        Friend Const DE_DESTSAMETREE As Integer = 125
        Friend Const DE_FLDDESTISFILE As Integer = 126
        Friend Const DE_FILEDESTISFLD As Integer = 128
        Friend Const DE_FILENAMETOOLONG As Integer = 129
        Friend Const DE_DEST_IS_CDROM As Integer = 130
        Friend Const DE_DEST_IS_DVD As Integer = 131
        Friend Const DE_DEST_IS_CDRECORD As Integer = 132
        Friend Const DE_FILE_TOO_LARGE As Integer = 133
        Friend Const DE_SRC_IS_CDROM As Integer = 134
        Friend Const DE_SRC_IS_DVD As Integer = 135
        Friend Const DE_SRC_IS_CDRECORD As Integer = 136
        Friend Const DE_ERROR_MAX As Integer = 183
        Friend Const DE_ERROR_UNKNOWN As Integer = 1026
        Friend Const ERRORONDEST As Integer = 65536
        Friend Const DE_ROOTDIR_ERRORONDEST As Integer = 65652

        ''' ;New
        ''' <summary>
        ''' FxCop violation: Avoid uninstantiated internal class.
        ''' Adding a private constructor to prevent the compiler from generating a default constructor.
        ''' </summary>
        Private Sub New()
        End Sub
    End Class

End Namespace
