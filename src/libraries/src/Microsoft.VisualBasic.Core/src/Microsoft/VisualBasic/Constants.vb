' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Global.Microsoft.VisualBasic

    <Global.System.Diagnostics.DebuggerNonUserCode()>
    Public Module Constants

        Public Const vbCrLf As String = ChrW(13) & ChrW(10)
        Public Const vbObjectError As Integer = &H80040000I
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

        'AppWinStyle
        Public Const vbHide As AppWinStyle = AppWinStyle.Hide
        Public Const vbNormalFocus As AppWinStyle = AppWinStyle.NormalFocus
        Public Const vbMinimizedFocus As AppWinStyle = AppWinStyle.MinimizedFocus
        Public Const vbMaximizedFocus As AppWinStyle = AppWinStyle.MaximizedFocus
        Public Const vbNormalNoFocus As AppWinStyle = AppWinStyle.NormalNoFocus
        Public Const vbMinimizedNoFocus As AppWinStyle = AppWinStyle.MinimizedNoFocus

        'vbCallType Enum values
        Public Const vbMethod As CallType = CallType.Method
        Public Const vbGet As CallType = CallType.Get
        Public Const vbLet As CallType = CallType.Let
        Public Const vbSet As CallType = CallType.Set

        'vbCompareMethod enum values
        Public Const vbBinaryCompare As CompareMethod = CompareMethod.Binary
        Public Const vbTextCompare As CompareMethod = CompareMethod.Text

        'vbDateTimeFormat
        Public Const vbGeneralDate As DateFormat = DateFormat.GeneralDate
        Public Const vbLongDate As DateFormat = DateFormat.LongDate
        Public Const vbShortDate As DateFormat = DateFormat.ShortDate
        Public Const vbLongTime As DateFormat = DateFormat.LongTime
        Public Const vbShortTime As DateFormat = DateFormat.ShortTime

        'vbDayOfWeek
        Public Const vbUseSystemDayOfWeek As FirstDayOfWeek = FirstDayOfWeek.System
        Public Const vbSunday As FirstDayOfWeek = FirstDayOfWeek.Sunday
        Public Const vbMonday As FirstDayOfWeek = FirstDayOfWeek.Monday
        Public Const vbTuesday As FirstDayOfWeek = FirstDayOfWeek.Tuesday
        Public Const vbWednesday As FirstDayOfWeek = FirstDayOfWeek.Wednesday
        Public Const vbThursday As FirstDayOfWeek = FirstDayOfWeek.Thursday
        Public Const vbFriday As FirstDayOfWeek = FirstDayOfWeek.Friday
        Public Const vbSaturday As FirstDayOfWeek = FirstDayOfWeek.Saturday

        'FileAttribute
        Public Const vbNormal As FileAttribute = FileAttribute.Normal
        Public Const vbReadOnly As FileAttribute = FileAttribute.ReadOnly
        Public Const vbHidden As FileAttribute = FileAttribute.Hidden
        Public Const vbSystem As FileAttribute = FileAttribute.System
        Public Const vbVolume As FileAttribute = FileAttribute.Volume
        Public Const vbDirectory As FileAttribute = FileAttribute.Directory
        Public Const vbArchive As FileAttribute = FileAttribute.Archive

        'vbFirstWeekOfYear
        Public Const vbUseSystem As FirstWeekOfYear = FirstWeekOfYear.System
        Public Const vbFirstJan1 As FirstWeekOfYear = FirstWeekOfYear.Jan1
        Public Const vbFirstFourDays As FirstWeekOfYear = FirstWeekOfYear.FirstFourDays
        Public Const vbFirstFullWeek As FirstWeekOfYear = FirstWeekOfYear.FirstFullWeek

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

        'VariantType
        Public Const vbEmpty As VariantType = VariantType.Empty
        Public Const vbNull As VariantType = VariantType.Null
        Public Const vbInteger As VariantType = VariantType.Integer
        Public Const vbLong As VariantType = VariantType.Long
        Public Const vbSingle As VariantType = VariantType.Single
        Public Const vbDouble As VariantType = VariantType.Double
        Public Const vbCurrency As VariantType = VariantType.Currency
        Public Const vbDate As VariantType = VariantType.Date
        Public Const vbString As VariantType = VariantType.String
        Public Const vbObject As VariantType = VariantType.Object
        Public Const vbBoolean As VariantType = VariantType.Boolean
        Public Const vbVariant As VariantType = VariantType.Variant
        Public Const vbDecimal As VariantType = VariantType.Decimal
        Public Const vbByte As VariantType = VariantType.Byte
        Public Const vbUserDefinedType As VariantType = VariantType.UserDefinedType
        Public Const vbArray As VariantType = VariantType.Array

        'MsgBoxResult
        Public Const vbOK As MsgBoxResult = MsgBoxResult.Ok
        Public Const vbCancel As MsgBoxResult = MsgBoxResult.Cancel
        Public Const vbAbort As MsgBoxResult = MsgBoxResult.Abort
        Public Const vbRetry As MsgBoxResult = MsgBoxResult.Retry
        Public Const vbIgnore As MsgBoxResult = MsgBoxResult.Ignore
        Public Const vbYes As MsgBoxResult = MsgBoxResult.Yes
        Public Const vbNo As MsgBoxResult = MsgBoxResult.No

        'MsgBoxStyle
        'You may BitOr one value from each group
        Public Const vbOKOnly As MsgBoxStyle = MsgBoxStyle.OkOnly
        Public Const vbOKCancel As MsgBoxStyle = MsgBoxStyle.OkCancel
        Public Const vbAbortRetryIgnore As MsgBoxStyle = MsgBoxStyle.AbortRetryIgnore
        Public Const vbYesNoCancel As MsgBoxStyle = MsgBoxStyle.YesNoCancel
        Public Const vbYesNo As MsgBoxStyle = MsgBoxStyle.YesNo
        Public Const vbRetryCancel As MsgBoxStyle = MsgBoxStyle.RetryCancel
        Public Const vbCritical As MsgBoxStyle = MsgBoxStyle.Critical
        Public Const vbQuestion As MsgBoxStyle = MsgBoxStyle.Question
        Public Const vbExclamation As MsgBoxStyle = MsgBoxStyle.Exclamation
        Public Const vbInformation As MsgBoxStyle = MsgBoxStyle.Information
        Public Const vbDefaultButton1 As MsgBoxStyle = MsgBoxStyle.DefaultButton1
        Public Const vbDefaultButton2 As MsgBoxStyle = MsgBoxStyle.DefaultButton2
        Public Const vbDefaultButton3 As MsgBoxStyle = MsgBoxStyle.DefaultButton3
        Public Const vbApplicationModal As MsgBoxStyle = MsgBoxStyle.ApplicationModal
        Public Const vbSystemModal As MsgBoxStyle = MsgBoxStyle.SystemModal
        Public Const vbMsgBoxHelp As MsgBoxStyle = MsgBoxStyle.MsgBoxHelp
        Public Const vbMsgBoxRight As MsgBoxStyle = MsgBoxStyle.MsgBoxRight
        Public Const vbMsgBoxRtlReading As MsgBoxStyle = MsgBoxStyle.MsgBoxRtlReading
        Public Const vbMsgBoxSetForeground As MsgBoxStyle = MsgBoxStyle.MsgBoxSetForeground

    End Module

End Namespace
