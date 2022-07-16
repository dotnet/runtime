' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.

Imports System
Imports System.Diagnostics.CodeAnalysis
Imports System.Runtime.Versioning

Imports Microsoft.VisualBasic.CompilerServices
Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic

    Public Module Conversion

        Private Const NUMPRS_LEADING_PLUS As Integer = &H4I
        Private Const NUMPRS_LEADING_MINUS As Integer = &H10I
        Private Const NUMPRS_HEX_OCT As Integer = &H40I
        Private Const NUMPRS_DECIMAL As Integer = &H100I
        Private Const NUMPRS_EXPONENT As Integer = &H800I

        ' flags used by VarNumFromParseNum to indicate acceptable result types:
        '
        Private Const VTBIT_I2 As Integer = &H4
        Private Const VTBIT_I4 As Integer = &H8
        Private Const VTBIT_R4 As Integer = &H10
        Private Const VTBIT_R8 As Integer = &H20
        Private Const VTBIT_DATE As Integer = &H80
        Private Const VTBIT_BSTR As Integer = &H100
        Private Const VTBIT_OBJECT As Integer = &H200
        Private Const VTBIT_ERROR As Integer = &H400
        Private Const VTBIT_BOOL As Integer = &H800
        Private Const VTBIT_VARIANT As Integer = &H1000
        Private Const VTBIT_DATAOBJECT As Integer = &H2000
        Private Const VTBIT_DECIMAL As Integer = &H4000
        Private Const VTBIT_BYTE As Integer = &H20000
        Private Const VTBIT_CHAR As Integer = &H40000
        Private Const VTBIT_LONG As Integer = &H100000

        Private Const MAX_ERR_NUMBER As Integer = 65535
        Private Const LOCALE_NOUSEROVERRIDE As Integer = &H80000000I
        Private Const LCID_US_ENGLISH As Integer = &H409I
        Private Const PRSFLAGS As Integer _
            = (NUMPRS_LEADING_PLUS Or NUMPRS_LEADING_MINUS Or NUMPRS_HEX_OCT Or NUMPRS_DECIMAL Or NUMPRS_EXPONENT)
        Private Const VTBITS As Integer = (VTBIT_I2 Or VTBIT_I4 Or VTBIT_R8 Or VTBIT_DECIMAL)

        Private Const TYPE_INDICATOR_INT16 As Char = "%"c
        Private Const TYPE_INDICATOR_INT32 As Char = "&"c
        Private Const TYPE_INDICATOR_SINGLE As Char = "!"c
        Private Const TYPE_INDICATOR_DECIMAL As Char = "@"c
        Private Const ConversionTrimmerMessage As String = "The Expression's underlying type cannot be statically analyzed and its members may be trimmed"

        '============================================================================
        ' Error message functions.
        '============================================================================
        Public Function ErrorToString() As String
            Return Information.Err().Description
        End Function

        Public Function ErrorToString(ByVal ErrorNumber As Integer) As String
            If ErrorNumber >= MAX_ERR_NUMBER Then
                Throw New ArgumentException(SR.MaxErrNumber)
            End If

            If ErrorNumber > 0 Then
                ErrorNumber = (SEVERITY_ERROR Or FACILITY_CONTROL Or ErrorNumber)
            End If

            If (ErrorNumber And SCODE_FACILITY) = FACILITY_CONTROL Then
                ErrorNumber = ErrorNumber And &HFFFFI
                Return GetResourceString(CType(ErrorNumber, vbErrors))
            ElseIf ErrorNumber <> 0 Then
                Return GetResourceString(vbErrors.UserDefined)
            Else
                Return ""
            End If
        End Function

        '============================================================================
        ' Numeric functions.
        '============================================================================

        Public Function Fix(ByVal Number As Short) As Short
            Return Number
        End Function

        Public Function Fix(ByVal Number As Integer) As Integer
            Return Number
        End Function

        Public Function Fix(ByVal Number As Long) As Long
            Return Number
        End Function

        Public Function Fix(ByVal Number As Double) As Double
            If Number >= 0 Then
                Return System.Math.Floor(Number)
            Else
                Return -System.Math.Floor(-Number)
            End If
        End Function

        Public Function Fix(ByVal Number As Single) As Single
            If Number >= 0 Then
                Return CSng(System.Math.Floor(CDbl(Number)))
            Else
                Return CSng(-System.Math.Floor(CDbl(-Number)))
            End If
        End Function

        Public Function Fix(ByVal Number As Decimal) As Decimal
            If System.Decimal.op_LessThan(Number, System.Decimal.Zero) Then
                Return System.Decimal.Negate(System.Decimal.Floor(System.Decimal.Negate(Number)))
            Else
                Return System.Decimal.Floor(Number)
            End If
        End Function

        Public Function Fix(ByVal Number As Object) As Object
            If Number Is Nothing Then
                Throw New ArgumentNullException(SR.Format(SR.Argument_InvalidNullValue1, "Number"))
            End If

            Dim ValueInterface As IConvertible

            ValueInterface = TryCast(Number, IConvertible)

            If Not ValueInterface Is Nothing Then

                Select Case ValueInterface.GetTypeCode()

                    Case TypeCode.SByte,
                         TypeCode.Byte,
                         TypeCode.Int16,
                         TypeCode.UInt16,
                         TypeCode.Int32,
                         TypeCode.UInt32,
                         TypeCode.Int64,
                         TypeCode.UInt64

                        Return Number

                    Case TypeCode.Single
                        Return Fix(ValueInterface.ToSingle(Nothing))

                    Case TypeCode.Double
                        Return Fix(ValueInterface.ToDouble(Nothing))

                    Case TypeCode.Decimal
                        Return Fix(ValueInterface.ToDecimal(Nothing))

                    Case TypeCode.Boolean
                        Return ValueInterface.ToInt32(Nothing)

                    Case TypeCode.String
                        Return Fix(CDbl(ValueInterface.ToString(Nothing)))

                    Case Else
                        'TypeCode.Char
                        'TypeCode.DateTime
                        ' Fall through to error

                End Select

            End If

            Throw VbMakeException(New ArgumentException(SR.Format(SR.Argument_NotNumericType2, NameOf(Number), Number.GetType().FullName)), vbErrors.TypeMismatch)
        End Function

        Public Function Int(ByVal Number As Short) As Short
            Return Number
        End Function

        Public Function Int(ByVal Number As Integer) As Integer
            Return Number
        End Function

        Public Function Int(ByVal Number As Long) As Long
            Return Number
        End Function

        Public Function Int(ByVal Number As Double) As Double
            Return System.Math.Floor(Number)
        End Function

        Public Function Int(ByVal Number As Single) As Single
            Return CSng(System.Math.Floor(CDbl(Number)))
        End Function

        Public Function Int(ByVal Number As Decimal) As Decimal
            Return System.Decimal.Floor(Number)
        End Function

        Public Function Int(ByVal Number As Object) As Object
            If Number Is Nothing Then
                Throw New ArgumentNullException(SR.Format(SR.Argument_InvalidNullValue1, NameOf(Number)))
            End If

            Dim ValueInterface As IConvertible

            ValueInterface = TryCast(Number, IConvertible)

            If Not ValueInterface Is Nothing Then

                Select Case ValueInterface.GetTypeCode()

                    Case TypeCode.SByte,
                         TypeCode.Byte,
                         TypeCode.Int16,
                         TypeCode.UInt16,
                         TypeCode.Int32,
                         TypeCode.UInt32,
                         TypeCode.Int64,
                         TypeCode.UInt64

                        Return Number

                    Case TypeCode.Single
                        Return Int(ValueInterface.ToSingle(Nothing))

                    Case TypeCode.Double
                        Return Int(ValueInterface.ToDouble(Nothing))

                    Case TypeCode.Decimal
                        Return Int(ValueInterface.ToDecimal(Nothing))

                    Case TypeCode.Boolean
                        Return ValueInterface.ToInt32(Nothing)

                    Case TypeCode.String
                        Return Int(CDbl(ValueInterface.ToString(Nothing)))

                    Case Else
                        'TypeCode.Char
                        'TypeCode.DateTime
                        ' Fall through to error
                End Select
            End If

            Throw VbMakeException(New ArgumentException(SR.Format(SR.Argument_NotNumericType2, NameOf(Number), Number.GetType().FullName)), vbErrors.TypeMismatch)
        End Function

        '============================================================================
        ' Number to string conversion
        '============================================================================
        <CLSCompliant(False)>
        Public Function Hex(ByVal Number As SByte) As String
            Return Number.ToString("X")
        End Function

        Public Function Hex(ByVal Number As Byte) As String
            Return Number.ToString("X")
        End Function

        Public Function Hex(ByVal Number As Short) As String
            Return Number.ToString("X")
        End Function

        <CLSCompliant(False)>
        Public Function Hex(ByVal Number As UShort) As String
            Return Number.ToString("X")
        End Function

        Public Function Hex(ByVal Number As Integer) As String
            Return Number.ToString("X")
        End Function

        <CLSCompliant(False)>
        Public Function Hex(ByVal Number As UInteger) As String
            Return Number.ToString("X")
        End Function

        Public Function Hex(ByVal Number As Long) As String
            Return Number.ToString("X")
        End Function

        <CLSCompliant(False)>
        Public Function Hex(ByVal Number As ULong) As String
            Return Number.ToString("X")
        End Function

        Public Function Hex(ByVal Number As Object) As String
            Dim LongValue As Long

            If Number Is Nothing Then
                Throw New ArgumentNullException(SR.Format(SR.Argument_InvalidNullValue1, NameOf(Number)))
            End If

            Dim ValueInterface As IConvertible

            ValueInterface = TryCast(Number, IConvertible)

            If Not ValueInterface Is Nothing Then

                Select Case ValueInterface.GetTypeCode()

                    Case TypeCode.SByte
                        Return Hex(ValueInterface.ToSByte(Nothing))

                    Case TypeCode.Byte
                        Return Hex(ValueInterface.ToByte(Nothing))

                    Case TypeCode.Int16
                        Return Hex(ValueInterface.ToInt16(Nothing))

                    Case TypeCode.UInt16
                        Return Hex(ValueInterface.ToUInt16(Nothing))

                    Case TypeCode.Int32
                        Return Hex(ValueInterface.ToInt32(Nothing))

                    Case TypeCode.UInt32
                        Return Hex(ValueInterface.ToUInt32(Nothing))

                    Case TypeCode.Int64,
                         TypeCode.Single,
                         TypeCode.Double,
                         TypeCode.Decimal
                        LongValue = ValueInterface.ToInt64(Nothing)
                        GoTo RangeCheck

                    Case TypeCode.UInt64
                        Return Hex(ValueInterface.ToUInt64(Nothing))

                    Case TypeCode.String
                        Try
                            LongValue = CLng(ValueInterface.ToString(Nothing))
                        Catch ex As OverflowException
                            'If the conversion to Long overflows, we can try ULong.
                            Return Hex(CULng(ValueInterface.ToString(Nothing)))
                        End Try
RangeCheck:
                        'Optimization case
                        If LongValue = 0 Then
                            Return "0"
                        End If

                        If (LongValue > 0) Then
                            Return Hex(LongValue)
                        Else
                            'For VB6 compatibility, format as Int32 value
                            ' unless it overflows into an Int64
                            If (LongValue >= System.Int32.MinValue) Then
                                Return Hex(CInt(LongValue))
                            End If
                            Return Hex(LongValue)
                        End If

                    Case TypeCode.Boolean,
                         TypeCode.Char,
                         TypeCode.DateTime
                        ' Fall through to error

                    Case Else
                        ' Fall through to error
                End Select
            End If

            Throw New ArgumentException(SR.Format(SR.Argument_InvalidValueType2, NameOf(Number), VBFriendlyName(Number)))
        End Function

        <CLSCompliant(False)>
        Public Function Oct(ByVal Number As SByte) As String
            Return OctFromLong(CLng(Number) And &HFFL)
        End Function

        Public Function Oct(ByVal Number As Byte) As String
            Return OctFromULong(CULng(Number))
        End Function

        Public Function Oct(ByVal Number As Short) As String
            Return OctFromLong(CLng(Number) And &HFFFFL)
        End Function

        <CLSCompliant(False)>
        Public Function Oct(ByVal Number As UShort) As String
            Return OctFromULong(CULng(Number))
        End Function

        Public Function Oct(ByVal Number As Integer) As String
            Return OctFromLong(CLng(Number) And &HFFFFFFFFL)
        End Function

        <CLSCompliant(False)>
        Public Function Oct(ByVal Number As UInteger) As String
            Return OctFromULong(CULng(Number))
        End Function

        Public Function Oct(ByVal Number As Long) As String
            Return OctFromLong(Number)
        End Function

        <CLSCompliant(False)>
        Public Function Oct(ByVal Number As ULong) As String
            Return OctFromULong(Number)
        End Function

        Public Function Oct(ByVal Number As Object) As String
            Dim LongValue As Long

            If Number Is Nothing Then
                Throw New ArgumentNullException(SR.Format(SR.Argument_InvalidNullValue1, NameOf(Number)))
            End If

            Dim ValueInterface As IConvertible

            ValueInterface = TryCast(Number, IConvertible)

            If Not ValueInterface Is Nothing Then

                Select Case ValueInterface.GetTypeCode()

                    Case TypeCode.SByte
                        Return Oct(ValueInterface.ToSByte(Nothing))
                    Case TypeCode.Byte
                        Return Oct(ValueInterface.ToByte(Nothing))
                    Case TypeCode.Int16
                        Return Oct(ValueInterface.ToInt16(Nothing))
                    Case TypeCode.UInt16
                        Return Oct(ValueInterface.ToUInt16(Nothing))
                    Case TypeCode.Int32
                        Return Oct(ValueInterface.ToInt32(Nothing))
                    Case TypeCode.UInt32
                        Return Oct(ValueInterface.ToUInt32(Nothing))

                    Case TypeCode.Int64,
                         TypeCode.Single,
                         TypeCode.Double,
                         TypeCode.Decimal
                        LongValue = ValueInterface.ToInt64(Nothing)
                        GoTo RangeCheck

                    Case TypeCode.UInt64
                        Return Oct(ValueInterface.ToUInt64(Nothing))

                    Case TypeCode.String
                        Try
                            LongValue = CLng(ValueInterface.ToString(Nothing))
                        Catch ex As OverflowException
                            'If the conversion to Long overflows, we can try ULong.
                            Return Oct(CULng(ValueInterface.ToString(Nothing)))
                        End Try
RangeCheck:
                        'Optimization case
                        If LongValue = 0 Then
                            Return "0"
                        End If

                        If (LongValue > 0) Then
                            Return Oct(LongValue)
                        Else
                            'For VB6 compatibility, format as Int32 value
                            ' unless it overflows into an Int64
                            If (LongValue >= System.Int32.MinValue) Then
                                Return Oct(CInt(LongValue))
                            End If
                            Return Oct(LongValue)
                        End If

                    Case TypeCode.Boolean,
                         TypeCode.Char,
                         TypeCode.DateTime
                        ' Fall through to error

                    Case Else
                        ' Fall through to error
                End Select
            End If

            Throw New ArgumentException(SR.Format(SR.Argument_InvalidValueType2, NameOf(Number), VBFriendlyName(Number)))
        End Function

        Public Function Str(ByVal Number As Object) As String
            Dim s As String

            If Number Is Nothing Then
                Throw New ArgumentNullException(SR.Format(SR.Argument_InvalidNullValue1, NameOf(Number)))
            End If

            Dim ValueInterface As IConvertible
            Dim ValueTypeCode As TypeCode

            ValueInterface = TryCast(Number, IConvertible)

            If ValueInterface Is Nothing Then
                Throw New InvalidCastException(SR.Format(SR.ArgumentNotNumeric1, NameOf(Number)))
            End If

            ValueTypeCode = ValueInterface.GetTypeCode()
            Select Case ValueTypeCode

                Case TypeCode.DBNull
                    Return "Null"

                Case TypeCode.Boolean
                    If ValueInterface.ToBoolean(Nothing) Then
                        Return "True"
                    Else
                        Return "False"
                    End If

                Case TypeCode.SByte,
                     TypeCode.Byte,
                     TypeCode.Int16,
                     TypeCode.UInt16,
                     TypeCode.Int32,
                     TypeCode.UInt32,
                     TypeCode.Int64,
                     TypeCode.UInt64,
                     TypeCode.Single,
                     TypeCode.Double,
                     TypeCode.Decimal
                    s = CStr(Number)

                Case Else
                    If ValueTypeCode = TypeCode.String Then
                        Try
                            s = CStr(CDbl(ValueInterface.ToString(Nothing)))
                            GoTo FormatAndExit
                        Catch ex As StackOverflowException
                            Throw ex
                        Catch ex As OutOfMemoryException
                            Throw ex
                        Catch
                            'Throw our own exception below
                        End Try
                    End If
                    Throw New InvalidCastException(SR.Format(SR.ArgumentNotNumeric1, NameOf(Number)))
            End Select

FormatAndExit:
            If s.Length > 0 AndAlso s.Chars(0) <> "-"c Then
                Return " " & StdFormat(s)
            Else
                Return StdFormat(s)
            End If
        End Function

        Private Function HexOrOctValue(ByVal InputStr As String, ByVal i As Integer) As Double
            Dim digits As Integer = 0
            Dim ch As Char
            Dim iLen As Integer
            Dim ivalue As Long
            Dim digitValue As Integer

            Const asc0 As Integer = AscW("0"c)
            Const ascUpperAoffset As Integer = AscW("A"c) - 10
            Const ascLowerAoffset As Integer = AscW("a"c) - 10

            iLen = InputStr.Length

            ch = InputStr.Chars(i)
            i += 1

            If ch = "H"c OrElse ch = "h"c Then
                'Loop for octal
                Do While (i < iLen AndAlso digits < 17)
                    ch = InputStr.Chars(i)
                    i += 1
                    Select Case ch
                        Case ControlChars.Tab, ControlChars.Lf, ControlChars.Cr, ChrW(32), ChrW(&H3000S)
                            GoTo NextHexCharacter

                        Case "0"c
                            If digits = 0 Then
                                'leading zeros do not affect type
                                GoTo NextHexCharacter
                            End If
                            digitValue = 0

                        Case "1"c To "9"c
                            digitValue = AscW(ch) - asc0

                        Case "A"c To "F"c
                            digitValue = AscW(ch) - ascUpperAoffset

                        Case "a"c To "f"c
                            digitValue = AscW(ch) - ascLowerAoffset

                        Case Else
                            Exit Do
                    End Select
AddHexDigit:
                    '                If digits = 15 AndAlso ivalue >= &H800000000000000L Then
                    If digits = 15 AndAlso ivalue > &H7FFFFFFFFFFFFFFL Then
                        'This will overflow because we don't have a shift operator
                        'and must do multiplication
                        ivalue = (ivalue And &H7FFFFFFFFFFFFFFL) * 16
                        ivalue = ivalue Or &H8000000000000000L
                    Else
                        ivalue = ivalue * 16
                    End If
                    ivalue += digitValue
                    digits += 1
NextHexCharacter:

                Loop

                If digits = 16 Then
                    i += 1
                    If i < iLen Then
                        'We fell out of the loop before getting the typechar
                        ch = InputStr.Chars(i)
                    End If
                End If

                If digits > 8 Then
                    'leave ivalue unchanged

                ElseIf digits > 4 OrElse ch = TYPE_INDICATOR_INT32 Then
                    If ivalue > &H7FFFFFFFL Then
                        ivalue = Int32.MinValue + (ivalue And &H7FFFFFFFL)
                    End If

                ElseIf digits > 2 OrElse ch = TYPE_INDICATOR_INT16 Then
                    If ivalue > &H7FFFL Then
                        ivalue = Int16.MinValue + (ivalue And &H7FFFL)
                    End If

                End If

                If ch = TYPE_INDICATOR_INT16 Then
                    ivalue = CShort(ivalue)
                ElseIf ch = TYPE_INDICATOR_INT32 Then
                    ivalue = CInt(ivalue)
                End If
                Return ivalue

            ElseIf ch = "O"c OrElse ch = "o"c Then

                'Loop for octal
                Do While (i < iLen AndAlso digits < 22)
                    ch = InputStr.Chars(i)
                    i += 1

                    Select Case ch
                        Case ControlChars.Tab, ControlChars.Lf, ControlChars.Cr, ChrW(32), ChrW(&H3000S)
                            GoTo NextOctCharacter

                        Case "0"c
                            If digits = 0 Then
                                'leading zeros do not affect type
                                GoTo NextOctCharacter
                            End If
                            digitValue = 0

                        Case "1"c To "7"c
                            digitValue = AscW(ch) - asc0

                        Case Else
                            Exit Do

                    End Select

AddOctDigit:
                    If ivalue >= &O100000000000000000000L Then
                        'This will overflow because we don't have a shift operator
                        'and must do multiplication
                        ivalue = (ivalue And &O77777777777777777777L) * 8
                        ivalue = ivalue Or &O100000000000000000000L
                    Else
                        ivalue = ivalue * 8
                    End If
                    ivalue += digitValue
                    digits += 1
NextOctCharacter:

                Loop

                If digits = 22 Then
                    i += 1
                    If i < iLen Then
                        'We fell out of the loop before getting the typechar
                        ch = InputStr.Chars(i)
                    End If
                End If

                If ivalue > &H100000000L Then
                    'leave ivalue unchanged

                ElseIf ivalue > &HFFFFL OrElse ch = TYPE_INDICATOR_INT32 Then
                    If ivalue > &H7FFFFFFFL Then
                        ivalue = Int32.MinValue + (ivalue And &H7FFFFFFFL)
                    End If

                ElseIf ivalue > &HFFL OrElse ch = TYPE_INDICATOR_INT16 Then
                    If ivalue > &H7FFFL Then
                        ivalue = Int16.MinValue + (ivalue And &H7FFFL)
                    End If

                End If

                If ch = TYPE_INDICATOR_INT16 Then
                    ivalue = CShort(ivalue)
                ElseIf ch = TYPE_INDICATOR_INT32 Then
                    ivalue = CInt(ivalue)
                End If
                Return ivalue
            Else
                'input is invalid
                Return 0
            End If

        End Function

        Public Function Val(ByVal InputStr As String) As Double

            Dim ch As Char
            Dim i As Integer
            Dim iLen As Integer
            Dim digits As Integer
            Dim digitsAfterDecimal, digitsBeforeDecimal As Integer

            Const asc0 As Integer = AscW("0"c)

            If InputStr Is Nothing Then
                iLen = 0
            Else
                iLen = InputStr.Length
            End If

            i = 0
            'Skip over leading whitespace
            Do While (i < iLen)
                ch = InputStr.Chars(i)
                Select Case ch
                    Case ControlChars.Tab, ControlChars.Lf, ControlChars.Cr, ChrW(32), ChrW(&H3000S)
                        i += 1
                    Case Else
                        Exit Do
                End Select
            Loop

            If i >= iLen Then
                Return 0
            End If

            ch = InputStr.Chars(i)
            If ch = "&"c Then 'We are dealing with hex or octal numbers
                Return HexOrOctValue(InputStr, i + 1)

            Else 'we are dealing with base 10 decimal
                Dim value As Double
                Dim afterdecimal As Boolean = False
                Dim aftere As Boolean = False
                Dim negative As Boolean = False
                Dim eval As Double = 0

                'Check for negative
                ch = InputStr.Chars(i)
                If ch = "-"c Then
                    negative = True
                    i += 1
                ElseIf ch = "+"c Then
                    i += 1
                End If

                'check for numbers before a decimal or E
                Do While (i < iLen)
                    ch = InputStr.Chars(i)
                    Select Case ch
                        Case ControlChars.Tab, ControlChars.Lf, ControlChars.Cr, ChrW(32), ChrW(&H3000S)
                            i += 1

                        Case "0"c
                            If digits <> 0 OrElse afterdecimal Then
                                value = value * 10 + AscW(ch) - asc0
                                i += 1
                                digits += 1
                            Else
                                i += 1
                                'don't count as digit
                            End If

                        Case "1"c To "9"c
                            value = value * 10 + AscW(ch) - asc0
                            i += 1
                            digits += 1

                        Case "."c
                            i += 1
                            If afterdecimal = False Then
                                afterdecimal = True
                                digitsBeforeDecimal = digits
                            Else
                                'handle "1..1" or "1.2.1"
                                Exit Do
                            End If

                        Case "e"c, "E"c, "d"c, "D"c
                            aftere = True
                            i += 1
                            Exit Do

                        Case Else
                            Exit Do
                    End Select
                Loop

                If afterdecimal Then
                    digitsAfterDecimal = digits - digitsBeforeDecimal
                End If

                If aftere Then
                    Dim afterplusminus As Boolean = False
                    Dim enegative As Boolean = False
                    Do While (i < iLen)
                        ch = InputStr.Chars(i)
                        Select Case ch
                            Case ControlChars.Tab, ControlChars.Lf, ControlChars.Cr, ChrW(32), ChrW(&H3000S)
                                i += 1

                            Case "0"c To "9"c
                                eval = eval * 10 + AscW(ch) - asc0
                                i += 1

                            Case "+"c
                                If Not afterplusminus Then
                                    afterplusminus = True
                                    i += 1
                                Else
                                    Exit Do
                                End If

                            Case "-"c
                                If Not afterplusminus Then
                                    afterplusminus = True
                                    enegative = True
                                    i += 1
                                Else
                                    Exit Do
                                End If

                            Case Else
                                Exit Do
                        End Select
                    Loop

                    If enegative Then
                        eval += digitsAfterDecimal
                        value = value * (10 ^ (-eval))
                    Else
                        eval -= digitsAfterDecimal
                        value = value * (10 ^ (eval))
                    End If
                Else
                    If afterdecimal AndAlso digitsAfterDecimal <> 0 Then
                        'Need to adjust for decimal
                        value = value / (10 ^ digitsAfterDecimal)
                    End If
                End If

                If System.Double.IsInfinity(value) Then
                    Throw VbMakeException(vbErrors.Overflow)
                End If

                If negative Then
                    value = -value
                End If

                Select Case ch

                    Case TYPE_INDICATOR_INT16
                        If digitsAfterDecimal > 0 Then
                            Throw VbMakeException(vbErrors.TypeMismatch)
                        End If
                        value = CShort(value)

                    Case TYPE_INDICATOR_INT32
                        If digitsAfterDecimal > 0 Then
                            Throw VbMakeException(vbErrors.TypeMismatch)
                        End If
                        value = CInt(value)

                    Case TYPE_INDICATOR_SINGLE
                        value = CSng(value)

                    Case TYPE_INDICATOR_DECIMAL
                        value = CDec(value)

                    Case Else

                End Select

                Return value
            End If
        End Function

        Public Function Val(ByVal Expression As Char) As Integer
            'Val only handles Ascii decimal chars '0' to '9'
            Dim CharValue As Integer

            CharValue = AscW(Expression) 'CType(Expression, IConvertible).ToInt32(Nothing)
            If CharValue >= AscW("1"c) AndAlso CharValue <= AscW("9"c) Then
                Return CharValue - AscW("0")
            End If
            Return 0
        End Function

        Public Function Val(ByVal Expression As Object) As Double

            Dim StringExpression As String = TryCast(Expression, String)

            If StringExpression IsNot Nothing Then
                Return Val(StringExpression)

            ElseIf TypeOf Expression Is Char Then
                Return Val(DirectCast(Expression, Char))

            ElseIf CompilerServices.Versioned.IsNumeric(Expression) Then
                Return CDbl(Expression)

            Else
                Dim sValue As String
                Try
                    sValue = CStr(Expression)
                Catch ex As StackOverflowException
                    Throw ex
                Catch ex As OutOfMemoryException
                    Throw ex
                Catch
                    Throw VbMakeException(New ArgumentException(SR.Format(SR.Argument_InvalidValueType2, NameOf(Expression), VBFriendlyName(Expression))), vbErrors.OLENoPropOrMethod)
                End Try

                Return Val(sValue)
            End If

        End Function


        <ResourceExposure(ResourceScope.None)>
        <ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)>
        <RequiresUnreferencedCode("Calls UnsafeNativeMethods.VariantChangeType")>
        Friend Function ParseInputField(ByVal Value As Object, ByVal vtInput As VariantType) As Object
#If TARGET_WINDOWS Then
            Dim numprsPtr() As Byte
            Dim vtSuffix As Integer
            Dim cDecMax As Integer
            Dim StringValue As String = CStr(Value)
            Dim DigitArray() As Byte
            Dim pd As ProjectData
            Dim cchUsed As Int32
            Dim nPwr10 As Int32
            Dim chTypeChar As Char
            Dim dwOutFlags As Int32
            Dim nBaseShift As Int32

            Const INTEGER_SIZE As Integer = 4
            Const INFLAGS_OFFSET As Integer = 4

            If ((vtInput = VariantType.Empty) AndAlso ((Value Is Nothing) OrElse Len(CStr(Value)) = 0)) Then
                Return Nothing
            End If

            pd = ProjectData.GetProjectData()
            numprsPtr = pd.m_numprsPtr
            DigitArray = pd.m_DigitArray

            'numprsPtr is actually a struct. The first two fields are cDig (the size of the digits array)
            'and dwInFlags which we set to PRSFLAGS

            'Init NUMPARSE.cDig
            Array.Copy(BitConverter.GetBytes(Convert.ToInt32(DigitArray.Length)), 0, numprsPtr, 0, INTEGER_SIZE)
            'Init NUMPARSE.dwInFlags
            Array.Copy(BitConverter.GetBytes(Convert.ToInt32(PRSFLAGS)), 0, numprsPtr, INFLAGS_OFFSET, INTEGER_SIZE)

            ' For file interchangeability, we always use US decimal.
            If UnsafeNativeMethods.VarParseNumFromStr(StringValue, LCID_US_ENGLISH, LOCALE_NOUSEROVERRIDE, numprsPtr, DigitArray) < 0 Then
                If (vtInput <> VariantType.Empty) Then
                    ' Just return 0 if we don't understand the number
                    Return 0
                End If
                Return StringValue
            End If

            ' Look for type character following string
            dwOutFlags = BitConverter.ToInt32(numprsPtr, 8)
            cchUsed = BitConverter.ToInt32(numprsPtr, 12)
            nBaseShift = BitConverter.ToInt32(numprsPtr, 16)
            nPwr10 = BitConverter.ToInt32(numprsPtr, 20)

            If cchUsed < StringValue.Length Then
                chTypeChar = StringValue.Chars(cchUsed)
            End If

            Select Case (chTypeChar)
                Case "%"c
                    vtSuffix = VariantType.Short
                    cDecMax = 0
                Case "&"c
                    vtSuffix = VariantType.Integer
                    cDecMax = 0
                Case "@"c
                    'Convert currency to Decimal
                    'vtSuffix = VariantType.Currency
                    vtSuffix = VariantType.Decimal
                    cDecMax = 4
                Case "!"c
                    If (vtInput = VariantType.Double) Then
                        vtSuffix = VariantType.Double
                    Else
                        vtSuffix = VariantType.Single
                    End If
                    cDecMax = System.Int32.MaxValue
                Case "#"c
                    vtSuffix = VariantType.Double
                    cDecMax = System.Int32.MaxValue
                Case Else
                    ' No type suffix.
                    If (vtInput = VariantType.Empty) Then
                        ' no indication of type, either from suffix or defined
                        ' by type we're inputting to.
                        Dim dwVtBits As Integer = VTBITS

                        If (dwOutFlags And NUMPRS_EXPONENT) <> 0 Then
                            ' if exponent specified, result is R8 only.
                            dwVtBits = VTBIT_R8
                        End If

                        Return UnsafeNativeMethods.VarNumFromParseNum(numprsPtr, DigitArray, dwVtBits)
                    End If

                    If (nBaseShift <> 0) Then
                        Dim Int32Value As Integer

                        ' Have a hex/octal number.  Sign extend if short.
                        Value = UnsafeNativeMethods.VarNumFromParseNum(numprsPtr, DigitArray, VTBIT_I4)
                        Int32Value = CInt(Value)

                        If ((Int32Value And &HFFFF0000I) = 0) Then
                            ' Sign extend if short.
                            Int32Value = CShort(Int32Value)
                        End If

                        UnsafeNativeMethods.VariantChangeType(Value, Value, 0, CType(vtInput, Int16))
                        Return Value
                    End If

                    Return UnsafeNativeMethods.VarNumFromParseNum(numprsPtr, DigitArray, ShiftVTBits(vtInput))
            End Select

            ' Have a type character suffix.  Convert to that type.
            If (-nPwr10 > cDecMax) Then
                Throw VbMakeException(vbErrors.TypeMismatch)
            End If

            Value = UnsafeNativeMethods.VarNumFromParseNum(numprsPtr, DigitArray, ShiftVTBits(vtSuffix))

            If (vtInput = VariantType.Empty) Then
                Return Value
            End If

            UnsafeNativeMethods.VariantChangeType(Value, Value, 0, CType(vtInput, Int16))
            Return Value
#Else
            Throw New PlatformNotSupportedException()
#End If
        End Function

        Private Function ShiftVTBits(ByVal vt As Integer) As Integer
            Select Case vt
                'Case VariantType.Empty
                'Case VariantType.Null
                Case VariantType.Short
                    Return VTBIT_I2
                Case VariantType.Integer
                    Return VTBIT_I4
                Case VariantType.Single
                    Return VTBIT_R4
                Case VariantType.Double
                    Return VTBIT_R8
                Case VariantType.Decimal, VariantType.Currency
                    Return VTBIT_DECIMAL
                Case VariantType.Date
                    Return VTBIT_DATE
                Case VariantType.String
                    Return VTBIT_BSTR
                Case VariantType.Object
                    Return VTBIT_OBJECT
                Case VariantType.Error
                    Return VTBIT_ERROR
                Case VariantType.Boolean
                    Return VTBIT_BOOL
                Case VariantType.Variant
                    Return VTBIT_VARIANT
                Case VariantType.DataObject
                    Return VTBIT_DATAOBJECT
                Case VariantType.Decimal
                    Return VTBIT_DECIMAL
                Case VariantType.Byte
                    Return VTBIT_BYTE
                Case VariantType.Char
                    Return VTBIT_CHAR
                Case VariantType.Long
                    Return VTBIT_LONG
                Case Else
                    Return 0
            End Select
        End Function

        <RequiresUnreferencedCode(ConversionTrimmerMessage)>
        Public Function CTypeDynamic(
                ByVal Expression As Object,
                <DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)>
                ByVal TargetType As System.Type) As Object
            Return Conversions.ChangeType(Expression, TargetType, True)
        End Function

        <RequiresUnreferencedCode(ConversionTrimmerMessage)>
        Public Function CTypeDynamic(Of TargetType)(ByVal Expression As Object) As TargetType
            Return DirectCast(Conversions.ChangeType(Expression, GetType(TargetType), True), TargetType)
        End Function
    End Module
End Namespace

