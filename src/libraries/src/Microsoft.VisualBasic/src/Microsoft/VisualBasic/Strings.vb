' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports Microsoft.VisualBasic.CompilerServices
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Global.Microsoft.VisualBasic
    <Global.System.Diagnostics.DebuggerNonUserCode()>
    Public Module Strings
        Public Function ChrW(charCode As Integer) As Char
            If charCode < -32768 OrElse charCode > 65535 Then
                Throw New Global.System.ArgumentException()
            End If
            Return Global.System.Convert.ToChar(charCode And &HFFFFI)
        End Function
        Public Function AscW([string] As String) As Integer
            If ([string] Is Nothing) OrElse ([string].Length = 0) Then
                Throw New Global.System.ArgumentException()
            End If
            Return AscW([string].Chars(0))
        End Function
        Public Function AscW([string] As Char) As Integer
            Return AscW([string])
        End Function

        '============================================================================
        ' Left/Right/Mid/Trim functions.
        '============================================================================
        Public Function Left(ByVal [str] As String, ByVal length As Integer) As String
            '-------------------------------------------------------------
            '   lLen < 0 throws InvalidArgument exception
            '   lLen > Len([str]) let lLen = Len([str])
            '   returned computed string
            '-------------------------------------------------------------
            If length < 0 Then
                Throw New System.ArgumentException(GetResourceString(SR.Argument_GEZero1, "Length"))
            ElseIf length = 0 OrElse [str] Is Nothing Then
                Return ""
            Else
                If length >= [str].Length Then
                    Return [str]
                Else
                    Return [str].Substring(0, length)
                End If
            End If
        End Function
    End Module
End Namespace
