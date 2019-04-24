' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Diagnostics
Imports System.Globalization

Imports Microsoft.VisualBasic.CompilerServices.StringType
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.CompilerServices

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public NotInheritable Class ObjectType

        Private Const TCMAX As Integer = 19

        Private Enum VType
            t_bad
            t_bool
            t_ui1
            t_i2
            t_i4
            t_i8
            t_dec
            t_r4
            t_r8
            t_char
            t_str
            t_date
        End Enum

        '***
        '*** Enum for indexing into ConversionClassTable
        '*** NOTE: Post RTM revisions should merge VType and VType2 usage 
        '***       into a single enum and remove use of WiderType table
        '***
        Private Enum VType2
            t_bad
            t_bool
            t_ui1
            t_char
            t_i2
            t_i4
            t_i8
            t_r4
            t_r8
            t_date
            t_dec
            t_ref
            t_str
        End Enum

        '            '    t_bad ,      t_bool,       t_ui1 ,       t_i2  ,      t_i4  ,      t_i8  ,      t_dec ,      t_r4  ,      t_r8  ,      t_char,       t_str ,       t_date
        Private Shared ReadOnly WiderType(,) As VType = {
                        {VType.t_bad, VType.t_bad, VType.t_bad, VType.t_bad, VType.t_bad, VType.t_bad, VType.t_bad, VType.t_bad, VType.t_bad, VType.t_bad, VType.t_bad, VType.t_bad},
                        {VType.t_bad, VType.t_bool, VType.t_bool, VType.t_i2, VType.t_i4, VType.t_i8, VType.t_dec, VType.t_r4, VType.t_r8, VType.t_bad, VType.t_r8, VType.t_bad},
                        {VType.t_bad, VType.t_bool, VType.t_ui1, VType.t_i2, VType.t_i4, VType.t_i8, VType.t_dec, VType.t_r4, VType.t_r8, VType.t_bad, VType.t_r8, VType.t_bad},
                        {VType.t_bad, VType.t_i2, VType.t_i2, VType.t_i2, VType.t_i4, VType.t_i8, VType.t_dec, VType.t_r4, VType.t_r8, VType.t_bad, VType.t_r8, VType.t_bad},
                        {VType.t_bad, VType.t_i4, VType.t_i4, VType.t_i4, VType.t_i4, VType.t_i8, VType.t_dec, VType.t_r4, VType.t_r8, VType.t_bad, VType.t_r8, VType.t_bad},
                        {VType.t_bad, VType.t_i8, VType.t_i8, VType.t_i8, VType.t_i8, VType.t_i8, VType.t_dec, VType.t_r4, VType.t_r8, VType.t_bad, VType.t_r8, VType.t_bad},
                        {VType.t_bad, VType.t_dec, VType.t_dec, VType.t_dec, VType.t_dec, VType.t_dec, VType.t_dec, VType.t_r4, VType.t_r8, VType.t_bad, VType.t_r8, VType.t_bad},
                        {VType.t_bad, VType.t_r4, VType.t_r4, VType.t_r4, VType.t_r4, VType.t_r4, VType.t_r4, VType.t_r4, VType.t_r8, VType.t_bad, VType.t_r8, VType.t_bad},
                        {VType.t_bad, VType.t_r8, VType.t_r8, VType.t_r8, VType.t_r8, VType.t_r8, VType.t_r8, VType.t_r8, VType.t_r8, VType.t_bad, VType.t_r8, VType.t_bad},
                        {VType.t_bad, VType.t_bad, VType.t_bad, VType.t_bad, VType.t_bad, VType.t_bad, VType.t_bad, VType.t_bad, VType.t_bad, VType.t_char, VType.t_str, VType.t_bad},
                        {VType.t_bad, VType.t_r8, VType.t_r8, VType.t_r8, VType.t_r8, VType.t_r8, VType.t_r8, VType.t_r8, VType.t_r8, VType.t_str, VType.t_str, VType.t_date},
                        {VType.t_bad, VType.t_bad, VType.t_bad, VType.t_bad, VType.t_bad, VType.t_bad, VType.t_bad, VType.t_bad, VType.t_bad, VType.t_bad, VType.t_date, VType.t_date}
                    }

        Private Enum CC As Byte
            Err = 0
            Same = 1
            Narr = 2
            Wide = 3
        End Enum

        '            '*** This table comes from compiler sources in vb\bc\OverloadResolution.cpp 
        '            '*** From->bad bool     ui1      char       i2      i4      i8          r4    r8        date    dec         ref   str
        '            '*** Access using CC(totype, fromtype)
        Private Shared ReadOnly ConversionClassTable(,) As CC = {
                        {CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err},
                        {CC.Err, CC.Same, CC.Narr, CC.Err, CC.Narr, CC.Narr, CC.Narr, CC.Narr, CC.Narr, CC.Err, CC.Narr, CC.Err, CC.Narr},
                        {CC.Err, CC.Narr, CC.Same, CC.Err, CC.Narr, CC.Narr, CC.Narr, CC.Narr, CC.Narr, CC.Err, CC.Narr, CC.Err, CC.Narr},
                        {CC.Err, CC.Err, CC.Err, CC.Same, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Narr},
                        {CC.Err, CC.Narr, CC.Wide, CC.Err, CC.Same, CC.Narr, CC.Narr, CC.Narr, CC.Narr, CC.Err, CC.Narr, CC.Err, CC.Narr},
                        {CC.Err, CC.Narr, CC.Wide, CC.Err, CC.Wide, CC.Same, CC.Narr, CC.Narr, CC.Narr, CC.Err, CC.Narr, CC.Err, CC.Narr},
                        {CC.Err, CC.Narr, CC.Wide, CC.Err, CC.Wide, CC.Wide, CC.Same, CC.Narr, CC.Narr, CC.Err, CC.Narr, CC.Err, CC.Narr},
                        {CC.Err, CC.Narr, CC.Wide, CC.Err, CC.Wide, CC.Wide, CC.Wide, CC.Same, CC.Narr, CC.Err, CC.Wide, CC.Err, CC.Narr},
                        {CC.Err, CC.Narr, CC.Wide, CC.Err, CC.Wide, CC.Wide, CC.Wide, CC.Wide, CC.Same, CC.Err, CC.Wide, CC.Err, CC.Narr},
                        {CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Same, CC.Err, CC.Err, CC.Narr},
                        {CC.Err, CC.Narr, CC.Wide, CC.Err, CC.Wide, CC.Wide, CC.Wide, CC.Narr, CC.Narr, CC.Err, CC.Same, CC.Err, CC.Narr},
                        {CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err, CC.Err},
                        {CC.Err, CC.Narr, CC.Narr, CC.Wide, CC.Narr, CC.Narr, CC.Narr, CC.Narr, CC.Narr, CC.Narr, CC.Narr, CC.Err, CC.Same}
                    }

        Private Shared Function VTypeFromTypeCode(ByVal typ As TypeCode) As VType

            Select Case typ

                Case TypeCode.Boolean
                    Return VType.t_bool

                Case TypeCode.Byte
                    Return VType.t_ui1

                Case TypeCode.Int16
                    Return VType.t_i2

                Case TypeCode.Int32
                    Return VType.t_i4

                Case TypeCode.Int64
                    Return VType.t_i8

                Case TypeCode.Decimal
                    Return VType.t_dec

                Case TypeCode.Single
                    Return VType.t_r4

                Case TypeCode.Double
                    Return VType.t_r8

                Case TypeCode.Char
                    Return VType.t_char

                Case TypeCode.String
                    Return VType.t_str

                Case TypeCode.DateTime
                    Return VType.t_date

                Case Else
                    Return VType.t_bad

            End Select

        End Function

        '*** FUNCTION: VType2FromTypeCode 
        '***
        '*** Helper for indexing into ConversionClassTable
        '*** NOTE: Post RTM revisions should merge VType and VType2 usage 
        '***       into a single enum and remove use of WiderType table
        '***
        Private Shared Function VType2FromTypeCode(ByVal typ As TypeCode) As VType2

            Select Case typ

                Case TypeCode.Boolean
                    Return VType2.t_bool

                Case TypeCode.Byte
                    Return VType2.t_ui1

                Case TypeCode.Int16
                    Return VType2.t_i2

                Case TypeCode.Int32
                    Return VType2.t_i4

                Case TypeCode.Int64
                    Return VType2.t_i8

                Case TypeCode.Decimal
                    Return VType2.t_dec

                Case TypeCode.Single
                    Return VType2.t_r4

                Case TypeCode.Double
                    Return VType2.t_r8

                Case TypeCode.Char
                    Return VType2.t_char

                Case TypeCode.String
                    Return VType2.t_str

                Case TypeCode.DateTime
                    Return VType2.t_date

                Case Else
                    Return VType2.t_bad

            End Select

        End Function

        Private Shared Function TypeCodeFromVType(ByVal vartyp As VType) As TypeCode

            Select Case vartyp

                Case VType.t_bool
                    Return TypeCode.Boolean

                Case VType.t_ui1
                    Return TypeCode.Byte

                Case VType.t_i2
                    Return TypeCode.Int16

                Case VType.t_i4
                    Return TypeCode.Int32

                Case VType.t_i8
                    Return TypeCode.Int64

                Case VType.t_dec
                    Return TypeCode.Decimal

                Case VType.t_r4
                    Return TypeCode.Single

                Case VType.t_r8
                    Return TypeCode.Double

                Case VType.t_char
                    Return TypeCode.Char

                Case VType.t_str
                    Return TypeCode.String

                Case VType.t_date
                    Return TypeCode.DateTime

                Case Else
                    Return TypeCode.Object

            End Select

        End Function

        Friend Shared Function TypeFromTypeCode(ByVal vartyp As TypeCode) As Type

            Select Case vartyp

                Case TypeCode.Boolean
                    Return GetType(Boolean)

                Case TypeCode.Byte
                    Return GetType(Byte)

                Case TypeCode.Int16
                    Return GetType(Int16)

                Case TypeCode.Int32
                    Return GetType(Int32)

                Case TypeCode.Int64
                    Return GetType(Int64)

                Case TypeCode.Decimal
                    Return GetType(Decimal)

                Case TypeCode.Single
                    Return GetType(Single)

                Case TypeCode.Double
                    Return GetType(Double)

                Case TypeCode.Char
                    Return GetType(Char)

                Case TypeCode.String
                    Return GetType(String)

                Case TypeCode.DateTime
                    Return GetType(DateTime)

                Case TypeCode.SByte
                    Return GetType(System.SByte)

                Case TypeCode.UInt16
                    Return GetType(System.UInt16)

                Case TypeCode.UInt32
                    Return GetType(System.UInt32)

                Case TypeCode.UInt64
                    Return GetType(System.UInt64)

                Case TypeCode.Object
                    Return GetType(System.Object)

                Case TypeCode.DBNull
                    Return GetType(System.DBNull)

                Case Else
                    Return Nothing

            End Select

        End Function

        '*** Type1 - Type converting To
        '*** Type2 - Type converting From
        '
        Friend Shared Function IsWiderNumeric(ByVal Type1 As Type, ByVal Type2 As Type) As Boolean

            Dim TypeCode1, TypeCode2 As TypeCode

            TypeCode1 = System.Type.GetTypeCode(Type1)
            TypeCode2 = System.Type.GetTypeCode(Type2)

            ' We can't just return here if the two type codes are the same because one
            ' or both might be enums

            If IsOldNumericTypeCode(TypeCode1) AndAlso IsOldNumericTypeCode(TypeCode2) Then

                If TypeCode1 = TypeCode.Boolean OrElse TypeCode2 = TypeCode.Boolean Then
                    ' No conversion to or from a boolean is widening
                    Return False
                End If

                If Type1.IsEnum() Then
                    ' No conversion to an enum is widening
                    Return False
                End If

                ' Type2 can be an enum, because then we want to know if it's widening

                Return (WiderType(VTypeFromTypeCode(TypeCode1), VTypeFromTypeCode(TypeCode2)) = VTypeFromTypeCode(TypeCode1))
            End If
            Return False

        End Function

        Friend Shared Function IsWideningConversion(ByVal FromType As Type, ByVal ToType As Type) As Boolean
            Dim FromTypeCode, ToTypeCode As TypeCode

            Diagnostics.Debug.Assert(Not FromType Is ToType, "IsWideningConversion invalid for like types")

            FromTypeCode = System.Type.GetTypeCode(FromType)
            ToTypeCode = System.Type.GetTypeCode(ToType)

            If FromTypeCode = TypeCode.Object Then
                If FromType Is GetType(Char()) Then
                    If ToTypeCode = TypeCode.String OrElse ToType Is GetType(Char()) Then
                        Return True
                    End If
                End If

                If ToTypeCode = TypeCode.Object Then
                    If FromType.IsArray AndAlso ToType.IsArray Then
                        If FromType.GetArrayRank() = ToType.GetArrayRank() Then
                            Return ToType.GetElementType().IsAssignableFrom(FromType.GetElementType())
                        Else
                            Return False
                        End If
                    Else
                        Return ToType.IsAssignableFrom(FromType)
                    End If
                End If
                Return False
            End If

            If ToTypeCode = TypeCode.Object Then
                If ToType Is GetType(Char()) Then
                    If FromTypeCode = TypeCode.String Then
                        Return False
                    End If
                End If
                Return ToType.IsAssignableFrom(FromType)
            End If

            If ToType.IsEnum() Then
                ' No conversion to an enum is widening
                Return False
            End If

            Dim ConversionType As CC = ConversionClassTable(VType2FromTypeCode(ToTypeCode), VType2FromTypeCode(FromTypeCode))
            Return (ConversionType = CC.Wide OrElse ConversionType = CC.Same)

        End Function

        Friend Overloads Shared Function GetWidestType(ByVal obj1 As Object, ByVal obj2 As Object, Optional ByVal IsAdd As Boolean = False) As TypeCode
            Dim type1, type2 As TypeCode
            Dim conv1, conv2 As IConvertible

            conv1 = TryCast(obj1, IConvertible)
            conv2 = TryCast(obj2, IConvertible)

            If Not conv1 Is Nothing Then
                type1 = conv1.GetTypeCode()
            Else
                If obj1 Is Nothing Then
                    type1 = TypeCode.Empty
                ElseIf TypeOf obj1 Is Char() AndAlso CType(obj1, Array).Rank = 1 Then
                    type1 = TypeCode.String
                Else
                    type1 = TypeCode.Object
                End If
            End If

            If Not conv2 Is Nothing Then
                type2 = conv2.GetTypeCode()
            Else
                If obj2 Is Nothing Then
                    type2 = TypeCode.Empty
                ElseIf TypeOf obj2 Is Char() AndAlso CType(obj2, Array).Rank = 1 Then
                    type2 = TypeCode.String
                Else
                    type2 = TypeCode.Object
                End If
            End If

            If obj1 Is Nothing Then
                Return type2
            ElseIf obj2 Is Nothing Then
                Return type1
            Else
                ' If we do x + y and one of them is DBNull and one of them is String,
                ' then we convert DBNull to "" and do concatenation. We communicate this by passing
                ' back TypeCode.DBNull
                If IsAdd AndAlso
                       (((type1 = TypeCode.DBNull) AndAlso (type2 = TypeCode.String)) OrElse
                        ((type1 = TypeCode.String) AndAlso (type2 = TypeCode.DBNull))) Then
                    Return TypeCode.DBNull
                Else
                    Return TypeCodeFromVType(WiderType(VTypeFromTypeCode(type1), VTypeFromTypeCode(type2)))
                End If
            End If
        End Function

        Friend Overloads Shared Function GetWidestType(ByVal obj1 As Object, ByVal type2 As TypeCode) As TypeCode
            Dim type1 As TypeCode
            Dim conv1 As IConvertible

            conv1 = TryCast(obj1, IConvertible)

            If Not conv1 Is Nothing Then
                type1 = conv1.GetTypeCode()
            ElseIf obj1 Is Nothing Then
                type1 = TypeCode.Empty
            ElseIf TypeOf obj1 Is Char() AndAlso CType(obj1, Array).Rank = 1 Then
                type1 = TypeCode.String
            Else
                type1 = TypeCode.Object
            End If

            If obj1 Is Nothing Then
                Return type2
            Else
                Return TypeCodeFromVType(WiderType(VTypeFromTypeCode(type1), VTypeFromTypeCode(type2)))
            End If

        End Function

        Public Shared Function ObjTst(ByVal o1 As Object, ByVal o2 As Object, ByVal TextCompare As Boolean) As Integer
            Dim conv1, conv2 As IConvertible
            Dim tc1, tc2 As TypeCode

            conv1 = TryCast(o1, IConvertible)

            If conv1 Is Nothing Then
                If o1 Is Nothing Then
                    tc1 = TypeCode.Empty
                Else
                    tc1 = TypeCode.Object
                End If
            Else
                tc1 = conv1.GetTypeCode()
            End If

            conv2 = TryCast(o2, IConvertible)

            If conv2 Is Nothing Then
                If o2 Is Nothing Then
                    tc2 = TypeCode.Empty
                Else
                    tc2 = TypeCode.Object
                End If
            Else
                tc2 = conv2.GetTypeCode()
            End If

            'Special cases for Char()
            If (tc1 = TypeCode.Object) AndAlso (TypeOf o1 Is Char()) Then
                If tc2 = TypeCode.String OrElse tc2 = TypeCode.Empty OrElse ((tc2 = TypeCode.Object) AndAlso (TypeOf o2 Is Char())) Then
                    'Treat Char() as String for these cases
                    o1 = CStr(CharArrayType.FromObject(o1))
                    conv1 = CType(o1, IConvertible)
                    tc1 = TypeCode.String
                End If
            End If

            If (tc2 = TypeCode.Object) AndAlso (TypeOf o2 Is Char()) Then
                If tc1 = TypeCode.String OrElse tc1 = TypeCode.Empty Then
                    o2 = CStr(CharArrayType.FromObject(o2))
                    conv2 = DirectCast(o2, IConvertible)
                    tc2 = TypeCode.String
                End If
            End If

            Select Case tc1 * TCMAX + tc2

                Case TypeCode.Empty * TCMAX + TypeCode.String
                    Return ObjTstStringString(Nothing, o2.ToString(), TextCompare)

                Case TypeCode.String * TCMAX + TypeCode.Empty
                    Return ObjTstStringString(o1.ToString(), Nothing, TextCompare)

                Case TypeCode.Empty * TCMAX + TypeCode.Empty
                    Return CInt(0)

                Case TypeCode.Byte * TCMAX + TypeCode.Empty
                    Return ObjTstByte(conv1.ToByte(Nothing), 0)

                Case TypeCode.Empty * TCMAX + TypeCode.Byte
                    Return ObjTstByte(0, conv2.ToByte(Nothing))

                Case TypeCode.Boolean * TCMAX + TypeCode.Empty
                    Return ObjTstInt32(ToVBBool(conv1), 0)

                Case TypeCode.Empty * TCMAX + TypeCode.Boolean
                    Return ObjTstInt32(0, ToVBBool(conv2))

                Case TypeCode.Int16 * TCMAX + TypeCode.Empty
                    Return ObjTstInt16(conv1.ToInt16(Nothing), 0)

                Case TypeCode.Empty * TCMAX + TypeCode.Int16
                    Return ObjTstInt16(0, conv2.ToInt16(Nothing))

                Case TypeCode.Int32 * TCMAX + TypeCode.Empty
                    Return ObjTstInt32(conv1.ToInt32(Nothing), 0)

                Case TypeCode.Empty * TCMAX + TypeCode.Int32
                    Return ObjTstInt32(0, conv2.ToInt32(Nothing))

                Case TypeCode.Int64 * TCMAX + TypeCode.Empty
                    Return ObjTstInt64(conv1.ToInt64(Nothing), 0)

                Case TypeCode.Empty * TCMAX + TypeCode.Int64
                    Return ObjTstInt64(0, conv2.ToInt64(Nothing))

                Case TypeCode.Single * TCMAX + TypeCode.Empty
                    Return ObjTstSingle(conv1.ToSingle(Nothing), 0)

                Case TypeCode.Empty * TCMAX + TypeCode.Single
                    Return ObjTstSingle(0, conv2.ToSingle(Nothing))

                Case TypeCode.Double * TCMAX + TypeCode.Empty
                    Return ObjTstDouble(conv1.ToDouble(Nothing), 0)

                Case TypeCode.Empty * TCMAX + TypeCode.Double
                    Return ObjTstDouble(0, conv2.ToDouble(Nothing))

                Case TypeCode.Decimal * TCMAX + TypeCode.Empty
                    Return ObjTstDecimal(conv1, 0)

                Case TypeCode.Empty * TCMAX + TypeCode.Decimal
                    Return ObjTstDecimal(0, conv2)

                Case TypeCode.Char * TCMAX + TypeCode.Empty
                    Return ObjTstChar(conv1.ToChar(Nothing), ChrW(0))

                Case TypeCode.Empty * TCMAX + TypeCode.Char
                    Return ObjTstChar(ChrW(0), conv2.ToChar(Nothing))

                Case TypeCode.DateTime * TCMAX + TypeCode.Empty
                    Return ObjTstDateTime(conv1.ToDateTime(Nothing), DateType.FromObject(Nothing))

                Case TypeCode.Empty * TCMAX + TypeCode.DateTime
                    Return ObjTstDateTime(DateType.FromObject(Nothing), conv2.ToDateTime(Nothing))

                Case TypeCode.Decimal * TCMAX + TypeCode.Byte,
                    TypeCode.Decimal * TCMAX + TypeCode.Int16,
                    TypeCode.Decimal * TCMAX + TypeCode.Int32,
                    TypeCode.Decimal * TCMAX + TypeCode.Int64,
                    TypeCode.Decimal * TCMAX + TypeCode.Decimal,
                    TypeCode.Int16 * TCMAX + TypeCode.Decimal,
                    TypeCode.Byte * TCMAX + TypeCode.Decimal,
                    TypeCode.Int32 * TCMAX + TypeCode.Decimal,
                    TypeCode.Int64 * TCMAX + TypeCode.Decimal
                    Return ObjTstDecimal(conv1, conv2)

                Case TypeCode.Boolean * TCMAX + TypeCode.Decimal
                    Return ObjTstDecimal(ToVBBool(conv1), conv2)

                Case TypeCode.Decimal * TCMAX + TypeCode.Boolean
                    Return ObjTstDecimal(conv1, ToVBBool(conv2))

                Case TypeCode.String * TCMAX + TypeCode.Byte,
                    TypeCode.String * TCMAX + TypeCode.Int16,
                    TypeCode.String * TCMAX + TypeCode.Int32,
                    TypeCode.String * TCMAX + TypeCode.Int64,
                    TypeCode.String * TCMAX + TypeCode.Single,
                    TypeCode.String * TCMAX + TypeCode.Double,
                    TypeCode.String * TCMAX + TypeCode.Decimal
                    Return ObjTstString(conv1, tc1, conv2, tc2)

                Case TypeCode.Byte * TCMAX + TypeCode.String,
                    TypeCode.Int16 * TCMAX + TypeCode.String,
                    TypeCode.Int32 * TCMAX + TypeCode.String,
                    TypeCode.Int64 * TCMAX + TypeCode.String,
                    TypeCode.Single * TCMAX + TypeCode.String,
                    TypeCode.Double * TCMAX + TypeCode.String,
                    TypeCode.Decimal * TCMAX + TypeCode.String
                    Return ObjTstString(conv1, tc1, conv2, tc2)

                Case TypeCode.Empty * TCMAX + TypeCode.DateTime,
                     TypeCode.DateTime * TCMAX + TypeCode.Empty
                    Return ObjTstDateTime(CDate(conv1.ToDateTime(Nothing)), conv2.ToDateTime(Nothing))

                Case TypeCode.DateTime * TCMAX + TypeCode.DateTime
                    Return ObjTstDateTime(CDate(conv1.ToDateTime(Nothing)), conv2.ToDateTime(Nothing))

                Case TypeCode.String * TCMAX + TypeCode.DateTime
                    Return ObjTstDateTime(DateType.FromString(conv1.ToString(Nothing), GetCultureInfo()), conv2.ToDateTime(Nothing))

                Case TypeCode.DateTime * TCMAX + TypeCode.String
                    Return ObjTstDateTime(conv1.ToDateTime(Nothing), DateType.FromString(conv2.ToString(Nothing), GetCultureInfo()))

                Case TypeCode.String * TCMAX + TypeCode.String
                    Return ObjTstStringString(conv1.ToString(Nothing), conv2.ToString(Nothing), TextCompare)

                Case TypeCode.Boolean * TCMAX + TypeCode.String
                    Return ObjTstBoolean(conv1.ToBoolean(Nothing), BooleanType.FromString(conv2.ToString(Nothing)))

                Case TypeCode.String * TCMAX + TypeCode.Boolean
                    Return ObjTstBoolean(BooleanType.FromString(conv1.ToString(Nothing)), conv2.ToBoolean(Nothing))

                Case TypeCode.String * TCMAX + TypeCode.Char,
                     TypeCode.Char * TCMAX + TypeCode.String
                    Return ObjTstStringString(conv1.ToString(Nothing), conv2.ToString(Nothing), TextCompare)

                Case TypeCode.Double * TCMAX + TypeCode.Byte,
                    TypeCode.Double * TCMAX + TypeCode.Int16,
                    TypeCode.Double * TCMAX + TypeCode.Int32,
                    TypeCode.Double * TCMAX + TypeCode.Int64,
                    TypeCode.Double * TCMAX + TypeCode.Single,
                    TypeCode.Double * TCMAX + TypeCode.Double,
                    TypeCode.Byte * TCMAX + TypeCode.Double,
                    TypeCode.Int16 * TCMAX + TypeCode.Double,
                    TypeCode.Int32 * TCMAX + TypeCode.Double,
                    TypeCode.Int64 * TCMAX + TypeCode.Double,
                    TypeCode.Single * TCMAX + TypeCode.Double,
                    TypeCode.Double * TCMAX + TypeCode.Decimal,
                    TypeCode.Decimal * TCMAX + TypeCode.Double
                    Return ObjTstDouble(conv1.ToDouble(Nothing), conv2.ToDouble(Nothing))

                Case TypeCode.Double * TCMAX + TypeCode.Boolean
                    Return ObjTstDouble(conv1.ToDouble(Nothing), ToVBBool(conv2))

                Case TypeCode.Boolean * TCMAX + TypeCode.Double
                    Return ObjTstDouble(ToVBBool(conv1), conv2.ToDouble(Nothing))

                Case TypeCode.Single * TCMAX + TypeCode.Byte,
                    TypeCode.Single * TCMAX + TypeCode.Int16,
                    TypeCode.Single * TCMAX + TypeCode.Int32,
                    TypeCode.Single * TCMAX + TypeCode.Int64,
                    TypeCode.Single * TCMAX + TypeCode.Single,
                    TypeCode.Byte * TCMAX + TypeCode.Single,
                    TypeCode.Int16 * TCMAX + TypeCode.Single,
                    TypeCode.Int32 * TCMAX + TypeCode.Single,
                    TypeCode.Int64 * TCMAX + TypeCode.Single,
                    TypeCode.Decimal * TCMAX + TypeCode.Single,
                    TypeCode.Single * TCMAX + TypeCode.Decimal
                    Return ObjTstSingle(conv1.ToSingle(Nothing), conv2.ToSingle(Nothing))

                Case TypeCode.Single * TCMAX + TypeCode.Boolean
                    Return ObjTstSingle(conv1.ToSingle(Nothing), ToVBBool(conv2))

                Case TypeCode.Boolean * TCMAX + TypeCode.Single
                    Return ObjTstSingle(ToVBBool(conv1), conv2.ToSingle(Nothing))

                Case TypeCode.Byte * TCMAX + TypeCode.Int64,
                    TypeCode.Int64 * TCMAX + TypeCode.Byte,
                    TypeCode.Int64 * TCMAX + TypeCode.Int16,
                    TypeCode.Int64 * TCMAX + TypeCode.Int32,
                    TypeCode.Int64 * TCMAX + TypeCode.Int64,
                     TypeCode.Int16 * TCMAX + TypeCode.Int64,
                    TypeCode.Int32 * TCMAX + TypeCode.Int64
                    Return ObjTstInt64(conv1.ToInt64(Nothing), conv2.ToInt64(Nothing))

                Case TypeCode.Int64 * TCMAX + TypeCode.Boolean
                    Return ObjTstInt64(conv1.ToInt64(Nothing), ToVBBool(conv2))

                Case TypeCode.Boolean * TCMAX + TypeCode.Int64
                    Return ObjTstInt64(ToVBBool(conv1), conv2.ToInt64(Nothing))

                Case TypeCode.Int16 * TCMAX + TypeCode.Int32,
                    TypeCode.Int32 * TCMAX + TypeCode.Int16,
                    TypeCode.Int32 * TCMAX + TypeCode.Int32,
                    TypeCode.Int32 * TCMAX + TypeCode.Byte,
                    TypeCode.Byte * TCMAX + TypeCode.Int32
                    Return ObjTstInt32(conv1.ToInt32(Nothing), conv2.ToInt32(Nothing))

                Case TypeCode.Int32 * TCMAX + TypeCode.Boolean
                    Return ObjTstInt32(conv1.ToInt32(Nothing), ToVBBool(conv2))

                Case TypeCode.Boolean * TCMAX + TypeCode.Int32
                    Return ObjTstInt32(ToVBBool(conv1), conv2.ToInt32(Nothing))

                Case TypeCode.Int16 * TCMAX + TypeCode.Byte,
                    TypeCode.Int16 * TCMAX + TypeCode.Int16,
                    TypeCode.Byte * TCMAX + TypeCode.Int16
                    Return ObjTstInt16(conv1.ToInt16(Nothing), conv2.ToInt16(Nothing))

                Case TypeCode.Boolean * TCMAX + TypeCode.Byte,
                    TypeCode.Boolean * TCMAX + TypeCode.Int16
                    Return ObjTstInt16(CShort(ToVBBool(conv1)), conv2.ToInt16(Nothing))

                Case TypeCode.Byte * TCMAX + TypeCode.Boolean,
                    TypeCode.Int16 * TCMAX + TypeCode.Boolean
                    Return ObjTstInt16(conv1.ToInt16(Nothing), CShort(ToVBBool(conv2)))

                Case TypeCode.Boolean * TCMAX + TypeCode.Boolean
                    Return ObjTstInt16(CShort(ToVBBool(conv1)), CShort(ToVBBool(conv2)))

                Case TypeCode.Byte * TCMAX + TypeCode.Byte
                    Return ObjTstByte(conv1.ToByte(Nothing), conv2.ToByte(Nothing))

                Case TypeCode.Char * TCMAX + TypeCode.Char
                    Return ObjTstChar(conv1.ToChar(Nothing), conv2.ToChar(Nothing))

                Case Else

            End Select

            Throw GetNoValidOperatorException(o1, o2)

        End Function

        Private Shared Function ObjTstDateTime(ByVal var1 As DateTime, ByVal var2 As DateTime) As Integer

            Dim ticks1, ticks2 As Int64

            ticks1 = var1.Ticks
            ticks2 = var2.Ticks

            If ticks1 < ticks2 Then
                Return -1
            ElseIf ticks1 > ticks2 Then
                Return 1
            End If
            Return 0
        End Function

        Private Shared Function ObjTstBoolean(ByVal b1 As Boolean, ByVal b2 As Boolean) As Integer
            If b1 = b2 Then
                Return 0
            ElseIf b1 > b2 Then
                Return 1
            Else
                Return -1
            End If
        End Function

        Private Shared Function ObjTstDouble(ByVal d1 As Double, ByVal d2 As Double) As Integer
            If d1 < d2 Then
                Return -1
            ElseIf d1 > d2 Then
                Return 1
            End If
            Return 0
        End Function

        Private Shared Function ObjTstChar(ByVal ch1 As Char, ByVal ch2 As Char) As Integer
            If ch1 < ch2 Then
                Return -1
            ElseIf ch1 > ch2 Then
                Return 1
            End If
            Return 0
        End Function

        Private Shared Function ObjTstByte(ByVal by1 As Byte, ByVal by2 As Byte) As Integer
            If by1 < by2 Then
                Return -1
            ElseIf by1 > by2 Then
                Return 1
            End If
            Return 0
        End Function

        Private Shared Function ObjTstSingle(ByVal d1 As Single, ByVal d2 As Single) As Integer
            If d1 < d2 Then
                Return -1
            ElseIf d1 > d2 Then
                Return 1
            End If
            Return 0
        End Function

        Private Shared Function ObjTstInt16(ByVal d1 As Int16, ByVal d2 As Int16) As Integer
            If d1 < d2 Then
                Return -1
            ElseIf d1 > d2 Then
                Return 1
            End If
            Return 0
        End Function

        Private Shared Function ObjTstInt32(ByVal d1 As Int32, ByVal d2 As Int32) As Integer
            If d1 < d2 Then
                Return -1
            ElseIf d1 > d2 Then
                Return 1
            End If
            Return 0
        End Function

        Private Shared Function ObjTstInt64(ByVal d1 As Int64, ByVal d2 As Int64) As Integer
            If d1 < d2 Then
                Return -1
            ElseIf d1 > d2 Then
                Return 1
            End If
            Return 0
        End Function

        'This function takes IConvertible because the JIT does not behave properly with Decimal temps
        Private Shared Function ObjTstDecimal(ByVal i1 As IConvertible, ByVal i2 As IConvertible) As Integer
            Dim d1, d2 As Decimal
            d1 = i1.ToDecimal(Nothing)
            d2 = i2.ToDecimal(Nothing)
            If d1 < d2 Then
                Return -1
            ElseIf d1 > d2 Then
                Return 1
            End If
            Return 0
        End Function

        Private Shared Function ObjTstString(ByVal conv1 As IConvertible, ByVal tc1 As TypeCode, ByVal conv2 As IConvertible, ByVal tc2 As TypeCode) As Integer
            Dim dbl1, dbl2 As Double

            If tc1 = TypeCode.String Then
                dbl1 = DoubleType.FromString(conv1.ToString(Nothing))
            ElseIf tc1 = TypeCode.Boolean Then
                dbl1 = ToVBBool(conv1)
            Else
                dbl1 = conv1.ToDouble(Nothing)
            End If

            If tc2 = TypeCode.String Then
                dbl2 = DoubleType.FromString(conv2.ToString(Nothing))
            ElseIf tc2 = TypeCode.Boolean Then
                dbl2 = ToVBBool(conv2)
            Else
                dbl2 = conv2.ToDouble(Nothing)
            End If

            Return ObjTstDouble(dbl1, dbl2)
        End Function

        Private Shared Function ObjTstStringString(ByVal s1 As String, ByVal s2 As String, ByVal TextCompare As Boolean) As Integer

            If s1 Is Nothing Then
                If s2.Length() > 0 Then
                    Return -1
                Else
                    Return 0
                End If
            ElseIf s2 Is Nothing Then
                If s1.Length() > 0 Then
                    Return 1
                Else
                    Return 0
                End If
            Else
                If TextCompare Then
                    Return GetCultureInfo().CompareInfo.Compare(s1, s2, OptionCompareTextFlags)
                Else
                    Return System.String.CompareOrdinal(s1, s2)
                End If
            End If

        End Function

        ' Plus ( +x )
        Public Shared Function PlusObj(ByVal obj As Object) As Object

            If obj Is Nothing Then
                Return +0I
            End If

            Dim conv As IConvertible
            Dim typ As TypeCode

            conv = TryCast(obj, IConvertible)

            If conv Is Nothing Then
                If obj Is Nothing Then
                    typ = TypeCode.Empty
                Else
                    typ = TypeCode.Object
                End If
            Else
                typ = conv.GetTypeCode()
            End If

            Select Case typ

                Case TypeCode.Boolean
                    If TypeOf obj Is Boolean Then
                        Return CShort(DirectCast(obj, Boolean))
                    Else
                        Return CShort(conv.ToBoolean(Nothing))
                    End If

                Case TypeCode.Byte,
                     TypeCode.Int16,
                     TypeCode.Int32,
                     TypeCode.Int64,
                     TypeCode.Decimal,
                     TypeCode.Single,
                     TypeCode.Double
                    Return obj

                Case TypeCode.String
                    Return DoubleType.FromObject(obj)

                Case TypeCode.Empty
                    Return CInt(0)

                Case TypeCode.Char
                    ' Fall through to error

                Case TypeCode.DateTime
                    ' Fall through to error

                Case Else
                    ' Fall through to error
            End Select

            Throw GetNoValidOperatorException(obj)
        End Function

        ' Negation ( -x )
        Public Shared Function NegObj(ByVal obj As Object) As Object

            Dim conv As IConvertible
            Dim tc As TypeCode

            conv = TryCast(obj, IConvertible)

            If conv Is Nothing Then
                If obj Is Nothing Then
                    tc = TypeCode.Empty
                Else
                    tc = TypeCode.Object
                End If
            Else
                tc = conv.GetTypeCode()
            End If

            Return InternalNegObj(obj, conv, tc)

        End Function

        Private Shared Function InternalNegObj(ByVal obj As Object, ByVal conv As IConvertible, ByVal tc As TypeCode) As Object

            Dim Int16Result As Int16
            Dim Int32Result As Int32
            Dim Int64Result As Int64
            Dim DecimalResult As Decimal
            Dim DoubleResult As Double

            Select Case tc

                Case TypeCode.Empty
                    Return -0I

                Case TypeCode.Boolean
                    If TypeOf obj Is Boolean Then
                        Int16Result = -CShort(DirectCast(obj, Boolean))
                    Else
                        Int16Result = -CShort(conv.ToBoolean(Nothing))
                    End If
                    GoTo Int16Exit

                Case TypeCode.Byte
                    If TypeOf obj Is Byte Then
                        Int16Result = -CType(DirectCast(obj, Byte), Int16)
                    Else
                        Int16Result = -CType(conv.ToByte(Nothing), Int16)
                    End If
                    GoTo Int16Exit

                Case TypeCode.Int16
                    If TypeOf obj Is Int16 Then
                        Int32Result = -CType(DirectCast(obj, Int16), Int32)
                    Else
                        Int32Result = -CType(conv.ToInt16(Nothing), Int32)
                    End If
                    GoTo Int32Int16Exit

                Case TypeCode.Int32
                    If TypeOf obj Is Int32 Then
                        Int64Result = -CType(DirectCast(obj, Int32), Int64)
                    Else
                        Int64Result = -CType(conv.ToInt32(Nothing), Int64)
                    End If
                    GoTo Int64Int32Exit

                Case TypeCode.Int64
                    'Using try/catch instead of check with MinValue
                    ' since the overflow case should be very rare
                    ' and a compare would be a big cost for the normal case
                    Try
                        If TypeOf obj Is Int64 Then
                            Int64Result = -DirectCast(obj, Int64)
                        Else
                            Int64Result = -conv.ToInt64(Nothing)
                        End If
                        GoTo Int64Exit
                    Catch ex As StackOverflowException
                        Throw ex
                    Catch ex As OutOfMemoryException
                        Throw ex
                    Catch ex As System.Threading.ThreadAbortException
                        Throw ex
                    Catch
                        DecimalResult = -conv.ToDecimal(Nothing)
                        GoTo DecimalExit
                    End Try

                Case TypeCode.Decimal
                    'Using try/catch instead of check with MinValue
                    ' since the overflow case should be very rare
                    ' and a compare would be a big cost for the normal case
                    Try
                        If TypeOf obj Is Decimal Then
                            DecimalResult = -DirectCast(obj, Decimal)
                        Else
                            DecimalResult = -conv.ToDecimal(Nothing)
                        End If
                        Return DecimalResult
                    Catch ex As StackOverflowException
                        Throw ex
                    Catch ex As OutOfMemoryException
                        Throw ex
                    Catch ex As System.Threading.ThreadAbortException
                        Throw ex
                    Catch
                        DoubleResult = -conv.ToDouble(Nothing)
                        GoTo DoubleExit
                    End Try

                Case TypeCode.Single
                    If TypeOf obj Is Single Then
                        Return -DirectCast(obj, Single)
                    Else
                        Return -conv.ToSingle(Nothing)
                    End If

                Case TypeCode.Double
                    If TypeOf obj Is Double Then
                        DoubleResult = -DirectCast(obj, Double)
                    Else
                        DoubleResult = -conv.ToDouble(Nothing)
                    End If

                    GoTo DoubleExit

                Case TypeCode.String
                    Dim ObjString As String = TryCast(obj, String)

                    If ObjString IsNot Nothing Then
                        DoubleResult = -DoubleType.FromString(ObjString)
                    Else
                        DoubleResult = -DoubleType.FromString(conv.ToString(Nothing))
                    End If
                    GoTo DoubleExit

                Case TypeCode.Char
                    ' Fall through to error

                Case TypeCode.DateTime
                    ' Fall through to error

                Case Else
                    ' Fall through to error

            End Select

            Throw GetNoValidOperatorException(obj)

            Exit Function

Int16ByteExit:
            '- Byte can only be zero or negative, so the OrElse is unnecessary
            Diagnostics.Debug.Assert(Int16Result <= 0, "Invalid result")
            If Int16Result < System.Byte.MinValue Then 'OrElse Int16Result > System.Byte.MaxValue Then
                Return Int16Result
            End If
            Return CType(Int16Result, Byte)

Int32Int16Exit:
            If Int32Result < System.Int16.MinValue OrElse Int32Result > System.Int16.MaxValue Then
                Return Int32Result
            End If
            Return CType(Int32Result, Int16)

Int64Int32Exit:
            If Int64Result < System.Int32.MinValue OrElse Int64Result > System.Int32.MaxValue Then
                Return Int64Result
            End If
            Return CType(Int64Result, Int32)

Int16Exit:
            Return Int16Result

Int32Exit:
            Return Int32Result

Int64Exit:
            Return Int64Result

DoubleExit:
            Return DoubleResult

DecimalExit:
            Return DecimalResult
        End Function

        ' NotObj performs a BitNot or Not, depending on the contained type
        ' Binary Not (BitNot x)
        ' Logical not (Not x)
        Public Shared Function NotObj(ByVal obj As Object) As Object

            Dim byteValue As Byte
            Dim int16Value As Int16
            Dim int32Value As Int32
            Dim int64Value As Int64
            Dim Type1 As Type
            Dim iconv As IConvertible
            Dim TypeCode1 As TypeCode

            If obj Is Nothing Then
                Return (Not 0I)
            End If

            iconv = TryCast(obj, IConvertible)

            If Not iconv Is Nothing Then
                TypeCode1 = iconv.GetTypeCode()
            Else
                TypeCode1 = TypeCode.Object
            End If

            Select Case TypeCode1

                Case TypeCode.Boolean
                    Return Not iconv.ToBoolean(Nothing)

                Case TypeCode.Byte
                    Type1 = obj.GetType()
                    byteValue = Not iconv.ToByte(Nothing)
                    If Type1.IsEnum Then
                        Return System.Enum.ToObject(Type1, byteValue)
                    End If
                    Return byteValue

                Case TypeCode.Int16
                    Type1 = obj.GetType()
                    int16Value = Not iconv.ToInt16(Nothing)
                    If Type1.IsEnum Then
                        Return System.Enum.ToObject(Type1, int16Value)
                    End If
                    Return int16Value

                Case TypeCode.Int32
                    Type1 = obj.GetType()
                    int32Value = Not iconv.ToInt32(Nothing)
                    If Type1.IsEnum Then
                        Return System.Enum.ToObject(Type1, int32Value)
                    End If
                    Return int32Value

                Case TypeCode.Int64
                    Type1 = obj.GetType()
                    int64Value = Not iconv.ToInt64(Nothing)
                    If Type1.IsEnum Then
                        Return System.Enum.ToObject(Type1, int64Value)
                    End If
                    Return int64Value

                Case TypeCode.Decimal
                    Return Not CType(iconv.ToDecimal(Nothing), Int64)

                Case TypeCode.Single
                    Return Not CType(iconv.ToDecimal(Nothing), Int64)

                Case TypeCode.Double
                    Return Not CType(iconv.ToDecimal(Nothing), Int64)

                Case TypeCode.String
                    Return Not LongType.FromString(iconv.ToString(Nothing))

                Case TypeCode.Char
                    ' Fall through to error

                Case TypeCode.DateTime
                    ' Fall through to error

                Case Else
                    ' Fall through to error

            End Select

            Throw GetNoValidOperatorException(obj)

        End Function

        ' Binary And (BitAnd x)
        Public Shared Function BitAndObj(ByVal obj1 As Object, ByVal obj2 As Object) As Object

            If obj1 Is Nothing AndAlso obj2 Is Nothing Then
                Return 0I
            End If

            Dim Type1 As Type = Nothing
            Dim Type2 As Type = Nothing

            Dim Type1IsEnum, Type2IsEnum As Boolean

            If Not obj1 Is Nothing Then
                Type1 = obj1.GetType()
                Type1IsEnum = Type1.IsEnum()
            End If
            If Not obj2 Is Nothing Then
                Type2 = obj2.GetType()
                Type2IsEnum = Type2.IsEnum()
            End If

            Select Case GetWidestType(obj1, obj2)

                Case TypeCode.Boolean
                    If Type1 Is Type2 Then
                        'Both Boolean
                        Return BooleanType.FromObject(obj1) And BooleanType.FromObject(obj2)
                    Else
                        Return ShortType.FromObject(obj1) And ShortType.FromObject(obj2)
                    End If

                Case TypeCode.Byte
                    Dim Result As Byte = ByteType.FromObject(obj1) And ByteType.FromObject(obj2)

                    If ((Type1IsEnum AndAlso Type2IsEnum) AndAlso (Not (Type1 Is Type2))) OrElse
                        (Not (Type1IsEnum AndAlso Type2IsEnum)) Then
                        Return Result
                    ElseIf Type1IsEnum Then
                        Return System.Enum.ToObject(Type1, Result)
                    ElseIf Type2IsEnum Then
                        Return System.Enum.ToObject(Type2, Result)
                    End If
                Case TypeCode.Int16
                    Dim Result As Short = ShortType.FromObject(obj1) And ShortType.FromObject(obj2)

                    If ((Type1IsEnum AndAlso Type2IsEnum) AndAlso (Not (Type1 Is Type2))) OrElse
                        (Not (Type1IsEnum AndAlso Type2IsEnum)) Then
                        Return Result
                    ElseIf Type1IsEnum Then
                        Return System.Enum.ToObject(Type1, Result)
                    ElseIf Type2IsEnum Then
                        Return System.Enum.ToObject(Type2, Result)
                    End If
                Case TypeCode.Int32
                    Dim Result As Integer = IntegerType.FromObject(obj1) And IntegerType.FromObject(obj2)

                    If ((Type1IsEnum AndAlso Type2IsEnum) AndAlso (Not (Type1 Is Type2))) OrElse
                        (Not (Type1IsEnum AndAlso Type2IsEnum)) Then
                        Return Result
                    ElseIf Type1IsEnum Then
                        Return System.Enum.ToObject(Type1, Result)
                    ElseIf Type2IsEnum Then
                        Return System.Enum.ToObject(Type2, Result)
                    End If
                Case TypeCode.Int64
                    Dim Result As Long = LongType.FromObject(obj1) And LongType.FromObject(obj2)

                    If ((Type1IsEnum AndAlso Type2IsEnum) AndAlso (Not (Type1 Is Type2))) OrElse
                        (Not (Type1IsEnum AndAlso Type2IsEnum)) Then
                        Return Result
                    ElseIf Type1IsEnum Then
                        Return System.Enum.ToObject(Type1, Result)
                    ElseIf Type2IsEnum Then
                        Return System.Enum.ToObject(Type2, Result)
                    End If

                Case TypeCode.Decimal,
                     TypeCode.Single,
                     TypeCode.Double,
                     TypeCode.String
                    Return LongType.FromObject(obj1) And LongType.FromObject(obj2)

                Case TypeCode.Char
                    ' Fall through to error

                Case TypeCode.DateTime
                    ' Fall through to error

                Case Else
                    ' Fall through to error

            End Select

            Throw GetNoValidOperatorException(obj1, obj2)

        End Function

        ' Binary OR (BitOr x)
        Public Shared Function BitOrObj(ByVal obj1 As Object, ByVal obj2 As Object) As Object

            If obj1 Is Nothing AndAlso obj2 Is Nothing Then
                Return 0I
            End If

            Dim Type1 As Type = Nothing
            Dim Type2 As Type = Nothing

            Dim Type1IsEnum, Type2IsEnum As Boolean

            If Not obj1 Is Nothing Then
                Type1 = obj1.GetType()
                Type1IsEnum = Type1.IsEnum()
            End If
            If Not obj2 Is Nothing Then
                Type2 = obj2.GetType()
                Type2IsEnum = Type2.IsEnum()
            End If

            Select Case GetWidestType(obj1, obj2)

                Case TypeCode.Boolean
                    If Type1 Is Type2 Then
                        'Both Boolean
                        Return BooleanType.FromObject(obj1) Or BooleanType.FromObject(obj2)
                    Else
                        Return ShortType.FromObject(obj1) Or ShortType.FromObject(obj2)
                    End If

                Case TypeCode.Byte
                    Dim Result As Byte = ByteType.FromObject(obj1) Or ByteType.FromObject(obj2)

                    If ((Type1IsEnum AndAlso Type2IsEnum) AndAlso (Not (Type1 Is Type2))) OrElse
                        (Not (Type1IsEnum AndAlso Type2IsEnum)) Then
                        Return Result
                    ElseIf Type1IsEnum Then
                        Return System.Enum.ToObject(Type1, Result)
                    ElseIf Type2IsEnum Then
                        Return System.Enum.ToObject(Type2, Result)
                    End If
                Case TypeCode.Int16
                    Dim Result As Short = ShortType.FromObject(obj1) Or ShortType.FromObject(obj2)

                    If ((Type1IsEnum AndAlso Type2IsEnum) AndAlso (Not (Type1 Is Type2))) OrElse
                        (Not (Type1IsEnum AndAlso Type2IsEnum)) Then
                        Return Result
                    ElseIf Type1IsEnum Then
                        Return System.Enum.ToObject(Type1, Result)
                    ElseIf Type2IsEnum Then
                        Return System.Enum.ToObject(Type2, Result)
                    End If
                Case TypeCode.Int32
                    Dim Result As Integer = IntegerType.FromObject(obj1) Or IntegerType.FromObject(obj2)

                    If ((Type1IsEnum AndAlso Type2IsEnum) AndAlso (Not (Type1 Is Type2))) OrElse
                        (Not (Type1IsEnum AndAlso Type2IsEnum)) Then
                        Return Result
                    ElseIf Type1IsEnum Then
                        Return System.Enum.ToObject(Type1, Result)
                    ElseIf Type2IsEnum Then
                        Return System.Enum.ToObject(Type2, Result)
                    End If
                Case TypeCode.Int64
                    Dim Result As Long = LongType.FromObject(obj1) Or LongType.FromObject(obj2)

                    If ((Type1IsEnum AndAlso Type2IsEnum) AndAlso (Not (Type1 Is Type2))) OrElse
                        (Not (Type1IsEnum AndAlso Type2IsEnum)) Then
                        Return Result
                    ElseIf Type1IsEnum Then
                        Return System.Enum.ToObject(Type1, Result)
                    ElseIf Type2IsEnum Then
                        Return System.Enum.ToObject(Type2, Result)
                    End If

                Case TypeCode.Decimal,
                     TypeCode.Single,
                     TypeCode.Double,
                     TypeCode.String
                    Return LongType.FromObject(obj1) Or LongType.FromObject(obj2)

                Case TypeCode.Char
                    ' Fall through to error

                Case TypeCode.DateTime
                    ' Fall through to error

                Case Else
                    ' Fall through to error

            End Select

            Throw GetNoValidOperatorException(obj1, obj2)

        End Function

        ' Binary Xor (BitXor x)
        Public Shared Function BitXorObj(ByVal obj1 As Object, ByVal obj2 As Object) As Object

            If obj1 Is Nothing AndAlso obj2 Is Nothing Then
                Return 0I
            End If

            Dim Type1 As Type = Nothing
            Dim Type2 As Type = Nothing

            Dim Type1IsEnum, Type2IsEnum As Boolean

            If Not obj1 Is Nothing Then
                Type1 = obj1.GetType()
                Type1IsEnum = Type1.IsEnum()
            End If
            If Not obj2 Is Nothing Then
                Type2 = obj2.GetType()
                Type2IsEnum = Type2.IsEnum()
            End If

            Select Case GetWidestType(obj1, obj2)

                Case TypeCode.Boolean
                    If Type1 Is Type2 Then
                        'Both Boolean
                        Return BooleanType.FromObject(obj1) Xor BooleanType.FromObject(obj2)
                    Else
                        Return ShortType.FromObject(obj1) Xor ShortType.FromObject(obj2)
                    End If

                Case TypeCode.Byte
                    Dim Result As Byte = ByteType.FromObject(obj1) Xor ByteType.FromObject(obj2)

                    If ((Type1IsEnum AndAlso Type2IsEnum) AndAlso (Not (Type1 Is Type2))) OrElse
                        (Not (Type1IsEnum AndAlso Type2IsEnum)) Then
                        Return Result
                    ElseIf Type1IsEnum Then
                        Return System.Enum.ToObject(Type1, Result)
                    ElseIf Type2IsEnum Then
                        Return System.Enum.ToObject(Type2, Result)
                    End If

                Case TypeCode.Int16
                    Dim Result As Short = ShortType.FromObject(obj1) Xor ShortType.FromObject(obj2)

                    If ((Type1IsEnum AndAlso Type2IsEnum) AndAlso (Not (Type1 Is Type2))) OrElse
                        (Not (Type1IsEnum AndAlso Type2IsEnum)) Then
                        Return Result
                    ElseIf Type1IsEnum Then
                        Return System.Enum.ToObject(Type1, Result)
                    ElseIf Type2IsEnum Then
                        Return System.Enum.ToObject(Type2, Result)
                    End If

                Case TypeCode.Int32
                    Dim Result As Integer = IntegerType.FromObject(obj1) Xor IntegerType.FromObject(obj2)

                    If ((Type1IsEnum AndAlso Type2IsEnum) AndAlso (Not (Type1 Is Type2))) OrElse
                        (Not (Type1IsEnum AndAlso Type2IsEnum)) Then
                        Return Result
                    ElseIf Type1IsEnum Then
                        Return System.Enum.ToObject(Type1, Result)
                    ElseIf Type2IsEnum Then
                        Return System.Enum.ToObject(Type2, Result)
                    End If

                Case TypeCode.Int64
                    Dim Result As Long = LongType.FromObject(obj1) Xor LongType.FromObject(obj2)

                    If ((Type1IsEnum AndAlso Type2IsEnum) AndAlso (Not (Type1 Is Type2))) OrElse
                        (Not (Type1IsEnum AndAlso Type2IsEnum)) Then
                        Return Result
                    ElseIf Type1IsEnum Then
                        Return System.Enum.ToObject(Type1, Result)
                    ElseIf Type2IsEnum Then
                        Return System.Enum.ToObject(Type2, Result)
                    End If

                Case TypeCode.Decimal,
                     TypeCode.Single,
                     TypeCode.Double,
                     TypeCode.String
                    Return LongType.FromObject(obj1) Xor LongType.FromObject(obj2)

                Case TypeCode.Char
                    ' Fall through to error
                Case TypeCode.DateTime
                    ' Fall through to error
                Case Else
                    ' Fall through to error

            End Select

            Throw GetNoValidOperatorException(obj1, obj2)

        End Function

        Public Shared Function AddObj(ByVal o1 As Object, ByVal o2 As Object) As Object
            Dim conv1, conv2 As IConvertible
            Dim tc1, tc2 As TypeCode

            conv1 = TryCast(o1, IConvertible)

            If conv1 Is Nothing Then
                If o1 Is Nothing Then
                    tc1 = TypeCode.Empty
                Else
                    tc1 = TypeCode.Object
                End If
            Else
                tc1 = conv1.GetTypeCode()
            End If

            conv2 = TryCast(o2, IConvertible)

            If conv2 Is Nothing Then
                If o2 Is Nothing Then
                    tc2 = TypeCode.Empty
                Else
                    tc2 = TypeCode.Object
                End If
            Else
                tc2 = conv2.GetTypeCode()
            End If

            'Special cases for Char()
            If (tc1 = TypeCode.Object) AndAlso (TypeOf o1 Is Char()) Then
                If tc2 = TypeCode.String OrElse tc2 = TypeCode.Empty OrElse ((tc2 = TypeCode.Object) AndAlso (TypeOf o2 Is Char())) Then
                    'Treat Char() as String for these cases
                    o1 = CStr(CharArrayType.FromObject(o1))
                    conv1 = CType(o1, IConvertible)
                    tc1 = TypeCode.String
                End If
            End If

            If (tc2 = TypeCode.Object) AndAlso (TypeOf o2 Is Char()) Then
                If tc1 = TypeCode.String OrElse tc1 = TypeCode.Empty Then
                    o2 = CStr(CharArrayType.FromObject(o2))
                    conv2 = DirectCast(o2, IConvertible)
                    tc2 = TypeCode.String
                End If
            End If

            Select Case tc1 * TCMAX + tc2

                'STRING
                Case TypeCode.String * TCMAX + TypeCode.Empty,
                    TypeCode.String * TCMAX + TypeCode.DBNull
                    Return o1

                Case TypeCode.Empty * TCMAX + TypeCode.String,
                    TypeCode.DBNull * TCMAX + TypeCode.String
                    Return o2

                Case TypeCode.String * TCMAX + TypeCode.Byte,
                    TypeCode.String * TCMAX + TypeCode.Int16,
                    TypeCode.String * TCMAX + TypeCode.Int32,
                    TypeCode.String * TCMAX + TypeCode.Int64,
                    TypeCode.String * TCMAX + TypeCode.Single,
                    TypeCode.String * TCMAX + TypeCode.Double,
                    TypeCode.String * TCMAX + TypeCode.Decimal
                    Return AddString(conv1, tc1, conv2, tc2)

                Case TypeCode.Byte * TCMAX + TypeCode.String,
                    TypeCode.Int16 * TCMAX + TypeCode.String,
                    TypeCode.Int32 * TCMAX + TypeCode.String,
                    TypeCode.Int64 * TCMAX + TypeCode.String,
                    TypeCode.Single * TCMAX + TypeCode.String,
                    TypeCode.Double * TCMAX + TypeCode.String,
                    TypeCode.Decimal * TCMAX + TypeCode.String
                    Return AddString(conv1, tc1, conv2, tc2)

                Case TypeCode.String * TCMAX + TypeCode.String,
                     TypeCode.String * TCMAX + TypeCode.Char,
                     TypeCode.String * TCMAX + TypeCode.DateTime,
                     TypeCode.Char * TCMAX + TypeCode.String,
                     TypeCode.Char * TCMAX + TypeCode.Char,
                     TypeCode.DateTime * TCMAX + TypeCode.DateTime,
                     TypeCode.DateTime * TCMAX + TypeCode.String
                    Return StringType.FromObject(o1) + StringType.FromObject(o2)

                Case TypeCode.Boolean * TCMAX + TypeCode.String,
                     TypeCode.String * TCMAX + TypeCode.Boolean
                    Return AddString(conv1, tc1, conv2, tc2)

                    'EMPTY
                Case TypeCode.Empty * TCMAX + TypeCode.Empty
                    Return CInt(0)

                Case TypeCode.Boolean * TCMAX + TypeCode.Empty,
                    TypeCode.Byte * TCMAX + TypeCode.Empty,
                    TypeCode.Int16 * TCMAX + TypeCode.Empty,
                    TypeCode.Int32 * TCMAX + TypeCode.Empty,
                    TypeCode.Int64 * TCMAX + TypeCode.Empty,
                    TypeCode.Single * TCMAX + TypeCode.Empty,
                    TypeCode.Double * TCMAX + TypeCode.Empty,
                    TypeCode.Decimal * TCMAX + TypeCode.Empty
                    Return o1

                Case TypeCode.Empty * TCMAX + TypeCode.Boolean,
                    TypeCode.Empty * TCMAX + TypeCode.Byte,
                    TypeCode.Empty * TCMAX + TypeCode.Int16,
                    TypeCode.Empty * TCMAX + TypeCode.Int32,
                    TypeCode.Empty * TCMAX + TypeCode.Int64,
                     TypeCode.Empty * TCMAX + TypeCode.Single,
                    TypeCode.Empty * TCMAX + TypeCode.Double,
                    TypeCode.Empty * TCMAX + TypeCode.Decimal
                    Return o2

                Case TypeCode.Decimal * TCMAX + TypeCode.Byte,
                    TypeCode.Decimal * TCMAX + TypeCode.Int16,
                    TypeCode.Decimal * TCMAX + TypeCode.Int32,
                    TypeCode.Decimal * TCMAX + TypeCode.Int64,
                    TypeCode.Decimal * TCMAX + TypeCode.Decimal,
                    TypeCode.Int16 * TCMAX + TypeCode.Decimal,
                     TypeCode.Byte * TCMAX + TypeCode.Decimal,
                     TypeCode.Int32 * TCMAX + TypeCode.Decimal,
                    TypeCode.Int64 * TCMAX + TypeCode.Decimal
                    Return AddDecimal(conv1, conv2)

                Case TypeCode.Boolean * TCMAX + TypeCode.Decimal
                    Return AddDecimal(ToVBBoolConv(conv1), conv2)

                Case TypeCode.Decimal * TCMAX + TypeCode.Boolean
                    Return AddDecimal(conv1, ToVBBoolConv(conv2))

                Case TypeCode.Double * TCMAX + TypeCode.Byte,
                    TypeCode.Double * TCMAX + TypeCode.Int16,
                    TypeCode.Double * TCMAX + TypeCode.Int32,
                    TypeCode.Double * TCMAX + TypeCode.Int64,
                    TypeCode.Double * TCMAX + TypeCode.Single,
                    TypeCode.Double * TCMAX + TypeCode.Double,
                    TypeCode.Byte * TCMAX + TypeCode.Double,
                    TypeCode.Int16 * TCMAX + TypeCode.Double,
                     TypeCode.Int32 * TCMAX + TypeCode.Double,
                     TypeCode.Int64 * TCMAX + TypeCode.Double,
                     TypeCode.Single * TCMAX + TypeCode.Double,
                     TypeCode.Double * TCMAX + TypeCode.Decimal,
                    TypeCode.Decimal * TCMAX + TypeCode.Double
                    Return AddDouble(conv1.ToDouble(Nothing), conv2.ToDouble(Nothing))

                Case TypeCode.Double * TCMAX + TypeCode.Boolean
                    Return AddDouble(conv1.ToDouble(Nothing), ToVBBool(conv2))

                Case TypeCode.Boolean * TCMAX + TypeCode.Double
                    Return AddDouble(ToVBBool(conv1), conv2.ToDouble(Nothing))

                Case TypeCode.Single * TCMAX + TypeCode.Byte,
                    TypeCode.Single * TCMAX + TypeCode.Int16,
                    TypeCode.Single * TCMAX + TypeCode.Int32,
                    TypeCode.Single * TCMAX + TypeCode.Int64,
                     TypeCode.Single * TCMAX + TypeCode.Single,
                    TypeCode.Byte * TCMAX + TypeCode.Single,
                    TypeCode.Int16 * TCMAX + TypeCode.Single,
                    TypeCode.Int32 * TCMAX + TypeCode.Single,
                    TypeCode.Int64 * TCMAX + TypeCode.Single,
                    TypeCode.Decimal * TCMAX + TypeCode.Single,
                    TypeCode.Single * TCMAX + TypeCode.Decimal
                    Return AddSingle(conv1.ToSingle(Nothing), conv2.ToSingle(Nothing))

                Case TypeCode.Single * TCMAX + TypeCode.Boolean
                    Return AddSingle(conv1.ToSingle(Nothing), ToVBBool(conv2))

                Case TypeCode.Boolean * TCMAX + TypeCode.Single
                    Return AddSingle(ToVBBool(conv1), conv2.ToSingle(Nothing))

                Case TypeCode.Byte * TCMAX + TypeCode.Int64,
                    TypeCode.Int64 * TCMAX + TypeCode.Byte,
                    TypeCode.Int64 * TCMAX + TypeCode.Int16,
                    TypeCode.Int64 * TCMAX + TypeCode.Int32,
                    TypeCode.Int64 * TCMAX + TypeCode.Int64,
                    TypeCode.Int16 * TCMAX + TypeCode.Int64,
                    TypeCode.Int32 * TCMAX + TypeCode.Int64
                    Return AddInt64(conv1.ToInt64(Nothing), conv2.ToInt64(Nothing))

                Case TypeCode.Int64 * TCMAX + TypeCode.Boolean
                    Return AddInt64(conv1.ToInt64(Nothing), ToVBBool(conv2))

                Case TypeCode.Boolean * TCMAX + TypeCode.Int64
                    Return AddInt64(ToVBBool(conv1), conv2.ToInt64(Nothing))

                Case TypeCode.Int16 * TCMAX + TypeCode.Int32,
                    TypeCode.Int32 * TCMAX + TypeCode.Int16,
                    TypeCode.Int32 * TCMAX + TypeCode.Int32,
                    TypeCode.Int32 * TCMAX + TypeCode.Byte,
                    TypeCode.Byte * TCMAX + TypeCode.Int32
                    Return AddInt32(conv1.ToInt32(Nothing), conv2.ToInt32(Nothing))

                Case TypeCode.Int32 * TCMAX + TypeCode.Boolean
                    Return AddInt32(conv1.ToInt32(Nothing), ToVBBool(conv2))

                Case TypeCode.Boolean * TCMAX + TypeCode.Int32
                    Return AddInt32(ToVBBool(conv1), conv2.ToInt32(Nothing))

                Case TypeCode.Int16 * TCMAX + TypeCode.Byte,
                    TypeCode.Int16 * TCMAX + TypeCode.Int16,
                    TypeCode.Byte * TCMAX + TypeCode.Int16
                    Return AddInt16(conv1.ToInt16(Nothing), conv2.ToInt16(Nothing))

                Case TypeCode.Boolean * TCMAX + TypeCode.Byte,
                    TypeCode.Boolean * TCMAX + TypeCode.Int16
                    Return AddInt16(CShort(ToVBBool(conv1)), conv2.ToInt16(Nothing))

                Case TypeCode.Byte * TCMAX + TypeCode.Boolean,
                     TypeCode.Int16 * TCMAX + TypeCode.Boolean
                    Return AddInt16(conv1.ToInt16(Nothing), CShort(ToVBBool(conv2)))

                Case TypeCode.Boolean * TCMAX + TypeCode.Boolean
                    Return AddInt16(CShort(ToVBBool(conv1)), CShort(ToVBBool(conv2)))

                Case TypeCode.Byte * TCMAX + TypeCode.Byte
                    Return AddByte(conv1.ToByte(Nothing), conv2.ToByte(Nothing))

                Case Else

            End Select

            Throw GetNoValidOperatorException(o1, o2)

        End Function

        Private Shared Function AddString(ByVal conv1 As IConvertible, ByVal tc1 As TypeCode, ByVal conv2 As IConvertible, ByVal tc2 As TypeCode) As Object
            Dim dbl1, dbl2 As Double

            If tc1 = TypeCode.String Then
                dbl1 = DoubleType.FromString(conv1.ToString(Nothing))
            ElseIf tc1 = TypeCode.Boolean Then
                dbl1 = ToVBBool(conv1)
            Else
                dbl1 = conv1.ToDouble(Nothing)
            End If

            If tc2 = TypeCode.String Then
                dbl2 = DoubleType.FromString(conv2.ToString(Nothing))
            ElseIf tc2 = TypeCode.Boolean Then
                dbl2 = ToVBBool(conv2)
            Else
                dbl2 = conv2.ToDouble(Nothing)
            End If

            Return dbl1 + dbl2
        End Function

        Private Shared Function AddByte(ByVal i1 As Byte, ByVal i2 As Byte) As Object
            Dim result As Short = CShort(i1) + CShort(i2)

            If result >= Byte.MinValue AndAlso result <= Byte.MaxValue Then
                Return CByte(result)
            Else
                Return result
            End If
        End Function

        Private Shared Function AddInt16(ByVal i1 As Short, ByVal i2 As Short) As Object
            Dim result As Integer = CInt(i1) + CInt(i2)

            If result >= Short.MinValue AndAlso result <= Short.MaxValue Then
                Return CShort(result)
            Else
                Return result
            End If
        End Function

        Private Shared Function AddInt32(ByVal i1 As Integer, ByVal i2 As Integer) As Object
            Dim result As Long = CLng(i1) + CLng(i2)
            If result >= Integer.MinValue AndAlso result <= Integer.MaxValue Then
                Return CInt(result)
            Else
                Return result
            End If
        End Function

        Private Shared Function AddInt64(ByVal i1 As Long, ByVal i2 As Long) As Object
            Try
                Return i1 + i2
            Catch e As OverflowException
                Return CDec(i1) + CDec(i2)
            End Try
        End Function

        Private Shared Function AddSingle(ByVal f1 As Single, ByVal f2 As Single) As Object
            Dim result As Double = CDbl(f1) + CDbl(f2)
            If ((result <= Single.MaxValue AndAlso result >= Single.MinValue)) Then
                Return CSng(result)
            ElseIf Double.IsInfinity(result) AndAlso (Single.IsInfinity(f1) OrElse Single.IsInfinity(f2)) Then
                Return CSng(result)
            Else
                Return result
            End If
        End Function

        Private Shared Function AddDouble(ByVal d1 As Double, ByVal d2 As Double) As Object
            Return d1 + d2
        End Function

        Private Shared Function AddDecimal(ByVal conv1 As IConvertible, ByVal conv2 As IConvertible) As Object

            Dim d1, d2 As Decimal

            If Not conv1 Is Nothing Then
                d1 = conv1.ToDecimal(Nothing)
            End If
            d2 = conv2.ToDecimal(Nothing)
            Try
                Return (d1 + d2)
            Catch e As OverflowException
                Return CDbl(d1) + CDbl(d2)
            End Try

        End Function

        Private Shared Function ToVBBool(ByVal conv As IConvertible) As Integer
            If conv.ToBoolean(Nothing) Then
                Return -1
            Else
                Return 0
            End If
        End Function

        Private Shared Function ToVBBoolConv(ByVal conv As IConvertible) As IConvertible
            If conv.ToBoolean(Nothing) Then
                Return -1
            Else
                Return 0
            End If
        End Function

        Public Shared Function SubObj(ByVal o1 As Object, ByVal o2 As Object) As Object
            Dim conv1, conv2 As IConvertible
            Dim tc1, tc2 As TypeCode

            conv1 = TryCast(o1, IConvertible)

            If conv1 Is Nothing Then
                If o1 Is Nothing Then
                    tc1 = TypeCode.Empty
                Else
                    tc1 = TypeCode.Object
                End If
            Else
                tc1 = conv1.GetTypeCode()
            End If

            conv2 = TryCast(o2, IConvertible)

            If conv2 Is Nothing Then
                If o2 Is Nothing Then
                    tc2 = TypeCode.Empty
                Else
                    tc2 = TypeCode.Object
                End If
            Else
                tc2 = conv2.GetTypeCode()
            End If

            Select Case tc1 * TCMAX + tc2

                Case TypeCode.Empty * TCMAX + TypeCode.Empty
                    Return 0I

                Case TypeCode.Empty * TCMAX + TypeCode.String
                    Return SubStringString(Nothing, conv2.ToString(Nothing))

                Case TypeCode.String * TCMAX + TypeCode.Empty
                    Return SubStringString(conv1.ToString(Nothing), Nothing)

                Case TypeCode.Boolean * TCMAX + TypeCode.Empty,
                    TypeCode.Byte * TCMAX + TypeCode.Empty,
                    TypeCode.Int16 * TCMAX + TypeCode.Empty,
                    TypeCode.Int32 * TCMAX + TypeCode.Empty,
                    TypeCode.Int64 * TCMAX + TypeCode.Empty,
                    TypeCode.Single * TCMAX + TypeCode.Empty,
                    TypeCode.Double * TCMAX + TypeCode.Empty,
                    TypeCode.Decimal * TCMAX + TypeCode.Empty
                    Return o1

                Case TypeCode.Empty * TCMAX + TypeCode.Boolean,
                    TypeCode.Empty * TCMAX + TypeCode.Byte,
                    TypeCode.Empty * TCMAX + TypeCode.Int16,
                    TypeCode.Empty * TCMAX + TypeCode.Int32,
                    TypeCode.Empty * TCMAX + TypeCode.Int64,
                    TypeCode.Empty * TCMAX + TypeCode.Single,
                    TypeCode.Empty * TCMAX + TypeCode.Double,
                    TypeCode.Empty * TCMAX + TypeCode.Decimal
                    Return InternalNegObj(o2, conv2, tc2)

                Case TypeCode.Decimal * TCMAX + TypeCode.Byte,
                    TypeCode.Decimal * TCMAX + TypeCode.Int16,
                    TypeCode.Decimal * TCMAX + TypeCode.Int32,
                    TypeCode.Decimal * TCMAX + TypeCode.Int64,
                     TypeCode.Decimal * TCMAX + TypeCode.Decimal,
                    TypeCode.Int16 * TCMAX + TypeCode.Decimal,
                    TypeCode.Byte * TCMAX + TypeCode.Decimal,
                    TypeCode.Int32 * TCMAX + TypeCode.Decimal,
                    TypeCode.Int64 * TCMAX + TypeCode.Decimal
                    Return SubDecimal(conv1, conv2)

                Case TypeCode.Boolean * TCMAX + TypeCode.Decimal
                    Return SubDecimal(ToVBBoolConv(conv1), conv2)

                Case TypeCode.Decimal * TCMAX + TypeCode.Boolean
                    Return SubDecimal(conv1, ToVBBoolConv(conv2))

                Case TypeCode.String * TCMAX + TypeCode.Byte,
                    TypeCode.String * TCMAX + TypeCode.Int16,
                    TypeCode.String * TCMAX + TypeCode.Int32,
                    TypeCode.String * TCMAX + TypeCode.Int64,
                    TypeCode.String * TCMAX + TypeCode.Single,
                    TypeCode.String * TCMAX + TypeCode.Double,
                    TypeCode.String * TCMAX + TypeCode.Decimal,
                    TypeCode.Byte * TCMAX + TypeCode.String,
                    TypeCode.Int16 * TCMAX + TypeCode.String,
                    TypeCode.Int32 * TCMAX + TypeCode.String,
                    TypeCode.Int64 * TCMAX + TypeCode.String,
                    TypeCode.Single * TCMAX + TypeCode.String,
                    TypeCode.Double * TCMAX + TypeCode.String,
                    TypeCode.Decimal * TCMAX + TypeCode.String,
                    TypeCode.Boolean * TCMAX + TypeCode.String,
                    TypeCode.String * TCMAX + TypeCode.Boolean
                    Return SubString(conv1, tc1, conv2, tc2)


                Case TypeCode.String * TCMAX + TypeCode.String
                    Return SubStringString(conv1.ToString(Nothing), conv2.ToString(Nothing))

                Case TypeCode.Double * TCMAX + TypeCode.Byte,
                    TypeCode.Double * TCMAX + TypeCode.Int16,
                    TypeCode.Double * TCMAX + TypeCode.Int32,
                    TypeCode.Double * TCMAX + TypeCode.Int64,
                    TypeCode.Double * TCMAX + TypeCode.Single,
                    TypeCode.Double * TCMAX + TypeCode.Double,
                    TypeCode.Byte * TCMAX + TypeCode.Double,
                    TypeCode.Int16 * TCMAX + TypeCode.Double,
                    TypeCode.Int32 * TCMAX + TypeCode.Double,
                    TypeCode.Int64 * TCMAX + TypeCode.Double,
                    TypeCode.Single * TCMAX + TypeCode.Double,
                    TypeCode.Double * TCMAX + TypeCode.Decimal,
                    TypeCode.Decimal * TCMAX + TypeCode.Double
                    Return SubDouble(conv1.ToDouble(Nothing), conv2.ToDouble(Nothing))

                Case TypeCode.Double * TCMAX + TypeCode.Boolean
                    Return SubDouble(conv1.ToDouble(Nothing), ToVBBool(conv2))

                Case TypeCode.Boolean * TCMAX + TypeCode.Double
                    Return SubDouble(ToVBBool(conv1), conv2.ToDouble(Nothing))

                Case TypeCode.Single * TCMAX + TypeCode.Byte,
                    TypeCode.Single * TCMAX + TypeCode.Int16,
                    TypeCode.Single * TCMAX + TypeCode.Int32,
                    TypeCode.Single * TCMAX + TypeCode.Int64,
                     TypeCode.Single * TCMAX + TypeCode.Single,
                    TypeCode.Byte * TCMAX + TypeCode.Single,
                    TypeCode.Int16 * TCMAX + TypeCode.Single,
                    TypeCode.Int32 * TCMAX + TypeCode.Single,
                    TypeCode.Int64 * TCMAX + TypeCode.Single,
                    TypeCode.Decimal * TCMAX + TypeCode.Single,
                    TypeCode.Single * TCMAX + TypeCode.Decimal
                    Return SubSingle(conv1.ToSingle(Nothing), conv2.ToSingle(Nothing))

                Case TypeCode.Single * TCMAX + TypeCode.Boolean
                    Return SubSingle(conv1.ToSingle(Nothing), ToVBBool(conv2))

                Case TypeCode.Boolean * TCMAX + TypeCode.Single
                    Return SubSingle(ToVBBool(conv1), conv2.ToSingle(Nothing))

                Case TypeCode.Byte * TCMAX + TypeCode.Int64,
                    TypeCode.Int64 * TCMAX + TypeCode.Byte,
                    TypeCode.Int64 * TCMAX + TypeCode.Int16,
                    TypeCode.Int64 * TCMAX + TypeCode.Int32,
                    TypeCode.Int64 * TCMAX + TypeCode.Int64,
                    TypeCode.Int16 * TCMAX + TypeCode.Int64,
                    TypeCode.Int32 * TCMAX + TypeCode.Int64
                    Return SubInt64(conv1.ToInt64(Nothing), conv2.ToInt64(Nothing))

                Case TypeCode.Int64 * TCMAX + TypeCode.Boolean
                    Return SubInt64(conv1.ToInt64(Nothing), ToVBBool(conv2))

                Case TypeCode.Boolean * TCMAX + TypeCode.Int64
                    Return SubInt64(ToVBBool(conv1), conv2.ToInt64(Nothing))

                Case TypeCode.Int16 * TCMAX + TypeCode.Int32,
                    TypeCode.Int32 * TCMAX + TypeCode.Int16,
                    TypeCode.Int32 * TCMAX + TypeCode.Int32,
                    TypeCode.Int32 * TCMAX + TypeCode.Byte,
                    TypeCode.Byte * TCMAX + TypeCode.Int32
                    Return SubInt32(conv1.ToInt32(Nothing), conv2.ToInt32(Nothing))

                Case TypeCode.Int32 * TCMAX + TypeCode.Boolean
                    Return SubInt32(conv1.ToInt32(Nothing), ToVBBool(conv2))

                Case TypeCode.Boolean * TCMAX + TypeCode.Int32
                    Return SubInt32(ToVBBool(conv1), conv2.ToInt32(Nothing))

                Case TypeCode.Int16 * TCMAX + TypeCode.Byte,
                    TypeCode.Int16 * TCMAX + TypeCode.Int16,
                    TypeCode.Byte * TCMAX + TypeCode.Int16
                    Return SubInt16(conv1.ToInt16(Nothing), conv2.ToInt16(Nothing))

                Case TypeCode.Boolean * TCMAX + TypeCode.Byte,
                    TypeCode.Boolean * TCMAX + TypeCode.Int16
                    Return SubInt16(CShort(ToVBBool(conv1)), conv2.ToInt16(Nothing))

                Case TypeCode.Byte * TCMAX + TypeCode.Boolean,
                    TypeCode.Int16 * TCMAX + TypeCode.Boolean
                    Return SubInt16(conv1.ToInt16(Nothing), CShort(ToVBBool(conv2)))

                Case TypeCode.Boolean * TCMAX + TypeCode.Boolean
                    Return SubInt16(CShort(ToVBBool(conv1)), CShort(ToVBBool(conv2)))

                Case TypeCode.Byte * TCMAX + TypeCode.Byte
                    Return SubByte(conv1.ToByte(Nothing), conv2.ToByte(Nothing))

                Case Else

            End Select

            Throw GetNoValidOperatorException(o1, o2)

        End Function

        Private Shared Function SubString(ByVal conv1 As IConvertible, ByVal tc1 As TypeCode, ByVal conv2 As IConvertible, ByVal tc2 As TypeCode) As Object
            Dim dbl1, dbl2 As Double

            If tc1 = TypeCode.String Then
                dbl1 = DoubleType.FromString(conv1.ToString(Nothing))
            ElseIf tc1 = TypeCode.Boolean Then
                dbl1 = ToVBBool(conv1)
            Else
                dbl1 = conv1.ToDouble(Nothing)
            End If

            If tc2 = TypeCode.String Then
                dbl2 = DoubleType.FromString(conv2.ToString(Nothing))
            ElseIf tc2 = TypeCode.Boolean Then
                dbl2 = ToVBBool(conv2)
            Else
                dbl2 = conv2.ToDouble(Nothing)
            End If

            Return dbl1 - dbl2
        End Function

        Private Shared Function SubStringString(ByVal s1 As String, ByVal s2 As String) As Object
            Dim dbl1, dbl2 As Double

            If Not s1 Is Nothing Then
                dbl1 = DoubleType.FromString(s1)
            End If

            If Not s2 Is Nothing Then
                dbl2 = DoubleType.FromString(s2)
            End If

            Return dbl1 - dbl2

        End Function

        Private Shared Function SubByte(ByVal i1 As Byte, ByVal i2 As Byte) As Object
            Dim result As Short = CShort(i1) - CShort(i2)

            If result >= Byte.MinValue AndAlso result <= Byte.MaxValue Then
                Return CByte(result)
            Else
                Return result
            End If
        End Function

        Private Shared Function SubInt16(ByVal i1 As Short, ByVal i2 As Short) As Object
            Dim result As Integer = CInt(i1) - CInt(i2)

            If result >= Short.MinValue AndAlso result <= Short.MaxValue Then
                Return CShort(result)
            Else
                Return result
            End If
        End Function

        Private Shared Function SubInt32(ByVal i1 As Integer, ByVal i2 As Integer) As Object
            Dim result As Long = CLng(i1) - CLng(i2)
            If result >= Integer.MinValue AndAlso result <= Integer.MaxValue Then
                Return CInt(result)
            Else
                Return result
            End If
        End Function

        Private Shared Function SubInt64(ByVal i1 As Long, ByVal i2 As Long) As Object
            Try
                Return i1 - i2
            Catch ex As StackOverflowException
                Throw ex
            Catch ex As OutOfMemoryException
                Throw ex
            Catch ex As System.Threading.ThreadAbortException
                Throw ex
            Catch 'e As OverflowException
                Return CDec(i1) - CDec(i2)
            End Try
        End Function

        Private Shared Function SubSingle(ByVal f1 As Single, ByVal f2 As Single) As Object
            Dim result As Double = CDbl(f1) - CDbl(f2)
            If ((result <= Single.MaxValue AndAlso result >= Single.MinValue)) Then
                Return CSng(result)
            ElseIf Double.IsInfinity(result) AndAlso (Single.IsInfinity(f1) OrElse Single.IsInfinity(f2)) Then
                Return CSng(result)
            Else
                Return result
            End If
        End Function

        Private Shared Function SubDouble(ByVal d1 As Double, ByVal d2 As Double) As Object
            Return d1 - d2
        End Function

        Private Shared Function SubDecimal(ByVal conv1 As IConvertible, ByVal conv2 As IConvertible) As Object
            Dim d1, d2 As Decimal
            d1 = conv1.ToDecimal(Nothing)
            d2 = conv2.ToDecimal(Nothing)
            Try
                Return (d1 - d2)
            Catch e As OverflowException
                Return CDbl(d1) - CDbl(d2)
            End Try
        End Function

        Public Shared Function MulObj(ByVal o1 As Object, ByVal o2 As Object) As Object
            Dim conv1, conv2 As IConvertible
            Dim tc1, tc2 As TypeCode

            conv1 = TryCast(o1, IConvertible)

            If conv1 Is Nothing Then
                If o1 Is Nothing Then
                    tc1 = TypeCode.Empty
                Else
                    tc1 = TypeCode.Object
                End If
            Else
                tc1 = conv1.GetTypeCode()
            End If

            conv2 = TryCast(o2, IConvertible)

            If conv2 Is Nothing Then
                If o2 Is Nothing Then
                    tc2 = TypeCode.Empty
                Else
                    tc2 = TypeCode.Object
                End If
            Else
                tc2 = conv2.GetTypeCode()
            End If

            Select Case tc1 * TCMAX + tc2

                Case TypeCode.Empty * TCMAX + TypeCode.String,
                     TypeCode.String * TCMAX + TypeCode.Empty
                    Return CDbl(0)

                Case TypeCode.Byte * TCMAX + TypeCode.Empty,
                     TypeCode.Empty * TCMAX + TypeCode.Byte
                    Return CByte(0)

                Case TypeCode.Boolean * TCMAX + TypeCode.Empty,
                     TypeCode.Empty * TCMAX + TypeCode.Boolean,
                     TypeCode.Int16 * TCMAX + TypeCode.Empty,
                     TypeCode.Empty * TCMAX + TypeCode.Int16
                    Return CShort(0)

                Case TypeCode.Empty * TCMAX + TypeCode.Empty,
                     TypeCode.Int32 * TCMAX + TypeCode.Empty,
                     TypeCode.Empty * TCMAX + TypeCode.Int32
                    Return CInt(0)

                Case TypeCode.Int64 * TCMAX + TypeCode.Empty,
                     TypeCode.Empty * TCMAX + TypeCode.Int64
                    Return CLng(0)

                Case TypeCode.Single * TCMAX + TypeCode.Empty,
                     TypeCode.Empty * TCMAX + TypeCode.Single
                    Return CSng(0)

                Case TypeCode.Double * TCMAX + TypeCode.Empty,
                     TypeCode.Empty * TCMAX + TypeCode.Double
                    Return CDbl(0)

                Case TypeCode.Decimal * TCMAX + TypeCode.Empty,
                     TypeCode.Empty * TCMAX + TypeCode.Decimal
                    Return CDec(0)

                Case TypeCode.Decimal * TCMAX + TypeCode.Byte,
                    TypeCode.Decimal * TCMAX + TypeCode.Int16,
                    TypeCode.Decimal * TCMAX + TypeCode.Int32,
                    TypeCode.Decimal * TCMAX + TypeCode.Int64,
                    TypeCode.Decimal * TCMAX + TypeCode.Decimal,
                    TypeCode.Int16 * TCMAX + TypeCode.Decimal,
                    TypeCode.Byte * TCMAX + TypeCode.Decimal,
                    TypeCode.Int32 * TCMAX + TypeCode.Decimal,
                    TypeCode.Int64 * TCMAX + TypeCode.Decimal
                    Return MulDecimal(conv1, conv2)

                Case TypeCode.Boolean * TCMAX + TypeCode.Decimal
                    Return MulDecimal(ToVBBoolConv(conv1), conv2)

                Case TypeCode.Decimal * TCMAX + TypeCode.Boolean
                    Return MulDecimal(conv1, ToVBBoolConv(conv2))

                Case TypeCode.String * TCMAX + TypeCode.Byte,
                    TypeCode.String * TCMAX + TypeCode.Int16,
                    TypeCode.String * TCMAX + TypeCode.Int32,
                    TypeCode.String * TCMAX + TypeCode.Int64,
                    TypeCode.String * TCMAX + TypeCode.Single,
                    TypeCode.String * TCMAX + TypeCode.Double,
                    TypeCode.String * TCMAX + TypeCode.Decimal,
                    TypeCode.Byte * TCMAX + TypeCode.String,
                    TypeCode.Int16 * TCMAX + TypeCode.String,
                    TypeCode.Int32 * TCMAX + TypeCode.String,
                    TypeCode.Int64 * TCMAX + TypeCode.String,
                    TypeCode.Single * TCMAX + TypeCode.String,
                    TypeCode.Double * TCMAX + TypeCode.String,
                    TypeCode.Decimal * TCMAX + TypeCode.String,
                    TypeCode.Boolean * TCMAX + TypeCode.String,
                    TypeCode.String * TCMAX + TypeCode.Boolean
                    Return MulString(conv1, tc1, conv2, tc2)

                Case TypeCode.String * TCMAX + TypeCode.String
                    Return MulStringString(conv1.ToString(Nothing), conv2.ToString(Nothing))

                Case TypeCode.Double * TCMAX + TypeCode.Byte,
                    TypeCode.Double * TCMAX + TypeCode.Int16,
                    TypeCode.Double * TCMAX + TypeCode.Int32,
                    TypeCode.Double * TCMAX + TypeCode.Int64,
                    TypeCode.Double * TCMAX + TypeCode.Decimal,
                    TypeCode.Double * TCMAX + TypeCode.Single,
                    TypeCode.Double * TCMAX + TypeCode.Double,
                    TypeCode.Byte * TCMAX + TypeCode.Double,
                    TypeCode.Int16 * TCMAX + TypeCode.Double,
                    TypeCode.Int32 * TCMAX + TypeCode.Double,
                    TypeCode.Int64 * TCMAX + TypeCode.Double,
                    TypeCode.Single * TCMAX + TypeCode.Double,
                    TypeCode.Decimal * TCMAX + TypeCode.Double
                    Return MulDouble(conv1.ToDouble(Nothing), conv2.ToDouble(Nothing))

                Case TypeCode.Double * TCMAX + TypeCode.Boolean
                    Return MulDouble(conv1.ToDouble(Nothing), ToVBBool(conv2))

                Case TypeCode.Boolean * TCMAX + TypeCode.Double
                    Return MulDouble(ToVBBool(conv1), conv2.ToDouble(Nothing))

                Case TypeCode.Single * TCMAX + TypeCode.Byte,
                    TypeCode.Single * TCMAX + TypeCode.Int16,
                    TypeCode.Single * TCMAX + TypeCode.Int32,
                    TypeCode.Single * TCMAX + TypeCode.Int64,
                    TypeCode.Single * TCMAX + TypeCode.Single,
                    TypeCode.Byte * TCMAX + TypeCode.Single,
                    TypeCode.Int16 * TCMAX + TypeCode.Single,
                    TypeCode.Int32 * TCMAX + TypeCode.Single,
                    TypeCode.Int64 * TCMAX + TypeCode.Single,
                    TypeCode.Decimal * TCMAX + TypeCode.Single,
                    TypeCode.Single * TCMAX + TypeCode.Decimal
                    Return MulSingle(conv1.ToSingle(Nothing), conv2.ToSingle(Nothing))

                Case TypeCode.Single * TCMAX + TypeCode.Boolean
                    Return MulSingle(conv1.ToSingle(Nothing), ToVBBool(conv2))

                Case TypeCode.Boolean * TCMAX + TypeCode.Single
                    Return MulSingle(ToVBBool(conv1), conv2.ToSingle(Nothing))

                Case TypeCode.Byte * TCMAX + TypeCode.Int64,
                    TypeCode.Int64 * TCMAX + TypeCode.Byte,
                    TypeCode.Int64 * TCMAX + TypeCode.Int16,
                    TypeCode.Int64 * TCMAX + TypeCode.Int32,
                    TypeCode.Int64 * TCMAX + TypeCode.Int64,
                    TypeCode.Int16 * TCMAX + TypeCode.Int64,
                    TypeCode.Int32 * TCMAX + TypeCode.Int64
                    Return MulInt64(conv1.ToInt64(Nothing), conv2.ToInt64(Nothing))

                Case TypeCode.Int64 * TCMAX + TypeCode.Boolean
                    Return MulInt64(conv1.ToInt64(Nothing), ToVBBool(conv2))

                Case TypeCode.Boolean * TCMAX + TypeCode.Int64
                    Return MulInt64(ToVBBool(conv1), conv2.ToInt64(Nothing))

                Case TypeCode.Int16 * TCMAX + TypeCode.Int32,
                    TypeCode.Int32 * TCMAX + TypeCode.Int16,
                    TypeCode.Int32 * TCMAX + TypeCode.Int32,
                    TypeCode.Int32 * TCMAX + TypeCode.Byte,
                    TypeCode.Byte * TCMAX + TypeCode.Int32
                    Return MulInt32(conv1.ToInt32(Nothing), conv2.ToInt32(Nothing))

                Case TypeCode.Int32 * TCMAX + TypeCode.Boolean
                    Return MulInt32(conv1.ToInt32(Nothing), ToVBBool(conv2))

                Case TypeCode.Boolean * TCMAX + TypeCode.Int32
                    Return MulInt32(ToVBBool(conv1), conv2.ToInt32(Nothing))

                Case TypeCode.Int16 * TCMAX + TypeCode.Byte,
                    TypeCode.Int16 * TCMAX + TypeCode.Int16,
                    TypeCode.Byte * TCMAX + TypeCode.Int16
                    Return MulInt16(conv1.ToInt16(Nothing), conv2.ToInt16(Nothing))

                Case TypeCode.Boolean * TCMAX + TypeCode.Byte,
                    TypeCode.Boolean * TCMAX + TypeCode.Int16
                    Return MulInt16(CShort(ToVBBool(conv1)), conv2.ToInt16(Nothing))

                Case TypeCode.Byte * TCMAX + TypeCode.Boolean,
                    TypeCode.Int16 * TCMAX + TypeCode.Boolean
                    Return MulInt16(conv1.ToInt16(Nothing), CShort(ToVBBool(conv2)))

                Case TypeCode.Boolean * TCMAX + TypeCode.Boolean
                    Return MulInt16(CShort(ToVBBool(conv1)), CShort(ToVBBool(conv2)))

                Case TypeCode.Byte * TCMAX + TypeCode.Byte
                    Return MulByte(conv1.ToByte(Nothing), conv2.ToByte(Nothing))

                Case Else

            End Select

            Throw GetNoValidOperatorException(o1, o2)

        End Function

        Private Shared Function MulString(ByVal conv1 As IConvertible, ByVal tc1 As TypeCode, ByVal conv2 As IConvertible, ByVal tc2 As TypeCode) As Object
            Dim dbl1, dbl2 As Double

            If tc1 = TypeCode.String Then
                dbl1 = DoubleType.FromString(conv1.ToString(Nothing))
            ElseIf tc1 = TypeCode.Boolean Then
                dbl1 = ToVBBool(conv1)
            Else
                dbl1 = conv1.ToDouble(Nothing)
            End If

            If tc2 = TypeCode.String Then
                dbl2 = DoubleType.FromString(conv2.ToString(Nothing))
            ElseIf tc2 = TypeCode.Boolean Then
                dbl2 = ToVBBool(conv2)
            Else
                dbl2 = conv2.ToDouble(Nothing)
            End If

            Return dbl1 * dbl2
        End Function

        Private Shared Function MulStringString(ByVal s1 As String, ByVal s2 As String) As Object
            Dim dbl1, dbl2 As Double

            If Not s1 Is Nothing Then
                dbl1 = DoubleType.FromString(s1)
            End If

            If Not s2 Is Nothing Then
                dbl2 = DoubleType.FromString(s2)
            End If

            Return dbl1 * dbl2

        End Function

        Private Shared Function MulByte(ByVal i1 As Byte, ByVal i2 As Byte) As Object
            Dim result As Integer = CInt(i1) * CInt(i2)

            If result >= Byte.MinValue AndAlso result <= Byte.MaxValue Then
                Return CByte(result)
            ElseIf result >= Int16.MinValue AndAlso result <= Int16.MaxValue Then
                Return CShort(result)
            Else
                Return result
            End If
        End Function

        Private Shared Function MulInt16(ByVal i1 As Short, ByVal i2 As Short) As Object
            Dim result As Integer = CInt(i1) * CInt(i2)

            If result >= Short.MinValue AndAlso result <= Short.MaxValue Then
                Return CShort(result)
            Else
                Return result
            End If
        End Function

        Private Shared Function MulInt32(ByVal i1 As Integer, ByVal i2 As Integer) As Object
            Dim result As Long = CLng(i1) * CLng(i2)
            If result >= Integer.MinValue AndAlso result <= Integer.MaxValue Then
                Return CInt(result)
            Else
                Return result
            End If
        End Function

        Private Shared Function MulInt64(ByVal i1 As Long, ByVal i2 As Long) As Object
            Try
                Return i1 * i2
            Catch ex1 As OverflowException
                Try
                    Return CDec(i1) * CDec(i2)
                Catch ex2 As OverflowException
                    Return CDbl(i1) * CDbl(i2)
                End Try
            End Try
        End Function

        Private Shared Function MulSingle(ByVal f1 As Single, ByVal f2 As Single) As Object
            Dim result As Double = CDbl(f1) * CDbl(f2)
            If ((result <= Single.MaxValue AndAlso result >= Single.MinValue)) Then
                Return CSng(result)
            ElseIf Double.IsInfinity(result) AndAlso (Single.IsInfinity(f1) OrElse Single.IsInfinity(f2)) Then
                Return CSng(result)
            Else
                Return result
            End If
        End Function

        Private Shared Function MulDouble(ByVal d1 As Double, ByVal d2 As Double) As Object
            Return d1 * d2
        End Function

        Private Shared Function MulDecimal(ByVal conv1 As IConvertible, ByVal conv2 As IConvertible) As Object
            Dim d1, d2 As Decimal
            d1 = conv1.ToDecimal(Nothing)
            d2 = conv2.ToDecimal(Nothing)
            Try
                Return (d1 * d2)
            Catch e As OverflowException
                Return CDbl(d1) * CDbl(d2)
            End Try
        End Function

        Public Shared Function DivObj(ByVal o1 As Object, ByVal o2 As Object) As Object

            Dim conv1, conv2 As IConvertible
            Dim tc1, tc2 As TypeCode

            conv1 = TryCast(o1, IConvertible)

            If conv1 Is Nothing Then
                If o1 Is Nothing Then
                    tc1 = TypeCode.Empty
                Else
                    tc1 = TypeCode.Object
                End If
            Else
                tc1 = conv1.GetTypeCode()
            End If

            conv2 = TryCast(o2, IConvertible)

            If conv2 Is Nothing Then
                If o2 Is Nothing Then
                    tc2 = TypeCode.Empty
                Else
                    tc2 = TypeCode.Object
                End If
            Else
                tc2 = conv2.GetTypeCode()
            End If

            Select Case tc1 * TCMAX + tc2

                'STRING
                Case TypeCode.Empty * TCMAX + TypeCode.String
                    Return DivString(conv1, tc1, conv2, tc2)

                Case TypeCode.String * TCMAX + TypeCode.Empty
                    Return DivString(conv1, tc1, conv2, tc2)

                Case TypeCode.String * TCMAX + TypeCode.Boolean
                    Return DivString(conv1, tc1, conv2, tc2)

                Case TypeCode.Boolean * TCMAX + TypeCode.String
                    Return DivString(conv1, tc1, conv2, tc2)

                Case TypeCode.String * TCMAX + TypeCode.Byte,
                    TypeCode.String * TCMAX + TypeCode.Int16,
                    TypeCode.String * TCMAX + TypeCode.Int32,
                    TypeCode.String * TCMAX + TypeCode.Int64,
                    TypeCode.String * TCMAX + TypeCode.Single,
                    TypeCode.String * TCMAX + TypeCode.Double,
                    TypeCode.String * TCMAX + TypeCode.Decimal
                    Return DivString(conv1, tc1, conv2, tc2)

                Case TypeCode.Byte * TCMAX + TypeCode.String,
                    TypeCode.Int16 * TCMAX + TypeCode.String,
                    TypeCode.Int32 * TCMAX + TypeCode.String,
                    TypeCode.Int64 * TCMAX + TypeCode.String,
                    TypeCode.Single * TCMAX + TypeCode.String,
                    TypeCode.Double * TCMAX + TypeCode.String,
                    TypeCode.Decimal * TCMAX + TypeCode.String
                    Return DivString(conv1, tc1, conv2, tc2)

                Case TypeCode.String * TCMAX + TypeCode.String
                    Return DivStringString(conv1.ToString(Nothing), conv2.ToString(Nothing))

                    'EMPTY
                Case TypeCode.Empty * TCMAX + TypeCode.Empty
                    Return DivDouble(0, 0)

                Case TypeCode.Boolean * TCMAX + TypeCode.Empty
                    Return DivDouble(ToVBBool(conv1), 0)

                Case TypeCode.Byte * TCMAX + TypeCode.Empty,
                     TypeCode.Int16 * TCMAX + TypeCode.Empty,
                     TypeCode.Int32 * TCMAX + TypeCode.Empty,
                     TypeCode.Int64 * TCMAX + TypeCode.Empty,
                     TypeCode.Decimal * TCMAX + TypeCode.Empty,
                     TypeCode.Single * TCMAX + TypeCode.Empty,
                     TypeCode.Double * TCMAX + TypeCode.Empty
                    Return DivDouble(conv1.ToDouble(Nothing), 0)

                Case TypeCode.Empty * TCMAX + TypeCode.Boolean,
                     TypeCode.Empty * TCMAX + TypeCode.Byte,
                     TypeCode.Empty * TCMAX + TypeCode.Int16,
                     TypeCode.Empty * TCMAX + TypeCode.Int32,
                     TypeCode.Empty * TCMAX + TypeCode.Int64,
                     TypeCode.Empty * TCMAX + TypeCode.Decimal,
                     TypeCode.Empty * TCMAX + TypeCode.Single,
                     TypeCode.Empty * TCMAX + TypeCode.Double
                    Return DivDouble(0, conv2.ToDouble(Nothing))

                    'BOOLEAN
                Case TypeCode.Boolean * TCMAX + TypeCode.Byte,
                     TypeCode.Boolean * TCMAX + TypeCode.Int16,
                     TypeCode.Boolean * TCMAX + TypeCode.Int32,
                     TypeCode.Boolean * TCMAX + TypeCode.Int64,
                     TypeCode.Boolean * TCMAX + TypeCode.Double
                    Return DivDouble(ToVBBool(conv1), conv2.ToDouble(Nothing))

                Case TypeCode.Boolean * TCMAX + TypeCode.Decimal
                    Return DivDecimal(ToVBBoolConv(conv1), conv2.ToDecimal(Nothing))

                Case TypeCode.Decimal * TCMAX + TypeCode.Boolean
                    Return DivDecimal(conv1, ToVBBoolConv(conv2))

                Case TypeCode.Boolean * TCMAX + TypeCode.Boolean
                    Return DivDouble(ToVBBool(conv1), ToVBBool(conv2))


                Case TypeCode.Boolean * TCMAX + TypeCode.Single
                    Return DivSingle(ToVBBool(conv1), conv2.ToSingle(Nothing))

                Case TypeCode.Single * TCMAX + TypeCode.Boolean
                    Return DivSingle(conv1.ToSingle(Nothing), ToVBBool(conv2))

                Case TypeCode.Byte * TCMAX + TypeCode.Boolean,
                     TypeCode.Int16 * TCMAX + TypeCode.Boolean,
                     TypeCode.Int32 * TCMAX + TypeCode.Boolean,
                     TypeCode.Int64 * TCMAX + TypeCode.Boolean,
                     TypeCode.Double * TCMAX + TypeCode.Boolean
                    Return DivDouble(conv1.ToDouble(Nothing), ToVBBool(conv2))

                    'DECIMAL
                Case TypeCode.Decimal * TCMAX + TypeCode.Byte,
                     TypeCode.Decimal * TCMAX + TypeCode.Int16,
                     TypeCode.Decimal * TCMAX + TypeCode.Int32,
                     TypeCode.Decimal * TCMAX + TypeCode.Int64,
                     TypeCode.Decimal * TCMAX + TypeCode.Decimal,
                     TypeCode.Int16 * TCMAX + TypeCode.Decimal,
                     TypeCode.Byte * TCMAX + TypeCode.Decimal,
                     TypeCode.Int32 * TCMAX + TypeCode.Decimal,
                     TypeCode.Int64 * TCMAX + TypeCode.Decimal
                    Return DivDecimal(conv1, conv2)

                    'SINGLE
                Case TypeCode.Decimal * TCMAX + TypeCode.Single,
                     TypeCode.Single * TCMAX + TypeCode.Byte,
                     TypeCode.Single * TCMAX + TypeCode.Int16,
                     TypeCode.Single * TCMAX + TypeCode.Int32,
                     TypeCode.Single * TCMAX + TypeCode.Int64,
                     TypeCode.Single * TCMAX + TypeCode.Single,
                     TypeCode.Byte * TCMAX + TypeCode.Single,
                     TypeCode.Int16 * TCMAX + TypeCode.Single,
                     TypeCode.Int32 * TCMAX + TypeCode.Single,
                     TypeCode.Int64 * TCMAX + TypeCode.Single,
                     TypeCode.Single * TCMAX + TypeCode.Decimal
                    Return DivSingle(conv1.ToSingle(Nothing), conv2.ToSingle(Nothing))

                    'DOUBLE
                Case TypeCode.Double * TCMAX + TypeCode.Byte,
                     TypeCode.Double * TCMAX + TypeCode.Int16,
                     TypeCode.Double * TCMAX + TypeCode.Int32,
                     TypeCode.Double * TCMAX + TypeCode.Int64,
                     TypeCode.Double * TCMAX + TypeCode.Single,
                     TypeCode.Double * TCMAX + TypeCode.Double,
                     TypeCode.Byte * TCMAX + TypeCode.Double,
                     TypeCode.Int16 * TCMAX + TypeCode.Double,
                     TypeCode.Int32 * TCMAX + TypeCode.Double,
                     TypeCode.Int64 * TCMAX + TypeCode.Double,
                     TypeCode.Single * TCMAX + TypeCode.Double,
                     TypeCode.Double * TCMAX + TypeCode.Decimal,
                     TypeCode.Decimal * TCMAX + TypeCode.Double,
                     TypeCode.Byte * TCMAX + TypeCode.Int64,
                     TypeCode.Byte * TCMAX + TypeCode.Int16,
                     TypeCode.Byte * TCMAX + TypeCode.Byte,
                     TypeCode.Int16 * TCMAX + TypeCode.Byte,
                     TypeCode.Int16 * TCMAX + TypeCode.Int16,
                     TypeCode.Int64 * TCMAX + TypeCode.Byte,
                     TypeCode.Int64 * TCMAX + TypeCode.Int16,
                     TypeCode.Int64 * TCMAX + TypeCode.Int32,
                     TypeCode.Int64 * TCMAX + TypeCode.Int64,
                     TypeCode.Int16 * TCMAX + TypeCode.Int64,
                     TypeCode.Int32 * TCMAX + TypeCode.Int64,
                     TypeCode.Int16 * TCMAX + TypeCode.Int32,
                     TypeCode.Int32 * TCMAX + TypeCode.Int16,
                     TypeCode.Int32 * TCMAX + TypeCode.Int32,
                     TypeCode.Int32 * TCMAX + TypeCode.Byte,
                     TypeCode.Byte * TCMAX + TypeCode.Int32
                    Return DivDouble(conv1.ToDouble(Nothing), conv2.ToDouble(Nothing))

                Case Else

            End Select

            Throw GetNoValidOperatorException(o1, o2)

        End Function

        Private Shared Function DivString(ByVal conv1 As IConvertible, ByVal tc1 As TypeCode, ByVal conv2 As IConvertible, ByVal tc2 As TypeCode) As Object
            Dim dbl1, dbl2 As Double

            If tc1 = TypeCode.String Then
                dbl1 = DoubleType.FromString(conv1.ToString(Nothing))
            ElseIf tc1 = TypeCode.Boolean Then
                dbl1 = ToVBBool(conv1)
            Else
                dbl1 = conv1.ToDouble(Nothing)
            End If

            If tc2 = TypeCode.String Then
                dbl2 = DoubleType.FromString(conv2.ToString(Nothing))
            ElseIf tc2 = TypeCode.Boolean Then
                dbl2 = ToVBBool(conv2)
            Else
                dbl2 = conv2.ToDouble(Nothing)
            End If

            Return dbl1 / dbl2
        End Function

        Private Shared Function DivStringString(ByVal s1 As String, ByVal s2 As String) As Object
            Dim dbl1, dbl2 As Double

            If Not s1 Is Nothing Then
                dbl1 = DoubleType.FromString(s1)
            End If

            If Not s2 Is Nothing Then
                dbl2 = DoubleType.FromString(s2)
            End If

            Return dbl1 / dbl2

        End Function

        Private Shared Function DivDouble(ByVal d1 As Double, ByVal d2 As Double) As Object
            Return d1 / d2
        End Function

        Private Shared Function DivSingle(ByVal sng1 As Single, ByVal sng2 As Single) As Object

            Dim sng As Single = sng1 / sng2

            If Single.IsInfinity(sng) Then
                If Single.IsInfinity(sng1) OrElse Single.IsInfinity(sng2) Then
                    Return sng
                End If
                Return CDbl(sng1) / CDbl(sng2)
            Else
                Return sng
            End If

        End Function

        Private Shared Function DivDecimal(ByVal conv1 As IConvertible, ByVal conv2 As IConvertible) As Object

            Dim d1, d2 As Decimal

            If Not conv1 Is Nothing Then
                d1 = conv1.ToDecimal(Nothing)
            End If
            If Not conv2 Is Nothing Then
                d2 = conv2.ToDecimal(Nothing)
            End If

            Try
                Return d1 / d2
            Catch e As OverflowException
                Return CSng(d1) / CSng(d2)
            End Try

        End Function

        Public Shared Function PowObj(ByVal obj1 As Object, ByVal obj2 As Object) As Object
            If obj1 Is Nothing AndAlso obj2 Is Nothing Then
                Return 1.0R
            End If

            Select Case GetWidestType(obj1, obj2)

                Case TypeCode.Boolean,
                     TypeCode.Byte,
                     TypeCode.Int16,
                     TypeCode.Int32,
                     TypeCode.Int64,
                     TypeCode.Decimal,
                     TypeCode.Single,
                     TypeCode.Double,
                     TypeCode.String
                    Return DoubleType.FromObject(obj1) ^ DoubleType.FromObject(obj2)

                Case TypeCode.Char
                    ' Fall through to error

                Case TypeCode.DateTime
                    ' Fall through to error
                Case Else
                    ' Fall through to error

            End Select

            Throw GetNoValidOperatorException(obj1, obj2)

        End Function

        Public Shared Function ModObj(ByVal o1 As Object, ByVal o2 As Object) As Object
            Dim conv1, conv2 As IConvertible
            Dim tc1, tc2 As TypeCode

            conv1 = TryCast(o1, IConvertible)
            conv2 = TryCast(o2, IConvertible)

            If Not conv1 Is Nothing Then
                tc1 = conv1.GetTypeCode()
            Else
                If o1 Is Nothing Then
                    tc1 = TypeCode.Empty
                Else
                    tc1 = TypeCode.Object
                End If
            End If

            If Not conv2 Is Nothing Then
                tc2 = conv2.GetTypeCode()
            Else
                conv2 = Nothing
                If o2 Is Nothing Then
                    tc2 = TypeCode.Empty
                Else
                    tc2 = TypeCode.Object
                End If
            End If

            Select Case tc1 * TCMAX + tc2

                'STRING
                Case TypeCode.Empty * TCMAX + TypeCode.String
                    Return ModString(conv1, tc1, conv2, tc2)

                Case TypeCode.String * TCMAX + TypeCode.Empty
                    Return ModString(conv1, tc1, conv2, tc2)

                Case TypeCode.String * TCMAX + TypeCode.Byte,
                     TypeCode.String * TCMAX + TypeCode.Int16,
                     TypeCode.String * TCMAX + TypeCode.Int32,
                     TypeCode.String * TCMAX + TypeCode.Int64,
                     TypeCode.String * TCMAX + TypeCode.Single,
                    TypeCode.String * TCMAX + TypeCode.Double,
                    TypeCode.String * TCMAX + TypeCode.Decimal
                    Return ModString(conv1, tc1, conv2, tc2)

                Case TypeCode.Byte * TCMAX + TypeCode.String,
                    TypeCode.Int16 * TCMAX + TypeCode.String,
                    TypeCode.Int32 * TCMAX + TypeCode.String,
                    TypeCode.Int64 * TCMAX + TypeCode.String,
                    TypeCode.Single * TCMAX + TypeCode.String,
                    TypeCode.Double * TCMAX + TypeCode.String,
                    TypeCode.Decimal * TCMAX + TypeCode.String
                    Return ModString(conv1, tc1, conv2, tc2)

                Case TypeCode.String * TCMAX + TypeCode.String
                    Return ModStringString(conv1.ToString(Nothing), conv2.ToString(Nothing))

                Case TypeCode.Boolean * TCMAX + TypeCode.String
                    Return ModString(conv1, tc1, conv2, tc2)

                Case TypeCode.String * TCMAX + TypeCode.Boolean
                    Return ModString(conv1, tc1, conv2, tc2)


                    'EMPTY 
                Case TypeCode.Empty * TCMAX + TypeCode.Empty
                    Return ModInt32(0, 0)

                Case TypeCode.Byte * TCMAX + TypeCode.Empty
                    Return ModByte(conv1.ToByte(Nothing), 0)

                Case TypeCode.Boolean * TCMAX + TypeCode.Empty
                    Return ModInt16(CShort(ToVBBool(conv1)), 0)

                Case TypeCode.Int16 * TCMAX + TypeCode.Empty
                    Return ModInt16(conv1.ToInt16(Nothing), 0)

                Case TypeCode.Int32 * TCMAX + TypeCode.Empty
                    Return ModInt32(conv1.ToInt32(Nothing), 0)

                Case TypeCode.Int64 * TCMAX + TypeCode.Empty
                    Return ModInt64(conv1.ToInt64(Nothing), 0)

                Case TypeCode.Single * TCMAX + TypeCode.Empty
                    Return ModSingle(conv1.ToSingle(Nothing), 0)

                Case TypeCode.Double * TCMAX + TypeCode.Empty
                    Return ModDouble(conv1.ToDouble(Nothing), 0)

                Case TypeCode.Decimal * TCMAX + TypeCode.Empty
                    Return ModDecimal(conv1, Nothing)

                Case TypeCode.Empty * TCMAX + TypeCode.Boolean
                    Return ModInt16(0, CShort(ToVBBool(conv2)))

                Case TypeCode.Empty * TCMAX + TypeCode.Byte
                    Return ModByte(0, conv2.ToByte(Nothing))

                Case TypeCode.Empty * TCMAX + TypeCode.Int16
                    Return ModInt16(0, CShort(ToVBBool(conv2)))

                Case TypeCode.Empty * TCMAX + TypeCode.Int32
                    Return ModInt32(0, conv2.ToInt32(Nothing))

                Case TypeCode.Empty * TCMAX + TypeCode.Int64
                    Return ModInt64(0, conv2.ToInt64(Nothing))

                Case TypeCode.Empty * TCMAX + TypeCode.Single
                    Return ModSingle(0, conv2.ToSingle(Nothing))

                Case TypeCode.Empty * TCMAX + TypeCode.Double
                    Return ModDouble(0, conv2.ToDouble(Nothing))

                Case TypeCode.Empty * TCMAX + TypeCode.Decimal
                    Return ModDecimal(Nothing, conv2)


                    'DECIMAL
                Case TypeCode.Decimal * TCMAX + TypeCode.Byte,
                    TypeCode.Decimal * TCMAX + TypeCode.Int16,
                    TypeCode.Decimal * TCMAX + TypeCode.Int32,
                    TypeCode.Decimal * TCMAX + TypeCode.Int64,
                    TypeCode.Decimal * TCMAX + TypeCode.Decimal,
                    TypeCode.Int16 * TCMAX + TypeCode.Decimal,
                    TypeCode.Byte * TCMAX + TypeCode.Decimal,
                    TypeCode.Int32 * TCMAX + TypeCode.Decimal,
                    TypeCode.Int64 * TCMAX + TypeCode.Decimal
                    Return ModDecimal(conv1, conv2)

                Case TypeCode.Boolean * TCMAX + TypeCode.Decimal
                    Return ModDecimal(ToVBBoolConv(conv1), conv2)

                Case TypeCode.Decimal * TCMAX + TypeCode.Boolean
                    Return ModDecimal(conv1, ToVBBoolConv(conv2))


                    'DOUBLE
                Case TypeCode.Double * TCMAX + TypeCode.Byte,
                    TypeCode.Double * TCMAX + TypeCode.Int16,
                    TypeCode.Double * TCMAX + TypeCode.Int32,
                    TypeCode.Double * TCMAX + TypeCode.Int64,
                    TypeCode.Double * TCMAX + TypeCode.Single,
                    TypeCode.Double * TCMAX + TypeCode.Double,
                    TypeCode.Byte * TCMAX + TypeCode.Double,
                    TypeCode.Int16 * TCMAX + TypeCode.Double,
                    TypeCode.Int32 * TCMAX + TypeCode.Double,
                    TypeCode.Int64 * TCMAX + TypeCode.Double,
                    TypeCode.Single * TCMAX + TypeCode.Double,
                    TypeCode.Double * TCMAX + TypeCode.Decimal,
                    TypeCode.Decimal * TCMAX + TypeCode.Double
                    Return ModDouble(conv1.ToDouble(Nothing), conv2.ToDouble(Nothing))

                Case TypeCode.Double * TCMAX + TypeCode.Boolean
                    Return ModDouble(conv1.ToDouble(Nothing), ToVBBool(conv2))

                Case TypeCode.Boolean * TCMAX + TypeCode.Double
                    Return ModDouble(ToVBBool(conv1), conv2.ToDouble(Nothing))


                    'SINGLE
                Case TypeCode.Single * TCMAX + TypeCode.Byte,
                    TypeCode.Single * TCMAX + TypeCode.Int16,
                    TypeCode.Single * TCMAX + TypeCode.Int32,
                    TypeCode.Single * TCMAX + TypeCode.Int64,
                    TypeCode.Single * TCMAX + TypeCode.Single,
                    TypeCode.Byte * TCMAX + TypeCode.Single,
                    TypeCode.Int16 * TCMAX + TypeCode.Single,
                    TypeCode.Int32 * TCMAX + TypeCode.Single,
                    TypeCode.Int64 * TCMAX + TypeCode.Single,
                    TypeCode.Decimal * TCMAX + TypeCode.Single,
                    TypeCode.Single * TCMAX + TypeCode.Decimal
                    Return ModSingle(conv1.ToSingle(Nothing), conv2.ToSingle(Nothing))

                Case TypeCode.Single * TCMAX + TypeCode.Boolean
                    Return ModSingle(conv1.ToSingle(Nothing), ToVBBool(conv2))

                Case TypeCode.Boolean * TCMAX + TypeCode.Single
                    Return ModSingle(ToVBBool(conv1), conv2.ToSingle(Nothing))


                    'INT64
                Case TypeCode.Byte * TCMAX + TypeCode.Int64,
                    TypeCode.Int64 * TCMAX + TypeCode.Byte,
                    TypeCode.Int64 * TCMAX + TypeCode.Int16,
                    TypeCode.Int64 * TCMAX + TypeCode.Int32,
                    TypeCode.Int64 * TCMAX + TypeCode.Int64,
                    TypeCode.Int16 * TCMAX + TypeCode.Int64,
                    TypeCode.Int32 * TCMAX + TypeCode.Int64
                    Return ModInt64(conv1.ToInt64(Nothing), conv2.ToInt64(Nothing))

                Case TypeCode.Int64 * TCMAX + TypeCode.Boolean
                    Return ModInt64(conv1.ToInt64(Nothing), ToVBBool(conv2))

                Case TypeCode.Boolean * TCMAX + TypeCode.Int64
                    Return ModInt64(ToVBBool(conv1), conv2.ToInt64(Nothing))


                    'INT32
                Case TypeCode.Int16 * TCMAX + TypeCode.Int32,
                    TypeCode.Int32 * TCMAX + TypeCode.Int16,
                    TypeCode.Int32 * TCMAX + TypeCode.Int32,
                    TypeCode.Int32 * TCMAX + TypeCode.Byte,
                    TypeCode.Byte * TCMAX + TypeCode.Int32
                    Return ModInt32(conv1.ToInt32(Nothing), conv2.ToInt32(Nothing))

                Case TypeCode.Int32 * TCMAX + TypeCode.Boolean
                    Return ModInt32(conv1.ToInt32(Nothing), ToVBBool(conv2))

                Case TypeCode.Boolean * TCMAX + TypeCode.Int32
                    Return ModInt32(ToVBBool(conv1), conv2.ToInt32(Nothing))


                    'INT16
                Case TypeCode.Int16 * TCMAX + TypeCode.Byte,
                    TypeCode.Int16 * TCMAX + TypeCode.Int16,
                    TypeCode.Byte * TCMAX + TypeCode.Int16
                    Return ModInt16(conv1.ToInt16(Nothing), conv2.ToInt16(Nothing))

                Case TypeCode.Boolean * TCMAX + TypeCode.Byte,
                    TypeCode.Boolean * TCMAX + TypeCode.Int16
                    Return ModInt16(CShort(ToVBBool(conv1)), conv2.ToInt16(Nothing))

                Case TypeCode.Byte * TCMAX + TypeCode.Boolean,
                    TypeCode.Int16 * TCMAX + TypeCode.Boolean
                    Return ModInt16(conv1.ToInt16(Nothing), CShort(ToVBBool(conv2)))

                Case TypeCode.Boolean * TCMAX + TypeCode.Boolean
                    Return ModInt16(CShort(ToVBBool(conv1)), CShort(ToVBBool(conv2)))


                    'BYTE
                Case TypeCode.Byte * TCMAX + TypeCode.Byte
                    Return ModByte(conv1.ToByte(Nothing), conv2.ToByte(Nothing))


                Case Else

            End Select

            Throw GetNoValidOperatorException(o1, o2)

        End Function

        Private Shared Function ModString(ByVal conv1 As IConvertible, ByVal tc1 As TypeCode, ByVal conv2 As IConvertible, ByVal tc2 As TypeCode) As Object
            Dim dbl1, dbl2 As Double

            If tc1 = TypeCode.String Then
                dbl1 = DoubleType.FromString(conv1.ToString(Nothing))
            ElseIf tc1 = TypeCode.Boolean Then
                dbl1 = ToVBBool(conv1)
            Else
                dbl1 = conv1.ToDouble(Nothing)
            End If

            If tc2 = TypeCode.String Then
                dbl2 = DoubleType.FromString(conv2.ToString(Nothing))
            ElseIf tc2 = TypeCode.Boolean Then
                dbl2 = ToVBBool(conv2)
            Else
                dbl2 = conv2.ToDouble(Nothing)
            End If

            Return dbl1 Mod dbl2
        End Function

        Private Shared Function ModStringString(ByVal s1 As String, ByVal s2 As String) As Object
            Dim dbl1, dbl2 As Double

            If Not s1 Is Nothing Then
                dbl1 = DoubleType.FromString(s1)
            End If

            If Not s2 Is Nothing Then
                dbl2 = DoubleType.FromString(s2)
            End If

            Return dbl1 Mod dbl2

        End Function

        Private Shared Function ModByte(ByVal i1 As Byte, ByVal i2 As Byte) As Object
            Return i1 Mod i2
        End Function

        Private Shared Function ModInt16(ByVal i1 As Short, ByVal i2 As Short) As Object
            'Do operation with Int64 to avoid OverflowException with Int16.MinValue and -1
            Dim result As Integer = CInt(i1) Mod CInt(i2)

            If result < Int16.MinValue OrElse result > Int16.MaxValue Then
                Return result
            Else
                Return CShort(result)
            End If
        End Function

        Private Shared Function ModInt32(ByVal i1 As Integer, ByVal i2 As Integer) As Object

            'Do operation with Int64 to avoid OverflowException with Int32.MinValue and -1
            Dim result As Long = CLng(i1) Mod CLng(i2)

            If result < Int32.MinValue OrElse result > Int32.MaxValue Then
                Return result
            Else
                Return CInt(result)
            End If
        End Function

        Private Shared Function ModInt64(ByVal i1 As Long, ByVal i2 As Long) As Object
            'If i1 = Int64.MinValue and i2 = -1, then we get an overflow
            Try
                Return i1 Mod i2
            Catch ex As OverflowException
                Dim DecimalResult As Decimal
                DecimalResult = CDec(i1) Mod CDec(i2)
                'Overflow is not caused by remainder, so we will most likely still return Int64
                If DecimalResult < Int64.MinValue OrElse DecimalResult > Int64.MaxValue Then
                    Return DecimalResult
                Else
                    Return CLng(DecimalResult)
                End If
            End Try
        End Function

        Private Shared Function ModSingle(ByVal sng1 As Single, ByVal sng2 As Single) As Object
            Return sng1 Mod sng2
        End Function

        Private Shared Function ModDouble(ByVal d1 As Double, ByVal d2 As Double) As Object
            Return d1 Mod d2
        End Function

        Private Shared Function ModDecimal(ByVal conv1 As IConvertible, ByVal conv2 As IConvertible) As Object
            Dim d1, d2 As Decimal
            If Not conv1 Is Nothing Then
                d1 = conv1.ToDecimal(Nothing)
            End If
            If Not conv2 Is Nothing Then
                d2 = conv2.ToDecimal(Nothing)
            End If

            Return (d1 Mod d2)
        End Function

        Public Shared Function IDivObj(ByVal o1 As Object, ByVal o2 As Object) As Object

            Dim conv1, conv2 As IConvertible
            Dim tc1, tc2 As TypeCode

            conv1 = TryCast(o1, IConvertible)

            If conv1 Is Nothing Then
                If o1 Is Nothing Then
                    tc1 = TypeCode.Empty
                Else
                    tc1 = TypeCode.Object
                End If
            Else
                tc1 = conv1.GetTypeCode()
            End If


            conv2 = TryCast(o2, IConvertible)

            If conv2 Is Nothing Then
                If o2 Is Nothing Then
                    tc2 = TypeCode.Empty
                Else
                    tc2 = TypeCode.Object
                End If
            Else
                tc2 = conv2.GetTypeCode()
            End If

            Select Case tc1 * TCMAX + tc2

                'STRING
                Case TypeCode.Empty * TCMAX + TypeCode.String
                    Return IDivideInt64(0, LongType.FromString(conv2.ToString(Nothing)))

                Case TypeCode.String * TCMAX + TypeCode.Empty
                    Return IDivideInt64(LongType.FromString(conv1.ToString(Nothing)), 0)

                Case TypeCode.Byte * TCMAX + TypeCode.String,
                    TypeCode.Int16 * TCMAX + TypeCode.String,
                    TypeCode.Int32 * TCMAX + TypeCode.String,
                    TypeCode.Int64 * TCMAX + TypeCode.String,
                    TypeCode.Single * TCMAX + TypeCode.String,
                    TypeCode.Double * TCMAX + TypeCode.String,
                    TypeCode.Decimal * TCMAX + TypeCode.String
                    Return IDivideString(conv1, tc1, conv2, tc2)

                Case TypeCode.String * TCMAX + TypeCode.String
                    Return IDivideStringString(conv1.ToString(Nothing), conv2.ToString(Nothing))

                Case TypeCode.String * TCMAX + TypeCode.Boolean
                    Return IDivideInt64(LongType.FromString(conv1.ToString(Nothing)), ToVBBool(conv2))

                Case TypeCode.String * TCMAX + TypeCode.Byte,
                    TypeCode.String * TCMAX + TypeCode.Int16,
                    TypeCode.String * TCMAX + TypeCode.Int32,
                    TypeCode.String * TCMAX + TypeCode.Int64,
                    TypeCode.String * TCMAX + TypeCode.Single,
                    TypeCode.String * TCMAX + TypeCode.Double,
                    TypeCode.String * TCMAX + TypeCode.Decimal
                    Return IDivideInt64(LongType.FromString(conv1.ToString(Nothing)), conv2.ToInt64(Nothing))


                    'EMPTY
                Case TypeCode.Empty * TCMAX + TypeCode.Empty
                    Return IDivideInt32(0, 0)

                Case TypeCode.Boolean * TCMAX + TypeCode.Empty
                    Return IDivideInt16(CShort(ToVBBool(conv1)), 0)

                Case TypeCode.Byte * TCMAX + TypeCode.Empty
                    Return IDivideByte(conv1.ToByte(Nothing), 0)

                Case TypeCode.Int16 * TCMAX + TypeCode.Empty
                    Return IDivideInt16(conv1.ToInt16(Nothing), 0)

                Case TypeCode.Int32 * TCMAX + TypeCode.Empty
                    Return IDivideInt32(conv1.ToInt32(Nothing), 0)

                Case TypeCode.Int64 * TCMAX + TypeCode.Empty,
                     TypeCode.Decimal * TCMAX + TypeCode.Empty,
                     TypeCode.Single * TCMAX + TypeCode.Empty,
                     TypeCode.Double * TCMAX + TypeCode.Empty
                    Return IDivideInt64(conv1.ToInt64(Nothing), 0)

                Case TypeCode.Empty * TCMAX + TypeCode.Boolean
                    Return IDivideInt64(0, ToVBBool(conv2))

                Case TypeCode.Empty * TCMAX + TypeCode.Byte
                    Return IDivideByte(0, conv2.ToByte(Nothing))

                Case TypeCode.Empty * TCMAX + TypeCode.Int16
                    Return IDivideInt16(0, conv2.ToInt16(Nothing))

                Case TypeCode.Empty * TCMAX + TypeCode.Int32
                    Return IDivideInt32(0, conv2.ToInt32(Nothing))

                Case TypeCode.Empty * TCMAX + TypeCode.Int64,
                     TypeCode.Empty * TCMAX + TypeCode.Decimal,
                     TypeCode.Empty * TCMAX + TypeCode.Single,
                     TypeCode.Empty * TCMAX + TypeCode.Double
                    Return IDivideInt64(0, conv2.ToInt64(Nothing))

                    'BOOLEAN
                Case TypeCode.Boolean * TCMAX + TypeCode.Byte,
                     TypeCode.Boolean * TCMAX + TypeCode.Int16
                    Return IDivideInt16(CShort(ToVBBool(conv1)), conv2.ToInt16(Nothing))

                Case TypeCode.Boolean * TCMAX + TypeCode.Int32
                    Return IDivideInt32(ToVBBool(conv1), conv2.ToInt32(Nothing))

                Case TypeCode.Boolean * TCMAX + TypeCode.Int64,
                     TypeCode.Boolean * TCMAX + TypeCode.Decimal,
                     TypeCode.Boolean * TCMAX + TypeCode.Single,
                     TypeCode.Boolean * TCMAX + TypeCode.Double

                    Return IDivideInt64(ToVBBool(conv1), conv2.ToInt64(Nothing))

                Case TypeCode.Boolean * TCMAX + TypeCode.Boolean
                    Return IDivideInt16(CShort(ToVBBool(conv1)), CShort(ToVBBool(conv2)))

                Case TypeCode.Boolean * TCMAX + TypeCode.String
                    Return IDivideInt64(ToVBBool(conv1), LongType.FromString(conv2.ToString(Nothing)))

                Case TypeCode.Byte * TCMAX + TypeCode.Boolean,
                     TypeCode.Int16 * TCMAX + TypeCode.Boolean
                    Return IDivideInt16(conv1.ToInt16(Nothing), CShort(ToVBBool(conv2)))

                Case TypeCode.Int32 * TCMAX + TypeCode.Boolean
                    Return IDivideInt32(conv1.ToInt32(Nothing), ToVBBool(conv2))

                Case TypeCode.Int64 * TCMAX + TypeCode.Boolean,
                     TypeCode.Decimal * TCMAX + TypeCode.Boolean,
                     TypeCode.Single * TCMAX + TypeCode.Boolean,
                     TypeCode.Double * TCMAX + TypeCode.Boolean
                    Return IDivideInt64(conv1.ToInt64(Nothing), ToVBBool(conv2))

                Case TypeCode.Byte * TCMAX + TypeCode.Byte
                    Return IDivideByte(conv1.ToByte(Nothing), conv2.ToByte(Nothing))

                Case TypeCode.Byte * TCMAX + TypeCode.Int16,
                     TypeCode.Int16 * TCMAX + TypeCode.Byte,
                     TypeCode.Int16 * TCMAX + TypeCode.Int16

                    Return IDivideInt16(conv1.ToInt16(Nothing), conv2.ToInt16(Nothing))

                Case TypeCode.Int16 * TCMAX + TypeCode.Int32,
                     TypeCode.Int32 * TCMAX + TypeCode.Int16,
                     TypeCode.Int32 * TCMAX + TypeCode.Int32,
                     TypeCode.Int32 * TCMAX + TypeCode.Byte,
                     TypeCode.Byte * TCMAX + TypeCode.Int32
                    Return IDivideInt32(conv1.ToInt32(Nothing), conv2.ToInt32(Nothing))

                    'OTHERS
                Case TypeCode.Double * TCMAX + TypeCode.Byte,
                     TypeCode.Double * TCMAX + TypeCode.Int16,
                     TypeCode.Double * TCMAX + TypeCode.Int32,
                     TypeCode.Double * TCMAX + TypeCode.Int64,
                     TypeCode.Double * TCMAX + TypeCode.Single,
                     TypeCode.Double * TCMAX + TypeCode.Double,
                     TypeCode.Byte * TCMAX + TypeCode.Double,
                     TypeCode.Int16 * TCMAX + TypeCode.Double,
                     TypeCode.Int32 * TCMAX + TypeCode.Double,
                     TypeCode.Int64 * TCMAX + TypeCode.Double,
                     TypeCode.Single * TCMAX + TypeCode.Double,
                     TypeCode.Double * TCMAX + TypeCode.Decimal,
                     TypeCode.Decimal * TCMAX + TypeCode.Double,
                     TypeCode.Single * TCMAX + TypeCode.Byte,
                     TypeCode.Single * TCMAX + TypeCode.Int16,
                     TypeCode.Single * TCMAX + TypeCode.Int32,
                     TypeCode.Single * TCMAX + TypeCode.Int64,
                     TypeCode.Single * TCMAX + TypeCode.Single,
                     TypeCode.Byte * TCMAX + TypeCode.Single,
                     TypeCode.Int16 * TCMAX + TypeCode.Single,
                     TypeCode.Int32 * TCMAX + TypeCode.Single,
                     TypeCode.Int64 * TCMAX + TypeCode.Single,
                     TypeCode.Decimal * TCMAX + TypeCode.Single,
                     TypeCode.Single * TCMAX + TypeCode.Decimal,
                     TypeCode.Byte * TCMAX + TypeCode.Int64,
                     TypeCode.Int64 * TCMAX + TypeCode.Byte,
                     TypeCode.Int64 * TCMAX + TypeCode.Int16,
                     TypeCode.Int64 * TCMAX + TypeCode.Int32,
                     TypeCode.Int64 * TCMAX + TypeCode.Int64,
                     TypeCode.Int16 * TCMAX + TypeCode.Int64,
                     TypeCode.Int32 * TCMAX + TypeCode.Int64,
                     TypeCode.Decimal * TCMAX + TypeCode.Byte,
                     TypeCode.Decimal * TCMAX + TypeCode.Int16,
                     TypeCode.Decimal * TCMAX + TypeCode.Int32,
                     TypeCode.Decimal * TCMAX + TypeCode.Int64,
                     TypeCode.Decimal * TCMAX + TypeCode.Decimal,
                     TypeCode.Int16 * TCMAX + TypeCode.Decimal,
                     TypeCode.Byte * TCMAX + TypeCode.Decimal,
                     TypeCode.Int32 * TCMAX + TypeCode.Decimal,
                     TypeCode.Int64 * TCMAX + TypeCode.Decimal
                    Return IDivideInt64(conv1.ToInt64(Nothing), conv2.ToInt64(Nothing))

                Case Else

            End Select

            Throw GetNoValidOperatorException(o1, o2)

        End Function

        Private Shared Function IDivideString(ByVal conv1 As IConvertible, ByVal tc1 As TypeCode, ByVal conv2 As IConvertible, ByVal tc2 As TypeCode) As Object
            Dim lng1, lng2 As Int64

            If tc1 = TypeCode.String Then
                Try
                    lng1 = LongType.FromString(conv1.ToString(Nothing))
                Catch ex As StackOverflowException
                    Throw ex
                Catch ex As OutOfMemoryException
                    Throw ex
                Catch ex As System.Threading.ThreadAbortException
                    Throw ex
                Catch
                    Throw GetNoValidOperatorException(conv1, conv2)
                End Try
            ElseIf tc1 = TypeCode.Boolean Then
                lng1 = ToVBBool(conv1)
            Else
                lng1 = conv1.ToInt64(Nothing)
            End If

            If tc2 = TypeCode.String Then
                Try
                    lng2 = LongType.FromString(conv2.ToString(Nothing))
                Catch ex As StackOverflowException
                    Throw ex
                Catch ex As OutOfMemoryException
                    Throw ex
                Catch ex As System.Threading.ThreadAbortException
                    Throw ex
                Catch
                    Throw GetNoValidOperatorException(conv1, conv2)
                End Try
            ElseIf tc2 = TypeCode.Boolean Then
                lng2 = ToVBBool(conv2)
            Else
                lng2 = conv2.ToInt64(Nothing)
            End If

            Return lng1 \ lng2
        End Function

        Private Shared Function IDivideStringString(ByVal s1 As String, ByVal s2 As String) As Object
            Dim lng1, lng2 As Int64

            If Not s1 Is Nothing Then
                lng1 = LongType.FromString(s1)
            End If

            If Not s2 Is Nothing Then
                lng2 = LongType.FromString(s2)
            End If

            Return lng1 \ lng2

        End Function

        Private Shared Function IDivideByte(ByVal d1 As Byte, ByVal d2 As Byte) As Object
            Return d1 \ d2
        End Function

        Private Shared Function IDivideInt16(ByVal d1 As Int16, ByVal d2 As Int16) As Object
            Return d1 \ d2
        End Function

        Private Shared Function IDivideInt32(ByVal d1 As Int32, ByVal d2 As Int32) As Object
            Return d1 \ d2
        End Function

        Private Shared Function IDivideInt64(ByVal d1 As Int64, ByVal d2 As Int64) As Object
            Return d1 \ d2
        End Function

        Public Shared Function ShiftLeftObj(ByVal o1 As Object, ByVal amount As Int32) As Object

            Dim conv1 As IConvertible
            Dim tc1 As TypeCode

            conv1 = TryCast(o1, IConvertible)

            If conv1 Is Nothing Then
                If o1 Is Nothing Then
                    tc1 = TypeCode.Empty
                Else
                    tc1 = TypeCode.Object
                End If
            Else
                tc1 = conv1.GetTypeCode()
            End If

            Select Case tc1
                Case TypeCode.Empty
                    Return Nothing << amount
                Case TypeCode.Boolean
                    Return CShort(conv1.ToBoolean(Nothing)) << amount
                Case TypeCode.Byte
                    Return conv1.ToByte(Nothing) << amount
                Case TypeCode.Int16
                    Return conv1.ToInt16(Nothing) << amount
                Case TypeCode.Int32
                    Return conv1.ToInt32(Nothing) << amount
                Case TypeCode.Int64, TypeCode.Single, TypeCode.Double, TypeCode.Decimal
                    Return conv1.ToInt64(Nothing) << amount
                Case TypeCode.String
                    Return LongType.FromString(conv1.ToString(Nothing)) << amount
            End Select

            Throw GetNoValidOperatorException(o1)
        End Function

        Public Shared Function ShiftRightObj(ByVal o1 As Object, ByVal amount As Int32) As Object

            Dim conv1 As IConvertible
            Dim tc1 As TypeCode

            conv1 = TryCast(o1, IConvertible)

            If conv1 Is Nothing Then
                If o1 Is Nothing Then
                    tc1 = TypeCode.Empty
                Else
                    tc1 = TypeCode.Object
                End If
            Else
                tc1 = conv1.GetTypeCode()
            End If

            Select Case tc1
                Case TypeCode.Empty
                    Return Nothing >> amount
                Case TypeCode.Boolean
                    Return CShort(conv1.ToBoolean(Nothing)) >> amount
                Case TypeCode.Byte
                    Return conv1.ToByte(Nothing) >> amount
                Case TypeCode.Int16
                    Return conv1.ToInt16(Nothing) >> amount
                Case TypeCode.Int32
                    Return conv1.ToInt32(Nothing) >> amount
                Case TypeCode.Int64, TypeCode.Single, TypeCode.Double, TypeCode.Decimal
                    Return conv1.ToInt64(Nothing) >> amount
                Case TypeCode.String
                    Return LongType.FromString(conv1.ToString(Nothing)) >> amount
            End Select

            Throw GetNoValidOperatorException(o1)
        End Function

        Public Shared Function XorObj(ByVal obj1 As Object, ByVal obj2 As Object) As Object

            If obj1 Is Nothing AndAlso obj2 Is Nothing Then
                Return False
            End If

            Select Case GetWidestType(obj1, obj2)

                Case TypeCode.Boolean,
                     TypeCode.Byte,
                     TypeCode.Int16,
                     TypeCode.Int32,
                     TypeCode.Int64,
                     TypeCode.Decimal,
                     TypeCode.Single,
                     TypeCode.Double,
                     TypeCode.String
                    Return BooleanType.FromObject(obj1) Xor BooleanType.FromObject(obj2)

                Case TypeCode.Char
                    ' Fall through to error

                Case TypeCode.DateTime
                    ' Fall through to error

                Case Else
                    ' Fall through to error

            End Select

            Throw GetNoValidOperatorException(obj1, obj2)

        End Function

        Public Shared Function LikeObj(ByVal vLeft As Object, ByVal vRight As Object, ByVal CompareOption As CompareMethod) As Boolean
            Return StrLike(StringType.FromObject(vLeft), StringType.FromObject(vRight), CompareOption)
        End Function

        Public Shared Function StrCatObj(ByVal vLeft As Object, ByVal vRight As Object) As Object
            Dim LeftIsNull As Boolean = TypeOf vLeft Is System.DBNull
            Dim RightIsNull As Boolean = TypeOf vRight Is System.DBNull

            If LeftIsNull And RightIsNull Then
                Return vLeft
            ElseIf LeftIsNull And Not RightIsNull Then
                vLeft = ""
            ElseIf RightIsNull And Not LeftIsNull Then
                vRight = ""
            End If

            Return StringType.FromObject(vLeft) & StringType.FromObject(vRight)
        End Function

        Friend Overloads Shared Function CTypeHelper(ByVal obj As Object, ByVal toType As TypeCode) As Object

            If obj Is Nothing Then
                Return Nothing
            End If

            Select Case toType

                Case TypeCode.Boolean
                    Return BooleanType.FromObject(obj)

                Case TypeCode.Byte
                    Return ByteType.FromObject(obj)

                Case TypeCode.Int16
                    Return ShortType.FromObject(obj)

                Case TypeCode.Int32
                    Return IntegerType.FromObject(obj)

                Case TypeCode.Int64
                    Return LongType.FromObject(obj)

                Case TypeCode.Decimal
                    Return DecimalType.FromObject(obj)

                Case TypeCode.Single
                    Return SingleType.FromObject(obj)

                Case TypeCode.Double
                    Return DoubleType.FromObject(obj)

                Case TypeCode.String
                    Return StringType.FromObject(obj)

                Case TypeCode.Char
                    Return CharType.FromObject(obj)

                Case TypeCode.DateTime
                    Return DateType.FromObject(obj)

                Case Else
                    ' Fall through and throw exception

            End Select

            Throw New InvalidCastException(GetResourceString(SR.InvalidCast_FromTo, VBFriendlyName(obj), VBFriendlyName(TypeFromTypeCode(toType))))

        End Function

        Friend Overloads Shared Function CTypeHelper(ByVal obj As Object, ByVal toType As Type) As Object
            Dim fromType As System.Type
            Dim IsToByRef As Boolean
            Dim Result As Object

            If obj Is Nothing Then
                Return Nothing
            End If

            If toType Is GetType(Object) Then
                Return obj
            End If

            fromType = obj.GetType()

            If toType.IsByRef Then
                toType = toType.GetElementType()
                IsToByRef = True
            End If

            If fromType.IsByRef Then
                fromType = fromType.GetElementType()
            End If

            If (fromType Is toType OrElse toType Is GetType(Object)) Then
                If IsToByRef Then
                    'Make sure we copy boxed primitives
                    Result = ObjectType.GetObjectValuePrimitive(obj)
                    GoTo CheckForEnumAndExit
                Else
                    Return obj
                End If
            End If

            Dim toTypeCode As TypeCode = Type.GetTypeCode(toType)

            If toTypeCode = TypeCode.Object Then
                If toType Is GetType(Object) OrElse toType.IsInstanceOfType(obj) Then
                    Return obj
                    'Char() typecode is object, so we need to test for it here
                Else
                    Dim ObjString As String = TryCast(obj, String)

                    If (ObjString IsNot Nothing) AndAlso (toType Is GetType(Char())) Then
                        Return CharArrayType.FromString(ObjString)
                    Else
                        Throw New InvalidCastException(GetResourceString(SR.InvalidCast_FromTo, VBFriendlyName(fromType), VBFriendlyName(toType)))
                    End If
                End If
            Else
                Result = CTypeHelper(obj, toTypeCode)
            End If

CheckForEnumAndExit:
            If toType.IsEnum Then
                Return System.Enum.ToObject(toType, Result)
            End If

            Return Result
        End Function

        Private Shared Function GetNoValidOperatorException(ByVal Operand As Object) As Exception
            Return New InvalidCastException(GetResourceString(SR.NoValidOperator_OneOperand, VBFriendlyName(Operand)))
        End Function

        Private Shared Function GetNoValidOperatorException(ByVal Left As Object, ByVal Right As Object) As Exception
            Const MAX_INSERTION_SIZE As Integer = 32

            Dim Substitution1 As String
            Dim Substitution2 As String

            If Left Is Nothing Then
                Substitution1 = "'Nothing'"
            Else
                Dim LeftString As String = TryCast(Left, String)

                If LeftString IsNot Nothing Then
                    Substitution1 =
                        GetResourceString(SR.NoValidOperator_StringType1, Strings.Left(LeftString, MAX_INSERTION_SIZE))
                Else
                    Substitution1 = GetResourceString(SR.NoValidOperator_NonStringType1, VBFriendlyName(Left))
                End If
            End If

            If Right Is Nothing Then
                Substitution2 = "'Nothing'"
            Else
                Dim RightString As String = TryCast(Right, String)

                If RightString IsNot Nothing Then
                    Substitution2 =
                        GetResourceString(SR.NoValidOperator_StringType1, Strings.Left(RightString, MAX_INSERTION_SIZE))
                Else
                    Substitution2 = GetResourceString(SR.NoValidOperator_NonStringType1, VBFriendlyName(Right))
                End If
            End If

            Return New InvalidCastException(GetResourceString(SR.NoValidOperator_TwoOperands, Substitution1, Substitution2))
        End Function

        '**
        '** Used when RuntimeHelpers.GetObjectValue has already been called
        '**
        '** This is used to prevent copying structures multiple times
        '**
        Public Shared Function GetObjectValuePrimitive(ByVal o As Object) As Object

            Dim iconv As IConvertible

            If o Is Nothing Then
                Return Nothing
            End If

            iconv = TryCast(o, IConvertible)

            If iconv Is Nothing Then
                Return o
            End If

            Select Case iconv.GetTypeCode()

                Case TypeCode.Char
                    Return iconv.ToChar(Nothing)

                Case TypeCode.String
                    Return o

                Case TypeCode.Boolean
                    Return iconv.ToBoolean(Nothing)

                Case TypeCode.Byte
                    Return iconv.ToByte(Nothing)

                Case TypeCode.SByte
                    Return iconv.ToSByte(Nothing)

                Case TypeCode.Int16
                    Return iconv.ToInt16(Nothing)

                Case TypeCode.UInt16
                    Return iconv.ToUInt16(Nothing)

                Case TypeCode.Int32
                    Return iconv.ToInt32(Nothing)

                Case TypeCode.UInt32
                    Return iconv.ToUInt32(Nothing)

                Case TypeCode.Int64
                    Return iconv.ToInt64(Nothing)

                Case TypeCode.UInt64
                    Return iconv.ToUInt64(Nothing)

                Case TypeCode.Single
                    Return iconv.ToSingle(Nothing)

                Case TypeCode.Double
                    Return iconv.ToDouble(Nothing)

                Case TypeCode.Decimal
                    Return iconv.ToDecimal(Nothing)

                Case TypeCode.DateTime
                    Return iconv.ToDateTime(Nothing)

                Case Else
                    Return o

            End Select

        End Function

    End Class

End Namespace

