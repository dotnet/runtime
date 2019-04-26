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

        'vbCompareMethod enum values
        Public Const vbBinaryCompare As CompareMethod = CompareMethod.Binary
        Public Const vbTextCompare As CompareMethod = CompareMethod.Text

        'vbDateTimeFormat
        Public Const vbGeneralDate As DateFormat = DateFormat.GeneralDate
        Public Const vbLongDate As DateFormat = DateFormat.LongDate
        Public Const vbShortDate As DateFormat = DateFormat.ShortDate
        Public Const vbLongTime As DateFormat = DateFormat.LongTime
        Public Const vbShortTime As DateFormat = DateFormat.ShortTime

        'FileAttribute
        Public Const vbNormal As FileAttribute = FileAttribute.Normal
        Public Const vbReadOnly As FileAttribute = FileAttribute.ReadOnly
        Public Const vbHidden As FileAttribute = FileAttribute.Hidden
        Public Const vbSystem As FileAttribute = FileAttribute.System
        Public Const vbVolume As FileAttribute = FileAttribute.Volume
        Public Const vbDirectory As FileAttribute = FileAttribute.Directory
        Public Const vbArchive As FileAttribute = FileAttribute.Archive

        'vbStrConv
        Public Const vbUpperCase As VbStrConv = VbStrConv.Uppercase
        Public Const vbLowerCase As VbStrConv = VbStrConv.Lowercase
        Public Const vbProperCase As VbStrConv = VbStrConv.ProperCase
        Public Const vbWide As VbStrConv = VbStrConv.Wide
        Public Const vbNarrow As VbStrConv = VbStrConv.Narrow
        Public Const vbKatakana As VbStrConv = VbStrConv.Katakana
        Public Const vbHiragana As VbStrConv = VbStrConv.Hiragana
        Public Const vbSimplifiedChinese As VbStrConv = VbStrConv.SimplifiedChinese
        Public Const vbTraditionalChinese As VbStrConv = VbStrConv.TraditionalChinese
        Public Const vbLinguisticCasing As VbStrConv = VbStrConv.LinguisticCasing

        'vbTriState
        Public Const vbUseDefault As TriState = TriState.UseDefault
        Public Const vbTrue As TriState = TriState.True
        Public Const vbFalse As TriState = TriState.False

    End Module

End Namespace
