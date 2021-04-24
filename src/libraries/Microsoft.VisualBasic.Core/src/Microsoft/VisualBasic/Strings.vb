' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.

Imports System
Imports System.Diagnostics.CodeAnalysis
Imports System.Globalization
Imports System.Runtime.Versioning
Imports System.Text
Imports Microsoft.VisualBasic.CompilerServices
Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Global.Microsoft.VisualBasic

    Friend NotInheritable Class FormatInfoHolder
        Implements IFormatProvider

        Friend Sub New(ByVal nfi As NumberFormatInfo)
            MyBase.New()
            Me.nfi = nfi
        End Sub

        Private nfi As NumberFormatInfo

        Private Function GetFormat(ByVal service As Type) As Object Implements IFormatProvider.GetFormat
            If service Is GetType(NumberFormatInfo) Then
                Return nfi
            End If
            Throw New ArgumentException(SR.InternalError_VisualBasicRuntime)
        End Function

    End Class

    Public Module Strings
        'Positive format strings
        '0      $n  
        '1      n$ 
        '2      $ n  
        '3      n $ 
        Private ReadOnly CurrencyPositiveFormatStrings() As String = {"'$'n", "n'$'", "'$' n", "n '$'"} 'Note, we wrap the $ in the literal symbol to avoid misinterpretation when using the escape character \ as a currency mark

        'The negative currency pattern needs to be selected based 
        '  on the criteria provided for parens
        'nfi.CurrencyPositivePattern 
        'Negative format strings
        '0      ($n) 
        '1      -$n  
        '2      $-n  
        '3      $n- 
        '4      (n$) 
        '5      -n$ 
        '6      n-$ 
        '7      n$- 
        '8      -n $ 
        '9      -$ n  
        '10     n $- 
        '11     $ n- 
        '12     $- n  
        '13     n- $ 
        '14     ($ n) 
        '15     (n $) 
        Private ReadOnly CurrencyNegativeFormatStrings() As String =
            {"('$'n)", "-'$'n", "'$'-n", "'$'n-", "(n'$')", "-n'$'", "n-'$'", "n'$'-",
              "-n '$'", "-'$' n", "n '$'-", "'$' n-", "'$'- n", "n- '$'", "('$' n)", "(n '$')"} 'Note, we wrap the $ in the literal symbol to avoid misinterpretation when using the escape character \ as a currency mark

        'Value Associated Pattern 
        '0 (n) 
        '1 -n  
        '2 - n  
        '3 n- 
        '4 n - 
        Private ReadOnly NumberNegativeFormatStrings() As String =
            {"(n)", "-n", "- n", "n-", "n -"}

        Friend Enum FormatType
            Number = 0
            Percent = 1
            [Currency] = 2
        End Enum

        Private Const CODEPAGE_SIMPLIFIED_CHINESE As Integer = 936
        Private Const CODEPAGE_TRADITIONAL_CHINESE As Integer = 950
        Private Const STANDARD_COMPARE_FLAGS As CompareOptions =
            CompareOptions.IgnoreCase Or CompareOptions.IgnoreWidth Or CompareOptions.IgnoreKanaType

        Private Const NAMEDFORMAT_FIXED As String = "fixed"
        Private Const NAMEDFORMAT_YES_NO As String = "yes/no"
        Private Const NAMEDFORMAT_ON_OFF As String = "on/off"
        Private Const NAMEDFORMAT_PERCENT As String = "percent"
        Private Const NAMEDFORMAT_STANDARD As String = "standard"
        Private Const NAMEDFORMAT_CURRENCY As String = "currency"
        Private Const NAMEDFORMAT_LONG_TIME As String = "long time"
        Private Const NAMEDFORMAT_LONG_DATE As String = "long date"
        Private Const NAMEDFORMAT_SCIENTIFIC As String = "scientific"
        Private Const NAMEDFORMAT_TRUE_FALSE As String = "true/false"
        Private Const NAMEDFORMAT_SHORT_TIME As String = "short time"
        Private Const NAMEDFORMAT_SHORT_DATE As String = "short date"
        Private Const NAMEDFORMAT_MEDIUM_DATE As String = "medium date"
        Private Const NAMEDFORMAT_MEDIUM_TIME As String = "medium time"
        Private Const NAMEDFORMAT_GENERAL_DATE As String = "general date"
        Private Const NAMEDFORMAT_GENERAL_NUMBER As String = "general number"

        Friend ReadOnly m_InvariantCompareInfo As CompareInfo = CultureInfo.InvariantCulture.CompareInfo

        'This is shared across Cached
        Private m_SyncObject As Object = New Object
        Private m_LastUsedYesNoCulture As CultureInfo
        Private m_CachedYesNoFormatStyle As String

        Private ReadOnly Property CachedYesNoFormatStyle() As String
            Get
                Dim ci As CultureInfo = GetCultureInfo()
                SyncLock m_SyncObject
                    If Not m_LastUsedYesNoCulture Is ci Then
                        m_LastUsedYesNoCulture = ci
                        m_CachedYesNoFormatStyle = SR.YesNoFormatStyle
                    End If
                    Return m_CachedYesNoFormatStyle
                End SyncLock
            End Get
        End Property

        Private m_LastUsedOnOffCulture As CultureInfo
        Private m_CachedOnOffFormatStyle As String
        Private ReadOnly Property CachedOnOffFormatStyle() As String
            Get
                Dim ci As CultureInfo = GetCultureInfo()
                SyncLock m_SyncObject
                    If Not m_LastUsedOnOffCulture Is ci Then
                        m_LastUsedOnOffCulture = ci
                        m_CachedOnOffFormatStyle = SR.OnOffFormatStyle
                    End If
                    Return m_CachedOnOffFormatStyle
                End SyncLock
            End Get
        End Property

        Private m_LastUsedTrueFalseCulture As CultureInfo
        Private m_CachedTrueFalseFormatStyle As String
        Private ReadOnly Property CachedTrueFalseFormatStyle() As String
            Get
                Dim ci As CultureInfo = GetCultureInfo()
                SyncLock m_SyncObject
                    If Not m_LastUsedTrueFalseCulture Is ci Then
                        m_LastUsedTrueFalseCulture = ci
                        m_CachedTrueFalseFormatStyle = SR.TrueFalseFormatStyle
                    End If
                    Return m_CachedTrueFalseFormatStyle
                End SyncLock
            End Get
        End Property

        Private Function PRIMARYLANGID(ByVal lcid As Integer) As Integer
            Return (lcid And &H3FF)
        End Function

        Private Function GetAscChrEncoding() As Encoding
            Return Encoding.GetEncoding(GetLocaleCodePage())
        End Function

        '============================================================================
        ' Character manipulation functions.
        '============================================================================
        Public Function Asc(ByVal [String] As Char) As Integer

            'The IConvertible.ToInt32 implementation on Char
            '   just calls Convert.ToInt32()
            Dim CharValue As Integer = Convert.ToInt32([String])

            If CharValue < 128 Then
                Return CharValue
            End If

            Try
                Dim enc As Encoding
                Dim b() As Byte
                Dim c() As Char
                Dim iByteCount As Integer

                enc = GetAscChrEncoding()

                c = New Char() {[String]}

                If enc.IsSingleByte Then
                    'SBCS
                    b = New Byte(0) {}
                    iByteCount = enc.GetBytes(c, 0, 1, b, 0)
                    Return b(0)
                End If

                'DBCS char
                b = New Byte(1) {}
                iByteCount = enc.GetBytes(c, 0, 1, b, 0)
                If iByteCount = 1 Then
                    Return b(0)
                End If
                If BitConverter.IsLittleEndian Then
                    'Swap the bytes since storage is big-endian
                    Dim byt As Byte
                    byt = b(0)
                    b(0) = b(1)
                    b(1) = byt
                End If
                Return BitConverter.ToInt16(b, 0)

            Catch ex As Exception
                Throw ex

            End Try

        End Function

        Public Function Asc(ByVal [String] As String) As Integer

            If ([String] Is Nothing) OrElse ([String].Length = 0) Then
                Throw New ArgumentException(SR.Format(SR.Argument_LengthGTZero1, NameOf([String])), NameOf([String]))
            End If

            Dim ch As Char = [String].Chars(0)
            Return Asc(ch)

        End Function

        Public Function AscW([String] As String) As Integer
            If ([String] Is Nothing) OrElse ([String].Length = 0) Then
                Throw New Global.System.ArgumentException(SR.Format(SR.Argument_LengthGTZero1, NameOf([String])), NameOf([String]))
            End If
            Return AscW([String].Chars(0))
        End Function

        Public Function AscW([String] As Char) As Integer
            Return AscW([String])
        End Function

        Public Function Chr(ByVal CharCode As Integer) As Char
            ' Documentation claims that < 0 or > 255 gives an ArgumentException
            If CharCode < -32768 OrElse CharCode > 65535 Then
                Throw New ArgumentException(SR.Format(SR.Argument_RangeTwoBytes1, NameOf(CharCode)), NameOf(CharCode))
            End If

            If CharCode >= 0 AndAlso CharCode <= 127 Then
                Return Convert.ToChar(CharCode)
            End If

            Try
                Dim enc As Encoding

                enc = GetAscChrEncoding()

                If enc.IsSingleByte Then
                    If CharCode < 0 OrElse CharCode > 255 Then
                        Throw VbMakeException(vbErrors.IllegalFuncCall)
                    End If
                End If

                Dim dec As Decoder
                Dim CharCount As Integer
                Dim c(1) As Char 'Use 2 char array, but only return first Char if two returned
                Dim b(1) As Byte

                dec = enc.GetDecoder()
                If CharCode >= 0 AndAlso CharCode <= 255 Then
                    b(0) = CByte(CharCode And &HFFS)
                    CharCount = dec.GetChars(b, 0, 1, c, 0)

                Else
                    'Bytes must be swapped in memory to HI/LO
                    b(0) = CByte((CharCode And &HFF00I) >> 8)
                    b(1) = CByte(CharCode And &HFFI)
                    CharCount = dec.GetChars(b, 0, 2, c, 0)

                End If

                'VB6 ignored the lobyte if it hibyte was not a valid lead character
                'CharCount will be zero if the hibyte was not a lead character

                Return c(0)

            Catch ex As Exception
                Throw ex
            End Try
        End Function

        Public Function ChrW(CharCode As Integer) As Char
            If CharCode < -32768 OrElse CharCode > 65535 Then
                Throw New ArgumentException(SR.Format(SR.Argument_RangeTwoBytes1, NameOf(CharCode)), NameOf(CharCode))
            End If
            Return Global.System.Convert.ToChar(CharCode And &HFFFFI)
        End Function

        '============================================================================
        ' String manipulation functions.
        '============================================================================
        Public Function Filter(ByVal Source() As Object, ByVal Match As String, Optional ByVal Include As Boolean = True, <Microsoft.VisualBasic.CompilerServices.OptionCompareAttribute()> Optional ByVal [Compare] As CompareMethod = CompareMethod.Binary) As String()

            Dim Size As Integer = UBound(Source)
            Dim StringSource(Size) As String

            Try
                For i As Integer = 0 To Size
                    StringSource(i) = CStr(Source(i))
                Next i
            Catch ex As StackOverflowException
                Throw ex
            Catch ex As OutOfMemoryException
                Throw ex
            Catch
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValueType2, NameOf(Source), "String"), NameOf(Source))
            End Try

            Return Filter(StringSource, Match, Include, [Compare])
        End Function

        Public Function Filter(ByVal Source() As String, ByVal Match As String, Optional ByVal Include As Boolean = True, <Microsoft.VisualBasic.CompilerServices.OptionCompareAttribute()> Optional ByVal [Compare] As CompareMethod = CompareMethod.Binary) As String()
            Try
                Dim TmpResult() As String
                Dim lNumElements As Integer
                Dim lSourceIndex As Integer
                Dim lResultIndex As Integer
                Dim sStringElement As String
                Dim iFlags As CompareOptions
                Dim CompInfo As CompareInfo
                Dim Loc As CultureInfo

                'Do error checking
                If Source.Rank <> 1 Then
                    Throw New ArgumentException(SR.Argument_RankEQOne1, NameOf(Source))
                End If

                If Match Is Nothing OrElse Match.Length = 0 Then
                    Return Nothing
                End If

                lNumElements = Source.Length

                'up the globalization info
                Loc = GetCultureInfo()
                CompInfo = Loc.CompareInfo

                If [Compare] = CompareMethod.Text Then
                    iFlags = CompareOptions.IgnoreCase
                End If

                'Compare each element and build the result array
                ReDim TmpResult(lNumElements - 1)

                For lSourceIndex = 0 To lNumElements - 1
                    sStringElement = Source(lSourceIndex)

                    If (sStringElement Is Nothing) Then
                        'Skip
                    ElseIf (CompInfo.IndexOf(sStringElement, Match, iFlags) >= 0) = Include Then
                        TmpResult(lResultIndex) = sStringElement
                        lResultIndex = lResultIndex + 1
                    End If
                Next lSourceIndex

                If lResultIndex = 0 Then
                    ReDim TmpResult(-1)
                    Return TmpResult
                End If

                If lResultIndex = TmpResult.Length Then
                    'No redim required
                    Return TmpResult
                End If

                ReDim Preserve TmpResult(lResultIndex - 1)
                Return TmpResult
            Catch ex As Exception
                Throw ex
            End Try
        End Function

        Public Function InStr(ByVal String1 As String, ByVal String2 As String, <Microsoft.VisualBasic.CompilerServices.OptionCompareAttribute()> Optional ByVal [Compare] As CompareMethod = CompareMethod.Binary) As Integer
            If Compare = CompareMethod.Binary Then
                Return (InternalInStrBinary(0, String1, String2) + 1)
            Else
                Return (InternalInStrText(0, String1, String2) + 1)
            End If
        End Function

        Public Function InStr(ByVal Start As Integer, ByVal String1 As String, ByVal String2 As String, <Microsoft.VisualBasic.CompilerServices.OptionCompareAttribute()> Optional ByVal [Compare] As CompareMethod = CompareMethod.Binary) As Integer
            If Start < 1 Then
                Throw New ArgumentException(SR.Format(SR.Argument_GTZero1, NameOf(Start)), NameOf(Start))
            End If

            If Compare = CompareMethod.Binary Then
                Return (InternalInStrBinary(Start - 1, String1, String2) + 1)
            Else
                Return (InternalInStrText(Start - 1, String1, String2) + 1)
            End If
        End Function

        'THIS FUNCTION IS ZERO BASED
        Private Function InternalInStrBinary(ByVal StartPos As Integer, ByVal sSrc As String, ByVal sFind As String) As Integer
            Dim SrcLength As Integer

            If sSrc IsNot Nothing Then
                SrcLength = sSrc.Length
            Else
                SrcLength = 0
            End If

            If StartPos > SrcLength OrElse SrcLength = 0 Then
                Return -1
            End If

            If (sFind Is Nothing) OrElse (sFind.Length = 0) Then
                Return StartPos
            End If

            Return m_InvariantCompareInfo.IndexOf(sSrc, sFind, StartPos, CompareOptions.Ordinal)
        End Function

        Private Function InternalInStrText(ByVal lStartPos As Integer, ByVal sSrc As String, ByVal sFind As String) As Integer
            Dim lSrcLen As Integer

            If Not sSrc Is Nothing Then
                lSrcLen = sSrc.Length
            Else
                lSrcLen = 0
            End If

            If lStartPos > lSrcLen OrElse lSrcLen = 0 Then
                Return -1
            End If

            If (sFind Is Nothing) OrElse (sFind.Length = 0) Then
                Return lStartPos
            End If

            Return GetCultureInfo().CompareInfo.IndexOf(sSrc, sFind, lStartPos, STANDARD_COMPARE_FLAGS)
        End Function

        Public Function InStrRev(ByVal StringCheck As String, ByVal StringMatch As String, Optional ByVal Start As Integer = -1, <Microsoft.VisualBasic.CompilerServices.OptionCompareAttribute()> Optional ByVal [Compare] As CompareMethod = CompareMethod.Binary) As Integer
            Try
                Dim lStrLen As Integer

                If Start = 0 OrElse Start < -1 Then
                    Throw New ArgumentException(SR.Format(SR.Argument_MinusOneOrGTZero1, NameOf(Start)), NameOf(Start))
                End If

                If StringCheck Is Nothing Then
                    lStrLen = 0
                Else
                    lStrLen = StringCheck.Length
                End If

                If Start = -1 Then
                    Start = lStrLen
                End If

                If (Start > lStrLen) OrElse (lStrLen = 0) Then
                    Return 0
                End If

                If StringMatch Is Nothing Then
                    GoTo EmptyMatchString
                End If

                If StringMatch.Length = 0 Then
EmptyMatchString:
                    Return Start
                End If

                If [Compare] = CompareMethod.Binary Then
                    Return (m_InvariantCompareInfo.LastIndexOf(StringCheck, StringMatch, Start - 1, Start, CompareOptions.Ordinal) + 1)
                Else
                    Return (GetCultureInfo().CompareInfo.LastIndexOf(StringCheck, StringMatch, Start - 1, Start, STANDARD_COMPARE_FLAGS) + 1)
                End If
            Catch ex As Exception
                Throw ex
            End Try
        End Function

        Public Function Join(ByVal SourceArray() As Object, Optional ByVal Delimiter As String = " ") As String
            Dim Size As Integer = UBound(SourceArray)
            Dim StringSource(Size) As String
            Dim i As Integer

            Try
                For i = 0 To Size
                    StringSource(i) = CStr(SourceArray(i))
                Next i
            Catch ex As StackOverflowException
                Throw ex
            Catch ex As OutOfMemoryException
                Throw ex
            Catch
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValueType2, "SourceArray", "String"))
            End Try

            Return Join(StringSource, Delimiter)
        End Function

        Public Function Join(ByVal SourceArray() As String, Optional ByVal Delimiter As String = " ") As String
            Try
                If IsArrayEmpty(SourceArray) Then
                    'EmptyArray returns empty string
                    Return Nothing
                End If

                If SourceArray.Rank <> 1 Then
                    Throw New ArgumentException(SR.Format(SR.Argument_RankEQOne1))
                End If

                Return System.String.Join(Delimiter, SourceArray)
            Catch ex As Exception
                Throw ex
            End Try
        End Function

        Public Function LCase(ByVal Value As String) As String
            Try
                If Value Is Nothing Then
                    Return Nothing
                Else
                    Return Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToLower(Value)
                End If
            Catch ex As Exception
                Throw ex
            End Try
        End Function

        Public Function LCase(ByVal Value As Char) As Char
            Try
                Return Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToLower(Value)
            Catch ex As Exception
                Throw ex
            End Try
        End Function

        Public Function Len(ByVal Expression As Boolean) As Integer
            Return 2
        End Function

        <CLSCompliant(False)>
        Public Function Len(ByVal Expression As SByte) As Integer
            Return 1
        End Function

        Public Function Len(ByVal Expression As Byte) As Integer
            Return 1
        End Function

        Public Function Len(ByVal Expression As Int16) As Integer
            Return 2
        End Function

        <CLSCompliant(False)>
        Public Function Len(ByVal Expression As UInt16) As Integer
            Return 2
        End Function

        Public Function Len(ByVal Expression As Int32) As Integer
            Return 4
        End Function

        <CLSCompliant(False)>
        Public Function Len(ByVal Expression As UInt32) As Integer
            Return 4
        End Function

        Public Function Len(ByVal Expression As Int64) As Integer
            Return 8
        End Function

        <CLSCompliant(False)>
        Public Function Len(ByVal Expression As UInt64) As Integer
            Return 8
        End Function

        Public Function Len(ByVal Expression As Decimal) As Integer
            'This must return the length for VB6 Currency
            Return 8
        End Function

        Public Function Len(ByVal Expression As Single) As Integer
            Return 4
        End Function

        Public Function Len(ByVal Expression As Double) As Integer
            Return 8
        End Function

        Public Function Len(ByVal Expression As DateTime) As Integer
            Return 8
        End Function

        Public Function Len(ByVal Expression As Char) As Integer
            Return 2
        End Function

        Public Function Len(ByVal Expression As String) As Integer
            If Expression Is Nothing Then
                Return 0
            End If

            Return Expression.Length
        End Function

        <RequiresUnreferencedCode("The object's type cannot be statically analyzed and its members may be trimmed")>
        Public Function Len(ByVal Expression As Object) As Integer
            If Expression Is Nothing Then
                Return 0
            End If

            Dim ValueInterface As IConvertible = TryCast(Expression, IConvertible)
            If Not ValueInterface Is Nothing Then
                Select Case ValueInterface.GetTypeCode()
                    Case TypeCode.Boolean
                        Return 2
                    Case TypeCode.SByte
                        Return 1
                    Case TypeCode.Byte
                        Return 1
                    Case TypeCode.Int16
                        Return 2
                    Case TypeCode.UInt16
                        Return 2
                    Case TypeCode.Int32
                        Return 4
                    Case TypeCode.UInt32
                        Return 4
                    Case TypeCode.Int64
                        Return 8
                    Case TypeCode.UInt64
                        Return 8
                    Case TypeCode.Decimal
                        Return 16
                    Case TypeCode.Single
                        Return 4
                    Case TypeCode.Double
                        Return 8
                    Case TypeCode.DateTime
                        Return 8
                    Case TypeCode.Char
                        Return 2
                    Case TypeCode.String
                        Return Expression.ToString().Length
                    Case TypeCode.Object
                        'Fallthrough to below
                End Select

            Else
                Dim CharArray As Char() = TryCast(Expression, Char())

                If CharArray IsNot Nothing Then
                    Return CharArray.Length
                End If
            End If

            If TypeOf Expression Is ValueType Then
                Dim Length As Integer = StructUtils.GetRecordLength(Expression, 1)
                Return Length
            End If

            Throw VbMakeException(vbErrors.TypeMismatch)
        End Function

        Public Function Replace(ByVal Expression As String, ByVal Find As String, ByVal Replacement As String, Optional ByVal Start As Integer = 1, Optional ByVal Count As Integer = -1, <Microsoft.VisualBasic.CompilerServices.OptionCompareAttribute()> Optional ByVal [Compare] As CompareMethod = CompareMethod.Binary) As String
            Try
                'Validate Parameters
                If Count < -1 Then
                    Throw New ArgumentException(SR.Format(SR.Argument_GEMinusOne1, "Count"))
                End If

                If Start <= 0 Then
                    Throw New ArgumentException(SR.Format(SR.Argument_GTZero1, "Start"))
                End If

                If (Expression Is Nothing) OrElse (Start > Expression.Length) Then
                    Return Nothing
                End If

                If Start <> 1 Then
                    Expression = Expression.Substring(Start - 1)
                End If

                If Find Is Nothing Then
                    GoTo EmptyFindString
                End If

                If Find.Length = 0 OrElse Count = 0 Then
EmptyFindString:
                    Return Expression
                End If

                If Count = -1 Then
                    Count = Expression.Length
                End If

                Return ReplaceInternal(Expression, Find, Replacement, Count, [Compare])

            Catch ex As Exception
                Throw ex
            End Try
        End Function

        Private Function ReplaceInternal(ByVal Expression As String, ByVal Find As String, ByVal Replacement As String, ByVal Count As Integer, ByVal [Compare] As CompareMethod) As String

            System.Diagnostics.Debug.Assert(Expression <> "", "Expression is empty")
            System.Diagnostics.Debug.Assert(Find <> "", "Find is empty")
            System.Diagnostics.Debug.Assert(Count > 0, "Number of replacements is 0 or less")
            System.Diagnostics.Debug.Assert([Compare] = CompareMethod.Text Or [Compare] = CompareMethod.Binary, "Unknown compare.")

            Dim ExpressionLength As Integer = Expression.Length
            Dim FindLength As Integer = Find.Length

            Dim Start As Integer
            Dim FindLocation As Integer
            Dim Replacements As Integer

            Dim Comparer As CompareInfo
            Dim CompareFlags As CompareOptions

            Dim Builder As StringBuilder = New StringBuilder(ExpressionLength)

            If [Compare] = CompareMethod.Text Then
                Comparer = GetCultureInfo().CompareInfo
                CompareFlags = STANDARD_COMPARE_FLAGS
            Else
                Comparer = m_InvariantCompareInfo
                CompareFlags = CompareOptions.Ordinal
            End If

            'We build the new string (with the replacements) by walking through Expression, searching for the
            'Find, and appending sections of Expression or Replacement as we go. For example, if 
            'Expression = "This is a test.", Find = "is" and Replacement = "YYY" then we would append
            '"Th" to the new string, then "YYY" then " " then "YYY" and finally " a test."
            While Start < ExpressionLength
                If Replacements = Count Then
                    'We've made all the replacements the caller wanted so append the remaining string
                    Builder.Append(Expression.Substring(Start))
                    Exit While
                End If

                FindLocation = Comparer.IndexOf(Expression, Find, Start, CompareFlags)
                If FindLocation < 0 Then
                    'We didn't find the Find string append the rest of the string
                    Builder.Append(Expression.Substring(Start))
                    Exit While
                Else
                    'Append to our string builder everything up to the found string, then
                    'append the replacement
                    Builder.Append(Expression.Substring(Start, FindLocation - Start))
                    Builder.Append(Replacement)
                    Replacements += 1

                    'Move the start of our search past the string we just replaced
                    Start = FindLocation + FindLength
                End If
            End While

            Return Builder.ToString()

        End Function

        Public Function Space(ByVal Number As Integer) As String

            If Number >= 0 Then
                Return New String(ChrW(32), Number)
            End If

            Throw New ArgumentException(SR.Format(SR.Argument_GEZero1, "Number"))

        End Function

        Public Function Split(ByVal Expression As String, Optional ByVal Delimiter As String = " ", Optional ByVal Limit As Integer = -1, <Microsoft.VisualBasic.CompilerServices.OptionCompareAttribute()> Optional ByVal [Compare] As CompareMethod = CompareMethod.Binary) As String()
            Try
                'Use String.Split
                Dim aList() As String
                Dim iDelLen As Integer

                If Expression Is Nothing Then
                    GoTo EmptyExpression
                End If

                If Expression.Length = 0 Then
EmptyExpression:
                    ReDim aList(0)
                    aList(0) = ""
                    Return aList
                End If

                If Limit = -1 Then
                    Limit = Expression.Length + 1
                End If

                If Delimiter Is Nothing Then
                    iDelLen = 0
                Else
                    iDelLen = Delimiter.Length
                End If

                If iDelLen = 0 Then
EmptyDelimiterString:
                    ReDim aList(0)
                    aList(0) = Expression
                    Return aList
                End If

                'Not handled: LIGATURE expansion
                Return SplitHelper(Expression, Delimiter, Limit, [Compare])
            Catch ex As Exception
                Throw ex
            End Try
        End Function

        Private Function SplitHelper(ByVal sSrc As String, ByVal sFind As String, ByVal cMaxSubStrings As Integer, ByVal [Compare] As Integer) As String()
            Dim cSubStrings As Integer
            Dim iIndex As Integer
            Dim iFindLen As Integer
            Dim iSrcLen As Integer
            Dim asSubstrings() As String
            Dim sSubString As String
            Dim iLastIndex As Integer
            Dim cDelimPosMax As Integer
            Dim cmpInfo As CompareInfo
            Dim flags As CompareOptions

            If sFind Is Nothing Then
                iFindLen = 0
            Else
                iFindLen = sFind.Length
            End If

            If sSrc Is Nothing Then
                iSrcLen = 0
            Else
                iSrcLen = sSrc.Length
            End If

            If iFindLen = 0 Then
                ReDim asSubstrings(0)
                asSubstrings(0) = sSrc
                Return asSubstrings
            End If

            If iSrcLen = 0 Then
                ReDim asSubstrings(0)
                asSubstrings(0) = sSrc
                Return asSubstrings
            End If

            cDelimPosMax = 20

            If cDelimPosMax > cMaxSubStrings Then
                cDelimPosMax = cMaxSubStrings
            End If

            ReDim asSubstrings(cDelimPosMax)

            If [Compare] = CompareMethod.Binary Then
                flags = CompareOptions.Ordinal
                cmpInfo = m_InvariantCompareInfo
            Else
                cmpInfo = GetCultureInfo().CompareInfo
                flags = STANDARD_COMPARE_FLAGS
            End If

            Do While (iLastIndex < iSrcLen)
                iIndex = cmpInfo.IndexOf(sSrc, sFind, iLastIndex, iSrcLen - iLastIndex, flags)

                If (iIndex = -1) OrElse (cSubStrings + 1 = cMaxSubStrings) Then
                    'Just put the remainder of the string in the next element
                    sSubString = sSrc.Substring(iLastIndex)
                    If sSubString Is Nothing Then
                        sSubString = ""
                    End If
                    asSubstrings(cSubStrings) = sSubString
                    Exit Do
                Else
                    'Put the characters between iLastIndex and iIndex into the next element
                    sSubString = sSrc.Substring(iLastIndex, iIndex - iLastIndex)
                    If sSubString Is Nothing Then
                        sSubString = ""
                    End If
                    asSubstrings(cSubStrings) = sSubString
                    iLastIndex = iIndex + iFindLen
                End If

                cSubStrings += 1

                If (cSubStrings > cDelimPosMax) Then
                    cDelimPosMax += 20
                    If cDelimPosMax > cMaxSubStrings Then
                        cDelimPosMax = cMaxSubStrings + 1
                    End If
                    ReDim Preserve asSubstrings(cDelimPosMax)
                End If

                'Must Initialize to empty string, otherwise it looks like an object
                asSubstrings(cSubStrings) = ""

                If cSubStrings = cMaxSubStrings Then
                    sSubString = sSrc.Substring(iLastIndex)
                    If sSubString Is Nothing Then
                        sSubString = ""
                    End If
                    asSubstrings(cSubStrings) = sSubString
                    Exit Do
                End If
            Loop

RedimAndExit:
            If cSubStrings + 1 = asSubstrings.Length Then
                Return asSubstrings
            End If

            ReDim Preserve asSubstrings(cSubStrings)
            Return asSubstrings
        End Function

        '============================================================================
        ' Fixed-length string functions.
        '============================================================================

        Public Function LSet(ByVal Source As String, ByVal Length As Integer) As String
            If (Length = 0) Then
                Return ""
            ElseIf (Source Is Nothing) Then
                Return New String(" "c, Length)
            End If

            If Length > Source.Length Then
                Return Source.PadRight(Length)
            Else
                Return Source.Substring(0, Length)
            End If
        End Function

        Public Function RSet(ByVal Source As String, ByVal Length As Integer) As String
            If (Length = 0) Then
                Return ""
            ElseIf Source Is Nothing Then
                Return New String(" "c, Length)
            End If

            If Length > Source.Length Then
                Return Source.PadLeft(Length)
            Else
                Return Source.Substring(0, Length)
            End If
        End Function

        Public Function StrDup(ByVal Number As Integer, ByVal Character As Object) As Object
            Dim s As String
            Dim SingleChar As Char

            If Number < 0 Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Number"))
            End If

            If Character Is Nothing Then
                Throw New ArgumentNullException(SR.Format(SR.Argument_InvalidNullValue1, "Character"))
            End If

            s = TryCast(Character, String)

            If s IsNot Nothing Then
                If s.Length = 0 Then
                    Throw New ArgumentException(SR.Format(SR.Argument_LengthGTZero1, "Character"))
                End If
                SingleChar = s.Chars(0)
            Else
                Try
                    SingleChar = CChar(Character)
                Catch ex As StackOverflowException
                    Throw ex
                Catch ex As OutOfMemoryException
                    Throw ex
                Catch
                    Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Character"))
                End Try
            End If

            Return New String(SingleChar, Number)
        End Function

        Public Function StrDup(ByVal Number As Integer, ByVal Character As Char) As String
            If Number < 0 Then
                Throw New ArgumentException(SR.Format(SR.Argument_GEZero1, "Number"))
            End If

            Return New String(Character, Number)
        End Function

        Public Function StrDup(ByVal Number As Integer, ByVal Character As String) As String
            If Number < 0 Then
                Throw New ArgumentException(SR.Format(SR.Argument_GEZero1, "Number"))
            End If

            If Character Is Nothing OrElse Character.Length = 0 Then
                Throw New ArgumentException(SR.Format(SR.Argument_LengthGTZero1, "Character"))
            End If

            Return New String(Character.Chars(0), Number)
        End Function

        Public Function StrReverse(ByVal Expression As String) As String

            If (Expression Is Nothing) Then
                Return ""
            ElseIf Expression.Length <= 1 Then
                Return Expression
            End If

            'Use TextElementEnumerator to iterate through the grapheme clusters, then
            'add them to the destination array in reverse order. A grapheme cluster
            'is a text element that displays as a single character; it might consist
            'of multiple chars.
            Dim TextEnum As TextElementEnumerator = StringInfo.GetTextElementEnumerator(Expression)
            Dim Output(Expression.Length - 1) As Char
            Dim LastSrcIndex As Integer

            'Initialize the enumerator
            TextEnum.MoveNext()
            LastSrcIndex = 0

            'Iterate through the enumerator, performing a forward-copy of the source
            'expression to the end of the StringBuilder.
            ' Example: input = [ (ABC) (D) (EFGH) (IJ) (K) ]
            '         output = [ (K) (IJ) (EFGH) (D) (ABC) ]
            Do While TextEnum.MoveNext()
                Expression.CopyTo(LastSrcIndex, Output, Output.Length - TextEnum.ElementIndex, TextEnum.ElementIndex - LastSrcIndex)
                LastSrcIndex = TextEnum.ElementIndex
            Loop

            'The above loop won't hit the last element - we need to copy the remaining data manually
            Expression.CopyTo(LastSrcIndex, Output, 0, Expression.Length - LastSrcIndex)

            Return New String(Output)

        End Function

        Public Function UCase(ByVal [Value] As String) As String
            Try
                If Value Is Nothing Then
                    Return ""
                Else
                    Return Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToUpper(Value)
                End If
            Catch ex As Exception
                Throw ex
            End Try
        End Function

        Public Function UCase(ByVal Value As Char) As Char
            Try
                Return Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToUpper(Value)
            Catch ex As Exception
                Throw ex
            End Try
        End Function

        '*************************************************************
        '** PERF NOTE:
        '** All Format calls must go through FormatNamed
        '** But we don't want to put a bunch of overhead on the more
        '** common cases that are not named formats
        '** The expensive CompareInfo.Compare calls have been limited
        '** just one call 
        '**************************************************************

        Private Function FormatNamed(ByVal Expression As Object, ByVal Style As String, ByRef ReturnValue As String) As Boolean
            Dim StyleLength As Integer = Style.Length

            ReturnValue = Nothing

            Select Case StyleLength

                Case 5
                    Select Case Style.Chars(0)
                        '(F)ixed
                        Case "f"c, "F"c
                            If String.Compare(Style, NAMEDFORMAT_FIXED, StringComparison.OrdinalIgnoreCase) = 0 Then
                                ReturnValue = CDbl(Expression).ToString("0.00", Nothing)
                                Return True
                            End If
                    End Select

                Case 6
                    'switch off 1st char (index 0) to reduce number of string compares
                    '(Y)es/no
                    '(O)n/off
                    Select Case Style.Chars(0)
                        Case "y"c, "Y"c
                            If String.Compare(Style, NAMEDFORMAT_YES_NO, StringComparison.OrdinalIgnoreCase) = 0 Then
                                ReturnValue = CInt(CBool(Expression)).ToString(CachedYesNoFormatStyle, Nothing)
                                Return True
                            End If

                        Case "o"c, "O"c
                            If String.Compare(Style, NAMEDFORMAT_ON_OFF, StringComparison.OrdinalIgnoreCase) = 0 Then
                                ReturnValue = CInt(CBool(Expression)).ToString(CachedOnOffFormatStyle, Nothing)
                                Return True
                            End If
                    End Select

                Case 7
                    'switch off 1st char (index 0) to reduce number of string compares
                    '(P)ercent
                    Select Case Style.Chars(0)
                        Case "p"c, "P"c
                            If String.Compare(Style, NAMEDFORMAT_PERCENT, StringComparison.OrdinalIgnoreCase) = 0 Then
                                ReturnValue = CDbl(Expression).ToString("0.00%", Nothing)
                                Return True
                            End If
                    End Select

                Case 8
                    'switch off 6th char (index 5) to reduce number of string compares
                    '(S)tandard
                    '(C)urrency

                    Select Case Style.Chars(0)
                        Case "s"c, "S"c
                            If String.Compare(Style, NAMEDFORMAT_STANDARD, StringComparison.OrdinalIgnoreCase) = 0 Then
                                ReturnValue = CDbl(Expression).ToString("N2", Nothing)
                                Return True
                            End If
                        Case "c"c, "C"c
                            If String.Compare(Style, NAMEDFORMAT_CURRENCY, StringComparison.OrdinalIgnoreCase) = 0 Then
                                ReturnValue = CDbl(Expression).ToString("C", Nothing)
                                Return True
                            End If
                    End Select

                Case 9
                    'switch off 6th char (index 5) to reduce number of string compares
                    'Long (T)ime
                    'Long (D)ate

                    Select Case Style.Chars(5)
                        Case "t"c, "T"c
                            If String.Compare(Style, NAMEDFORMAT_LONG_TIME, StringComparison.OrdinalIgnoreCase) = 0 Then
                                ReturnValue = CDate(Expression).ToString("T", Nothing)
                                Return True
                            End If

                        Case "d"c, "D"c
                            If String.Compare(Style, NAMEDFORMAT_LONG_DATE, StringComparison.OrdinalIgnoreCase) = 0 Then
                                ReturnValue = CDate(Expression).ToString("D", Nothing)
                                Return True
                            End If
                    End Select

                Case 10
                    'switch off 7th char (index 6) to reduce number of string compares
                    'true/f(A)lse
                    'short (T)ime
                    'short (D)ate
                    'scient(I)fic

                    Select Case Style.Chars(6)
                        Case "a"c, "A"c
                            If String.Compare(Style, NAMEDFORMAT_TRUE_FALSE, StringComparison.OrdinalIgnoreCase) = 0 Then
                                ReturnValue = CInt(CBool(Expression)).ToString(CachedTrueFalseFormatStyle, Nothing)
                                Return True
                            End If

                        Case "t"c, "T"c
                            If String.Compare(Style, NAMEDFORMAT_SHORT_TIME, StringComparison.OrdinalIgnoreCase) = 0 Then
                                ReturnValue = CDate(Expression).ToString("t", Nothing)
                                Return True
                            End If

                        Case "d"c, "D"c
                            If String.Compare(Style, NAMEDFORMAT_SHORT_DATE, StringComparison.OrdinalIgnoreCase) = 0 Then
                                ReturnValue = CDate(Expression).ToString("d", Nothing)
                                Return True
                            End If

                        Case "i"c, "I"c
                            If String.Compare(Style, NAMEDFORMAT_SCIENTIFIC, StringComparison.OrdinalIgnoreCase) = 0 Then
                                Dim dbl As Double
                                dbl = CDbl(Expression)
                                If System.Double.IsNaN(dbl) OrElse System.Double.IsInfinity(dbl) Then
                                    ReturnValue = dbl.ToString("G", Nothing)
                                Else
                                    ReturnValue = dbl.ToString("0.00E+00", Nothing)
                                End If
                                Return True
                            End If

                    End Select

                Case 11
                    'switch off 8th char (index 7) to reduce number of string compares
                    'medium (T)ime
                    'medium (D)ate

                    Select Case Style.Chars(7)
                        Case "t"c, "T"c
                            If String.Compare(Style, NAMEDFORMAT_MEDIUM_TIME, StringComparison.OrdinalIgnoreCase) = 0 Then
                                ReturnValue = CDate(Expression).ToString("T", Nothing)
                                Return True
                            End If

                        Case "d"c, "D"c
                            If String.Compare(Style, NAMEDFORMAT_MEDIUM_DATE, StringComparison.OrdinalIgnoreCase) = 0 Then
                                ReturnValue = CDate(Expression).ToString("D", Nothing)
                                Return True
                            End If
                    End Select

                Case 12
                    Select Case Style.Chars(0)
                        Case "g"c, "G"c
                            If String.Compare(Style, NAMEDFORMAT_GENERAL_DATE, StringComparison.OrdinalIgnoreCase) = 0 Then
                                ReturnValue = CDate(Expression).ToString("G", Nothing)
                                Return True
                            End If
                    End Select

                Case 14
                    Select Case Style.Chars(0)
                        Case "g"c, "G"c
                            If String.Compare(Style, NAMEDFORMAT_GENERAL_NUMBER, StringComparison.OrdinalIgnoreCase) = 0 Then
                                ReturnValue = CDbl(Expression).ToString("G", Nothing)
                                Return True
                            End If
                    End Select

            End Select

            Return False

        End Function

        '============================================================================
        ' Format functions.
        '============================================================================
        Public Function Format(ByVal Expression As Object, Optional ByVal Style As String = "") As String
            Try
                Dim cp As IFormatProvider = Nothing 'GetCultureInfo()
                Dim tc As TypeCode
                Dim iformat As IFormattable = Nothing

                If (Expression Is Nothing) OrElse (Expression.GetType() Is Nothing) Then
                    Return ""
                End If

                If Style Is Nothing OrElse Style.Length = 0 Then
                    Return CStr(Expression)
                End If

                Dim ConvertibleExpression As IConvertible = CType(Expression, IConvertible)
                tc = ConvertibleExpression.GetTypeCode()

                If Style.Length > 0 Then
                    Try
                        Dim ReturnValue As String = Nothing

                        If FormatNamed(Expression, Style, ReturnValue) Then
                            Return ReturnValue
                        End If
                    Catch ex As StackOverflowException
                        Throw ex
                    Catch ex As OutOfMemoryException
                        Throw ex
                    Catch
                        'Object could not be converted to required type
                        'so just return the string
                        Return CStr(Expression)
                    End Try
                End If

                iformat = TryCast(Expression, IFormattable)

                If iformat Is Nothing Then
                    tc = System.Convert.GetTypeCode(Expression)
                    If tc <> TypeCode.String AndAlso tc <> TypeCode.Boolean Then
                        Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Expression"))
                    End If
                End If

                Select Case tc
                    Case TypeCode.Boolean
                        Return System.String.Format(cp, Style, CStr(ConvertibleExpression.ToBoolean(Nothing)))
                    Case TypeCode.SByte,
                         TypeCode.Byte,
                         TypeCode.Int16,
                         TypeCode.UInt16,
                         TypeCode.Int32,
                         TypeCode.UInt32,
                         TypeCode.Int64,
                         TypeCode.UInt64,
                         TypeCode.Decimal,
                         TypeCode.DateTime,
                         TypeCode.Char,
                         TypeCode.Object
                        Return iformat.ToString(Style, cp)
                    Case TypeCode.DBNull
                        Return ""
                    Case TypeCode.Double
                        Dim dbl As Double

                        dbl = ConvertibleExpression.ToDouble(Nothing)

                        If Style Is Nothing OrElse Style.Length = 0 Then
                            Return CStr(dbl)
                        End If

                        If dbl = 0 Then
                            'Used to get rid of possible negative zero, 
                            'which will format as -0
                            dbl = 0
                        End If
                        Return dbl.ToString(Style, cp)
                    Case TypeCode.Empty
                        Return ""
                    Case TypeCode.Single
                        Dim sng As Single

                        sng = ConvertibleExpression.ToSingle(Nothing)

                        If Style Is Nothing OrElse Style.Length = 0 Then
                            Return CStr(sng)
                        End If

                        If sng = 0 Then
                            'Used to get rid of possible negative zero
                            sng = 0
                        End If

                        Return sng.ToString(Style, cp)
                    Case TypeCode.String
                        Return System.String.Format(cp, Style, Expression)
                    Case Else
                        Return iformat.ToString(Style, cp)
                End Select
            Catch ex As Exception
                Throw ex
            End Try
        End Function

        Public Function FormatCurrency(ByVal Expression As Object,
            Optional ByVal NumDigitsAfterDecimal As Integer = -1,
            Optional ByVal IncludeLeadingDigit As TriState = TriState.UseDefault,
            Optional ByVal UseParensForNegativeNumbers As TriState = TriState.UseDefault,
            Optional ByVal GroupDigits As TriState = TriState.UseDefault) As String

            Dim ifmt As IFormattable
            Dim typ As Type
            Dim fp As IFormatProvider = Nothing
            Dim dbl As Double

            Try
                ValidateTriState(IncludeLeadingDigit)
                ValidateTriState(UseParensForNegativeNumbers)
                ValidateTriState(GroupDigits)

                If NumDigitsAfterDecimal > 99 Then  'Was 255 in VB6, but System.Globalization.NumberFormatInfo.CurrencyDecimalDigits limits this to 99.
                    Throw New ArgumentException(SR.Format(SR.Argument_Range0to99_1, "NumDigitsAfterDecimal"))
                End If

                If Expression Is Nothing Then
                    Return ""
                End If

                typ = Expression.GetType()

                If typ Is GetType(System.String) Then
                    Expression = CDbl(Expression)
                ElseIf Not Symbols.IsNumericType(typ) Then
                    Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromTo, VBFriendlyName(typ), "Currency"))
                End If

                ifmt = CType(Expression, IFormattable)

                Dim FormatStyle As String
                If IncludeLeadingDigit = TriState.False Then
                    dbl = CDbl(Expression)
                    If dbl >= 1 OrElse dbl <= -1 Then
                        ' If leading digit doesn't matter, this avoids
                        ' going through the overhead of creating a format string
                        IncludeLeadingDigit = TriState.True
                    End If
                End If
                FormatStyle = GetCurrencyFormatString(IncludeLeadingDigit, NumDigitsAfterDecimal, UseParensForNegativeNumbers, GroupDigits, fp)

                Return ifmt.ToString(FormatStyle, fp)

            Catch ex As Exception
                Throw ex
            End Try
        End Function

        Public Function FormatDateTime(ByVal Expression As DateTime, Optional ByVal NamedFormat As DateFormat = DateFormat.GeneralDate) As String
            Dim sFormat As String

            Try
                Select Case NamedFormat
                    Case DateFormat.LongDate
                        sFormat = "D"
                    Case DateFormat.ShortDate
                        sFormat = "d"
                    Case DateFormat.LongTime
                        sFormat = "T"
                    Case DateFormat.ShortTime
                        sFormat = "HH:mm"
                    Case DateFormat.GeneralDate
                        If Expression.TimeOfDay.Ticks = Expression.Ticks Then
                            'Date is 1/1/0001 - don't print date part
                            'Same as LongTime
                            sFormat = "T"
                        ElseIf Expression.TimeOfDay.Ticks = 0 Then
                            '12AM - don't print time part
                            'Same as ShortDate
                            sFormat = "d"
                        Else
                            'Short date + Long Time
                            sFormat = "G"
                        End If
                    Case Else
                        Throw VbMakeException(vbErrors.IllegalFuncCall)
                End Select

                Return Expression.ToString(sFormat, Nothing)
            Catch ex As Exception
                Throw ex
            End Try
        End Function

        Public Function FormatNumber(ByVal Expression As Object, Optional ByVal NumDigitsAfterDecimal As Integer = -1, Optional ByVal IncludeLeadingDigit As TriState = TriState.UseDefault, Optional ByVal UseParensForNegativeNumbers As TriState = TriState.UseDefault, Optional ByVal GroupDigits As TriState = TriState.UseDefault) As String
            Dim ifmt As IFormattable
            Dim typ As Type

            Try
                ValidateTriState(IncludeLeadingDigit)
                ValidateTriState(UseParensForNegativeNumbers)
                ValidateTriState(GroupDigits)

                If Expression Is Nothing Then
                    Return ""
                End If

                typ = Expression.GetType()

                If typ Is GetType(System.String) Then
                    Expression = CDbl(Expression)
                ElseIf typ Is GetType(System.Boolean) Then
                    If CBool(Expression) Then
                        Expression = -1.0
                    Else
                        Expression = 0.0
                    End If
                ElseIf Not Symbols.IsNumericType(typ) Then
                    Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromTo, VBFriendlyName(typ), "Currency"))
                End If

                ifmt = CType(Expression, IFormattable)

                Return ifmt.ToString(GetNumberFormatString(NumDigitsAfterDecimal, IncludeLeadingDigit,
                    UseParensForNegativeNumbers, GroupDigits), Nothing)
            Catch ex As Exception
                Throw ex
            End Try
        End Function

        Friend Function GetFormatString(ByVal NumDigitsAfterDecimal As Integer,
            ByVal IncludeLeadingDigit As TriState, ByVal UseParensForNegativeNumbers As TriState,
            ByVal GroupDigits As TriState, ByVal FormatTypeValue As FormatType) As String

            Dim nfi As NumberFormatInfo
            Dim sb As StringBuilder
            Dim sGroup As String
            Dim sLeadDigit As String
            Dim sDigitsAfterDecimal As String
            Dim ci As CultureInfo

            sb = New StringBuilder(30)

            ci = GetCultureInfo()
            nfi = CType(ci.GetFormat(GetType(System.Globalization.NumberFormatInfo)), NumberFormatInfo)

            If NumDigitsAfterDecimal < -1 Then
                Throw VbMakeException(vbErrors.IllegalFuncCall)
            ElseIf NumDigitsAfterDecimal = -1 Then
                If FormatTypeValue = FormatType.Percent Then
                    'NOTE: We use NumberDecimalDigits, which is set in the 
                    ' control panel for VB6 compatibility
                    ' The urt does not use this setting, but makes a default
                    ' of their own.
                    NumDigitsAfterDecimal = nfi.NumberDecimalDigits
                ElseIf FormatTypeValue = FormatType.Number Then
                    NumDigitsAfterDecimal = nfi.NumberDecimalDigits
                ElseIf FormatTypeValue = FormatType.Currency Then
                    NumDigitsAfterDecimal = nfi.CurrencyDecimalDigits
                End If
            End If

            If GroupDigits = TriState.UseDefault Then
                GroupDigits = TriState.True
                If FormatTypeValue = FormatType.Percent Then
                    If IsArrayEmpty(nfi.PercentGroupSizes) Then
                        GroupDigits = TriState.False
                    End If
                ElseIf FormatTypeValue = FormatType.Number Then
                    If IsArrayEmpty(nfi.NumberGroupSizes) Then
                        GroupDigits = TriState.False
                    End If
                ElseIf FormatTypeValue = FormatType.Currency Then
                    If IsArrayEmpty(nfi.CurrencyGroupSizes) Then
                        GroupDigits = TriState.False
                    End If
                End If
            End If

            If UseParensForNegativeNumbers = TriState.UseDefault Then
                UseParensForNegativeNumbers = TriState.False
                'If FormatTypeValue = FormatType.Percent Then
                '    If nfi.PercentNegativePattern = 0 Then
                '        UseParensForNegativeNumbers = TriState.True
                '    End If
                'Else

                If FormatTypeValue = FormatType.Number Then
                    If nfi.NumberNegativePattern = 0 Then
                        UseParensForNegativeNumbers = TriState.True
                    End If
                ElseIf FormatTypeValue = FormatType.Currency Then
                    If nfi.CurrencyNegativePattern = 0 Then
                        UseParensForNegativeNumbers = TriState.True
                    End If
                End If
            End If

            If GroupDigits = TriState.True Then
                sGroup = "#,##"
            Else
                sGroup = ""
            End If

            If IncludeLeadingDigit <> TriState.False Then
                sLeadDigit = "0"
            Else
                sLeadDigit = "#"
            End If

            If NumDigitsAfterDecimal > 0 Then
                sDigitsAfterDecimal = "." & (New System.String("0"c, NumDigitsAfterDecimal))
            Else
                sDigitsAfterDecimal = ""
            End If

            'Now put together the string
            If FormatTypeValue = FormatType.Currency Then
                sb.Append(nfi.CurrencySymbol)
            End If

            sb.Append(sGroup)
            sb.Append(sLeadDigit)
            sb.Append(sDigitsAfterDecimal)

            If FormatTypeValue = FormatType.Percent Then
                sb.Append(nfi.PercentSymbol)
            End If

            If UseParensForNegativeNumbers = TriState.True Then
                Dim sTmp As String
                sTmp = sb.ToString()
                sb.Append(";(")
                sb.Append(sTmp)
                sb.Append(")"c)
            End If

            Return sb.ToString()
        End Function

        Friend Function GetCurrencyFormatString(
            ByVal IncludeLeadingDigit As TriState,
            ByVal NumDigitsAfterDecimal As Integer,
            ByVal UseParensForNegativeNumbers As TriState,
            ByVal GroupDigits As TriState,
            ByRef formatProvider As IFormatProvider) As String

            Dim nfi As NumberFormatInfo
            Dim ci As CultureInfo
            Dim CurrencyNegativePattern, CurrencyPositivePattern As Integer
            Dim FormatString, NumberFormat As String

            GetCurrencyFormatString = "C"

            ci = GetCultureInfo()
            nfi = CType(ci.GetFormat(GetType(System.Globalization.NumberFormatInfo)), NumberFormatInfo)
            nfi = CType(nfi.Clone(), NumberFormatInfo)

            If GroupDigits = TriState.False Then
                nfi.CurrencyGroupSizes = New Int32() {0}
            End If

            CurrencyPositivePattern = nfi.CurrencyPositivePattern
            CurrencyNegativePattern = nfi.CurrencyNegativePattern

            If UseParensForNegativeNumbers = TriState.UseDefault Then

                Select Case CurrencyNegativePattern
                    Case 0, 4, 14, 15
                        UseParensForNegativeNumbers = TriState.True
                    Case Else
                        UseParensForNegativeNumbers = TriState.False
                End Select

            ElseIf UseParensForNegativeNumbers = TriState.False Then

                Select Case CurrencyNegativePattern
                    Case 0
                        CurrencyNegativePattern = 1
                    Case 4
                        CurrencyNegativePattern = 5
                    Case 14
                        CurrencyNegativePattern = 9
                    Case 15
                        CurrencyNegativePattern = 10
                End Select

            Else

                UseParensForNegativeNumbers = TriState.True

                Select Case CurrencyNegativePattern
                    Case 1, 2, 3    'leading $ w/o space
                        CurrencyNegativePattern = 0
                    Case 5, 6, 7    'trailing $ w/o space
                        CurrencyNegativePattern = 4
                    Case 8, 10, 13  'Trailing $ / leading with space
                        CurrencyNegativePattern = 15
                    Case 9, 11, 12
                        CurrencyNegativePattern = 14
                End Select

            End If

            nfi.CurrencyNegativePattern = CurrencyNegativePattern

            If NumDigitsAfterDecimal = -1 Then
                NumDigitsAfterDecimal = nfi.CurrencyDecimalDigits
            End If
            nfi.CurrencyDecimalDigits = NumDigitsAfterDecimal

            formatProvider = New FormatInfoHolder(nfi)

            If IncludeLeadingDigit = TriState.False Then
                'We need to build our own string in this case, since the NDP does not
                ' make this accessible

                nfi.NumberGroupSizes = nfi.CurrencyGroupSizes

                FormatString = CurrencyPositiveFormatStrings(CurrencyPositivePattern) & ";" &
                    CurrencyNegativeFormatStrings(CurrencyNegativePattern)

                If GroupDigits = TriState.False Then
                    If IncludeLeadingDigit = TriState.False Then
                        NumberFormat = "#"
                    Else
                        NumberFormat = "0"
                    End If
                Else
                    If IncludeLeadingDigit = TriState.False Then
                        NumberFormat = "#,###"
                    Else
                        NumberFormat = "#,##0"
                    End If
                End If

                If NumDigitsAfterDecimal > 0 Then
                    NumberFormat = NumberFormat & "." & New String("0"c, NumDigitsAfterDecimal)
                End If

                If System.String.CompareOrdinal("$", nfi.CurrencySymbol) <> 0 Then
                    'Replace the '$' sign with the locale specific symbol
                    'Note, the currency symbol in the FormatString is surrounded by the literal symbol ', e.g. '$'
                    'We do this to guard against the case where the currency symbol is the literal symbol \  This was causing problems on Japanese
                    'systems because that meant our format string ended up as "\#,###.00" when we wanted "'\'#,###.00"  But because the currency symbol
                    'we are replacing with could concievably be the literal symbol ' as well, we need to make sure we don't end up with an invalid string like "'''#,###.00"
                    'So if the currency symbol is a ' we replace it with '' so that our format string will be balanced like "''''#,###.00" which will result in the format
                    'succeeding.  You won't see the ' as the currency symbol in this case but this was never supported anyway.
                    FormatString = FormatString.Replace("$", nfi.CurrencySymbol.Replace("'", "''"))
                End If

                Return FormatString.Replace("n", NumberFormat)
            End If

        End Function

        Friend Function GetNumberFormatString(
            ByVal NumDigitsAfterDecimal As Integer,
            ByVal IncludeLeadingDigit As TriState,
            ByVal UseParensForNegativeNumbers As TriState,
            ByVal GroupDigits As TriState) As String

            Dim nfi As NumberFormatInfo
            Dim ci As CultureInfo
            Dim NumberNegativePattern As Integer
            Dim FormatString, NumberFormat As String

            ci = GetCultureInfo()
            nfi = CType(ci.GetFormat(GetType(System.Globalization.NumberFormatInfo)), NumberFormatInfo)

            If NumDigitsAfterDecimal = -1 Then
                NumDigitsAfterDecimal = nfi.NumberDecimalDigits
            ElseIf (NumDigitsAfterDecimal > 99) OrElse (NumDigitsAfterDecimal < -1) Then
                Throw New ArgumentException(SR.Format(SR.Argument_Range0to99_1, "NumDigitsAfterDecimal"))
            End If

            If GroupDigits = TriState.UseDefault Then
                If nfi.NumberGroupSizes Is Nothing OrElse nfi.NumberGroupSizes.Length = 0 Then
                    GroupDigits = TriState.False
                Else
                    GroupDigits = TriState.True
                End If
            End If

            NumberNegativePattern = nfi.NumberNegativePattern

            'Value Associated Pattern 
            '0 (n) 
            '1 - n  
            '2 - n  
            '3 n - 
            '4 n - 
            If UseParensForNegativeNumbers = TriState.UseDefault Then
                Select Case NumberNegativePattern
                    Case 0
                        UseParensForNegativeNumbers = TriState.True
                    Case Else
                        UseParensForNegativeNumbers = TriState.False
                End Select
            ElseIf UseParensForNegativeNumbers = TriState.False Then
                If NumberNegativePattern = 0 Then
                    NumberNegativePattern = 1
                End If
            Else
                UseParensForNegativeNumbers = TriState.True

                Select Case NumberNegativePattern
                    Case 1, 2, 3, 4
                        NumberNegativePattern = 0
                End Select
            End If

            If UseParensForNegativeNumbers = TriState.UseDefault Then
                UseParensForNegativeNumbers = TriState.True
            End If

            FormatString = "n;" & NumberNegativeFormatStrings(NumberNegativePattern)
            If System.String.CompareOrdinal("-", nfi.NegativeSign) <> 0 Then
                'Replace the "-" sign with the actual locale-specific symbol (escaped with quotes).
                '  Note: there appears to be no performance benefit in using a StringBuilder over simple concats.
                FormatString = FormatString.Replace("-", """" & nfi.NegativeSign & """")
            End If

            If IncludeLeadingDigit <> TriState.False Then
                NumberFormat = "0"
            Else
                NumberFormat = "#"
            End If

            If GroupDigits = TriState.False OrElse nfi.NumberGroupSizes.Length = 0 Then
                'Just use setting done above '#' or '0'
            Else
                If nfi.NumberGroupSizes.Length = 1 Then
                    NumberFormat = "#," & New String("#"c, nfi.NumberGroupSizes(0)) & NumberFormat
                Else
                    Dim i As Integer

                    NumberFormat = New String("#"c, nfi.NumberGroupSizes(0) - 1) & NumberFormat
                    For i = 1 To nfi.NumberGroupSizes.GetUpperBound(0)
                        NumberFormat = "," & New String("#"c, nfi.NumberGroupSizes(i)) & "," & NumberFormat
                    Next i
                End If
            End If

            If NumDigitsAfterDecimal > 0 Then
                NumberFormat = NumberFormat & "." & New String("0"c, NumDigitsAfterDecimal)
            End If

            Return Replace(FormatString, "n", NumberFormat)
        End Function

        Public Function FormatPercent(ByVal Expression As Object,
            Optional ByVal NumDigitsAfterDecimal As Integer = -1,
            Optional ByVal IncludeLeadingDigit As TriState = TriState.UseDefault,
            Optional ByVal UseParensForNegativeNumbers As TriState = TriState.UseDefault,
            Optional ByVal GroupDigits As TriState = TriState.UseDefault) As String

            Dim ifmt As IFormattable
            Dim typ As Type
            Dim sFormat As String

            ValidateTriState(IncludeLeadingDigit)
            ValidateTriState(UseParensForNegativeNumbers)
            ValidateTriState(GroupDigits)

            If Expression Is Nothing Then
                Return ""
            End If

            typ = Expression.GetType()

            If typ Is GetType(System.String) Then
                Expression = CDbl(Expression)
            ElseIf Not Symbols.IsNumericType(typ) Then
                Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromTo, VBFriendlyName(typ), "numeric"))
            End If

            ifmt = CType(Expression, IFormattable)
            sFormat = GetFormatString(NumDigitsAfterDecimal, IncludeLeadingDigit, UseParensForNegativeNumbers,
                 GroupDigits, FormatType.Percent)
            Return ifmt.ToString(sFormat, Nothing)
        End Function

        '============================================================================
        ' GetChar function (new for VB7)
        '============================================================================
        Public Function GetChar(ByVal [str] As String, ByVal Index As Integer) As Char
            If [str] Is Nothing Then
                Throw New ArgumentException(SR.Format(SR.Argument_LengthGTZero1, "String"))
            ElseIf (Index < 1) Then
                Throw New ArgumentException(SR.Format(SR.Argument_GEOne1, "Index"))
            ElseIf (Index > [str].Length) Then
                Throw New ArgumentException(SR.Format(SR.Argument_IndexLELength2, "Index", "String"))
            Else
                Return [str].Chars(Index - 1)
            End If
        End Function

        '============================================================================
        ' Left/Right/Mid/Trim functions.
        '============================================================================
        Public Function Left(ByVal [str] As String, ByVal Length As Integer) As String
            '-------------------------------------------------------------
            '   lLen < 0 throws InvalidArgument exception
            '   lLen > Len([str]) let lLen = Len([str])
            '   returned computed string
            '-------------------------------------------------------------
            If Length < 0 Then
                Throw New ArgumentException(SR.Format(SR.Argument_GEZero1, NameOf(Length)), NameOf(Length))
            ElseIf Length = 0 OrElse [str] Is Nothing Then
                Return ""
            Else
                If Length >= [str].Length Then
                    Return [str]
                Else
                    Return [str].Substring(0, Length)
                End If
            End If
        End Function

        Public Function LTrim(ByVal str As String) As String
            If str Is Nothing OrElse str.Length = 0 Then
                Return ""
            Else
                Dim ch As Char
                ch = str.Chars(0)

                If ch = chSpace OrElse ch = chIntlSpace Then
                    Return str.TrimStart(m_achIntlSpace)
                End If
                Return str
            End If
        End Function

        Public Function Mid(ByVal [str] As String, ByVal Start As Integer) As String
            Try
                If [str] Is Nothing Then
                    Return Nothing
                Else
                    Return Mid([str], Start, [str].Length)
                End If
            Catch ex As Exception
                Throw ex
            End Try
        End Function

        Public Function Mid(ByVal [str] As String, ByVal Start As Integer, ByVal Length As Integer) As String
            '-------------------------------------------------------------
            '  Notes:
            '   VB6 order of execution
            '   verify Start > 0 ==> vbIllegalFuncCall
            '   verify lLen > 0 ==> vbIllegalFuncCall
            '   return computed string
            '-------------------------------------------------------------
            If Start <= 0 Then
                Throw New ArgumentException(SR.Format(SR.Argument_GTZero1, NameOf(Start)), NameOf(Start))
            ElseIf Length < 0 Then
                Throw New ArgumentException(SR.Format(SR.Argument_GEZero1, NameOf(Length)), NameOf(Length))
            ElseIf Length = 0 OrElse [str] Is Nothing Then
                Return ""
            End If

            Dim lStrLen As Integer

            lStrLen = [str].Length

            If Start > lStrLen Then
                Return ""
            ElseIf (Start + Length) > lStrLen Then
                Return [str].Substring(Start - 1)
            Else
                Return [str].Substring(Start - 1, Length)
            End If
        End Function

        Public Function Right(ByVal [str] As String, ByVal Length As Integer) As String
            If Length < 0 Then
                Throw New ArgumentException(SR.Format(SR.Argument_GEZero1, NameOf(Length)), NameOf(Length))
            End If

            If Length = 0 OrElse [str] Is Nothing Then
                Return ""
            End If

            Dim lStrLen As Integer

            lStrLen = [str].Length

            If Length >= lStrLen Then
                Return [str]
            End If

            Return [str].Substring(lStrLen - Length, Length)
        End Function

        Public Function RTrim(ByVal [str] As String) As String
            Try
                If [str] Is Nothing OrElse str.Length = 0 Then
                    Return ""
                End If

                Dim ch As Char
                ch = str.Chars(str.Length - 1)

                If ch = chSpace OrElse ch = chIntlSpace Then
                    Return [str].TrimEnd(m_achIntlSpace)
                End If
                Return str
            Catch ex As Exception
                Throw ex
            End Try
        End Function

        Public Function Trim(ByVal str As String) As String
            Try
                If str Is Nothing OrElse str.Length = 0 Then
                    Return ""
                End If

                Dim ch As Char = str.Chars(0)
                If ch = chSpace OrElse ch = chIntlSpace Then
                    Return str.Trim(m_achIntlSpace)
                Else
                    ch = str.Chars(str.Length - 1)
                    If ch = chSpace OrElse ch = chIntlSpace Then
                        Return str.Trim(m_achIntlSpace)
                    End If
                End If
                Return str

            Catch ex As Exception
                Throw ex
            End Try
        End Function

        '============================================================================
        ' String comparison/conversion functions.
        '============================================================================
        Public Function StrComp(ByVal String1 As String, ByVal String2 As String, <Microsoft.VisualBasic.CompilerServices.OptionCompareAttribute()> Optional ByVal [Compare] As CompareMethod = CompareMethod.Binary) As Integer
            Try
                If ([Compare] = CompareMethod.Binary) Then
                    Return Operators.CompareString(String1, String2, False)
                ElseIf ([Compare] = CompareMethod.Text) Then
                    Return Operators.CompareString(String1, String2, True)
                Else
                    Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Compare"))
                End If
            Catch ex As Exception
                Throw ex
            End Try
        End Function

        Friend Function IsValidCodePage(ByVal codepage As Integer) As Boolean
            IsValidCodePage = False

            Try
                If Encoding.GetEncoding(codepage) IsNot Nothing Then
                    IsValidCodePage = True
                End If
            Catch ex As StackOverflowException
                Throw ex
            Catch ex As OutOfMemoryException
                Throw ex
            Catch
            End Try
        End Function

        <SupportedOSPlatform("windows")>
        Public Function StrConv(ByVal [str] As String, ByVal Conversion As VbStrConv, Optional ByVal LocaleID As Integer = 0) As String
#If TARGET_WINDOWS Then
            Try
                Const LANG_CHINESE As Integer = &H4I
                Const LANG_JAPANESE As Integer = &H11I
                Const LANG_KOREAN As Integer = &H12I
                Dim dwMapFlags As Integer
                Dim loc As CultureInfo
                Dim langid As Integer

                If (LocaleID = 0 OrElse LocaleID = 1) Then
                    loc = GetCultureInfo()
                    LocaleID = loc.LCID()
                Else
                    Try
                        loc = New CultureInfo(LocaleID And &HFFFFI)
                    Catch ex As StackOverflowException
                        Throw ex
                    Catch ex As OutOfMemoryException
                        Throw ex
                    Catch
                        Throw New ArgumentException(SR.Format(SR.Argument_LCIDNotSupported1, CStr(LocaleID)))
                    End Try
                End If

                langid = PRIMARYLANGID(LocaleID)

                'Ensure only valid bits for Conversion are passed in.
                If (Conversion And Not (VbStrConv.Uppercase Or VbStrConv.Lowercase Or VbStrConv.Wide Or VbStrConv.Narrow _
                    Or VbStrConv.Katakana Or VbStrConv.Hiragana Or VbStrConv.SimplifiedChinese Or VbStrConv.TraditionalChinese _
                    Or VbStrConv.LinguisticCasing)) <> 0 Then
                    Throw New ArgumentException(SR.Argument_InvalidVbStrConv)
                End If

                '*** VbStrConv.SimplifiedChinese/VbStrConv.TraditionalChinese handling
                Select Case (Conversion And (VbStrConv.SimplifiedChinese + VbStrConv.TraditionalChinese))

                    Case 0
                        'Flags not used
                    Case (VbStrConv.SimplifiedChinese + VbStrConv.TraditionalChinese)
                        Throw New ArgumentException(SR.Argument_StrConvSCandTC)
                    Case VbStrConv.SimplifiedChinese
                        If IsValidCodePage(CODEPAGE_SIMPLIFIED_CHINESE) AndAlso IsValidCodePage(CODEPAGE_TRADITIONAL_CHINESE) Then
                            dwMapFlags = dwMapFlags Or NativeTypes.LCMAP_SIMPLIFIED_CHINESE
                        Else
                            Throw New ArgumentException(SR.Argument_SCNotSupported)
                        End If
                    Case VbStrConv.TraditionalChinese
                        If IsValidCodePage(CODEPAGE_SIMPLIFIED_CHINESE) AndAlso IsValidCodePage(CODEPAGE_TRADITIONAL_CHINESE) Then
                            dwMapFlags = dwMapFlags Or NativeTypes.LCMAP_TRADITIONAL_CHINESE
                        Else
                            Throw New ArgumentException(SR.Argument_TCNotSupported)
                        End If
                End Select

                '*** Upper/Lowercase handling
                Select Case (Conversion And (VbStrConv.Uppercase Or VbStrConv.Lowercase))
                    Case VbStrConv.None
                        'No conversion
                        If (Conversion And VbStrConv.LinguisticCasing) <> 0 Then
                            Throw New ArgumentException(SR.LinguisticRequirements)
                        End If

                    Case (VbStrConv.Uppercase Or VbStrConv.Lowercase)       '  VbStrConv.ProperCase is special: see below    
                        'Proper casing gets done below
                        dwMapFlags = 0
                    Case VbStrConv.Uppercase
                        If Conversion = VbStrConv.Uppercase Then
                            Return loc.TextInfo.ToUpper(str)
                        Else
                            dwMapFlags = dwMapFlags Or NativeTypes.LCMAP_UPPERCASE
                        End If
                    Case VbStrConv.Lowercase
                        If Conversion = VbStrConv.Lowercase Then
                            Return loc.TextInfo.ToLower(str)
                        Else
                            dwMapFlags = dwMapFlags Or NativeTypes.LCMAP_LOWERCASE
                        End If
                End Select

                If ((Conversion And (VbStrConv.Katakana + VbStrConv.Hiragana)) <> 0) Then
                    If (langid <> LANG_JAPANESE) OrElse (Not ValidLCID(LocaleID)) Then
                        Throw New ArgumentException(SR.Argument_JPNNotSupported)
                    Else
                        'Locale is ok
                    End If
                End If

                If (Conversion And (VbStrConv.Wide Or VbStrConv.Narrow)) <> 0 Then
                    If (langid = LANG_JAPANESE) OrElse
                       (langid = LANG_KOREAN) OrElse
                       (langid = LANG_CHINESE) Then
                        If Not ValidLCID(LocaleID) Then
                            Throw New ArgumentException(SR.Argument_LocalNotSupported)
                        End If
                    Else
                        Throw New ArgumentException(SR.Argument_WideNarrowNotApplicable)
                    End If
                End If

                '***  Width handling
                Select Case (Conversion And (VbStrConv.Wide Or VbStrConv.Narrow))
                    Case VbStrConv.None
                    Case VbStrConv.Wide Or VbStrConv.Narrow  '  VbStrConv.Wide+VbStrConv.Narrow is reserved
                        Throw New ArgumentException(SR.Argument_IllegalWideNarrow)
                    Case VbStrConv.Wide             '  VbStrConv.Wide
                        dwMapFlags = dwMapFlags Or NativeTypes.LCMAP_FULLWIDTH
                    Case VbStrConv.Narrow           '  VbStrConv.Narrow
                        dwMapFlags = dwMapFlags Or NativeTypes.LCMAP_HALFWIDTH
                End Select

                '*** Kana handling
                Select Case (Conversion And (VbStrConv.Katakana Or VbStrConv.Hiragana))
                    Case VbStrConv.None
                    Case (VbStrConv.Katakana Or VbStrConv.Hiragana)  '  VbStrConv.Katakana+VbStrConv.Hiragana is reserved
                        Throw New ArgumentException(SR.Argument_IllegalKataHira)
                    Case VbStrConv.Katakana '  VbStrConv.Katakana
                        dwMapFlags = dwMapFlags Or NativeTypes.LCMAP_KATAKANA
                    Case VbStrConv.Hiragana '  VbStrConv.Hiragana
                        dwMapFlags = dwMapFlags Or NativeTypes.LCMAP_HIRAGANA
                End Select

                ' accents field (Conversion And 192) in Conversion is reserved
                If ((Conversion And VbStrConv.ProperCase) = VbStrConv.ProperCase) Then
                    Return ProperCaseString(loc, dwMapFlags, [str])
                ElseIf dwMapFlags <> 0 Then
                    Return vbLCMapString(loc, dwMapFlags, [str])
                Else
                    Return [str]
                End If
            Catch ex As Exception
                Throw ex
            End Try
#Else
            Throw New PlatformNotSupportedException()
#End If
        End Function

#If TARGET_WINDOWS Then
        Friend Function ValidLCID(ByVal LocaleID As Integer) As Boolean
            Try
                Dim loc As CultureInfo = New CultureInfo(LocaleID)
                ValidLCID = True
            Catch ex As StackOverflowException
                Throw ex
            Catch ex As OutOfMemoryException
                Throw ex
            Catch
                ValidLCID = False
            End Try
        End Function

        Private Function ProperCaseString(ByVal loc As CultureInfo, ByVal dwMapFlags As Integer, ByVal sSrc As String) As String
            Dim iSrcLen As Integer
            Dim sb As StringBuilder

            If sSrc Is Nothing Then
                iSrcLen = 0
            Else
                iSrcLen = sSrc.Length
            End If

            If iSrcLen = 0 Then
                Return ""
            End If

            '   do the mapping specified by dwMapFlags, and at the same time, lowercase
            '   the whole string
            sb = New StringBuilder(vbLCMapString(loc, dwMapFlags Or NativeTypes.LCMAP_LOWERCASE, sSrc))

            'ToTitleCase is a more linguistically correct casing for the current locale
            Return loc.TextInfo.ToTitleCase(sb.ToString())

        End Function

        <ResourceExposure(ResourceScope.None)>
        <ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)>
        Friend Function vbLCMapString(ByVal loc As CultureInfo, ByVal dwMapFlags As Integer, ByVal sSrc As String) As String
            Dim length As Integer

            If sSrc Is Nothing Then
                length = 0
            Else
                length = sSrc.Length
            End If

            If length = 0 Then
                Return ""
            End If

            Dim sDest As String
            Dim lenDest As Integer
            Dim lcid As Integer = loc.LCID
            Dim enc As Text.Encoding = Text.Encoding.GetEncoding(loc.TextInfo.ANSICodePage)

            If Not enc.IsSingleByte Then

                'VB6 syntax note: ByVal String in Declare statements is really a ByRef String syntax
                'So sTemp will always be updated here on copyback 
                Dim sTemp As String = sSrc
                Dim bytesSrc, bytesDest As Byte()

                'Forced to use ANSI here
                'Char count can actual increase or decrease

                'Get byte array
                bytesSrc = enc.GetBytes(sTemp)

                'Get required byte length for new destination
                lenDest = UnsafeNativeMethods.LCMapStringA(lcid, dwMapFlags, bytesSrc, bytesSrc.Length, Nothing, 0)

                'Create destination byte array of required length
                bytesDest = New Byte(lenDest - 1) {}

                'Call again to do the actual translation
                lenDest = UnsafeNativeMethods.LCMapStringA(lcid, dwMapFlags, bytesSrc, bytesSrc.Length, bytesDest, lenDest)

                'Now convert back to a string
                sDest = enc.GetString(bytesDest)

                Return sDest

            Else
                'We do not use StringBuilder here because embedded NULLs cause an early termination of the string
                sDest = New String(" "c, length)
                lenDest = UnsafeNativeMethods.LCMapString(lcid, dwMapFlags, sSrc, length, sDest, length)
                Return sDest
            End If

        End Function
#End If

        Private Sub ValidateTriState(ByVal Param As TriState)
            If (Param <> vbTrue) AndAlso (Param <> vbFalse) AndAlso (Param <> vbUseDefault) Then
                Throw VbMakeException(vbErrors.IllegalFuncCall)
            End If
        End Sub

        Private Function IsArrayEmpty(ByVal array As System.Array) As Boolean
            If array Is Nothing Then
                Return True
            End If
            Return (array.Length = 0)
        End Function
    End Module
End Namespace
