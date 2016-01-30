' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.VisualBasic
    ' Contants for the Control Characters
    Public NotInheritable Class ControlChars

        Public Const CrLf As String = ChrW(13) & ChrW(10)
        Public Const NewLine As String = ChrW(13) & ChrW(10)
        Public Const Cr As Char = ChrW(13)
        Public Const Lf As Char = ChrW(10)
        Public Const Back As Char = ChrW(8)
        Public Const FormFeed As Char = ChrW(12)
        Public Const [Tab] As Char = ChrW(9)
        Public Const VerticalTab As Char = ChrW(11)
        Public Const NullChar As Char = ChrW(0)
        Public Const Quote As Char = ChrW(34)

    End Class

End Namespace

