' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Option Explicit On
Option Strict On

Imports System
Imports System.Globalization
Imports System.Collections.Generic
Imports System.Diagnostics

Imports Microsoft.VisualBasic.Strings
Imports Microsoft.VisualBasic.CompilerServices.Symbols
Imports Microsoft.VisualBasic.CompilerServices.Operators
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.CompilerServices

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public NotInheritable Class LikeOperator

        Private Sub New()
        End Sub

        ' The list of ligatures
        '
        Private Enum Ligatures
            Invalid = 0
            Min = &HC6
            ssBeta = &HDF
            szBeta = &HDF
            aeUpper = &HC6
            ae = &HE6
            thUpper = &HDE
            th = &HFE
            oeUpper = &H152
            oe = &H153
            Max = &H153
        End Enum

        ' The expansions for the ligatures. Note that the order of these is the same as their
        ' order in the Ligatures enum
        Shared LigatureExpansions() As String = {"", "ss", "sz", "AE", "ae", "TH", "th", "OE", "oe"}

        Shared LigatureMap() As Byte

        Shared Sub New()
            LigatureMap = New Byte(Ligatures.Max - Ligatures.Min) {}

            LigatureMap(Ligatures.ssBeta - Ligatures.Min) = 1
            LigatureMap(Ligatures.szBeta - Ligatures.Min) = 2
            LigatureMap(Ligatures.aeUpper - Ligatures.Min) = 3
            LigatureMap(Ligatures.ae - Ligatures.Min) = 4
            LigatureMap(Ligatures.thUpper - Ligatures.Min) = 5
            LigatureMap(Ligatures.th - Ligatures.Min) = 6
            LigatureMap(Ligatures.oeUpper - Ligatures.Min) = 7
            LigatureMap(Ligatures.oe - Ligatures.Min) = 8

        End Sub

        Private Shared Function LigatureIndex(ByVal ch As Char) As Byte

            If AscW(ch) < Ligatures.Min OrElse AscW(ch) > Ligatures.Max Then
                Return 0
            End If

            Return LigatureMap(AscW(ch) - Ligatures.Min)
        End Function

        Private Shared Function CanCharExpand _
        (
            ByVal ch As Char,
            ByVal LocaleSpecificLigatureTable As Byte(),
            ByVal Comparer As CompareInfo,
            ByVal Options As CompareOptions
        ) As Integer

            Debug.Assert(Options <> CompareOptions.Ordinal, "Char expansion check unexpected during binary compare")

            Dim Index As Byte = LigatureIndex(ch)

            If Index = 0 Then
                Return 0
            End If

            If LocaleSpecificLigatureTable(Index) = 0 Then
                If Comparer.Compare(ch, LigatureExpansions(Index)) = 0 Then
                    LocaleSpecificLigatureTable(Index) = 1
                Else
                    LocaleSpecificLigatureTable(Index) = 2
                End If
            End If

            If LocaleSpecificLigatureTable(Index) = 1 Then
                Return Index
            End If

            Return 0
        End Function

        Private Shared Function GetCharExpansion _
        (
            ByVal ch As Char,
            ByVal LocaleSpecificLigatureTable As Byte(),
            ByVal Comparer As CompareInfo,
            ByVal Options As CompareOptions
        ) As String

            Dim Index As Integer = CanCharExpand(ch, LocaleSpecificLigatureTable, Comparer, Options)

            If Index = 0 Then
                Return ch
            End If

            Return LigatureExpansions(Index)
        End Function

        Private Enum CharKind
            None
            ExpandedChar1
            ExpandedChar2
        End Enum

        Private Structure LigatureInfo
            Friend Kind As CharKind
            Friend CharBeforeExpansion As Char
        End Structure

        ': What I've been able to divine about this function is that its purpose is to normalize the string 
        'that is going to be used in the Like operator.  The string may contain liguratures (two letters being represented by
        'a single glpyh) that need to be expanded.  It also may contain Katakana characters that need to be mapped to
        'narrow width characters.  
        Private Shared Sub ExpandString _
        (
            ByRef Input As String,
            ByRef Length As Integer,
            ByRef InputLigatureInfo As LigatureInfo(),
            ByVal LocaleSpecificLigatureTable As Byte(),
            ByVal Comparer As CompareInfo,
            ByVal Options As CompareOptions,
            ByRef WidthChanged As Boolean,
            ByVal UseFullWidth As Boolean
        )

            WidthChanged = False

            If Length = 0 Then Return
            Input = Input.ToLower(System.Globalization.CultureInfo.InvariantCulture)

            Dim ExtraChars As Integer

            For i As Integer = 0 To Length - 1
                Dim ch As Char = Input.Chars(i)

                If CanCharExpand(ch, LocaleSpecificLigatureTable, Comparer, Options) <> 0 Then
                    ExtraChars += 1
                End If
            Next


            If ExtraChars > 0 Then
                InputLigatureInfo = New LigatureInfo(Length + ExtraChars - 1) {}
                Dim NewInput As New Text.StringBuilder(Length + ExtraChars - 1)


                Dim NewCharIndex As Integer = 0

                For i As Integer = 0 To Length - 1

                    Dim ch As Char = Input.Chars(i)

                    If CanCharExpand(ch, LocaleSpecificLigatureTable, Comparer, Options) <> 0 Then

                        Dim Expansion As String = GetCharExpansion(ch, LocaleSpecificLigatureTable, Comparer, Options)
                        NewInput.Append(Expansion)

                        InputLigatureInfo(NewCharIndex).Kind = CharKind.ExpandedChar1
                        InputLigatureInfo(NewCharIndex).CharBeforeExpansion = ch

                        NewCharIndex += 1

                        InputLigatureInfo(NewCharIndex).Kind = CharKind.ExpandedChar2
                        InputLigatureInfo(NewCharIndex).CharBeforeExpansion = ch

                    Else
                        NewInput.Append(ch)
                    End If

                    NewCharIndex += 1
                Next

                Input = NewInput.ToString()
                Length = NewInput.Length
            End If
        End Sub

        Public Shared Function LikeObject(ByVal Source As Object, ByVal Pattern As Object, ByVal CompareOption As CompareMethod) As Object

            Dim conv1, conv2 As IConvertible
            Dim tc1, tc2 As TypeCode

            conv1 = TryCast(Source, IConvertible)
            If conv1 Is Nothing Then
                If Source Is Nothing Then
                    tc1 = TypeCode.Empty
                Else
                    tc1 = TypeCode.Object
                End If
            Else
                tc1 = conv1.GetTypeCode()
            End If

            conv2 = TryCast(Pattern, IConvertible)
            If conv2 Is Nothing Then
                If Pattern Is Nothing Then
                    tc2 = TypeCode.Empty
                Else
                    tc2 = TypeCode.Object
                End If
            Else
                tc2 = conv2.GetTypeCode()
            End If

            'Special cases for Char()
            If (tc1 = TypeCode.Object) AndAlso (TypeOf Source Is Char()) Then
                tc1 = TypeCode.String
            End If

            If (tc2 = TypeCode.Object) AndAlso (TypeOf Pattern Is Char()) Then
                tc2 = TypeCode.String
            End If

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return InvokeUserDefinedOperator(UserDefinedOperator.Like, Source, Pattern)
            End If

            Return LikeString(CStr(Source), CStr(Pattern), CompareOption)
        End Function

        Public Shared Function LikeString(ByVal Source As String, ByVal Pattern As String, ByVal CompareOption As CompareMethod) As Boolean
            Dim SourceIndex As Integer
            Dim PatternIndex As Integer

            Dim SourceLength As Integer
            Dim PatternLength As Integer

            Dim SourceLigatureInfo As LigatureInfo() = Nothing
            Dim PatternLigatureInfo As LigatureInfo() = Nothing

            Dim Options As CompareOptions
            Dim Comparer As CompareInfo

            If Pattern Is Nothing Then
                PatternLength = 0
            Else
                PatternLength = Pattern.Length
            End If

            If Source Is Nothing Then
                SourceLength = 0
            Else
                SourceLength = Source.Length
            End If

            '
            ' We expand ligatures up front, but we need to keep track of
            ' where they were.  We need the source ligature positions so
            ' that "?" in the pattern will match both characters of the
            ' ligature.  We need the pattern ligature positions for
            ' bracketed character lists (e.g. [abc0-9]), because a
            ' ligature would look like two separate characters. But note
            ' that we do this only for option compare text mode.
            '

            If CompareOption = CompareMethod.Binary Then
                Options = CompareOptions.Ordinal
                Comparer = Nothing
            Else
                Comparer = GetCultureInfo().CompareInfo
                Options = CompareOptions.IgnoreCase Or
                          CompareOptions.IgnoreWidth Or
                          CompareOptions.IgnoreKanaType

                Dim LocaleSpecificLigatureTable As Byte() = New Byte(LigatureExpansions.Length - 1) {}

                ExpandString(Source, SourceLength, SourceLigatureInfo, LocaleSpecificLigatureTable, Comparer, Options, False, False)
                ExpandString(Pattern, PatternLength, PatternLigatureInfo, LocaleSpecificLigatureTable, Comparer, Options, False, False)
            End If

            ' The first phase is an optimization for anything in the pattern
            ' before the first "*".  (If the pattern has no "*" in it, this
            ' will do the whole thing.)
            '
            ' Visit each character in the pattern, and see if it matches the
            ' source.
            '
            '
            Dim p As Char

            Do While (PatternIndex < PatternLength AndAlso SourceIndex < SourceLength)
                p = Pattern.Chars(PatternIndex)

                Select Case p
                    Case "?"c, ChrW(&HFF1F)
                        'AdvanceToNextChar(Source, SourceLength, SourceIndex, Options)
                        SkipToEndOfExpandedChar(SourceLigatureInfo, SourceLength, SourceIndex)

                    Case "#"c, ChrW(&HFF03)

                        If Not System.Char.IsDigit(Source.Chars(SourceIndex)) Then
                            Return False
                        End If

                    Case "["c, ChrW(&HFF3B)
                        'Match ranges like "[ACE-TZ]"

                        Dim RangePatternEmpty, RangeMismatch, RangePatternError As Boolean
                        MatchRange(
                            Source,
                            SourceLength,
                            SourceIndex,
                            SourceLigatureInfo,
                            Pattern,
                            PatternLength,
                            PatternIndex,
                            PatternLigatureInfo,
                            RangePatternEmpty,
                            RangeMismatch,
                            RangePatternError,
                            Comparer,
                            Options)

                        If RangePatternError Then
                            Throw New ArgumentException(GetResourceString(SR.Argument_InvalidValue1, "Pattern"))
                        End If

                        If RangeMismatch Then
                            Return False
                        End If

                        If RangePatternEmpty Then
                            PatternIndex += 1
                            Continue Do
                        End If

                    Case "*"c, ChrW(&HFF0A)
                        Dim AsteriskMismatch, AsteriskPatternError As Boolean

                        MatchAsterisk(
                            Source,
                            SourceLength,
                            SourceIndex,
                            SourceLigatureInfo,
                            Pattern,
                            PatternLength,
                            PatternIndex,
                            PatternLigatureInfo,
                            AsteriskMismatch,
                            AsteriskPatternError,
                            Comparer,
                            Options)

                        If AsteriskPatternError Then
                            Throw New ArgumentException(GetResourceString(SR.Argument_InvalidValue1, "Pattern"))
                        End If

                        Return Not AsteriskMismatch

                    Case Else
                        ' Not a special pattern character.  Just see if we have a match.
                        '
                        If CompareChars(
                                Source,
                                SourceLength,
                                SourceIndex,
                                SourceIndex,
                                SourceLigatureInfo,
                                Pattern,
                                PatternLength,
                                PatternIndex,
                                PatternIndex,
                                PatternLigatureInfo,
                                Comparer,
                                Options) <> 0 Then
                            Return False
                        End If

                End Select

                PatternIndex += 1
                SourceIndex += 1
            Loop

            ' Check for the special case that we're at the end of the source,
            ' and the pattern has nothing left in it but *'s or empty []'s.
            '
            While PatternIndex < PatternLength
                p = Pattern.Chars(PatternIndex)

                If p = "*"c OrElse p = ChrW(&HFF0A) Then
                    PatternIndex += 1

                ElseIf PatternIndex + 1 < PatternLength AndAlso
                        ((p = "["c AndAlso Pattern.Chars(PatternIndex + 1) = "]"c) OrElse
                         (p = ChrW(&HFF3B) AndAlso Pattern.Chars(PatternIndex + 1) = ChrW(&HFF3D))) Then

                    PatternIndex += 2
                Else
                    Exit While
                End If
            End While

            Return PatternIndex >= PatternLength AndAlso SourceIndex >= SourceLength
        End Function

        Private Shared Sub SkipToEndOfExpandedChar(ByVal InputLigatureInfo As LigatureInfo(), ByVal Length As Integer, ByRef Current As Integer)

            If InputLigatureInfo Is Nothing Then
                'Nothing to do for the option compare binary case or the simple option compare text case
            Else
                If Current < Length AndAlso InputLigatureInfo(Current).Kind = CharKind.ExpandedChar1 Then
                    Current = Current + 1
                End If
            End If
        End Sub

        Private Shared Function CompareChars _
        (
            ByVal Left As String,
            ByVal LeftLength As Integer,
            ByVal LeftStart As Integer,
            ByRef LeftEnd As Integer,
            ByVal LeftLigatureInfo As LigatureInfo(),
            ByVal Right As String,
            ByVal RightLength As Integer,
            ByVal RightStart As Integer,
            ByRef RightEnd As Integer,
            ByVal RightLigatureInfo As LigatureInfo(),
            ByVal Comparer As CompareInfo,
            ByVal Options As CompareOptions,
            Optional ByVal MatchBothCharsOfExpandedCharInRight As Boolean = False,
            Optional ByVal UseUnexpandedCharForRight As Boolean = False
        ) As Integer

            LeftEnd = LeftStart
            RightEnd = RightStart

            If Options = CompareOptions.Ordinal Then
                ' Ordinal compare
                '
                Return AscW(Left.Chars(LeftStart)) - AscW(Right.Chars(RightStart))
            End If

            Debug.Assert(Comparer IsNot Nothing, "Like Operator - Comparer expected for option compare text")
            Debug.Assert(Not MatchBothCharsOfExpandedCharInRight OrElse Not UseUnexpandedCharForRight, "Conflicting compare options")

            If UseUnexpandedCharForRight Then
                If RightLigatureInfo IsNot Nothing AndAlso RightLigatureInfo(RightEnd).Kind = CharKind.ExpandedChar1 Then

                    Right = Right.Substring(RightStart, RightEnd - RightStart)
                    Right = Right & RightLigatureInfo(RightEnd).CharBeforeExpansion
                    RightEnd += 1
                    Return CompareChars(Left.Substring(LeftStart, LeftEnd - LeftStart + 1), Right, Comparer, Options)

                End If

            ElseIf MatchBothCharsOfExpandedCharInRight Then

                Dim SavedRightEnd As Integer = RightEnd
                SkipToEndOfExpandedChar(RightLigatureInfo, RightLength, RightEnd)

                ' If matching both expanded characters on the right, then consider multiple characters on the left too
                '
                If SavedRightEnd < RightEnd Then

                    Dim NumberOfExtraCharsToCompare As Integer = 0
                    If LeftEnd + 1 < LeftLength Then
                        NumberOfExtraCharsToCompare = 1
                    End If

                    Dim MatchResult As Integer =
                        CompareChars(Left.Substring(LeftStart, LeftEnd - LeftStart + 1 + NumberOfExtraCharsToCompare), Right.Substring(RightStart, RightEnd - RightStart + 1), Comparer, Options)

                    If MatchResult = 0 Then
                        LeftEnd = LeftEnd + NumberOfExtraCharsToCompare
                    End If

                    Return MatchResult
                End If
            End If

            Debug.Assert(LeftEnd < LeftLength AndAlso RightEnd < RightLength, "Comparing chars beyond end of string")

            If LeftEnd = LeftStart AndAlso RightEnd = RightStart Then
                Return Comparer.Compare(Left.Chars(LeftStart), Right.Chars(RightStart), Options)
            End If

            Return CompareChars(Left.Substring(LeftStart, LeftEnd - LeftStart + 1), Right.Substring(RightStart, RightEnd - RightStart + 1), Comparer, Options)

        End Function

        Private Shared Function CompareChars _
        (
            ByVal Left As String,
            ByVal Right As String,
            ByVal Comparer As CompareInfo,
            ByVal Options As CompareOptions
        ) As Integer

            If Options = CompareOptions.Ordinal Then
                ' Ordinal compare
                '
                Return AscW(Left.Chars(0)) - AscW(Right.Chars(0))
            End If

            Debug.Assert(Comparer IsNot Nothing, "Like Operator - Comparer expected for option compare text")

            Return Comparer.Compare(Left, Right, Options)

        End Function

        Private Shared Function CompareChars _
        (
            ByVal Left As Char,
            ByVal Right As Char,
            ByVal Comparer As CompareInfo,
            ByVal Options As CompareOptions
        ) As Integer

            If Options = CompareOptions.Ordinal Then
                ' Ordinal compare
                '
                Return AscW(Left) - AscW(Right)
            End If

            Debug.Assert(Comparer IsNot Nothing, "Like Operator - Comparer expected for option compare text")

            Return Comparer.Compare(Left, Right, Options)

        End Function

        Private Shared Sub MatchRange _
        (
            ByVal Source As String,
            ByVal SourceLength As Integer,
            ByRef SourceIndex As Integer,
            ByVal SourceLigatureInfo As LigatureInfo(),
            ByVal Pattern As String,
            ByVal PatternLength As Integer,
            ByRef PatternIndex As Integer,
            ByVal PatternLigatureInfo As LigatureInfo(),
            ByRef RangePatternEmpty As Boolean,
            ByRef Mismatch As Boolean,
            ByRef PatternError As Boolean,
            ByVal Comparer As CompareInfo,
            ByVal Options As CompareOptions,
            Optional ByRef SeenNot As Boolean = False,
            Optional ByVal RangeList As List(Of Range) = Nothing,
            Optional ByVal ValidatePatternWithoutMatching As Boolean = False
        )

            Debug.Assert(PatternIndex <= PatternLength AndAlso
                         (Pattern.Chars(PatternIndex) = "[" OrElse Pattern.Chars(PatternIndex) = ChrW(&HFF3B)),
                            "Like operator - Unexpected range matching")

            Debug.Assert(RangeList Is Nothing OrElse ValidatePatternWithoutMatching, "Unexpected options to MatchRange")

            Dim RangeStart, RangeEnd As String
            Dim Range As Range

            RangePatternEmpty = False
            Mismatch = False
            PatternError = False
            SeenNot = False

            PatternIndex += 1

            If PatternIndex >= PatternLength Then
                PatternError = True
                Return
            End If

            Dim p As Char = Pattern.Chars(PatternIndex)

            If p = "!"c OrElse p = ChrW(&HFF01) Then
                SeenNot = True
                PatternIndex += 1

                If PatternIndex >= PatternLength Then
                    Mismatch = True
                    Return
                End If

                p = Pattern.Chars(PatternIndex)
            End If

            If p = "]"c OrElse p = ChrW(&HFF3D) Then

                If SeenNot Then
                    'We got "[!]" ?	Treat it as the single literal character "!".
                    '
                    SeenNot = False

                    If (Not ValidatePatternWithoutMatching) Then
                        Mismatch = Not (CompareChars(Source.Chars(SourceIndex), "!"c, Comparer, Options) = 0)
                    End If

                    If RangeList IsNot Nothing Then
                        Range.Start = PatternIndex - 1
                        Range.StartLength = 1
                        Range.End = -1
                        Range.EndLength = 0
                        RangeList.Add(Range)
                    End If

                    Return
                End If

                ' Ignore empty brackets
                RangePatternEmpty = True
                Return
            End If

            ' Scan through character list
            '
            Do
                RangeStart = Nothing
                RangeEnd = Nothing

                If p = "]"c OrElse p = ChrW(&HFF3D) Then
                    Mismatch = Not SeenNot
                    Return      'End of "[...]" match
                End If

                ' Try to match the expanded ligature
                '
                Dim SourceNextIndex, PatternNextIndex As Integer
                Dim CompareResult As Integer

                If Not ValidatePatternWithoutMatching AndAlso
                   PatternLigatureInfo IsNot Nothing AndAlso
                   PatternLigatureInfo(PatternIndex).Kind = CharKind.ExpandedChar1 Then

                    ' VB6 compat - Match expanded char and return in this case without even validating RangeStart > RangeEnd
                    '
                    CompareResult =
                        CompareChars(
                            Source,
                            SourceLength,
                            SourceIndex,
                            SourceNextIndex,
                            SourceLigatureInfo,
                            Pattern,
                            PatternLength,
                            PatternIndex,
                            PatternNextIndex,
                            PatternLigatureInfo,
                            Comparer,
                            Options,
                            MatchBothCharsOfExpandedCharInRight:=True)

                    If CompareResult = 0 Then
                        SourceIndex = SourceNextIndex
                        PatternIndex = PatternNextIndex
                        GoTo OneCharMatch
                    End If

                Else
                    PatternNextIndex = PatternIndex
                    SkipToEndOfExpandedChar(PatternLigatureInfo, PatternLength, PatternNextIndex)
                End If

                Range.Start = PatternIndex
                Range.StartLength = PatternNextIndex - PatternIndex + 1

                ' Store the range start char
                '
                If Options = CompareOptions.Ordinal Then
                    RangeStart = Pattern.Chars(PatternIndex)
                ElseIf PatternLigatureInfo IsNot Nothing AndAlso PatternLigatureInfo(PatternIndex).Kind = CharKind.ExpandedChar1 Then
                    RangeStart = PatternLigatureInfo(PatternIndex).CharBeforeExpansion
                    PatternIndex = PatternNextIndex
                Else
                    RangeStart = Pattern.Substring(PatternIndex, PatternNextIndex - PatternIndex + 1)
                    PatternIndex = PatternNextIndex
                End If


                If PatternNextIndex + 2 < PatternLength AndAlso
                   (Pattern.Chars(PatternNextIndex + 1) = "-"c OrElse Pattern.Chars(PatternNextIndex + 1) = ChrW(&HFF0D)) AndAlso
                   (Pattern.Chars(PatternNextIndex + 2) <> "]"c AndAlso Pattern.Chars(PatternNextIndex + 2) <> ChrW(&HFF3D)) Then

                    ' We're at the last character of a range.
                    '
                    PatternIndex += 2

                    ' Try to match one char
                    '
                    If Not ValidatePatternWithoutMatching AndAlso
                       PatternLigatureInfo IsNot Nothing AndAlso
                       PatternLigatureInfo(PatternIndex).Kind = CharKind.ExpandedChar1 Then

                        ' VB6 compat - Match expanded char and return in this case without even validating RangeStart > RangeEnd
                        '
                        CompareResult =
                           CompareChars(
                               Source,
                               SourceLength,
                               SourceIndex,
                               SourceNextIndex,
                               SourceLigatureInfo,
                               Pattern,
                               PatternLength,
                               PatternIndex,
                               PatternNextIndex,
                               PatternLigatureInfo,
                               Comparer,
                               Options,
                               MatchBothCharsOfExpandedCharInRight:=True)

                        If CompareResult = 0 Then
                            PatternIndex = PatternNextIndex
                            GoTo OneCharMatch
                        End If

                    Else
                        PatternNextIndex = PatternIndex
                        SkipToEndOfExpandedChar(PatternLigatureInfo, PatternLength, PatternNextIndex)
                    End If

                    Range.End = PatternIndex
                    Range.EndLength = PatternNextIndex - PatternIndex + 1

                    ' Store the range end char
                    '
                    If Options = CompareOptions.Ordinal Then
                        RangeEnd = Pattern.Chars(PatternIndex)
                    ElseIf PatternLigatureInfo IsNot Nothing AndAlso PatternLigatureInfo(PatternIndex).Kind = CharKind.ExpandedChar1 Then
                        RangeEnd = PatternLigatureInfo(PatternIndex).CharBeforeExpansion
                        PatternIndex = PatternNextIndex
                    Else
                        RangeEnd = Pattern.Substring(PatternIndex, PatternNextIndex - PatternIndex + 1)
                        PatternIndex = PatternNextIndex
                    End If


                    If CompareChars(RangeStart, RangeEnd, Comparer, Options) > 0 Then
                        PatternError = True
                        Return
                    End If

                    If Not ValidatePatternWithoutMatching AndAlso
                           CompareChars(
                               Source,
                               SourceLength,
                               SourceIndex,
                               SourceNextIndex,
                               SourceLigatureInfo,
                               Pattern,
                               Range.Start + Range.StartLength,
                               Range.Start,
                               Nothing,
                               PatternLigatureInfo,
                               Comparer,
                               Options,
                               UseUnexpandedCharForRight:=True) >= 0 AndAlso
                           CompareChars(
                               Source,
                               SourceLength,
                               SourceIndex,
                               SourceNextIndex,
                               SourceLigatureInfo,
                               Pattern,
                               Range.End + Range.EndLength,
                               Range.End,
                               Nothing,
                               PatternLigatureInfo,
                               Comparer,
                               Options,
                               UseUnexpandedCharForRight:=True) <= 0 Then
                        'Character was within range
OneCharMatch:
                        Debug.Assert(Not ValidatePatternWithoutMatching, "Unexpected string matching when validating pattern string")

                        If SeenNot Then
                            Mismatch = True
                            Return
                        End If

                        Do
                            PatternIndex += 1

                            If PatternIndex >= PatternLength Then
                                PatternError = True
                                Return
                            End If

                        Loop While Pattern.Chars(PatternIndex) <> "]"c AndAlso
                                   Pattern.Chars(PatternIndex) <> ChrW(&HFF3D)

                        SourceIndex = SourceNextIndex
                        Return  'Match
                    End If

                Else
                    ' Single character match
                    '
                    '
                    If Not ValidatePatternWithoutMatching AndAlso
                        CompareChars(
                            Source,
                            SourceLength,
                            SourceIndex,
                            SourceNextIndex,
                            SourceLigatureInfo,
                            Pattern,
                            Range.Start + Range.StartLength,
                            Range.Start,
                            Nothing,
                            PatternLigatureInfo,
                            Comparer,
                            Options,
                            UseUnexpandedCharForRight:=True) = 0 Then

                        GoTo OneCharMatch
                    End If

                    ' No range end for single characters in list
                    '
                    Range.End = -1
                    Range.EndLength = 0

                End If


                If RangeList IsNot Nothing Then
                    RangeList.Add(Range)
                End If

                PatternIndex += 1

                If PatternIndex >= PatternLength Then
                    PatternError = True
                    Return
                End If

                p = Pattern.Chars(PatternIndex)
            Loop

        End Sub

        Private Shared Function ValidateRangePattern _
        (
            ByVal Pattern As String,
            ByVal PatternLength As Integer,
            ByRef PatternIndex As Integer,
            ByVal PatternLigatureInfo As LigatureInfo(),
            ByVal Comparer As CompareInfo,
            ByVal Options As CompareOptions,
            ByRef SeenNot As Boolean,
            ByRef RangeList As List(Of Range)
        ) As Boolean

            Const ValidPatternWithoutMatching As Boolean = True

            Dim PatternError As Boolean

            MatchRange(
                Nothing,
                -1,
                -1,
                Nothing,
                Pattern,
                PatternLength,
                PatternIndex,
                PatternLigatureInfo,
                Nothing,
                Nothing,
                PatternError,
                Comparer,
                Options,
                SeenNot,
                RangeList,
                ValidPatternWithoutMatching)

            Return Not PatternError
        End Function

        Private Enum PatternType
            [STRING]
            EXCLIST
            INCLIST
            DIGIT
            ANYCHAR
            STAR
            NONE
        End Enum

        Private Structure PatternGroup
            Friend PatType As PatternType
            Friend MaxSourceIndex As Integer
            Friend CharCount As Integer

            ' StringPatternStart, StringPatternEnd - there are the indices into the original source string
            ' and are NOT indices into StringPattern.
            '
#If DEBUG Then
            Private m_StringPatternStart As Integer  'For PatternType.[STRING]
            Friend Property StringPatternStart() As Integer
                Get
                    Debug.Assert(PatType = PatternType.STRING, "Unexpected pattern group type")
                    Return m_StringPatternStart
                End Get
                Set(ByVal Value As Integer)
                    Debug.Assert(PatType = PatternType.STRING, "Unexpected pattern group type")
                    m_StringPatternStart = Value
                End Set
            End Property

            Private m_StringPatternEnd As Integer  'For PatternType.[STRING]
            Friend Property StringPatternEnd() As Integer
                Get
                    Debug.Assert(PatType = PatternType.STRING, "Unexpected pattern group type")
                    Return m_StringPatternEnd
                End Get
                Set(ByVal Value As Integer)
                    Debug.Assert(PatType = PatternType.STRING, "Unexpected pattern group type")
                    m_StringPatternEnd = Value
                End Set
            End Property

#Else
            Friend StringPatternStart As Integer
            Friend StringPatternEnd As Integer
#End If

#If DEBUG Then
            Private m_MinSourceIndex As Integer
            Friend Property MinSourceIndex() As Integer
                Get
                    Debug.Assert(PatType = PatternType.STAR OrElse PatType = PatternType.NONE, "Unexpected pattern group type")
                    Return m_MinSourceIndex
                End Get
                Set(ByVal Value As Integer)
                    Debug.Assert(PatType = PatternType.STAR OrElse PatType = PatternType.NONE, "Unexpected pattern group type")
                    m_MinSourceIndex = Value
                End Set
            End Property
#Else
            Friend MinSourceIndex As Integer
#End If

#If DEBUG Then
            Private m_RangeStarts As String()
            Property RangeStarts() As String()
                Get
                    Debug.Assert(PatType = PatternType.EXCLIST OrElse PatType = PatternType.INCLIST, "Unexpected pattern group type")
                    Return m_RangeStarts
                End Get
                Set(ByVal value As String())
                    Debug.Assert(PatType = PatternType.EXCLIST OrElse PatType = PatternType.INCLIST, "Unexpected pattern group type")
                    m_RangeStarts = value
                End Set
            End Property


            Private m_RangeList As List(Of Range)
            Property RangeList() As List(Of Range)
                Get
                    Debug.Assert(PatType = PatternType.EXCLIST OrElse PatType = PatternType.INCLIST, "Unexpected pattern group type")
                    Return m_RangeList
                End Get
                Set(ByVal Value As List(Of Range))
                    Debug.Assert(PatType = PatternType.EXCLIST OrElse PatType = PatternType.INCLIST, "Unexpected pattern group type")
                    m_RangeList = Value
                End Set
            End Property
#Else
            Friend RangeList As List(Of Range)
#End If

            Public StartIndexOfPossibleMatch As Integer
        End Structure

        Private Structure Range
            Friend Start As Integer     'Index into the pattern string
            Friend StartLength As Integer

            Friend [End] As Integer     'Index into the pattern string
            Friend EndLength As Integer
        End Structure

        Private Shared Sub BuildPatternGroups _
        (
            ByVal Source As String,
            ByVal SourceLength As Integer,
            ByRef SourceIndex As Integer,
            ByVal SourceLigatureInfo As LigatureInfo(),
            ByVal Pattern As String,
            ByVal PatternLength As Integer,
            ByRef PatternIndex As Integer,
            ByVal PatternLigatureInfo As LigatureInfo(),
            ByRef PatternError As Boolean,
            ByRef PGIndexForLastAsterisk As Integer,
            ByVal Comparer As CompareInfo,
            ByVal Options As CompareOptions,
            ByRef PatternGroups() As PatternGroup
        )
            ' Pattern groups:
            '
            ' 1. A string of characters not containing a special pattern
            ' character.
            '
            ' 2. Any number of consecutive "?".
            '
            ' 3. Any number of consecutive "#".
            '
            ' 4. A bracketed character list.
            '
            ' 5. Any number of consecutive "*" (collapsed together).
            '
            ' We have a local array that is good for small patterns.
            ' If the pattern gets large, we allocate additional memory.
            '
            ' PG - pattern group

            PatternError = False
            PGIndexForLastAsterisk = 0

            Dim PGIndex As Integer

            Const PGMaxCount As Integer = 16
            PatternGroups = New PatternGroup(PGMaxCount - 1) {}

            Dim PGLast As Integer = PGMaxCount - 1
            Dim PrevPatType As PatternType = PatternType.NONE
            PGIndex = 0

            Do
                ' Increase the size of the Pattern groups array if required
                '
                If PGIndex >= PGLast Then
                    Dim NewPatternGroups(PGLast + PGMaxCount) As PatternGroup
                    PatternGroups.CopyTo(NewPatternGroups, 0)
                    PatternGroups = NewPatternGroups
                    PGLast = PGLast + PGMaxCount
                End If

                Dim p As Char = Pattern.Chars(PatternIndex)

                Select Case p

                    Case "*"c, ChrW(&HFF0A)
                        ' Record the "*" pattern and collapse multiple contiguous "*"'s if possible
                        '
                        If PrevPatType <> PatternType.STAR Then
                            PrevPatType = PatternType.STAR
                            PatternGroups(PGIndex).PatType = PatternType.STAR
                            PGIndexForLastAsterisk = PGIndex
                            PGIndex += 1
                        End If

                    Case "["c, ChrW(&HFF3B)
                        Dim SeenNot As Boolean = False
                        Dim RangeList As New List(Of Range)

                        If Not ValidateRangePattern(Pattern, PatternLength, PatternIndex, PatternLigatureInfo, Comparer, Options, SeenNot, RangeList) Then
                            PatternError = True
                            Return
                        End If

                        ' Ignore empty "[]" and don't build a pattern group for it
                        '
                        If RangeList.Count = 0 Then
                            Exit Select
                        End If

                        If SeenNot Then
                            PrevPatType = PatternType.EXCLIST
                        Else
                            PrevPatType = PatternType.INCLIST
                        End If

                        PatternGroups(PGIndex).PatType = PrevPatType
                        PatternGroups(PGIndex).CharCount = 1
                        PatternGroups(PGIndex).RangeList = RangeList

                        PGIndex += 1

                    Case "#"c, ChrW(&HFF03)

                        If PrevPatType = PatternType.DIGIT Then
                            PatternGroups(PGIndex - 1).CharCount += 1
                        Else
                            PatternGroups(PGIndex).PatType = PatternType.DIGIT
                            PatternGroups(PGIndex).CharCount = 1
                            PGIndex += 1
                            PrevPatType = PatternType.DIGIT
                        End If

                    Case "?"c, ChrW(&HFF1F)

                        If PrevPatType = PatternType.ANYCHAR Then
                            PatternGroups(PGIndex - 1).CharCount += 1
                        Else
                            PatternGroups(PGIndex).PatType = PatternType.ANYCHAR
                            PatternGroups(PGIndex).CharCount = 1
                            PGIndex += 1
                            PrevPatType = PatternType.ANYCHAR
                        End If

                    Case Else

                        Dim StringPatternStart As Integer = PatternIndex

                        Dim StringPatternEnd As Integer = PatternIndex

                        If StringPatternEnd >= PatternLength Then
                            StringPatternEnd = PatternLength - 1
                        End If

                        If PrevPatType = PatternType.STRING Then
                            PatternGroups(PGIndex - 1).CharCount += 1
                            PatternGroups(PGIndex - 1).StringPatternEnd = StringPatternEnd
                        Else
                            PatternGroups(PGIndex).PatType = PatternType.STRING
                            PatternGroups(PGIndex).CharCount = 1
                            PatternGroups(PGIndex).StringPatternStart = StringPatternStart
                            PatternGroups(PGIndex).StringPatternEnd = StringPatternEnd

                            PGIndex += 1
                            PrevPatType = PatternType.STRING
                        End If

                End Select

                PatternIndex += 1

            Loop While PatternIndex < PatternLength

            'Add ending mark
            '
            PatternGroups(PGIndex).PatType = PatternType.NONE
            PatternGroups(PGIndex).MinSourceIndex = SourceLength

            ' Pattern is compiled into an array of Pattern groups.  Walk backward through list to assign max positions.
            '
            Dim MaxPossibleStart As Integer = SourceLength
            Do While PGIndex > 0
                Select Case PatternGroups(PGIndex).PatType
                    Case PatternType.STRING
                        MaxPossibleStart -= PatternGroups(PGIndex).CharCount

                    Case PatternType.DIGIT, PatternType.ANYCHAR
                        MaxPossibleStart -= PatternGroups(PGIndex).CharCount

                    Case PatternType.EXCLIST, PatternType.INCLIST
                        MaxPossibleStart -= 1

                    Case PatternType.STAR, PatternType.NONE
                        'Can start anywhere

                    Case Else
                        Debug.Assert(False, "Unexpected pattern kind")
                End Select

                PatternGroups(PGIndex).MaxSourceIndex = MaxPossibleStart
                PGIndex -= 1
            Loop

        End Sub

        Private Shared Sub MatchAsterisk _
        (
            ByVal Source As String,
            ByVal SourceLength As Integer,
            ByVal SourceIndex As Integer,
            ByVal SourceLigatureInfo As LigatureInfo(),
            ByVal Pattern As String,
            ByVal PatternLength As Integer,
            ByVal PatternIndex As Integer,
            ByVal PattternLigatureInfo As LigatureInfo(),
            ByRef Mismatch As Boolean,
            ByRef PatternError As Boolean,
            ByVal Comparer As CompareInfo,
            ByVal Options As CompareOptions
        )

            Debug.Assert(PatternIndex <= PatternLength AndAlso
                         (Pattern.Chars(PatternIndex) = "*"c OrElse Pattern.Chars(PatternIndex) = ChrW(&HFF0A)),
                            "Like operator - Unexpected asterisk matching")

            Mismatch = False
            PatternError = False

            If PatternIndex >= PatternLength Then
                Return  'Successful match
            End If

            ' We've found a "*" in the pattern that is not at the end.
            ' Now we need to scan ahead in the pattern and compile it
            ' into an array of structs describing each pattern group.
            '

            Dim PatternGroups() As PatternGroup = Nothing
            Dim PGIndex As Integer
            Dim PGIndexForLastAsterisk As Integer

            BuildPatternGroups(
                Source,
                SourceLength,
                SourceIndex,
                SourceLigatureInfo,
                Pattern,
                PatternLength,
                PatternIndex,
                PattternLigatureInfo,
                PatternError,
                PGIndexForLastAsterisk,
                Comparer,
                Options,
                PatternGroups)

            If PatternError Then
                Return
            End If

            Debug.Assert(PatternGroups IsNot Nothing AndAlso
                         PatternGroups.Length > 0 AndAlso
                         PatternGroups(0).PatType = PatternType.STAR, "Pattern parsing failed")

            ' Start the search
            '

            If PatternGroups(PGIndexForLastAsterisk + 1).PatType <> PatternType.NONE Then
                '
                ' Optimize for the "<AnyPattern>*<NonStarPatterns>" case
                ' Helps discard mismatches faster and in some cases, the match are also
                ' faster
                '
                Dim SavedSourceIndex As Integer = SourceIndex
                Dim NumberOfCharsToMatch As Integer

                PGIndex = PGIndexForLastAsterisk + 1
                Do
                    NumberOfCharsToMatch += PatternGroups(PGIndex).CharCount
                    PGIndex += 1
                Loop While PatternGroups(PGIndex).PatType <> PatternType.NONE

                SourceIndex = SourceLength
                SubtractChars(Source, SourceLength, SourceIndex, NumberOfCharsToMatch, SourceLigatureInfo, Options)

                MatchAsterisk(
                    Source,
                    SourceLength,
                    SourceIndex,
                    SourceLigatureInfo,
                    Pattern,
                    PattternLigatureInfo,
                    PatternGroups,
                    PGIndexForLastAsterisk,
                    Mismatch,
                    PatternError,
                    Comparer,
                    Options)

                If PatternError OrElse Mismatch Then
                    Return
                End If

                SourceLength = PatternGroups(PGIndexForLastAsterisk + 1).StartIndexOfPossibleMatch

                If SourceLength <= 0 Then
                    Return
                End If

                ' Move the end marker to just after the last asterisk because everything afterwards have been
                ' matched successfully.
                '
                Debug.Assert(PatternGroups(PGIndex).PatType = PatternType.NONE, "Unexpected pattern end")
                PatternGroups(PGIndex).MaxSourceIndex = SourceLength
                PatternGroups(PGIndex).MinSourceIndex = SourceLength
                PatternGroups(PGIndex).StartIndexOfPossibleMatch = 0
                PatternGroups(PGIndexForLastAsterisk + 1) = PatternGroups(PGIndex)

                ' Reset the pattern group corresponding to the last asterisk because it needs to be reused in
                ' the next phase of matching
                '
                PatternGroups(PGIndexForLastAsterisk).MinSourceIndex = 0
                PatternGroups(PGIndexForLastAsterisk).StartIndexOfPossibleMatch = 0

                PGIndex = PGIndexForLastAsterisk + 1
                Dim MaxPossibleStart As Integer = SourceLength
                Do While PGIndex > 0
                    Select Case PatternGroups(PGIndex).PatType
                        Case PatternType.STRING
                            MaxPossibleStart -= PatternGroups(PGIndex).CharCount

                        Case PatternType.DIGIT, PatternType.ANYCHAR
                            MaxPossibleStart -= PatternGroups(PGIndex).CharCount

                        Case PatternType.EXCLIST, PatternType.INCLIST
                            MaxPossibleStart -= 1

                        Case PatternType.STAR, PatternType.NONE
                            'Can start anywhere

                        Case Else
                            Debug.Assert(False, "Unexpected pattern kind")
                    End Select

                    PatternGroups(PGIndex).MaxSourceIndex = MaxPossibleStart
                    PGIndex -= 1
                Loop

                SourceIndex = SavedSourceIndex
            End If

            MatchAsterisk(
                Source,
                SourceLength,
                SourceIndex,
                SourceLigatureInfo,
                Pattern,
                PattternLigatureInfo,
                PatternGroups,
                0,
                Mismatch,
                PatternError,
                Comparer,
                Options)
        End Sub

        Private Shared Sub MatchAsterisk _
        (
            ByVal Source As String,
            ByVal SourceLength As Integer,
            ByVal SourceIndex As Integer,
            ByVal SourceLigatureInfo As LigatureInfo(),
            ByVal Pattern As String,
            ByVal PatternLigatureInfo As LigatureInfo(),
            ByVal PatternGroups() As PatternGroup,
            ByVal PGIndex As Integer,
            ByRef Mismatch As Boolean,
            ByRef PatternError As Boolean,
            ByVal Comparer As CompareInfo,
            ByVal Options As CompareOptions
        )

            Dim PGPrevMismatchIndex As Integer = PGIndex
            Dim PrevMismatchSourceIndex As Integer = SourceIndex
            Dim PGSaved As Integer = -1
            Dim PGRestartAsteriskIndex As Integer = -1

            Debug.Assert(PatternGroups(PGIndex).PatType = PatternType.STAR, "Unexpected start of pattern groups list")

            PatternGroups(PGIndex).MinSourceIndex = SourceIndex
            PatternGroups(PGIndex).StartIndexOfPossibleMatch = SourceIndex
            PGIndex += 1

            Do
                Dim PGCurrent As PatternGroup = PatternGroups(PGIndex)

                Select Case PGCurrent.PatType

                    Case PatternType.STRING
MatchString:
                        If SourceIndex > PGCurrent.MaxSourceIndex Then
                            Mismatch = True
                            Return
                        End If

                        PatternGroups(PGIndex).StartIndexOfPossibleMatch = SourceIndex

                        Dim StringPatternIndex As Integer = PGCurrent.StringPatternStart
                        Dim SourceSecondCharIndex As Integer = 0
                        Dim SourceMatchIndex As Integer = SourceIndex
                        Dim FirstIteration As Boolean = True

                        Do
                            Dim CompareResult As Integer =
                                CompareChars(
                                    Source,
                                    SourceLength,
                                    SourceMatchIndex,
                                    SourceMatchIndex,
                                    SourceLigatureInfo,
                                    Pattern,
                                    PGCurrent.StringPatternEnd + 1,
                                    StringPatternIndex,
                                    StringPatternIndex,
                                    PatternLigatureInfo,
                                    Comparer,
                                    Options)

                            If FirstIteration Then
                                FirstIteration = False
                                SourceSecondCharIndex = SourceMatchIndex + 1
                            End If

                            If CompareResult <> 0 Then
                                SourceIndex = SourceSecondCharIndex
                                PGPrevMismatchIndex = PGIndex - 1
                                PrevMismatchSourceIndex = SourceIndex
                                GoTo MatchString
                            End If

                            StringPatternIndex += 1
                            SourceMatchIndex += 1

                            If StringPatternIndex > PGCurrent.StringPatternEnd Then
                                SourceIndex = SourceMatchIndex
                                Exit Select
                            End If

                            If SourceMatchIndex >= SourceLength Then
                                Mismatch = True
                                Return
                            End If

                        Loop

                    Case PatternType.DIGIT
MatchDigits:
                        If SourceIndex > PGCurrent.MaxSourceIndex Then
                            Mismatch = True
                            Return
                        End If

                        PatternGroups(PGIndex).StartIndexOfPossibleMatch = SourceIndex

                        For i As Integer = 1 To PGCurrent.CharCount

                            Dim c As Char = Source.Chars(SourceIndex)
                            SourceIndex += 1

                            If Not Char.IsDigit(c) Then
                                PGPrevMismatchIndex = PGIndex - 1
                                PrevMismatchSourceIndex = SourceIndex
                                GoTo MatchDigits
                            End If

                        Next

                        'Match

                    Case PatternType.EXCLIST, PatternType.INCLIST
MatchList:
                        If SourceIndex > PGCurrent.MaxSourceIndex Then
                            Mismatch = True
                            Return
                        End If

                        PatternGroups(PGIndex).StartIndexOfPossibleMatch = SourceIndex

                        If Not MatchRangeAfterAsterisk(
                                    Source,
                                    SourceLength,
                                    SourceIndex,
                                    SourceLigatureInfo,
                                    Pattern,
                                    PatternLigatureInfo,
                                    PGCurrent,
                                    Comparer,
                                    Options) Then

                            PGPrevMismatchIndex = PGIndex - 1
                            PrevMismatchSourceIndex = SourceIndex
                            GoTo MatchList
                        End If

                        'Match

                    Case PatternType.ANYCHAR

                        If SourceIndex > PGCurrent.MaxSourceIndex Then
                            Mismatch = True
                            Return
                        End If

                        PatternGroups(PGIndex).StartIndexOfPossibleMatch = SourceIndex

                        For i As Integer = 1 To PGCurrent.CharCount
                            If SourceIndex >= SourceLength Then
                                Mismatch = True
                                Return
                            End If

                            SkipToEndOfExpandedChar(SourceLigatureInfo, SourceLength, SourceIndex)
                            SourceIndex += 1
                        Next

                    Case PatternType.NONE

                        PatternGroups(PGIndex).StartIndexOfPossibleMatch = PGCurrent.MaxSourceIndex

                        Debug.Assert(SourceIndex <= PGCurrent.MaxSourceIndex, "Pattern matching lost")

                        If SourceIndex < PGCurrent.MaxSourceIndex Then
                            PGPrevMismatchIndex = PGIndex - 1
                            PrevMismatchSourceIndex = PGCurrent.MaxSourceIndex
                        End If

                        If PatternGroups(PGPrevMismatchIndex).PatType <> PatternType.STAR AndAlso
                           PatternGroups(PGPrevMismatchIndex).PatType <> PatternType.NONE Then
                            GoTo ShiftPosition
                        End If

                        Return  'Match

                    Case PatternType.STAR

                        PatternGroups(PGIndex).StartIndexOfPossibleMatch = SourceIndex
                        PGCurrent.MinSourceIndex = SourceIndex

                        ' See if we've moved our starting point.  If so, we
                        ' back up from the last place it moved, assigning a
                        ' new minimum position in the source string.  Then
                        ' we can start the search over from the new minimum
                        ' position.
                        '

                        Debug.Assert(PatternGroups(PGPrevMismatchIndex).PatType <> PatternType.NONE, "Bad previous mismatch index")

                        If PatternGroups(PGPrevMismatchIndex).PatType <> PatternType.STAR Then

                            If SourceIndex > PGCurrent.MaxSourceIndex Then
                                Mismatch = True
                                Return
                            End If

ShiftPosition:
                            PGSaved = PGIndex
                            SourceIndex = PrevMismatchSourceIndex
                            PGIndex = PGPrevMismatchIndex

                            Do
                                SubtractChars(Source, SourceLength, SourceIndex, PatternGroups(PGIndex).CharCount, SourceLigatureInfo, Options)
                                PGIndex -= 1
                            Loop While PatternGroups(PGIndex).PatType <> PatternType.STAR

                            SourceIndex = Math.Max(SourceIndex, PatternGroups(PGIndex).MinSourceIndex + 1)
                            PatternGroups(PGIndex).MinSourceIndex = SourceIndex
                            PGRestartAsteriskIndex = PGIndex
                        End If

                        PGIndex += 1

                        Continue Do

                End Select

                If PGIndex = PGPrevMismatchIndex Then
                    ' Reached a point where we've matched before.

                    If SourceIndex = PrevMismatchSourceIndex Then
                        ' Reached a point where we've matched before.  Jump ahead
                        ' to where that left off.
                        '
                        SourceIndex = PatternGroups(PGSaved).MinSourceIndex
                        PGIndex = PGSaved
                        PGPrevMismatchIndex = PGSaved

                    ElseIf SourceIndex < PrevMismatchSourceIndex Then
                        ' In certain cases involving ligatures/modifiers, the source
                        ' index could be moved too far back and thus result in this
                        ' scenario.
                        '
                        PatternGroups(PGRestartAsteriskIndex).MinSourceIndex += 1
                        SourceIndex = PatternGroups(PGRestartAsteriskIndex).MinSourceIndex
                        PGIndex = PGRestartAsteriskIndex + 1

                    Else 'SourceIndex > PrevMismatchSourceIndex
                        ' In certain cases involving ligatures/modifiers, more source
                        ' chars than the number of chars between the previous match
                        ' corresponding to a "*" and the start of match for the pattern
                        ' groups where PGIndex > PGPrevMismatchIndex may be matched
                        ' against the pattern groups between the "*" and PGPrevMismatchIndex
                        ' and thus result in this scenario.
                        '
                        PGIndex += 1
                        PGPrevMismatchIndex = PGRestartAsteriskIndex
                    End If
                Else
                    PGIndex += 1
                End If

            Loop
        End Sub

        Private Shared Function MatchRangeAfterAsterisk _
        (
            ByVal Source As String,
            ByVal SourceLength As Integer,
            ByRef SourceIndex As Integer,
            ByVal SourceLigatureInfo As LigatureInfo(),
            ByVal Pattern As String,
            ByVal PatternLigatureInfo As LigatureInfo(),
            ByVal PG As PatternGroup,
            ByVal Comparer As CompareInfo,
            ByVal Options As CompareOptions
        ) As Boolean

            Debug.Assert(PG.PatType = PatternType.EXCLIST OrElse PG.PatType = PatternType.INCLIST, "Unexpected pattern group")

            Dim RangeList As List(Of Range) = PG.RangeList

            'empty [] match can be ignored
            '
            Debug.Assert(RangeList IsNot Nothing AndAlso RangeList.Count > 0, "Empty RangeList unexpected")

            Dim SourceNextIndex As Integer = SourceIndex
            Dim Match As Boolean = False

            For Each Range As Range In RangeList
                Debug.Assert(Range.Start >= 0, "NULL Range start unexpected")
                Dim CompareResultEnd As Integer = 1
                Dim CompareResultStart As Integer

                If PatternLigatureInfo IsNot Nothing AndAlso PatternLigatureInfo(Range.Start).Kind = CharKind.ExpandedChar1 Then
                    CompareResultStart =
                        CompareChars(
                            Source,
                            SourceLength,
                            SourceIndex,
                            SourceNextIndex,
                            SourceLigatureInfo,
                            Pattern,
                            Range.Start + Range.StartLength,
                            Range.Start,
                            0,
                            PatternLigatureInfo,
                            Comparer,
                            Options,
                            MatchBothCharsOfExpandedCharInRight:=True)

                    If CompareResultStart = 0 Then
                        Match = True
                        Exit For
                    End If
                End If

                CompareResultStart =
                    CompareChars(
                        Source,
                        SourceLength,
                        SourceIndex,
                        SourceNextIndex,
                        SourceLigatureInfo,
                        Pattern,
                        Range.Start + Range.StartLength,
                        Range.Start,
                        0,
                        PatternLigatureInfo,
                        Comparer,
                        Options,
                        UseUnexpandedCharForRight:=True)

                If CompareResultStart > 0 AndAlso Range.End >= 0 Then
                    CompareResultEnd =
                        CompareChars(
                            Source,
                            SourceLength,
                            SourceIndex,
                            SourceNextIndex,
                            SourceLigatureInfo,
                            Pattern,
                            Range.End + Range.EndLength,
                            Range.End,
                            0,
                            PatternLigatureInfo,
                            Comparer,
                            Options,
                            UseUnexpandedCharForRight:=True)
                End If

                If CompareResultStart = 0 OrElse
                   (CompareResultStart > 0 AndAlso CompareResultEnd <= 0) Then
                    Match = True
                    Exit For
                End If

            Next

            If PG.PatType = PatternType.EXCLIST Then
                Match = Not Match
            End If

            SourceIndex = SourceNextIndex + 1
            Return Match
        End Function

        Private Shared Sub SubtractChars _
        (
            ByVal Input As String,
            ByVal InputLength As Integer,
            ByRef Current As Integer,
            ByVal CharsToSubtract As Integer,
            ByVal InputLigatureInfo As LigatureInfo(),
            ByVal Options As CompareOptions
        )

            If Options = CompareOptions.Ordinal Then
                Current -= CharsToSubtract
                If Current < 0 Then Current = 0
                Return
            End If

            For i As Integer = 1 To CharsToSubtract
                SubtractOneCharInTextCompareMode(Input, InputLength, Current, InputLigatureInfo, Options)

                If Current < 0 Then
                    Current = 0
                    Exit For
                End If
            Next

        End Sub

        Private Shared Sub SubtractOneCharInTextCompareMode _
        (
            ByVal Input As String,
            ByVal InputLength As Integer,
            ByRef Current As Integer,
            ByVal InputLigatureInfo As LigatureInfo(),
            ByVal Options As CompareOptions
        )
            Debug.Assert(Options <> CompareOptions.Ordinal, "This method should not be invoked in Option compare binary mode")

            If Current >= InputLength Then
                Current -= 1
                Return
            End If

            If InputLigatureInfo IsNot Nothing AndAlso
               InputLigatureInfo(Current).Kind = CharKind.ExpandedChar2 Then
                Current -= 2
            Else
                Current -= 1
            End If

        End Sub

    End Class

End Namespace

