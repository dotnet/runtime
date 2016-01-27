' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Global.Microsoft.VisualBasic
    <Global.System.Diagnostics.DebuggerNonUserCode()>
    Public Module Constants
        Public Const vbCrLf As String = ChrW(13) & ChrW(10)
        <Global.System.Obsolete("For a carriage return and line feed, use vbCrLf.  For the current platform's newline, use System.Environment.NewLine.")>
        Public Const vbNewLine As String = ChrW(13) & ChrW(10)
        Public Const vbCr As String = ChrW(13)
        Public Const vbLf As String = ChrW(10)
        Public Const vbBack As String = ChrW(8)
        Public Const vbFormFeed As String = ChrW(12)
        Public Const vbTab As String = ChrW(9)
        Public Const vbVerticalTab As String = ChrW(11)
        Public Const vbNullChar As String = ChrW(0)
        Public Const vbNullString As String = Nothing
    End Module
End Namespace
