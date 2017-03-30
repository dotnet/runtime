' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Option Strict On

Imports System
Imports System.Diagnostics
Imports System.Globalization
Imports System.Collections.Generic
Imports System.Reflection

Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils
Imports Microsoft.VisualBasic.CompilerServices.Symbols
Imports Microsoft.VisualBasic.CompilerServices.Utils


Namespace Microsoft.VisualBasic.CompilerServices

    ' Implements Object operators
    Public NotInheritable Class Operators

        Friend Shared ReadOnly Boxed_ZeroDouble As Object = 0.0R
        Friend Shared ReadOnly Boxed_ZeroSinge As Object = 0.0F
        Friend Shared ReadOnly Boxed_ZeroDecimal As Object = CDec(0)
        Friend Shared ReadOnly Boxed_ZeroLong As Object = 0L
        Friend Shared ReadOnly Boxed_ZeroInteger As Object = 0I
        Friend Shared ReadOnly Boxed_ZeroShort As Object = 0S
        Friend Shared ReadOnly Boxed_ZeroULong As Object = 0UL
        Friend Shared ReadOnly Boxed_ZeroUInteger As Object = 0UI
        Friend Shared ReadOnly Boxed_ZeroUShort As Object = 0US
        Friend Shared ReadOnly Boxed_ZeroSByte As Object = CSByte(0)
        Friend Shared ReadOnly Boxed_ZeroByte As Object = CByte(0)

        Private Sub New()
        End Sub

        Private Const s_TCMAX As Integer = TypeCode.String + 1

        Private Shared Function ToVBBool(ByVal conv As Object) As SByte
            Return CSByte(Convert.ToBoolean(conv))
        End Function

        Private Shared Function GetTypeCode(ByVal o As Object) As TypeCode
            If o Is Nothing Then
                Return TypeCode.Empty
            Else
                Return o.GetType.GetTypeCode
            End If
        End Function

        'This function determines the enum result type of And, Or, Xor operations.
        'If the type of Left and Right are the same enum type, then return that type, otherwise if
        'one is an enum and the other is Nothing, return that type, otherwise return Nothing.
        Private Shared Function GetEnumResult(ByVal left As Object, ByVal right As Object) As Type

            Debug.Assert(left Is Nothing OrElse right Is Nothing OrElse left.GetType.GetTypeCode = right.GetType.GetTypeCode,
                         "Expected identical type codes for checking enum result")


            If left IsNot Nothing Then

                If TypeOf left Is System.Enum Then

                    If right Is Nothing Then
                        Return left.GetType

                    ElseIf TypeOf right Is System.Enum Then
                        Dim leftType As Type = left.GetType
                        If leftType Is right.GetType Then
                            Return leftType
                        End If

                    End If

                End If

            ElseIf TypeOf right Is System.Enum Then
                Return right.GetType

            End If

            Return Nothing

        End Function

        Private Shared Function GetNoValidOperatorException(ByVal op As UserDefinedOperator, ByVal operand As Object) As Exception
            Return New InvalidCastException(GetResourceString(SR.UnaryOperand2, OperatorNames(op), VBFriendlyName(operand)))
        End Function

        Private Shared Function GetNoValidOperatorException(ByVal op As UserDefinedOperator, ByVal left As Object, ByVal right As Object) As Exception
            Const maxInsertionSize As Integer = 32

            Dim substitution1 As String
            Dim substitution2 As String

            If left Is Nothing Then
                substitution1 = "'Nothing'"
            Else
                Dim leftString As String = TryCast(left, String)

                If leftString IsNot Nothing Then
                    substitution1 =
                        GetResourceString(SR.NoValidOperator_StringType1, Strings.Left(leftString, maxInsertionSize))
                Else
                    substitution1 = GetResourceString(SR.NoValidOperator_NonStringType1, VBFriendlyName(left))
                End If
            End If

            If right Is Nothing Then
                substitution2 = "'Nothing'"
            Else
                Dim rightString As String = TryCast(right, String)

                If rightString IsNot Nothing Then
                    substitution2 =
                        GetResourceString(SR.NoValidOperator_StringType1, Strings.Left(rightString, maxInsertionSize))
                Else
                    substitution2 = GetResourceString(SR.NoValidOperator_NonStringType1, VBFriendlyName(right))
                End If
            End If

            Return New InvalidCastException(GetResourceString(SR.BinaryOperands3, OperatorNames(op), substitution1, substitution2))
        End Function

#Region " Comparison Operators = <> < <= > >= "

        Private Enum CompareClass
            Less = -1
            Equal = 0
            Greater = 1
            Unordered
            UserDefined
            Undefined
        End Enum

        Public Shared Function CompareObjectEqual(ByVal left As Object, ByVal right As Object, ByVal textCompare As Boolean) As Object
            Dim comparison As CompareClass = CompareObject2(left, right, textCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return False
                Case CompareClass.UserDefined
                    Return InvokeUserDefinedOperator(UserDefinedOperator.Equal, left, right)
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.Equal, left, right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison = 0
            End Select
        End Function

        Public Shared Function ConditionalCompareObjectEqual(ByVal left As Object, ByVal right As Object, ByVal textCompare As Boolean) As Boolean
            Dim comparison As CompareClass = CompareObject2(left, right, textCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return False
                Case CompareClass.UserDefined
                    Return CBool(InvokeUserDefinedOperator(UserDefinedOperator.Equal, left, right))
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.Equal, left, right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison = 0
            End Select
        End Function

        Public Shared Function CompareObjectNotEqual(ByVal left As Object, ByVal right As Object, ByVal textCompare As Boolean) As Object
            Dim comparison As CompareClass = CompareObject2(left, right, textCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return True
                Case CompareClass.UserDefined
                    Return InvokeUserDefinedOperator(UserDefinedOperator.NotEqual, left, right)
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.NotEqual, left, right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison <> 0
            End Select
        End Function

        Public Shared Function ConditionalCompareObjectNotEqual(ByVal left As Object, ByVal right As Object, ByVal textCompare As Boolean) As Boolean
            Dim comparison As CompareClass = CompareObject2(left, right, textCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return True
                Case CompareClass.UserDefined
                    Return CBool(InvokeUserDefinedOperator(UserDefinedOperator.NotEqual, left, right))
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.NotEqual, left, right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison <> 0
            End Select
        End Function

        Public Shared Function CompareObjectLess(ByVal left As Object, ByVal right As Object, ByVal textCompare As Boolean) As Object
            Dim comparison As CompareClass = CompareObject2(left, right, textCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return False
                Case CompareClass.UserDefined
                    Return InvokeUserDefinedOperator(UserDefinedOperator.Less, left, right)
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.Less, left, right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison < 0
            End Select
        End Function

        Public Shared Function ConditionalCompareObjectLess(ByVal left As Object, ByVal right As Object, ByVal textCompare As Boolean) As Boolean
            Dim comparison As CompareClass = CompareObject2(left, right, textCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return False
                Case CompareClass.UserDefined
                    Return CBool(InvokeUserDefinedOperator(UserDefinedOperator.Less, left, right))
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.Less, left, right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison < 0
            End Select
        End Function

        Public Shared Function CompareObjectLessEqual(ByVal left As Object, ByVal right As Object, ByVal textCompare As Boolean) As Object
            Dim comparison As CompareClass = CompareObject2(left, right, textCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return False
                Case CompareClass.UserDefined
                    Return InvokeUserDefinedOperator(UserDefinedOperator.LessEqual, left, right)
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.LessEqual, left, right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison <= 0
            End Select
        End Function

        Public Shared Function ConditionalCompareObjectLessEqual(ByVal left As Object, ByVal right As Object, ByVal textCompare As Boolean) As Boolean
            Dim comparison As CompareClass = CompareObject2(left, right, textCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return False
                Case CompareClass.UserDefined
                    Return CBool(InvokeUserDefinedOperator(UserDefinedOperator.LessEqual, left, right))
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.LessEqual, left, right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison <= 0
            End Select
        End Function

        Public Shared Function CompareObjectGreaterEqual(ByVal left As Object, ByVal right As Object, ByVal textCompare As Boolean) As Object
            Dim comparison As CompareClass = CompareObject2(left, right, textCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return False
                Case CompareClass.UserDefined
                    Return InvokeUserDefinedOperator(UserDefinedOperator.GreaterEqual, left, right)
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.GreaterEqual, left, right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison >= 0
            End Select
        End Function

        Public Shared Function ConditionalCompareObjectGreaterEqual(ByVal left As Object, ByVal right As Object, ByVal textCompare As Boolean) As Boolean
            Dim comparison As CompareClass = CompareObject2(left, right, textCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return False
                Case CompareClass.UserDefined
                    Return CBool(InvokeUserDefinedOperator(UserDefinedOperator.GreaterEqual, left, right))
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.GreaterEqual, left, right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison >= 0
            End Select
        End Function

        Public Shared Function CompareObjectGreater(ByVal left As Object, ByVal right As Object, ByVal textCompare As Boolean) As Object
            Dim comparison As CompareClass = CompareObject2(left, right, textCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return False
                Case CompareClass.UserDefined
                    Return InvokeUserDefinedOperator(UserDefinedOperator.Greater, left, right)
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.Greater, left, right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison > 0
            End Select
        End Function

        Public Shared Function ConditionalCompareObjectGreater(ByVal left As Object, ByVal right As Object, ByVal textCompare As Boolean) As Boolean
            Dim comparison As CompareClass = CompareObject2(left, right, textCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return False
                Case CompareClass.UserDefined
                    Return CBool(InvokeUserDefinedOperator(UserDefinedOperator.Greater, left, right))
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.Greater, left, right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison > 0
            End Select
        End Function

        Public Shared Function CompareObject(ByVal left As Object, ByVal right As Object, ByVal textCompare As Boolean) As Integer
            Dim comparison As CompareClass = CompareObject2(left, right, textCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return 0
                Case CompareClass.UserDefined,
                     CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.IsTrue, left, right)
                Case Else
                    Return comparison
            End Select
        End Function

        Private Shared Function CompareObject2(ByVal left As Object, ByVal right As Object, ByVal textCompare As Boolean) As CompareClass

            Dim tc1 As TypeCode = GetTypeCode(left)
            Dim tc2 As TypeCode = GetTypeCode(right)

            'Special cases for Char()
            If tc1 = TypeCode.Object Then
                Dim leftCharArray As Char() = TryCast(left, Char())

                If leftCharArray IsNot Nothing Then
                    If tc2 = TypeCode.String OrElse tc2 = TypeCode.Empty OrElse ((tc2 = TypeCode.Object) AndAlso (TypeOf right Is Char())) Then
                        'Treat Char() as String for these cases
                        left = CStr(leftCharArray)
                        tc1 = TypeCode.String
                    End If
                End If
            End If

            If (tc2 = TypeCode.Object) Then
                Dim rightCharArray As Char() = TryCast(right, Char())

                If rightCharArray IsNot Nothing Then
                    If tc1 = TypeCode.String OrElse tc1 = TypeCode.Empty Then
                        right = CStr(rightCharArray)
                        tc2 = TypeCode.String
                    End If
                End If
            End If

            Select Case tc1 * s_TCMAX + tc2

                Case TypeCode.Empty * s_TCMAX + TypeCode.Empty
                    Return 0

                Case TypeCode.Empty * s_TCMAX + TypeCode.Boolean
                    Return CompareBoolean(Nothing, Convert.ToBoolean(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.SByte
                    Return CompareInt32(Nothing, Convert.ToSByte(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Byte
                    Return CompareInt32(Nothing, Convert.ToByte(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int16
                    Return CompareInt32(Nothing, Convert.ToInt16(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt16
                    Return CompareInt32(Nothing, Convert.ToUInt16(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int32
                    Return CompareInt32(Nothing, Convert.ToInt32(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt32
                    Return CompareUInt32(Nothing, Convert.ToUInt32(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int64
                    Return CompareInt64(Nothing, Convert.ToInt64(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt64
                    Return CompareUInt64(Nothing, Convert.ToUInt64(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Decimal
                    Return CompareDecimal(0D, right)

                Case TypeCode.Empty * s_TCMAX + TypeCode.Single
                    Return CompareSingle(Nothing, Convert.ToSingle(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Double
                    Return CompareDouble(Nothing, Convert.ToDouble(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.DateTime
                    Return CompareDate(Nothing, Convert.ToDateTime(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Char
                    Return CompareChar(Nothing, Convert.ToChar(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.String
                    Return CType(CompareString(Nothing, Convert.ToString(right), textCompare), CompareClass)

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Empty
                    Return CompareBoolean(Convert.ToBoolean(left), Nothing)

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Boolean
                    Return CompareBoolean(Convert.ToBoolean(left), Convert.ToBoolean(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.SByte
                    Return CompareInt32(ToVBBool(left), Convert.ToSByte(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Byte,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int16
                    Return CompareInt32(ToVBBool(left), Convert.ToInt16(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int32
                    Return CompareInt32(ToVBBool(left), Convert.ToInt32(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int64
                    Return CompareInt64(ToVBBool(left), Convert.ToInt64(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Decimal
                    Return CompareDecimal(ToVBBool(left), right)

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Single
                    Return CompareSingle(ToVBBool(left), Convert.ToSingle(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Double
                    Return CompareDouble(ToVBBool(left), Convert.ToDouble(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.String
                    Return CompareBoolean(Convert.ToBoolean(left), CBool(Convert.ToString(right)))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Empty
                    Return CompareInt32(Convert.ToSByte(left), Nothing)

                Case TypeCode.SByte * s_TCMAX + TypeCode.Boolean
                    Return CompareInt32(Convert.ToSByte(left), ToVBBool(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.SByte
                    Return CompareInt32(Convert.ToSByte(left), Convert.ToSByte(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Byte,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Byte * s_TCMAX + TypeCode.SByte,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int16

                    Return CompareInt32(Convert.ToInt16(left), Convert.ToInt16(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int32,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int32 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int32

                    Return CompareInt32(Convert.ToInt32(left), Convert.ToInt32(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int64,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int64 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int64

                    Return CompareInt64(Convert.ToInt64(left), Convert.ToInt64(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.SByte * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Byte * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Decimal * s_TCMAX + TypeCode.SByte,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Byte,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int16,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int32,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int64,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Decimal

                    Return CompareDecimal(left, right)

                Case TypeCode.SByte * s_TCMAX + TypeCode.Single,
                     TypeCode.Byte * s_TCMAX + TypeCode.Single,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Single,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Single,
                     TypeCode.Single * s_TCMAX + TypeCode.SByte,
                     TypeCode.Single * s_TCMAX + TypeCode.Byte,
                     TypeCode.Single * s_TCMAX + TypeCode.Int16,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Single * s_TCMAX + TypeCode.Int32,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Single * s_TCMAX + TypeCode.Int64,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Single * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Single * s_TCMAX + TypeCode.Single

                    Return CompareSingle(Convert.ToSingle(left), Convert.ToSingle(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Double,
                     TypeCode.Byte * s_TCMAX + TypeCode.Double,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Double,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Double,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Double,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Double,
                     TypeCode.Single * s_TCMAX + TypeCode.Double,
                     TypeCode.Double * s_TCMAX + TypeCode.SByte,
                     TypeCode.Double * s_TCMAX + TypeCode.Byte,
                     TypeCode.Double * s_TCMAX + TypeCode.Int16,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Double * s_TCMAX + TypeCode.Int32,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Double * s_TCMAX + TypeCode.Int64,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Double * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Double * s_TCMAX + TypeCode.Single,
                     TypeCode.Double * s_TCMAX + TypeCode.Double

                    Return CompareDouble(Convert.ToDouble(left), Convert.ToDouble(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.String,
                     TypeCode.Byte * s_TCMAX + TypeCode.String,
                     TypeCode.Int16 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.String,
                     TypeCode.Int32 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.String,
                     TypeCode.Int64 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.String,
                     TypeCode.Decimal * s_TCMAX + TypeCode.String,
                     TypeCode.Single * s_TCMAX + TypeCode.String,
                     TypeCode.Double * s_TCMAX + TypeCode.String

                    Return CompareDouble(Convert.ToDouble(left), CDbl(Convert.ToString(right)))

                Case TypeCode.Byte * s_TCMAX + TypeCode.Empty
                    Return CompareInt32(Convert.ToByte(left), Nothing)

                Case TypeCode.Byte * s_TCMAX + TypeCode.Boolean
                    Return CompareInt32(Convert.ToInt16(left), ToVBBool(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.Byte
                    Return CompareInt32(Convert.ToByte(left), Convert.ToByte(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt16

                    Return CompareInt32(Convert.ToUInt16(left), Convert.ToUInt16(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt32

                    Return CompareUInt32(Convert.ToUInt32(left), Convert.ToUInt32(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt64

                    Return CompareUInt64(Convert.ToUInt64(left), Convert.ToUInt64(right))

                Case TypeCode.Int16 * s_TCMAX + TypeCode.Empty
                    Return CompareInt32(Convert.ToInt16(left), Nothing)

                Case TypeCode.Int16 * s_TCMAX + TypeCode.Boolean
                    Return CompareInt32(Convert.ToInt16(left), ToVBBool(right))


                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Empty
                    Return CompareInt32(Convert.ToUInt16(left), Nothing)

                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Boolean
                    Return CompareInt32(Convert.ToInt32(left), ToVBBool(right))

                Case TypeCode.Int32 * s_TCMAX + TypeCode.Empty
                    Return CompareInt32(Convert.ToInt32(left), Nothing)

                Case TypeCode.Int32 * s_TCMAX + TypeCode.Boolean
                    Return CompareInt32(Convert.ToInt32(left), ToVBBool(right))

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Empty
                    Return CompareUInt32(Convert.ToUInt32(left), Nothing)

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Boolean
                    Return CompareInt64(Convert.ToInt64(left), ToVBBool(right))

                Case TypeCode.Int64 * s_TCMAX + TypeCode.Empty
                    Return CompareInt64(Convert.ToInt64(left), Nothing)

                Case TypeCode.Int64 * s_TCMAX + TypeCode.Boolean
                    Return CompareInt64(Convert.ToInt64(left), ToVBBool(right))

                Case TypeCode.UInt64 * s_TCMAX + TypeCode.Empty
                    Return CompareUInt64(Convert.ToUInt64(left), Nothing)

                Case TypeCode.UInt64 * s_TCMAX + TypeCode.Boolean
                    Return CompareDecimal(left, ToVBBool(right))

                Case TypeCode.Decimal * s_TCMAX + TypeCode.Empty
                    Return CompareDecimal(left, 0D)

                Case TypeCode.Decimal * s_TCMAX + TypeCode.Boolean
                    Return CompareDecimal(left, ToVBBool(right))

                Case TypeCode.Single * s_TCMAX + TypeCode.Empty
                    Return CompareSingle(Convert.ToSingle(left), Nothing)

                Case TypeCode.Single * s_TCMAX + TypeCode.Boolean
                    Return CompareSingle(Convert.ToSingle(left), ToVBBool(right))

                Case TypeCode.Double * s_TCMAX + TypeCode.Empty
                    Return CompareDouble(Convert.ToDouble(left), Nothing)

                Case TypeCode.Double * s_TCMAX + TypeCode.Boolean
                    Return CompareDouble(Convert.ToDouble(left), ToVBBool(right))

                Case TypeCode.DateTime * s_TCMAX + TypeCode.Empty
                    Return CompareDate(Convert.ToDateTime(left), Nothing)

                Case TypeCode.DateTime * s_TCMAX + TypeCode.DateTime
                    Return CompareDate(Convert.ToDateTime(left), Convert.ToDateTime(right))

                Case TypeCode.DateTime * s_TCMAX + TypeCode.String
                    Return CompareDate(Convert.ToDateTime(left), CDate(Convert.ToString(right)))

                Case TypeCode.Char * s_TCMAX + TypeCode.Empty
                    Return CompareChar(Convert.ToChar(left), Nothing)

                Case TypeCode.Char * s_TCMAX + TypeCode.Char
                    Return CompareChar(Convert.ToChar(left), Convert.ToChar(right))

                Case TypeCode.Char * s_TCMAX + TypeCode.String,
                     TypeCode.String * s_TCMAX + TypeCode.Char,
                     TypeCode.String * s_TCMAX + TypeCode.String
                    Return CType(CompareString(Convert.ToString(left), Convert.ToString(right), textCompare), CompareClass)

                Case TypeCode.String * s_TCMAX + TypeCode.Empty
                    Return CType(CompareString(Convert.ToString(left), Nothing, textCompare), CompareClass)

                Case TypeCode.String * s_TCMAX + TypeCode.Boolean
                    Return CompareBoolean(CBool(Convert.ToString(left)), Convert.ToBoolean(right))

                Case TypeCode.String * s_TCMAX + TypeCode.SByte,
                     TypeCode.String * s_TCMAX + TypeCode.Byte,
                     TypeCode.String * s_TCMAX + TypeCode.Int16,
                     TypeCode.String * s_TCMAX + TypeCode.UInt16,
                     TypeCode.String * s_TCMAX + TypeCode.Int32,
                     TypeCode.String * s_TCMAX + TypeCode.UInt32,
                     TypeCode.String * s_TCMAX + TypeCode.Int64,
                     TypeCode.String * s_TCMAX + TypeCode.UInt64,
                     TypeCode.String * s_TCMAX + TypeCode.Decimal,
                     TypeCode.String * s_TCMAX + TypeCode.Single,
                     TypeCode.String * s_TCMAX + TypeCode.Double

                    Return CompareDouble(CDbl(Convert.ToString(left)), Convert.ToDouble(right))

                Case TypeCode.String * s_TCMAX + TypeCode.DateTime
                    Return CompareDate(CDate(Convert.ToString(left)), Convert.ToDateTime(right))

            End Select

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return CompareClass.UserDefined
            End If

            Return CompareClass.Undefined
        End Function

        Private Shared Function CompareBoolean(ByVal left As Boolean, ByVal right As Boolean) As CompareClass
            If left = right Then Return CompareClass.Equal
            If left > right Then Return CompareClass.Greater
            Return CompareClass.Less
        End Function

        Private Shared Function CompareInt32(ByVal left As Int32, ByVal right As Int32) As CompareClass
            If left = right Then Return CompareClass.Equal
            If left > right Then Return CompareClass.Greater
            Return CompareClass.Less
        End Function

        Private Shared Function CompareUInt32(ByVal left As UInt32, ByVal right As UInt32) As CompareClass
            If left = right Then Return CompareClass.Equal
            If left > right Then Return CompareClass.Greater
            Return CompareClass.Less
        End Function

        Private Shared Function CompareInt64(ByVal left As Int64, ByVal right As Int64) As CompareClass
            If left = right Then Return CompareClass.Equal
            If left > right Then Return CompareClass.Greater
            Return CompareClass.Less
        End Function

        Private Shared Function CompareUInt64(ByVal left As UInt64, ByVal right As UInt64) As CompareClass
            If left = right Then Return CompareClass.Equal
            If left > right Then Return CompareClass.Greater
            Return CompareClass.Less
        End Function

        'This function takes IConvertible because the JIT does not behave properly with Decimal temps
        Private Shared Function CompareDecimal(ByVal left As Object, ByVal right As Object) As CompareClass
            Dim result As Integer = System.Decimal.Compare(Convert.ToDecimal(left), Convert.ToDecimal(right))

            'Normalize the result.
            If result = 0 Then
                Return CompareClass.Equal
            ElseIf result > 0 Then
                Return CompareClass.Greater
            Else
                Return CompareClass.Less
            End If
        End Function

        Private Shared Function CompareSingle(ByVal left As Single, ByVal right As Single) As CompareClass
            If left = right Then Return CompareClass.Equal
            If left < right Then Return CompareClass.Less
            If left > right Then Return CompareClass.Greater
            Return CompareClass.Unordered
        End Function

        Private Shared Function CompareDouble(ByVal left As Double, ByVal right As Double) As CompareClass
            If left = right Then Return CompareClass.Equal
            If left < right Then Return CompareClass.Less
            If left > right Then Return CompareClass.Greater
            Return CompareClass.Unordered
        End Function

        Private Shared Function CompareDate(ByVal left As Date, ByVal right As Date) As CompareClass
            Dim result As Integer = System.DateTime.Compare(left, right)

            'Normalize the result.
            If result = 0 Then
                Return CompareClass.Equal
            ElseIf result > 0 Then
                Return CompareClass.Greater
            Else
                Return CompareClass.Less
            End If
        End Function

        Private Shared Function CompareChar(ByVal left As Char, ByVal right As Char) As CompareClass
            If left = right Then Return CompareClass.Equal
            If left > right Then Return CompareClass.Greater
            Return CompareClass.Less
        End Function

        'String comparisons occur often enough that maybe the TextCompare should be broken out into two members that the compiler statically selects
        Public Shared Function CompareString(ByVal left As String, ByVal right As String, ByVal textCompare As Boolean) As Integer
            If left Is right Then
                Return CompareClass.Equal
            End If

            If left Is Nothing Then
                If right.Length() = 0 Then
                    Return CompareClass.Equal
                End If

                Return CompareClass.Less
            End If

            If right Is Nothing Then
                If left.Length() = 0 Then
                    Return CompareClass.Equal
                End If

                Return CompareClass.Greater
            End If

            Dim result As Integer

            If textCompare Then
                result = GetCultureInfo().CompareInfo.Compare(left, right, OptionCompareTextFlags)
            Else
                result = System.String.CompareOrdinal(left, right)
            End If

            'Normalize the result.
            If result = 0 Then
                Return CompareClass.Equal
            ElseIf result > 0 Then
                Return CompareClass.Greater
            Else
                Return CompareClass.Less
            End If
        End Function

#End Region

#Region " Operator Unary Plus + "

        Public Shared Function PlusObject(ByVal operand As Object) As Object

            If operand Is Nothing Then
                Return Boxed_ZeroInteger
            End If

            Dim typ As TypeCode = GetTypeCode(operand)


            Select Case typ

                Case TypeCode.Empty
                    Return Boxed_ZeroInteger

                Case TypeCode.Boolean
                    Return CShort(Convert.ToBoolean(operand))

                Case TypeCode.SByte
                    Return Convert.ToSByte(operand)

                Case TypeCode.Byte
                    Return Convert.ToByte(operand)

                Case TypeCode.Int16
                    Return Convert.ToInt16(operand)

                Case TypeCode.UInt16
                    Return Convert.ToUInt16(operand)

                Case TypeCode.Int32
                    Return Convert.ToInt32(operand)

                Case TypeCode.UInt32
                    Return Convert.ToUInt32(operand)

                Case TypeCode.Int64
                    Return Convert.ToInt64(operand)

                Case TypeCode.UInt64
                    Return Convert.ToUInt64(operand)

                Case TypeCode.Decimal,
                     TypeCode.Single,
                     TypeCode.Double
                    Return operand

                Case TypeCode.DateTime,
                     TypeCode.Char
                ' Fall through to error

                Case TypeCode.String
                    Return CDbl(Convert.ToString(operand))

                Case TypeCode.Object
                    Return InvokeUserDefinedOperator(UserDefinedOperator.UnaryPlus, operand)

                Case Else
                    ' Fall through to error
            End Select

            Throw GetNoValidOperatorException(UserDefinedOperator.UnaryPlus, operand)
        End Function

#End Region

#Region " Operator Negate - "

        Public Shared Function NegateObject(ByVal operand As Object) As Object

            Dim tc As TypeCode = GetTypeCode(operand)

            If operand Is Nothing Then
                tc = TypeCode.Empty
            Else
                tc = operand.GetType.GetTypeCode
            End If

            Select Case tc

                Case TypeCode.Empty
                    Return Boxed_ZeroInteger

                Case TypeCode.Boolean
                    If TypeOf operand Is Boolean Then
                        Return NegateBoolean(DirectCast(operand, Boolean))
                    Else
                        Return NegateBoolean(Convert.ToBoolean(operand))
                    End If

                Case TypeCode.SByte
                    If TypeOf operand Is SByte Then
                        Return NegateSByte(DirectCast(operand, SByte))
                    Else
                        Return NegateSByte(Convert.ToSByte(operand))
                    End If

                Case TypeCode.Byte
                    If TypeOf operand Is Byte Then
                        Return NegateByte(DirectCast(operand, Byte))
                    Else
                        Return NegateByte(Convert.ToByte(operand))
                    End If

                Case TypeCode.Int16
                    If TypeOf operand Is Int16 Then
                        Return NegateInt16(DirectCast(operand, Int16))
                    Else
                        Return NegateInt16(Convert.ToInt16(operand))
                    End If

                Case TypeCode.UInt16
                    If TypeOf operand Is UInt16 Then
                        Return NegateUInt16(DirectCast(operand, UInt16))
                    Else
                        Return NegateUInt16(Convert.ToUInt16(operand))
                    End If

                Case TypeCode.Int32
                    If TypeOf operand Is Int32 Then
                        Return NegateInt32(DirectCast(operand, Int32))
                    Else
                        Return NegateInt32(Convert.ToInt32(operand))
                    End If

                Case TypeCode.UInt32
                    If TypeOf operand Is UInt32 Then
                        Return NegateUInt32(DirectCast(operand, UInt32))
                    Else
                        Return NegateUInt32(Convert.ToUInt32(operand))
                    End If

                Case TypeCode.Int64
                    If TypeOf operand Is Int64 Then
                        Return NegateInt64(DirectCast(operand, Int64))
                    Else
                        Return NegateInt64(Convert.ToInt64(operand))
                    End If

                Case TypeCode.UInt64
                    If TypeOf operand Is UInt64 Then
                        Return NegateUInt64(DirectCast(operand, UInt64))
                    Else
                        Return NegateUInt64(Convert.ToUInt64(operand))
                    End If

                Case TypeCode.Decimal
                    If TypeOf operand Is Decimal Then
                        Return NegateDecimal(DirectCast(operand, Decimal))
                    Else
                        Return NegateDecimal(Convert.ToDecimal(operand))
                    End If

                Case TypeCode.Single
                    If TypeOf operand Is Single Then
                        Return NegateSingle(DirectCast(operand, Single))
                    Else
                        Return NegateSingle(Convert.ToSingle(operand))
                    End If

                Case TypeCode.Double
                    If TypeOf operand Is Double Then
                        Return NegateDouble(DirectCast(operand, Double))
                    Else
                        Return NegateDouble(Convert.ToDouble(operand))
                    End If

                Case TypeCode.DateTime,
                     TypeCode.Char
                'Fall through to error.

                Case TypeCode.String
                    Dim stringOperand As String = TryCast(operand, String)

                    If stringOperand IsNot Nothing Then
                        Return NegateString(stringOperand)
                    Else
                        Return NegateString(Convert.ToString(operand))
                    End If

                Case TypeCode.Object
                    Return InvokeUserDefinedOperator(UserDefinedOperator.Negate, operand)

                Case Else
                    'Fall through to error.
            End Select

            Throw GetNoValidOperatorException(UserDefinedOperator.Negate, operand)
        End Function

        Private Shared Function NegateBoolean(ByVal operand As Boolean) As Object
            Return -CShort(operand)
        End Function

        Private Shared Function NegateSByte(ByVal operand As SByte) As Object
            If operand = SByte.MinValue Then
                Return -CShort(SByte.MinValue)
            End If
            Return -operand
        End Function

        Private Shared Function NegateByte(ByVal operand As Byte) As Object
            Return -CShort(operand)
        End Function

        Private Shared Function NegateInt16(ByVal operand As Int16) As Object
            If operand = Int16.MinValue Then
                Return -CInt(Int16.MinValue)
            End If
            Return -operand
        End Function

        Private Shared Function NegateUInt16(ByVal operand As UInt16) As Object
            Return -CInt(operand)
        End Function

        Private Shared Function NegateInt32(ByVal operand As Int32) As Object
            If operand = Int32.MinValue Then
                Return -CLng(Int32.MinValue)
            End If
            Return -operand
        End Function

        Private Shared Function NegateUInt32(ByVal operand As UInt32) As Object
            Return -CLng(operand)
        End Function

        Private Shared Function NegateInt64(ByVal operand As Int64) As Object
            If operand = Int64.MinValue Then
                Return -CDec(Int64.MinValue)
            End If
            Return -operand
        End Function

        Private Shared Function NegateUInt64(ByVal operand As UInt64) As Object
            Return -CDec(operand)
        End Function

        Private Shared Function NegateDecimal(ByVal operand As Decimal) As Object
            'Using try/catch instead of check with MinValue since the overflow case should be very rare
            'and a compare would be a big cost for the normal case.
            Try
                Return -operand
            Catch ex As OverflowException
                Return -CDbl(operand)
            End Try
        End Function

        Private Shared Function NegateSingle(ByVal operand As Single) As Object
            Return -operand
        End Function

        Private Shared Function NegateDouble(ByVal operand As Double) As Object
            Return -operand
        End Function

        Private Shared Function NegateString(ByVal operand As String) As Object
            Return -CDbl(operand)
        End Function

#End Region

#Region " Operator Not "

        Public Shared Function NotObject(ByVal operand As Object) As Object

            Dim tc As TypeCode = GetTypeCode(operand)

            Select Case tc

                Case TypeCode.Empty
                    Return Not 0I

                Case TypeCode.Boolean
                    Return NotBoolean(Convert.ToBoolean(operand))

                Case TypeCode.SByte
                    Return NotSByte(Convert.ToSByte(operand), operand.GetType())

                Case TypeCode.Byte
                    Return NotByte(Convert.ToByte(operand), operand.GetType())

                Case TypeCode.Int16
                    Return NotInt16(Convert.ToInt16(operand), operand.GetType())

                Case TypeCode.UInt16
                    Return NotUInt16(Convert.ToUInt16(operand), operand.GetType())

                Case TypeCode.Int32
                    Return NotInt32(Convert.ToInt32(operand), operand.GetType())

                Case TypeCode.UInt32
                    Return NotUInt32(Convert.ToUInt32(operand), operand.GetType())

                Case TypeCode.Int64
                    Return NotInt64(Convert.ToInt64(operand), operand.GetType())

                Case TypeCode.UInt64
                    Return NotUInt64(Convert.ToUInt64(operand), operand.GetType())

                Case TypeCode.Decimal,
                     TypeCode.Single,
                     TypeCode.Double
                    Return NotInt64(Convert.ToInt64(operand))

                Case TypeCode.DateTime,
                     TypeCode.Char
                'Fall through to error.

                Case TypeCode.String
                    Return NotInt64(CLng(Convert.ToString(operand)))

                Case TypeCode.Object
                    Return InvokeUserDefinedOperator(UserDefinedOperator.Not, operand)

                Case Else
                    'Fall through to error.
            End Select

            Throw GetNoValidOperatorException(UserDefinedOperator.Not, operand)
        End Function

        Private Shared Function NotBoolean(ByVal operand As Boolean) As Object
            Return Not operand
        End Function

        Private Shared Function NotSByte(ByVal operand As SByte, ByVal operandType As Type) As Object
            Dim result As SByte = Not operand

            If operandType.IsEnum Then
                Return System.Enum.ToObject(operandType, result)
            End If
            Return result
        End Function

        Private Shared Function NotByte(ByVal operand As Byte, ByVal operandType As Type) As Object
            Dim result As Byte = Not operand

            If operandType.IsEnum Then
                Return System.Enum.ToObject(operandType, result)
            End If
            Return result
        End Function

        Private Shared Function NotInt16(ByVal operand As Int16, ByVal operandType As Type) As Object
            Dim result As Int16 = Not operand

            If operandType.IsEnum Then
                Return System.Enum.ToObject(operandType, result)
            End If
            Return result
        End Function

        Private Shared Function NotUInt16(ByVal operand As UInt16, ByVal operandType As Type) As Object
            Dim result As UInt16 = Not operand

            If operandType.IsEnum Then
                Return System.Enum.ToObject(operandType, result)
            End If
            Return result
        End Function

        Private Shared Function NotInt32(ByVal operand As Int32, ByVal operandType As Type) As Object
            Dim result As Int32 = Not operand

            If operandType.IsEnum Then
                Return System.Enum.ToObject(operandType, result)
            End If
            Return result
        End Function

        Private Shared Function NotUInt32(ByVal operand As UInt32, ByVal operandType As Type) As Object
            Dim result As UInt32 = Not operand

            If operandType.IsEnum Then
                Return System.Enum.ToObject(operandType, result)
            End If
            Return result
        End Function

        Private Shared Function NotInt64(ByVal operand As Int64) As Object
            Return Not operand
        End Function

        Private Shared Function NotInt64(ByVal operand As Int64, ByVal operandType As Type) As Object
            Dim result As Int64 = Not operand

            If operandType.IsEnum Then
                Return System.Enum.ToObject(operandType, result)
            End If
            Return result
        End Function

        Private Shared Function NotUInt64(ByVal operand As UInt64, ByVal operandType As Type) As Object
            Dim result As UInt64 = Not operand

            If operandType.IsEnum Then
                Return System.Enum.ToObject(operandType, result)
            End If
            Return result
        End Function

#End Region

#Region " Operator And "

        Public Shared Function AndObject(ByVal left As Object, ByVal right As Object) As Object

            Dim tc1 As TypeCode = GetTypeCode(left)
            Dim tc2 As TypeCode = GetTypeCode(right)


            Select Case tc1 * s_TCMAX + tc2

                Case TypeCode.Empty * s_TCMAX + TypeCode.Empty
                    Return Boxed_ZeroInteger

                Case TypeCode.Empty * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Empty
                    Return False

                Case TypeCode.Empty * s_TCMAX + TypeCode.SByte,
                     TypeCode.SByte * s_TCMAX + TypeCode.Empty
                    Return AndSByte(CSByte(0), CSByte(0), GetEnumResult(left, right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Byte,
                     TypeCode.Byte * s_TCMAX + TypeCode.Empty
                    Return AndByte(CByte(0), CByte(0), GetEnumResult(left, right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Empty
                    Return AndInt16(0S, 0S, GetEnumResult(left, right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Empty
                    Return AndUInt16(0US, 0US, GetEnumResult(left, right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Empty
                    Return AndInt32(0I, 0I, GetEnumResult(left, right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Empty
                    Return AndUInt32(0UI, 0UI, GetEnumResult(left, right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Empty
                    Return AndInt64(0L, 0L, GetEnumResult(left, right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Empty
                    Return AndUInt64(0UL, 0UL, GetEnumResult(left, right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Empty * s_TCMAX + TypeCode.Single,
                     TypeCode.Empty * s_TCMAX + TypeCode.Double
                    Return AndInt64(Nothing, Convert.ToInt64(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.String
                    Return AndInt64(Nothing, CLng(Convert.ToString(right)))


                Case TypeCode.Boolean * s_TCMAX + TypeCode.Boolean
                    Return AndBoolean(Convert.ToBoolean(left), Convert.ToBoolean(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.SByte
                    Return AndSByte(ToVBBool(left), Convert.ToSByte(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Byte,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int16
                    Return AndInt16(ToVBBool(left), Convert.ToInt16(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int32
                    Return AndInt32(ToVBBool(left), Convert.ToInt32(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Single,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Double

                    Return AndInt64(ToVBBool(left), Convert.ToInt64(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.String
                    Return AndBoolean(Convert.ToBoolean(left), CBool(Convert.ToString(right)))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Boolean
                    Return AndSByte(Convert.ToSByte(left), ToVBBool(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.SByte
                    Return AndSByte(Convert.ToSByte(left), Convert.ToSByte(right), GetEnumResult(left, right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Byte,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Byte * s_TCMAX + TypeCode.SByte,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Byte

                    Return AndInt16(Convert.ToInt16(left), Convert.ToInt16(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int32,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int32 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt16

                    Return AndInt32(Convert.ToInt32(left), Convert.ToInt32(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int64,
                     TypeCode.SByte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.SByte * s_TCMAX + TypeCode.Decimal,
                     TypeCode.SByte * s_TCMAX + TypeCode.Single,
                     TypeCode.SByte * s_TCMAX + TypeCode.Double,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int64,
                     TypeCode.Byte * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Byte * s_TCMAX + TypeCode.Single,
                     TypeCode.Byte * s_TCMAX + TypeCode.Double,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Double,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Double,
                     TypeCode.Int64 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Double,
                     TypeCode.Decimal * s_TCMAX + TypeCode.SByte,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Byte,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int16,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int32,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int64,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Single,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Double,
                     TypeCode.Single * s_TCMAX + TypeCode.SByte,
                     TypeCode.Single * s_TCMAX + TypeCode.Byte,
                     TypeCode.Single * s_TCMAX + TypeCode.Int16,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Single * s_TCMAX + TypeCode.Int32,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Single * s_TCMAX + TypeCode.Int64,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Single * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Single * s_TCMAX + TypeCode.Single,
                     TypeCode.Single * s_TCMAX + TypeCode.Double,
                     TypeCode.Double * s_TCMAX + TypeCode.SByte,
                     TypeCode.Double * s_TCMAX + TypeCode.Byte,
                     TypeCode.Double * s_TCMAX + TypeCode.Int16,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Double * s_TCMAX + TypeCode.Int32,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Double * s_TCMAX + TypeCode.Int64,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Double * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Double * s_TCMAX + TypeCode.Single,
                     TypeCode.Double * s_TCMAX + TypeCode.Double

                    Return AndInt64(Convert.ToInt64(left), Convert.ToInt64(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.String,
                     TypeCode.Byte * s_TCMAX + TypeCode.String,
                     TypeCode.Int16 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.String,
                     TypeCode.Int32 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.String,
                     TypeCode.Int64 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.String,
                     TypeCode.Decimal * s_TCMAX + TypeCode.String,
                     TypeCode.Single * s_TCMAX + TypeCode.String,
                     TypeCode.Double * s_TCMAX + TypeCode.String

                    Return AndInt64(Convert.ToInt64(left), CLng(Convert.ToString(right)))


                Case TypeCode.Byte * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Boolean
                    Return AndInt16(Convert.ToInt16(left), ToVBBool(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.Byte
                    Return AndByte(Convert.ToByte(left), Convert.ToByte(right), GetEnumResult(left, right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Byte
                    Return AndUInt16(Convert.ToUInt16(left), Convert.ToUInt16(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt16

                    Return AndUInt32(Convert.ToUInt32(left), Convert.ToUInt32(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt32

                    Return AndUInt64(Convert.ToUInt64(left), Convert.ToUInt64(right))


                Case TypeCode.Int16 * s_TCMAX + TypeCode.Int16
                    Return AndInt16(Convert.ToInt16(left), Convert.ToInt16(right), GetEnumResult(left, right))


                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Boolean
                    Return AndInt32(Convert.ToInt32(left), ToVBBool(right))

                Case TypeCode.UInt16 * s_TCMAX + TypeCode.UInt16
                    Return AndUInt16(Convert.ToUInt16(left), Convert.ToUInt16(right), GetEnumResult(left, right))


                Case TypeCode.Int32 * s_TCMAX + TypeCode.Int32
                    Return AndInt32(Convert.ToInt32(left), Convert.ToInt32(right), GetEnumResult(left, right))


                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Single * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Double * s_TCMAX + TypeCode.Boolean
                    Return AndInt64(Convert.ToInt64(left), ToVBBool(right))

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.UInt32
                    Return AndUInt32(Convert.ToUInt32(left), Convert.ToUInt32(right), GetEnumResult(left, right))


                Case TypeCode.Int64 * s_TCMAX + TypeCode.Int64
                    Return AndInt64(Convert.ToInt64(left), Convert.ToInt64(right), GetEnumResult(left, right))


                Case TypeCode.UInt64 * s_TCMAX + TypeCode.UInt64
                    Return AndUInt64(Convert.ToUInt64(left), Convert.ToUInt64(right), GetEnumResult(left, right))


                Case TypeCode.Decimal * s_TCMAX + TypeCode.Empty,
                     TypeCode.Single * s_TCMAX + TypeCode.Empty,
                     TypeCode.Double * s_TCMAX + TypeCode.Empty
                    Return AndInt64(Convert.ToInt64(left), Nothing)


                Case TypeCode.String * s_TCMAX + TypeCode.Empty
                    Return AndInt64(CLng(Convert.ToString(left)), Nothing)

                Case TypeCode.String * s_TCMAX + TypeCode.Boolean
                    Return AndBoolean(CBool(Convert.ToString(left)), Convert.ToBoolean(right))

                Case TypeCode.String * s_TCMAX + TypeCode.SByte,
                     TypeCode.String * s_TCMAX + TypeCode.Byte,
                     TypeCode.String * s_TCMAX + TypeCode.Int16,
                     TypeCode.String * s_TCMAX + TypeCode.UInt16,
                     TypeCode.String * s_TCMAX + TypeCode.Int32,
                     TypeCode.String * s_TCMAX + TypeCode.UInt32,
                     TypeCode.String * s_TCMAX + TypeCode.Int64,
                     TypeCode.String * s_TCMAX + TypeCode.UInt64,
                     TypeCode.String * s_TCMAX + TypeCode.Decimal,
                     TypeCode.String * s_TCMAX + TypeCode.Single,
                     TypeCode.String * s_TCMAX + TypeCode.Double

                    Return AndInt64(CLng(Convert.ToString(left)), Convert.ToInt64(right))

                Case TypeCode.String * s_TCMAX + TypeCode.String
                    Return AndInt64(CLng(Convert.ToString(left)), CLng(Convert.ToString(right)))

            End Select

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return InvokeUserDefinedOperator(UserDefinedOperator.And, left, right)
            End If

            Throw GetNoValidOperatorException(UserDefinedOperator.And, left, right)
        End Function

        Private Shared Function AndBoolean(ByVal left As Boolean, ByVal right As Boolean) As Object
            Return left And right
        End Function

        Private Shared Function AndSByte(ByVal left As SByte, ByVal right As SByte, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As SByte = left And right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

        Private Shared Function AndByte(ByVal left As Byte, ByVal right As Byte, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As Byte = left And right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

        Private Shared Function AndInt16(ByVal left As Int16, ByVal right As Int16, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As Int16 = left And right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

        Private Shared Function AndUInt16(ByVal left As UInt16, ByVal right As UInt16, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As UInt16 = left And right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

        Private Shared Function AndInt32(ByVal left As Int32, ByVal right As Int32, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As Int32 = left And right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

        Private Shared Function AndUInt32(ByVal left As UInt32, ByVal right As UInt32, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As UInt32 = left And right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

        Private Shared Function AndInt64(ByVal left As Int64, ByVal right As Int64, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As Int64 = left And right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

        Private Shared Function AndUInt64(ByVal left As UInt64, ByVal right As UInt64, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As UInt64 = left And right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

#End Region

#Region " Operator Or "

        Public Shared Function OrObject(ByVal left As Object, ByVal right As Object) As Object

            Dim tc1 As TypeCode = GetTypeCode(left)
            Dim tc2 As TypeCode = GetTypeCode(right)

            Select Case tc1 * s_TCMAX + tc2

                Case TypeCode.Empty * s_TCMAX + TypeCode.Empty
                    Return Boxed_ZeroInteger

                Case TypeCode.Empty * s_TCMAX + TypeCode.Boolean
                    Return OrBoolean(Nothing, Convert.ToBoolean(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.SByte,
                     TypeCode.Empty * s_TCMAX + TypeCode.Byte,
                     TypeCode.Empty * s_TCMAX + TypeCode.Int16,
                     TypeCode.Empty * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Empty * s_TCMAX + TypeCode.Int32,
                     TypeCode.Empty * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Empty * s_TCMAX + TypeCode.Int64,
                     TypeCode.Empty * s_TCMAX + TypeCode.UInt64

                    Return right

                Case TypeCode.Empty * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Empty * s_TCMAX + TypeCode.Single,
                     TypeCode.Empty * s_TCMAX + TypeCode.Double
                    Return OrInt64(Nothing, Convert.ToInt64(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.String
                    Return OrInt64(Nothing, CLng(Convert.ToString(right)))


                Case TypeCode.Boolean * s_TCMAX + TypeCode.Empty
                    Return OrBoolean(Convert.ToBoolean(left), Nothing)

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Boolean
                    Return OrBoolean(Convert.ToBoolean(left), Convert.ToBoolean(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.SByte
                    Return OrSByte(ToVBBool(left), Convert.ToSByte(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Byte,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int16
                    Return OrInt16(ToVBBool(left), Convert.ToInt16(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int32
                    Return OrInt32(ToVBBool(left), Convert.ToInt32(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Single,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Double

                    Return OrInt64(ToVBBool(left), Convert.ToInt64(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.String
                    Return OrBoolean(Convert.ToBoolean(left), CBool(Convert.ToString(right)))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Empty,
                     TypeCode.Byte * s_TCMAX + TypeCode.Empty,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Empty,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Empty,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Empty,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Empty,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Empty,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Empty

                    Return left

                Case TypeCode.SByte * s_TCMAX + TypeCode.Boolean
                    Return OrSByte(Convert.ToSByte(left), ToVBBool(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.SByte
                    Return OrSByte(Convert.ToSByte(left), Convert.ToSByte(right), GetEnumResult(left, right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Byte,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Byte * s_TCMAX + TypeCode.SByte,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Byte

                    Return OrInt16(Convert.ToInt16(left), Convert.ToInt16(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int32,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int32 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt16

                    Return OrInt32(Convert.ToInt32(left), Convert.ToInt32(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int64,
                     TypeCode.SByte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.SByte * s_TCMAX + TypeCode.Decimal,
                     TypeCode.SByte * s_TCMAX + TypeCode.Single,
                     TypeCode.SByte * s_TCMAX + TypeCode.Double,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int64,
                     TypeCode.Byte * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Byte * s_TCMAX + TypeCode.Single,
                     TypeCode.Byte * s_TCMAX + TypeCode.Double,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Double,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Double,
                     TypeCode.Int64 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Double,
                     TypeCode.Decimal * s_TCMAX + TypeCode.SByte,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Byte,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int16,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int32,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int64,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Single,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Double,
                     TypeCode.Single * s_TCMAX + TypeCode.SByte,
                     TypeCode.Single * s_TCMAX + TypeCode.Byte,
                     TypeCode.Single * s_TCMAX + TypeCode.Int16,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Single * s_TCMAX + TypeCode.Int32,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Single * s_TCMAX + TypeCode.Int64,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Single * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Single * s_TCMAX + TypeCode.Single,
                     TypeCode.Single * s_TCMAX + TypeCode.Double,
                     TypeCode.Double * s_TCMAX + TypeCode.SByte,
                     TypeCode.Double * s_TCMAX + TypeCode.Byte,
                     TypeCode.Double * s_TCMAX + TypeCode.Int16,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Double * s_TCMAX + TypeCode.Int32,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Double * s_TCMAX + TypeCode.Int64,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Double * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Double * s_TCMAX + TypeCode.Single,
                     TypeCode.Double * s_TCMAX + TypeCode.Double

                    Return OrInt64(Convert.ToInt64(left), Convert.ToInt64(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.String,
                     TypeCode.Byte * s_TCMAX + TypeCode.String,
                     TypeCode.Int16 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.String,
                     TypeCode.Int32 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.String,
                     TypeCode.Int64 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.String,
                     TypeCode.Decimal * s_TCMAX + TypeCode.String,
                     TypeCode.Single * s_TCMAX + TypeCode.String,
                     TypeCode.Double * s_TCMAX + TypeCode.String

                    Return OrInt64(Convert.ToInt64(left), CLng(Convert.ToString(right)))


                Case TypeCode.Byte * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Boolean
                    Return OrInt16(Convert.ToInt16(left), ToVBBool(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.Byte
                    Return OrByte(Convert.ToByte(left), Convert.ToByte(right), GetEnumResult(left, right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Byte
                    Return OrUInt16(Convert.ToUInt16(left), Convert.ToUInt16(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt16

                    Return OrUInt32(Convert.ToUInt32(left), Convert.ToUInt32(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt32

                    Return OrUInt64(Convert.ToUInt64(left), Convert.ToUInt64(right))


                Case TypeCode.Int16 * s_TCMAX + TypeCode.Int16
                    Return OrInt16(Convert.ToInt16(left), Convert.ToInt16(right), GetEnumResult(left, right))


                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Boolean
                    Return OrInt32(Convert.ToInt32(left), ToVBBool(right))

                Case TypeCode.UInt16 * s_TCMAX + TypeCode.UInt16
                    Return OrUInt16(Convert.ToUInt16(left), Convert.ToUInt16(right), GetEnumResult(left, right))


                Case TypeCode.Int32 * s_TCMAX + TypeCode.Int32
                    Return OrInt32(Convert.ToInt32(left), Convert.ToInt32(right), GetEnumResult(left, right))


                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Single * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Double * s_TCMAX + TypeCode.Boolean

                    Return OrInt64(Convert.ToInt64(left), ToVBBool(right))

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.UInt32
                    Return OrUInt32(Convert.ToUInt32(left), Convert.ToUInt32(right), GetEnumResult(left, right))


                Case TypeCode.Int64 * s_TCMAX + TypeCode.Int64
                    Return OrInt64(Convert.ToInt64(left), Convert.ToInt64(right), GetEnumResult(left, right))


                Case TypeCode.UInt64 * s_TCMAX + TypeCode.UInt64
                    Return OrUInt64(Convert.ToUInt64(left), Convert.ToUInt64(right), GetEnumResult(left, right))


                Case TypeCode.Decimal * s_TCMAX + TypeCode.Empty,
                     TypeCode.Single * s_TCMAX + TypeCode.Empty,
                     TypeCode.Double * s_TCMAX + TypeCode.Empty
                    Return OrInt64(Convert.ToInt64(left), Nothing)


                Case TypeCode.String * s_TCMAX + TypeCode.Empty
                    Return OrInt64(CLng(Convert.ToString(left)), Nothing)

                Case TypeCode.String * s_TCMAX + TypeCode.Boolean
                    Return OrBoolean(CBool(Convert.ToString(left)), Convert.ToBoolean(right))

                Case TypeCode.String * s_TCMAX + TypeCode.SByte,
                     TypeCode.String * s_TCMAX + TypeCode.Byte,
                     TypeCode.String * s_TCMAX + TypeCode.Int16,
                     TypeCode.String * s_TCMAX + TypeCode.UInt16,
                     TypeCode.String * s_TCMAX + TypeCode.Int32,
                     TypeCode.String * s_TCMAX + TypeCode.UInt32,
                     TypeCode.String * s_TCMAX + TypeCode.Int64,
                     TypeCode.String * s_TCMAX + TypeCode.UInt64,
                     TypeCode.String * s_TCMAX + TypeCode.Decimal,
                     TypeCode.String * s_TCMAX + TypeCode.Single,
                     TypeCode.String * s_TCMAX + TypeCode.Double

                    Return OrInt64(CLng(Convert.ToString(left)), Convert.ToInt64(right))

                Case TypeCode.String * s_TCMAX + TypeCode.String
                    Return OrInt64(CLng(Convert.ToString(left)), CLng(Convert.ToString(right)))

            End Select

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return InvokeUserDefinedOperator(UserDefinedOperator.Or, left, right)
            End If

            Throw GetNoValidOperatorException(UserDefinedOperator.Or, left, right)
        End Function

        Private Shared Function OrBoolean(ByVal left As Boolean, ByVal right As Boolean) As Object
            Return left Or right
        End Function

        Private Shared Function OrSByte(ByVal left As SByte, ByVal right As SByte, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As SByte = left Or right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

        Private Shared Function OrByte(ByVal left As Byte, ByVal right As Byte, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As Byte = left Or right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

        Private Shared Function OrInt16(ByVal left As Int16, ByVal right As Int16, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As Int16 = left Or right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

        Private Shared Function OrUInt16(ByVal left As UInt16, ByVal right As UInt16, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As UInt16 = left Or right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

        Private Shared Function OrInt32(ByVal left As Int32, ByVal right As Int32, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As Int32 = left Or right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

        Private Shared Function OrUInt32(ByVal left As UInt32, ByVal right As UInt32, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As UInt32 = left Or right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

        Private Shared Function OrInt64(ByVal left As Int64, ByVal right As Int64, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As Int64 = left Or right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

        Private Shared Function OrUInt64(ByVal left As UInt64, ByVal right As UInt64, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As UInt64 = left Or right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

#End Region

#Region " Operator Xor "

        Public Shared Function XorObject(ByVal left As Object, ByVal right As Object) As Object

            Dim tc1 As TypeCode = GetTypeCode(left)
            Dim tc2 As TypeCode = GetTypeCode(right)

            Select Case tc1 * s_TCMAX + tc2

                Case TypeCode.Empty * s_TCMAX + TypeCode.Empty
                    Return Boxed_ZeroInteger

                Case TypeCode.Empty * s_TCMAX + TypeCode.Boolean
                    Return XorBoolean(Nothing, Convert.ToBoolean(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.SByte
                    Return XorSByte(Nothing, Convert.ToSByte(right), GetEnumResult(left, right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Byte
                    Return XorByte(Nothing, Convert.ToByte(right), GetEnumResult(left, right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int16
                    Return XorInt16(Nothing, Convert.ToInt16(right), GetEnumResult(left, right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt16
                    Return XorUInt16(Nothing, Convert.ToUInt16(right), GetEnumResult(left, right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int32
                    Return XorInt32(Nothing, Convert.ToInt32(right), GetEnumResult(left, right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt32
                    Return XorUInt32(Nothing, Convert.ToUInt32(right), GetEnumResult(left, right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int64
                    Return XorInt64(Nothing, Convert.ToInt64(right), GetEnumResult(left, right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt64
                    Return XorUInt64(Nothing, Convert.ToUInt64(right), GetEnumResult(left, right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Empty * s_TCMAX + TypeCode.Single,
                     TypeCode.Empty * s_TCMAX + TypeCode.Double
                    Return XorInt64(Nothing, Convert.ToInt64(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.String
                    Return XorInt64(Nothing, CLng(Convert.ToString(right)))


                Case TypeCode.Boolean * s_TCMAX + TypeCode.Empty
                    Return XorBoolean(Convert.ToBoolean(left), Nothing)

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Boolean
                    Return XorBoolean(Convert.ToBoolean(left), Convert.ToBoolean(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.SByte
                    Return XorSByte(ToVBBool(left), Convert.ToSByte(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Byte,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int16
                    Return XorInt16(ToVBBool(left), Convert.ToInt16(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int32
                    Return XorInt32(ToVBBool(left), Convert.ToInt32(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Single,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Double

                    Return XorInt64(ToVBBool(left), Convert.ToInt64(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.String
                    Return XorBoolean(Convert.ToBoolean(left), CBool(Convert.ToString(right)))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Empty
                    Return XorSByte(Convert.ToSByte(left), Nothing, GetEnumResult(left, right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Boolean
                    Return XorSByte(Convert.ToSByte(left), ToVBBool(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.SByte
                    Return XorSByte(Convert.ToSByte(left), Convert.ToSByte(right), GetEnumResult(left, right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Byte,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Byte * s_TCMAX + TypeCode.SByte,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Byte

                    Return XorInt16(Convert.ToInt16(left), Convert.ToInt16(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int32,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int32 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt16

                    Return XorInt32(Convert.ToInt32(left), Convert.ToInt32(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int64,
                     TypeCode.SByte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.SByte * s_TCMAX + TypeCode.Decimal,
                     TypeCode.SByte * s_TCMAX + TypeCode.Single,
                     TypeCode.SByte * s_TCMAX + TypeCode.Double,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int64,
                     TypeCode.Byte * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Byte * s_TCMAX + TypeCode.Single,
                     TypeCode.Byte * s_TCMAX + TypeCode.Double,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Double,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Double,
                     TypeCode.Int64 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Double,
                     TypeCode.Decimal * s_TCMAX + TypeCode.SByte,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Byte,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int16,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int32,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int64,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Single,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Double,
                     TypeCode.Single * s_TCMAX + TypeCode.SByte,
                     TypeCode.Single * s_TCMAX + TypeCode.Byte,
                     TypeCode.Single * s_TCMAX + TypeCode.Int16,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Single * s_TCMAX + TypeCode.Int32,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Single * s_TCMAX + TypeCode.Int64,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Single * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Single * s_TCMAX + TypeCode.Single,
                     TypeCode.Single * s_TCMAX + TypeCode.Double,
                     TypeCode.Double * s_TCMAX + TypeCode.SByte,
                     TypeCode.Double * s_TCMAX + TypeCode.Byte,
                     TypeCode.Double * s_TCMAX + TypeCode.Int16,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Double * s_TCMAX + TypeCode.Int32,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Double * s_TCMAX + TypeCode.Int64,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Double * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Double * s_TCMAX + TypeCode.Single,
                     TypeCode.Double * s_TCMAX + TypeCode.Double

                    Return XorInt64(Convert.ToInt64(left), Convert.ToInt64(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.String,
                     TypeCode.Byte * s_TCMAX + TypeCode.String,
                     TypeCode.Int16 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.String,
                     TypeCode.Int32 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.String,
                     TypeCode.Int64 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.String,
                     TypeCode.Decimal * s_TCMAX + TypeCode.String,
                     TypeCode.Single * s_TCMAX + TypeCode.String,
                     TypeCode.Double * s_TCMAX + TypeCode.String

                    Return XorInt64(Convert.ToInt64(left), CLng(Convert.ToString(right)))


                Case TypeCode.Byte * s_TCMAX + TypeCode.Empty
                    Return XorByte(Convert.ToByte(left), Nothing, GetEnumResult(left, right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Boolean
                    Return XorInt16(Convert.ToInt16(left), ToVBBool(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.Byte
                    Return XorByte(Convert.ToByte(left), Convert.ToByte(right), GetEnumResult(left, right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Byte
                    Return XorUInt16(Convert.ToUInt16(left), Convert.ToUInt16(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt16

                    Return XorUInt32(Convert.ToUInt32(left), Convert.ToUInt32(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt32

                    Return XorUInt64(Convert.ToUInt64(left), Convert.ToUInt64(right))


                Case TypeCode.Int16 * s_TCMAX + TypeCode.Empty
                    Return XorInt16(Convert.ToInt16(left), Nothing, GetEnumResult(left, right))

                Case TypeCode.Int16 * s_TCMAX + TypeCode.Int16
                    Return XorInt16(Convert.ToInt16(left), Convert.ToInt16(right), GetEnumResult(left, right))

                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Empty
                    Return XorUInt16(Convert.ToUInt16(left), Nothing, GetEnumResult(left, right))

                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Boolean
                    Return XorInt32(Convert.ToInt32(left), ToVBBool(right))

                Case TypeCode.UInt16 * s_TCMAX + TypeCode.UInt16
                    Return XorUInt16(Convert.ToUInt16(left), Convert.ToUInt16(right), GetEnumResult(left, right))

                Case TypeCode.Int32 * s_TCMAX + TypeCode.Empty
                    Return XorInt32(Convert.ToInt32(left), Nothing, GetEnumResult(left, right))

                Case TypeCode.Int32 * s_TCMAX + TypeCode.Int32
                    Return XorInt32(Convert.ToInt32(left), Convert.ToInt32(right), GetEnumResult(left, right))

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Empty
                    Return XorUInt32(Convert.ToUInt32(left), Nothing, GetEnumResult(left, right))

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Single * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Double * s_TCMAX + TypeCode.Boolean

                    Return XorInt64(Convert.ToInt64(left), ToVBBool(right))

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.UInt32
                    Return XorUInt32(Convert.ToUInt32(left), Convert.ToUInt32(right), GetEnumResult(left, right))

                Case TypeCode.Int64 * s_TCMAX + TypeCode.Empty
                    Return XorInt64(Convert.ToInt64(left), Nothing, GetEnumResult(left, right))

                Case TypeCode.Int64 * s_TCMAX + TypeCode.Int64
                    Return XorInt64(Convert.ToInt64(left), Convert.ToInt64(right), GetEnumResult(left, right))

                Case TypeCode.UInt64 * s_TCMAX + TypeCode.Empty
                    Return XorUInt64(Convert.ToUInt64(left), Nothing, GetEnumResult(left, right))

                Case TypeCode.UInt64 * s_TCMAX + TypeCode.UInt64
                    Return XorUInt64(Convert.ToUInt64(left), Convert.ToUInt64(right), GetEnumResult(left, right))


                Case TypeCode.Decimal * s_TCMAX + TypeCode.Empty,
                     TypeCode.Single * s_TCMAX + TypeCode.Empty,
                     TypeCode.Double * s_TCMAX + TypeCode.Empty
                    Return XorInt64(Convert.ToInt64(left), Nothing)


                Case TypeCode.String * s_TCMAX + TypeCode.Empty
                    Return XorInt64(CLng(Convert.ToString(left)), Nothing)

                Case TypeCode.String * s_TCMAX + TypeCode.Boolean
                    Return XorBoolean(CBool(Convert.ToString(left)), Convert.ToBoolean(right))

                Case TypeCode.String * s_TCMAX + TypeCode.SByte,
                     TypeCode.String * s_TCMAX + TypeCode.Byte,
                     TypeCode.String * s_TCMAX + TypeCode.Int16,
                     TypeCode.String * s_TCMAX + TypeCode.UInt16,
                     TypeCode.String * s_TCMAX + TypeCode.Int32,
                     TypeCode.String * s_TCMAX + TypeCode.UInt32,
                     TypeCode.String * s_TCMAX + TypeCode.Int64,
                     TypeCode.String * s_TCMAX + TypeCode.UInt64,
                     TypeCode.String * s_TCMAX + TypeCode.Decimal,
                     TypeCode.String * s_TCMAX + TypeCode.Single,
                     TypeCode.String * s_TCMAX + TypeCode.Double

                    Return XorInt64(CLng(Convert.ToString(left)), Convert.ToInt64(right))

                Case TypeCode.String * s_TCMAX + TypeCode.String
                    Return XorInt64(CLng(Convert.ToString(left)), CLng(Convert.ToString(right)))

            End Select

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return InvokeUserDefinedOperator(UserDefinedOperator.Xor, left, right)
            End If

            Throw GetNoValidOperatorException(UserDefinedOperator.Xor, left, right)
        End Function

        Private Shared Function XorBoolean(ByVal left As Boolean, ByVal right As Boolean) As Object
            Return left Xor right
        End Function

        Private Shared Function XorSByte(ByVal left As SByte, ByVal right As SByte, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As SByte = left Xor right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

        Private Shared Function XorByte(ByVal left As Byte, ByVal right As Byte, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As Byte = left Xor right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

        Private Shared Function XorInt16(ByVal left As Int16, ByVal right As Int16, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As Int16 = left Xor right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

        Private Shared Function XorUInt16(ByVal left As UInt16, ByVal right As UInt16, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As UInt16 = left Xor right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

        Private Shared Function XorInt32(ByVal left As Int32, ByVal right As Int32, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As Int32 = left Xor right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

        Private Shared Function XorUInt32(ByVal left As UInt32, ByVal right As UInt32, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As UInt32 = left Xor right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

        Private Shared Function XorInt64(ByVal left As Int64, ByVal right As Int64, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As Int64 = left Xor right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

        Private Shared Function XorUInt64(ByVal left As UInt64, ByVal right As UInt64, Optional ByVal enumType As Type = Nothing) As Object
            Dim result As UInt64 = left Xor right

            If enumType IsNot Nothing Then Return System.Enum.ToObject(enumType, result)
            Return result
        End Function

#End Region

#Region " Operator Plus + "

        Public Shared Function AddObject(ByVal left As Object, ByVal right As Object) As Object

            Dim tc1 As TypeCode = GetTypeCode(left)
            Dim tc2 As TypeCode = GetTypeCode(right)


            'Special cases for Char()
            If tc1 = TypeCode.Object Then
                Dim leftCharArray As Char() = TryCast(left, Char())

                If leftCharArray IsNot Nothing Then
                    If tc2 = TypeCode.String OrElse tc2 = TypeCode.Empty OrElse ((tc2 = TypeCode.Object) AndAlso (TypeOf right Is Char())) Then
                        'Treat Char() as String for these cases
                        left = CStr(leftCharArray)
                        tc1 = TypeCode.String
                    End If
                End If
            End If

            If (tc2 = TypeCode.Object) Then
                Dim rightCharArray As Char() = TryCast(right, Char())

                If rightCharArray IsNot Nothing Then
                    If tc1 = TypeCode.String OrElse tc1 = TypeCode.Empty Then
                        right = CStr(rightCharArray)
                        tc2 = TypeCode.String
                    End If
                End If
            End If


            Select Case tc1 * s_TCMAX + tc2

                Case TypeCode.Empty * s_TCMAX + TypeCode.Empty
                    Return Boxed_ZeroInteger

                Case TypeCode.Empty * s_TCMAX + TypeCode.Boolean
                    Return AddInt16(Nothing, ToVBBool(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.SByte
                    Return Convert.ToSByte(right)

                Case TypeCode.Empty * s_TCMAX + TypeCode.Byte
                    Return Convert.ToByte(right)

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int16
                    Return Convert.ToInt16(right)

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt16
                    Return Convert.ToUInt16(right)

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int32
                    Return Convert.ToInt32(right)

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt32
                    Return Convert.ToUInt32(right)

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int64
                    Return Convert.ToInt64(right)

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt64
                    Return Convert.ToUInt64(right)

                Case TypeCode.Empty * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Empty * s_TCMAX + TypeCode.Single,
                     TypeCode.Empty * s_TCMAX + TypeCode.Double,
                     TypeCode.Empty * s_TCMAX + TypeCode.String

                    Return right

                Case TypeCode.Empty * s_TCMAX + TypeCode.DateTime
                    Return AddString(CStr(CDate(Nothing)), CStr(Convert.ToDateTime(right)))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Char
                    Return AddString(ControlChars.NullChar, Convert.ToString(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Empty
                    Return AddInt16(ToVBBool(left), Nothing)

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Boolean
                    Return AddInt16(ToVBBool(left), ToVBBool(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.SByte
                    Return AddSByte(ToVBBool(left), Convert.ToSByte(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Byte,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int16
                    Return AddInt16(ToVBBool(left), Convert.ToInt16(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int32
                    Return AddInt32(ToVBBool(left), Convert.ToInt32(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int64
                    Return AddInt64(ToVBBool(left), Convert.ToInt64(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Decimal
                    Return AddDecimal(ToVBBool(left), Convert.ToDecimal(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Single
                    Return AddSingle(ToVBBool(left), Convert.ToSingle(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Double
                    Return AddDouble(ToVBBool(left), Convert.ToDouble(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.String
                    Return AddDouble(ToVBBool(left), CDbl(Convert.ToString(right)))


                Case TypeCode.SByte * s_TCMAX + TypeCode.Empty
                    Return Convert.ToSByte(left)

                Case TypeCode.SByte * s_TCMAX + TypeCode.Boolean
                    Return AddSByte(Convert.ToSByte(left), ToVBBool(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.SByte
                    Return AddSByte(Convert.ToSByte(left), Convert.ToSByte(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Byte,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Byte * s_TCMAX + TypeCode.SByte,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int16

                    Return AddInt16(Convert.ToInt16(left), Convert.ToInt16(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int32,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int32 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int32

                    Return AddInt32(Convert.ToInt32(left), Convert.ToInt32(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int64,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int64 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int64

                    Return AddInt64(Convert.ToInt64(left), Convert.ToInt64(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.SByte * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Byte * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Decimal * s_TCMAX + TypeCode.SByte,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Byte,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int16,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int32,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int64,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Decimal

                    Return AddDecimal(left, right)

                Case TypeCode.SByte * s_TCMAX + TypeCode.Single,
                     TypeCode.Byte * s_TCMAX + TypeCode.Single,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Single,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Single,
                     TypeCode.Single * s_TCMAX + TypeCode.SByte,
                     TypeCode.Single * s_TCMAX + TypeCode.Byte,
                     TypeCode.Single * s_TCMAX + TypeCode.Int16,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Single * s_TCMAX + TypeCode.Int32,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Single * s_TCMAX + TypeCode.Int64,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Single * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Single * s_TCMAX + TypeCode.Single

                    Return AddSingle(Convert.ToSingle(left), Convert.ToSingle(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Double,
                     TypeCode.Byte * s_TCMAX + TypeCode.Double,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Double,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Double,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Double,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Double,
                     TypeCode.Single * s_TCMAX + TypeCode.Double,
                     TypeCode.Double * s_TCMAX + TypeCode.SByte,
                     TypeCode.Double * s_TCMAX + TypeCode.Byte,
                     TypeCode.Double * s_TCMAX + TypeCode.Int16,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Double * s_TCMAX + TypeCode.Int32,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Double * s_TCMAX + TypeCode.Int64,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Double * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Double * s_TCMAX + TypeCode.Single,
                     TypeCode.Double * s_TCMAX + TypeCode.Double

                    Return AddDouble(Convert.ToDouble(left), Convert.ToDouble(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.String,
                     TypeCode.Byte * s_TCMAX + TypeCode.String,
                     TypeCode.Int16 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.String,
                     TypeCode.Int32 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.String,
                     TypeCode.Int64 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.String,
                     TypeCode.Decimal * s_TCMAX + TypeCode.String,
                     TypeCode.Single * s_TCMAX + TypeCode.String,
                     TypeCode.Double * s_TCMAX + TypeCode.String

                    Return AddDouble(Convert.ToDouble(left), CDbl(Convert.ToString(right)))

                Case TypeCode.Byte * s_TCMAX + TypeCode.Empty
                    Return Convert.ToByte(left)

                Case TypeCode.Byte * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Boolean
                    Return AddInt16(Convert.ToInt16(left), ToVBBool(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.Byte
                    Return AddByte(Convert.ToByte(left), Convert.ToByte(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt16
                    Return AddUInt16(Convert.ToUInt16(left), Convert.ToUInt16(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt32
                    Return AddUInt32(Convert.ToUInt32(left), Convert.ToUInt32(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt64

                    Return AddUInt64(Convert.ToUInt64(left), Convert.ToUInt64(right))


                Case TypeCode.Int16 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToInt16(left)


                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToUInt16(left)

                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Boolean
                    Return AddInt32(Convert.ToInt32(left), ToVBBool(right))


                Case TypeCode.Int32 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToInt32(left)


                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToUInt32(left)

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Boolean
                    Return AddInt64(Convert.ToInt64(left), ToVBBool(right))


                Case TypeCode.Int64 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToInt64(left)


                Case TypeCode.UInt64 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToUInt64(left)

                Case TypeCode.UInt64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Boolean
                    Return AddDecimal(left, ToVBBool(right))


                Case TypeCode.Decimal * s_TCMAX + TypeCode.Empty,
                     TypeCode.Single * s_TCMAX + TypeCode.Empty,
                     TypeCode.Double * s_TCMAX + TypeCode.Empty,
                     TypeCode.String * s_TCMAX + TypeCode.Empty

                    Return left


                Case TypeCode.Single * s_TCMAX + TypeCode.Boolean
                    Return AddSingle(Convert.ToSingle(left), ToVBBool(right))


                Case TypeCode.Double * s_TCMAX + TypeCode.Boolean
                    Return AddDouble(Convert.ToDouble(left), ToVBBool(right))


                Case TypeCode.DateTime * s_TCMAX + TypeCode.Empty
                    Return AddString(CStr(CDate(Nothing)), CStr(CDate(left)))

                Case TypeCode.DateTime * s_TCMAX + TypeCode.DateTime
                    Return AddString(CStr(Convert.ToDateTime(left)), CStr(Convert.ToDateTime(right)))

                Case TypeCode.DateTime * s_TCMAX + TypeCode.String
                    Return AddString(CStr(Convert.ToDateTime(left)), Convert.ToString(right))


                Case TypeCode.Char * s_TCMAX + TypeCode.Empty
                    Return AddString(Convert.ToString(left), ControlChars.NullChar)

                Case TypeCode.Char * s_TCMAX + TypeCode.Char,
                     TypeCode.Char * s_TCMAX + TypeCode.String,
                     TypeCode.String * s_TCMAX + TypeCode.Char
                    Return AddString(Convert.ToString(left), Convert.ToString(right))


                Case TypeCode.String * s_TCMAX + TypeCode.Boolean
                    Return AddDouble(CDbl(Convert.ToString(left)), ToVBBool(right))

                Case TypeCode.String * s_TCMAX + TypeCode.SByte,
                     TypeCode.String * s_TCMAX + TypeCode.Byte,
                     TypeCode.String * s_TCMAX + TypeCode.Int16,
                     TypeCode.String * s_TCMAX + TypeCode.UInt16,
                     TypeCode.String * s_TCMAX + TypeCode.Int32,
                     TypeCode.String * s_TCMAX + TypeCode.UInt32,
                     TypeCode.String * s_TCMAX + TypeCode.Int64,
                     TypeCode.String * s_TCMAX + TypeCode.UInt64,
                     TypeCode.String * s_TCMAX + TypeCode.Decimal,
                     TypeCode.String * s_TCMAX + TypeCode.Single,
                     TypeCode.String * s_TCMAX + TypeCode.Double

                    Return AddDouble(CDbl(Convert.ToString(left)), Convert.ToDouble(right))

                Case TypeCode.String * s_TCMAX + TypeCode.DateTime
                    Return AddString(Convert.ToString(left), CStr(Convert.ToDateTime(right)))

                Case TypeCode.String * s_TCMAX + TypeCode.String
                    Return AddString(Convert.ToString(left), Convert.ToString(right))

                Case Else

            End Select

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return InvokeUserDefinedOperator(UserDefinedOperator.Plus, left, right)
            End If

            Throw GetNoValidOperatorException(UserDefinedOperator.Plus, left, right)
        End Function

        Private Shared Function AddByte(ByVal left As Byte, ByVal right As Byte) As Object
            'Range of possible values:  [0, 510]
            Dim result As Int16 = CShort(left) + CShort(right)

            If result > Byte.MaxValue Then
                Return result
            Else
                Return CByte(result)
            End If
        End Function

        Private Shared Function AddSByte(ByVal left As SByte, ByVal right As SByte) As Object
            'Range of possible values:  [-256, 254]
            Dim result As Int16 = CShort(left) + CShort(right)

            If result > SByte.MaxValue OrElse result < SByte.MinValue Then
                Return result
            Else
                Return CSByte(result)
            End If
        End Function

        Private Shared Function AddInt16(ByVal left As Int16, ByVal right As Int16) As Object
            'Range of possible values:  [-65536, 65534]
            Dim result As Int32 = CInt(left) + CInt(right)

            If result > Int16.MaxValue OrElse result < Int16.MinValue Then
                Return result
            Else
                Return CShort(result)
            End If
        End Function

        Private Shared Function AddUInt16(ByVal left As UInt16, ByVal right As UInt16) As Object
            'Range of possible values:  [0, 131070]
            Dim result As Int32 = CInt(left) + CInt(right)

            If result > UInt16.MaxValue Then
                Return result
            Else
                Return CUShort(result)
            End If
        End Function

        Private Shared Function AddInt32(ByVal left As Int32, ByVal right As Int32) As Object
            'Range of possible values:  [-4294967296, 4294967294]
            Dim result As Int64 = CLng(left) + CLng(right)

            If result > Int32.MaxValue OrElse result < Int32.MinValue Then
                Return result
            Else
                Return CInt(result)
            End If
        End Function

        Private Shared Function AddUInt32(ByVal left As UInt32, ByVal right As UInt32) As Object
            'Range of possible values:  [0, 8589934590]
            Dim result As Int64 = CLng(left) + CLng(right)

            If result > UInt32.MaxValue Then
                Return result
            Else
                Return CUInt(result)
            End If
        End Function

        Private Shared Function AddInt64(ByVal left As Int64, ByVal right As Int64) As Object
            'Range of possible values:  [-18446744073709551616, 18446744073709551614]
            Try
                Return left + right
            Catch e As OverflowException
                Return CDec(left) + CDec(right)
            End Try

        End Function

        Private Shared Function AddUInt64(ByVal left As UInt64, ByVal right As UInt64) As Object
            'Range of possible values:  [0, 36893488147419103230]
            Try
                Return left + right
            Catch e As OverflowException
                Return CDec(left) + CDec(right)
            End Try

        End Function

        Private Shared Function AddDecimal(ByVal left As Object, ByVal right As Object) As Object
            Dim leftValue As Decimal = Convert.ToDecimal(left)
            Dim rightValue As Decimal = Convert.ToDecimal(right)

            Try
                Return leftValue + rightValue
            Catch ex As OverflowException
                Return CDbl(leftValue) + CDbl(rightValue)
            End Try
        End Function

        Private Shared Function AddSingle(ByVal left As Single, ByVal right As Single) As Object
            Dim result As Double = CDbl(left) + CDbl(right)

            If ((result <= Single.MaxValue AndAlso result >= Single.MinValue)) Then
                Return CSng(result)
            ElseIf Double.IsInfinity(result) AndAlso (Single.IsInfinity(left) OrElse Single.IsInfinity(right)) Then
                Return CSng(result)
            Else
                Return result
            End If
        End Function

        Private Shared Function AddDouble(ByVal left As Double, ByVal right As Double) As Object
            Return left + right
        End Function

        Private Shared Function AddString(ByVal left As String, ByVal right As String) As Object
            Return left & right
        End Function

#End Region

#Region " Operator Minus - "

        Public Shared Function SubtractObject(ByVal left As Object, ByVal right As Object) As Object
            Dim tc1 As TypeCode = GetTypeCode(left)
            Dim tc2 As TypeCode = GetTypeCode(right)

            Select Case tc1 * s_TCMAX + tc2

                Case TypeCode.Empty * s_TCMAX + TypeCode.Empty
                    Return Boxed_ZeroInteger

                Case TypeCode.Empty * s_TCMAX + TypeCode.Boolean
                    Return SubtractInt16(Nothing, ToVBBool(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.SByte
                    Return SubtractSByte(Nothing, Convert.ToSByte(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Byte
                    Return SubtractByte(Nothing, Convert.ToByte(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int16
                    Return SubtractInt16(Nothing, Convert.ToInt16(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt16
                    Return SubtractUInt16(Nothing, Convert.ToUInt16(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int32
                    Return SubtractInt32(Nothing, Convert.ToInt32(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt32
                    Return SubtractUInt32(Nothing, Convert.ToUInt32(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int64
                    Return SubtractInt64(Nothing, Convert.ToInt64(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt64
                    Return SubtractUInt64(Nothing, Convert.ToUInt64(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Decimal
                    Return SubtractDecimal(0D, right)

                Case TypeCode.Empty * s_TCMAX + TypeCode.Single
                    Return SubtractSingle(Nothing, Convert.ToSingle(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Double
                    Return SubtractDouble(Nothing, Convert.ToDouble(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.String
                    Return SubtractDouble(Nothing, CDbl(Convert.ToString(right)))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Empty
                    Return SubtractInt16(ToVBBool(left), Nothing)

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Boolean
                    Return SubtractInt16(ToVBBool(left), ToVBBool(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.SByte
                    Return SubtractSByte(ToVBBool(left), Convert.ToSByte(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Byte,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int16
                    Return SubtractInt16(ToVBBool(left), Convert.ToInt16(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int32
                    Return SubtractInt32(ToVBBool(left), Convert.ToInt32(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int64
                    Return SubtractInt64(ToVBBool(left), Convert.ToInt64(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Decimal
                    Return SubtractDecimal(ToVBBool(left), Convert.ToDecimal(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Single
                    Return SubtractSingle(ToVBBool(left), Convert.ToSingle(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Double
                    Return SubtractDouble(ToVBBool(left), Convert.ToDouble(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.String
                    Return SubtractDouble(ToVBBool(left), CDbl(Convert.ToString(right)))


                Case TypeCode.SByte * s_TCMAX + TypeCode.Empty
                    Return Convert.ToSByte(left)

                Case TypeCode.SByte * s_TCMAX + TypeCode.Boolean
                    Return SubtractSByte(Convert.ToSByte(left), ToVBBool(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.SByte
                    Return SubtractSByte(Convert.ToSByte(left), Convert.ToSByte(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Byte,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Byte * s_TCMAX + TypeCode.SByte,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int16

                    Return SubtractInt16(Convert.ToInt16(left), Convert.ToInt16(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int32,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int32 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int32

                    Return SubtractInt32(Convert.ToInt32(left), Convert.ToInt32(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int64,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int64 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int64

                    Return SubtractInt64(Convert.ToInt64(left), Convert.ToInt64(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.SByte * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Byte * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Decimal * s_TCMAX + TypeCode.SByte,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Byte,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int16,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int32,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int64,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Decimal

                    Return SubtractDecimal(left, right)

                Case TypeCode.SByte * s_TCMAX + TypeCode.Single,
                     TypeCode.Byte * s_TCMAX + TypeCode.Single,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Single,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Single,
                     TypeCode.Single * s_TCMAX + TypeCode.SByte,
                     TypeCode.Single * s_TCMAX + TypeCode.Byte,
                     TypeCode.Single * s_TCMAX + TypeCode.Int16,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Single * s_TCMAX + TypeCode.Int32,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Single * s_TCMAX + TypeCode.Int64,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Single * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Single * s_TCMAX + TypeCode.Single

                    Return SubtractSingle(Convert.ToSingle(left), Convert.ToSingle(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Double,
                     TypeCode.Byte * s_TCMAX + TypeCode.Double,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Double,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Double,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Double,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Double,
                     TypeCode.Single * s_TCMAX + TypeCode.Double,
                     TypeCode.Double * s_TCMAX + TypeCode.SByte,
                     TypeCode.Double * s_TCMAX + TypeCode.Byte,
                     TypeCode.Double * s_TCMAX + TypeCode.Int16,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Double * s_TCMAX + TypeCode.Int32,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Double * s_TCMAX + TypeCode.Int64,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Double * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Double * s_TCMAX + TypeCode.Single,
                     TypeCode.Double * s_TCMAX + TypeCode.Double

                    Return SubtractDouble(Convert.ToDouble(left), Convert.ToDouble(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.String,
                     TypeCode.Byte * s_TCMAX + TypeCode.String,
                     TypeCode.Int16 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.String,
                     TypeCode.Int32 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.String,
                     TypeCode.Int64 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.String,
                     TypeCode.Decimal * s_TCMAX + TypeCode.String,
                     TypeCode.Single * s_TCMAX + TypeCode.String,
                     TypeCode.Double * s_TCMAX + TypeCode.String

                    Return SubtractDouble(Convert.ToDouble(left), CDbl(Convert.ToString(right)))


                Case TypeCode.Byte * s_TCMAX + TypeCode.Empty
                    Return Convert.ToByte(left)

                Case TypeCode.Byte * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Boolean
                    Return SubtractInt16(Convert.ToInt16(left), ToVBBool(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.Byte
                    Return SubtractByte(Convert.ToByte(left), Convert.ToByte(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt16
                    Return SubtractUInt16(Convert.ToUInt16(left), Convert.ToUInt16(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt32
                    Return SubtractUInt32(Convert.ToUInt32(left), Convert.ToUInt32(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt64

                    Return SubtractUInt64(Convert.ToUInt64(left), Convert.ToUInt64(right))


                Case TypeCode.Int16 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToInt16(left)

                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToUInt16(left)

                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Boolean
                    Return SubtractInt32(Convert.ToInt32(left), ToVBBool(right))


                Case TypeCode.Int32 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToInt32(left)

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToUInt32(left)

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Boolean
                    Return SubtractInt64(Convert.ToInt64(left), ToVBBool(right))


                Case TypeCode.Int64 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToInt64(left)

                Case TypeCode.UInt64 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToUInt64(left)

                Case TypeCode.UInt64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Boolean
                    Return SubtractDecimal(left, ToVBBool(right))


                Case TypeCode.Decimal * s_TCMAX + TypeCode.Empty,
                     TypeCode.Single * s_TCMAX + TypeCode.Empty,
                     TypeCode.Double * s_TCMAX + TypeCode.Empty
                    Return left


                Case TypeCode.Single * s_TCMAX + TypeCode.Boolean
                    Return SubtractSingle(Convert.ToSingle(left), ToVBBool(right))

                Case TypeCode.Double * s_TCMAX + TypeCode.Boolean
                    Return SubtractDouble(Convert.ToDouble(left), ToVBBool(right))

                Case TypeCode.String * s_TCMAX + TypeCode.Empty
                    Return CDbl(Convert.ToString(left))

                Case TypeCode.String * s_TCMAX + TypeCode.Boolean
                    Return SubtractDouble(CDbl(Convert.ToString(left)), ToVBBool(right))

                Case TypeCode.String * s_TCMAX + TypeCode.SByte,
                     TypeCode.String * s_TCMAX + TypeCode.Byte,
                     TypeCode.String * s_TCMAX + TypeCode.Int16,
                     TypeCode.String * s_TCMAX + TypeCode.UInt16,
                     TypeCode.String * s_TCMAX + TypeCode.Int32,
                     TypeCode.String * s_TCMAX + TypeCode.UInt32,
                     TypeCode.String * s_TCMAX + TypeCode.Int64,
                     TypeCode.String * s_TCMAX + TypeCode.UInt64,
                     TypeCode.String * s_TCMAX + TypeCode.Decimal,
                     TypeCode.String * s_TCMAX + TypeCode.Single,
                     TypeCode.String * s_TCMAX + TypeCode.Double

                    Return SubtractDouble(CDbl(Convert.ToString(left)), Convert.ToDouble(right))

                Case TypeCode.String * s_TCMAX + TypeCode.String
                    Return SubtractDouble(CDbl(Convert.ToString(left)), CDbl(Convert.ToString(right)))

                Case Else

            End Select

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object OrElse
               (tc1 = TypeCode.DateTime AndAlso tc2 = TypeCode.DateTime) OrElse
               (tc1 = TypeCode.DateTime AndAlso tc2 = TypeCode.Empty) OrElse
               (tc1 = TypeCode.Empty AndAlso tc2 = TypeCode.DateTime) Then

                Return InvokeUserDefinedOperator(UserDefinedOperator.Minus, left, right)
            End If

            Throw GetNoValidOperatorException(UserDefinedOperator.Minus, left, right)

        End Function

        Private Shared Function SubtractByte(ByVal left As Byte, ByVal right As Byte) As Object
            'Range of possible values:  [-255, 255]
            Dim result As Int16 = CShort(left) - CShort(right)

            If result < Byte.MinValue Then
                Return result
            Else
                Return CByte(result)
            End If
        End Function

        Private Shared Function SubtractSByte(ByVal left As SByte, ByVal right As SByte) As Object
            'Range of possible values:  [-255, 255]
            Dim result As Int16 = CShort(left) - CShort(right)

            If result < SByte.MinValue OrElse result > SByte.MaxValue Then
                Return result
            Else
                Return CSByte(result)
            End If
        End Function

        Private Shared Function SubtractInt16(ByVal left As Int16, ByVal right As Int16) As Object
            'Range of possible values:  [-65535, 65535]
            Dim result As Int32 = CInt(left) - CInt(right)

            If result < Int16.MinValue OrElse result > Int16.MaxValue Then
                Return result
            Else
                Return CShort(result)
            End If
        End Function

        Private Shared Function SubtractUInt16(ByVal left As UInt16, ByVal right As UInt16) As Object
            'Range of possible values:  [-65535, 65535]
            Dim result As Int32 = CInt(left) - CInt(right)

            If result < UInt16.MinValue Then
                Return result
            Else
                Return CUShort(result)
            End If
        End Function

        Private Shared Function SubtractInt32(ByVal left As Int32, ByVal right As Int32) As Object
            'Range of possible values:  [-4294967295, 4294967295]
            Dim result As Int64 = CLng(left) - CLng(right)

            If result < Int32.MinValue OrElse result > Int32.MaxValue Then
                Return result
            Else
                Return CInt(result)
            End If
        End Function

        Private Shared Function SubtractUInt32(ByVal left As UInt32, ByVal right As UInt32) As Object
            'Range of possible values:  [-4294967295, 4294967295]
            Dim result As Int64 = CLng(left) - CLng(right)

            If result < UInt32.MinValue Then
                Return result
            Else
                Return CUInt(result)
            End If
        End Function

        Private Shared Function SubtractInt64(ByVal left As Int64, ByVal right As Int64) As Object
            'Range of possible values:  [-18446744073709551615, 18446744073709551615]
            Try
                Return left - right
            Catch ex As OverflowException
                Return CDec(left) - CDec(right)
            End Try

        End Function

        Private Shared Function SubtractUInt64(ByVal left As UInt64, ByVal right As UInt64) As Object
            'Range of possible values:  [-18446744073709551615, 18446744073709551615]
            Try
                Return left - right
            Catch ex As OverflowException
                Return CDec(left) - CDec(right)
            End Try

        End Function

        Private Shared Function SubtractDecimal(ByVal left As Object, ByVal right As Object) As Object
            Dim leftValue As Decimal = Convert.ToDecimal(left)
            Dim rightValue As Decimal = Convert.ToDecimal(right)

            Try
                Return leftValue - rightValue
            Catch ex As OverflowException
                Return CDbl(leftValue) - CDbl(rightValue)
            End Try
        End Function

        Private Shared Function SubtractSingle(ByVal left As Single, ByVal right As Single) As Object
            Dim result As Double = CDbl(left) - CDbl(right)

            If ((result <= Single.MaxValue AndAlso result >= Single.MinValue)) Then
                Return CSng(result)
            ElseIf Double.IsInfinity(result) AndAlso (Single.IsInfinity(left) OrElse Single.IsInfinity(right)) Then
                Return CSng(result)
            Else
                Return result
            End If
        End Function

        Private Shared Function SubtractDouble(ByVal left As Double, ByVal right As Double) As Object
            Return left - right
        End Function

#End Region

#Region " Operator Multiply * "

        Public Shared Function MultiplyObject(ByVal left As Object, ByVal right As Object) As Object

            Dim tc1 As TypeCode = GetTypeCode(left)
            Dim tc2 As TypeCode = GetTypeCode(right)

            Select Case tc1 * s_TCMAX + tc2

                Case TypeCode.Empty * s_TCMAX + TypeCode.Empty,
                     TypeCode.Empty * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Empty
                    Return Boxed_ZeroInteger

                Case TypeCode.Empty * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Empty,
                     TypeCode.Empty * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Empty
                    Return Boxed_ZeroShort

                Case TypeCode.Empty * s_TCMAX + TypeCode.SByte,
                     TypeCode.SByte * s_TCMAX + TypeCode.Empty
                    Return Boxed_ZeroSByte

                Case TypeCode.Empty * s_TCMAX + TypeCode.Byte,
                     TypeCode.Byte * s_TCMAX + TypeCode.Empty
                    Return Boxed_ZeroByte

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Empty
                    Return Boxed_ZeroUShort

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Empty
                    Return Boxed_ZeroUInteger

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Empty
                    Return Boxed_ZeroLong

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Empty
                    Return Boxed_ZeroULong

                Case TypeCode.Empty * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Empty
                    Return Boxed_ZeroDecimal

                Case TypeCode.Empty * s_TCMAX + TypeCode.Single,
                     TypeCode.Single * s_TCMAX + TypeCode.Empty
                    Return Boxed_ZeroSinge

                Case TypeCode.Empty * s_TCMAX + TypeCode.Double,
                     TypeCode.Double * s_TCMAX + TypeCode.Empty
                    Return Boxed_ZeroDouble

                Case TypeCode.Empty * s_TCMAX + TypeCode.String
                    Return MultiplyDouble(Nothing, CDbl(Convert.ToString(right)))


                Case TypeCode.Boolean * s_TCMAX + TypeCode.Boolean
                    Return MultiplyInt16(ToVBBool(left), ToVBBool(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.SByte
                    Return MultiplySByte(ToVBBool(left), Convert.ToSByte(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Byte,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int16
                    Return MultiplyInt16(ToVBBool(left), Convert.ToInt16(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int32
                    Return MultiplyInt32(ToVBBool(left), Convert.ToInt32(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int64
                    Return MultiplyInt64(ToVBBool(left), Convert.ToInt64(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Decimal
                    Return MultiplyDecimal(ToVBBool(left), Convert.ToDecimal(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Single
                    Return MultiplySingle(ToVBBool(left), Convert.ToSingle(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Double
                    Return MultiplyDouble(ToVBBool(left), Convert.ToDouble(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.String
                    Return MultiplyDouble(ToVBBool(left), CDbl(Convert.ToString(right)))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Boolean
                    Return MultiplySByte(Convert.ToSByte(left), ToVBBool(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.SByte
                    Return MultiplySByte(Convert.ToSByte(left), Convert.ToSByte(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Byte,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Byte * s_TCMAX + TypeCode.SByte,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int16

                    Return MultiplyInt16(Convert.ToInt16(left), Convert.ToInt16(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int32,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int32 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int32

                    Return MultiplyInt32(Convert.ToInt32(left), Convert.ToInt32(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int64,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int64 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int64

                    Return MultiplyInt64(Convert.ToInt64(left), Convert.ToInt64(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.SByte * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Byte * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Decimal * s_TCMAX + TypeCode.SByte,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Byte,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int16,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int32,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int64,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Decimal

                    Return MultiplyDecimal(left, right)

                Case TypeCode.SByte * s_TCMAX + TypeCode.Single,
                     TypeCode.Byte * s_TCMAX + TypeCode.Single,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Single,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Single,
                     TypeCode.Single * s_TCMAX + TypeCode.SByte,
                     TypeCode.Single * s_TCMAX + TypeCode.Byte,
                     TypeCode.Single * s_TCMAX + TypeCode.Int16,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Single * s_TCMAX + TypeCode.Int32,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Single * s_TCMAX + TypeCode.Int64,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Single * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Single * s_TCMAX + TypeCode.Single

                    Return MultiplySingle(Convert.ToSingle(left), Convert.ToSingle(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Double,
                     TypeCode.Byte * s_TCMAX + TypeCode.Double,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Double,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Double,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Double,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Double,
                     TypeCode.Single * s_TCMAX + TypeCode.Double,
                     TypeCode.Double * s_TCMAX + TypeCode.SByte,
                     TypeCode.Double * s_TCMAX + TypeCode.Byte,
                     TypeCode.Double * s_TCMAX + TypeCode.Int16,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Double * s_TCMAX + TypeCode.Int32,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Double * s_TCMAX + TypeCode.Int64,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Double * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Double * s_TCMAX + TypeCode.Single,
                     TypeCode.Double * s_TCMAX + TypeCode.Double

                    Return MultiplyDouble(Convert.ToDouble(left), Convert.ToDouble(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.String,
                     TypeCode.Byte * s_TCMAX + TypeCode.String,
                     TypeCode.Int16 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.String,
                     TypeCode.Int32 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.String,
                     TypeCode.Int64 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.String,
                     TypeCode.Decimal * s_TCMAX + TypeCode.String,
                     TypeCode.Single * s_TCMAX + TypeCode.String,
                     TypeCode.Double * s_TCMAX + TypeCode.String

                    Return MultiplyDouble(Convert.ToDouble(left), CDbl(Convert.ToString(right)))


                Case TypeCode.Byte * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Boolean
                    Return MultiplyInt16(Convert.ToInt16(left), ToVBBool(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.Byte
                    Return MultiplyByte(Convert.ToByte(left), Convert.ToByte(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt16
                    Return MultiplyUInt16(Convert.ToUInt16(left), Convert.ToUInt16(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt32
                    Return MultiplyUInt32(Convert.ToUInt32(left), Convert.ToUInt32(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt64

                    Return MultiplyUInt64(Convert.ToUInt64(left), Convert.ToUInt64(right))


                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Boolean
                    Return MultiplyInt32(Convert.ToInt32(left), ToVBBool(right))

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Boolean
                    Return MultiplyInt64(Convert.ToInt64(left), ToVBBool(right))


                Case TypeCode.UInt64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Boolean
                    Return MultiplyDecimal(left, ToVBBool(right))

                Case TypeCode.Single * s_TCMAX + TypeCode.Boolean
                    Return MultiplySingle(Convert.ToSingle(left), ToVBBool(right))


                Case TypeCode.Double * s_TCMAX + TypeCode.Boolean
                    Return MultiplyDouble(Convert.ToDouble(left), ToVBBool(right))


                Case TypeCode.String * s_TCMAX + TypeCode.Empty
                    Return MultiplyDouble(CDbl(Convert.ToString(left)), Nothing)

                Case TypeCode.String * s_TCMAX + TypeCode.Boolean
                    Return MultiplyDouble(CDbl(Convert.ToString(left)), ToVBBool(right))

                Case TypeCode.String * s_TCMAX + TypeCode.SByte,
                     TypeCode.String * s_TCMAX + TypeCode.Byte,
                     TypeCode.String * s_TCMAX + TypeCode.Int16,
                     TypeCode.String * s_TCMAX + TypeCode.UInt16,
                     TypeCode.String * s_TCMAX + TypeCode.Int32,
                     TypeCode.String * s_TCMAX + TypeCode.UInt32,
                     TypeCode.String * s_TCMAX + TypeCode.Int64,
                     TypeCode.String * s_TCMAX + TypeCode.UInt64,
                     TypeCode.String * s_TCMAX + TypeCode.Decimal,
                     TypeCode.String * s_TCMAX + TypeCode.Single,
                     TypeCode.String * s_TCMAX + TypeCode.Double

                    Return MultiplyDouble(CDbl(Convert.ToString(left)), Convert.ToDouble(right))

                Case TypeCode.String * s_TCMAX + TypeCode.String
                    Return MultiplyDouble(CDbl(Convert.ToString(left)), CDbl(Convert.ToString(right)))

                Case Else

            End Select

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return InvokeUserDefinedOperator(UserDefinedOperator.Multiply, left, right)
            End If

            Throw GetNoValidOperatorException(UserDefinedOperator.Multiply, left, right)

        End Function

        Private Shared Function MultiplyByte(ByVal left As Byte, ByVal right As Byte) As Object
            'Range of possible values:  [0, 65025]
            Dim result As Int32 = CInt(left) * CInt(right)

            If result > Byte.MaxValue Then
                If result > Int16.MaxValue Then
                    Return result
                Else
                    Return CShort(result)
                End If
            Else
                Return CByte(result)
            End If
        End Function

        Private Shared Function MultiplySByte(ByVal left As SByte, ByVal right As SByte) As Object
            'Range of possible values:  [-16256 ,16384]
            Dim result As Int16 = CShort(left) * CShort(right)

            If result > SByte.MaxValue OrElse result < SByte.MinValue Then
                Return result
            Else
                Return CSByte(result)
            End If
        End Function

        Private Shared Function MultiplyInt16(ByVal left As Int16, ByVal right As Int16) As Object
            'Range of possible values:  [-1073709056, 1073741824]
            Dim result As Int32 = CInt(left) * CInt(right)

            If result > Int16.MaxValue OrElse result < Int16.MinValue Then
                Return result
            Else
                Return CShort(result)
            End If
        End Function

        Private Shared Function MultiplyUInt16(ByVal left As UInt16, ByVal right As UInt16) As Object
            'Range of possible values:  [0, 4294836225]
            Dim result As Int64 = CLng(left) * CLng(right)

            If result > UInt16.MaxValue Then
                If result > Int32.MaxValue Then
                    Return result
                Else
                    Return CInt(result)
                End If
            Else
                Return CUShort(result)
            End If
        End Function

        Private Shared Function MultiplyInt32(ByVal left As Int32, ByVal right As Int32) As Object
            'Range of possible values:  [-4611686016279904256, 4611686018427387904]
            Dim result As Int64 = CLng(left) * CLng(right)

            If result > Int32.MaxValue OrElse result < Int32.MinValue Then
                Return result
            Else
                Return CInt(result)
            End If
        End Function

        Private Shared Function MultiplyUInt32(ByVal left As UInt32, ByVal right As UInt32) As Object
            'Range of possible values:  [0, 18446744065119617025]
            Dim result As UInt64 = CULng(left) * CULng(right)

            If result > UInt32.MaxValue Then
                If result > Int64.MaxValue Then
                    Return CDec(result)
                Else
                    Return CLng(result)
                End If
            Else
                Return CUInt(result)
            End If
        End Function

        Private Shared Function MultiplyInt64(ByVal left As Int64, ByVal right As Int64) As Object
            Try
                Return left * right
            Catch ex As OverflowException
            End Try

            Try
                Return CDec(left) * CDec(right)
            Catch ex As OverflowException
                Return CDbl(left) * CDbl(right)
            End Try
        End Function

        Private Shared Function MultiplyUInt64(ByVal left As UInt64, ByVal right As UInt64) As Object
            Try
                Return left * right
            Catch ex As OverflowException
            End Try

            Try
                Return CDec(left) * CDec(right)
            Catch ex As OverflowException
                Return CDbl(left) * CDbl(right)
            End Try
        End Function

        Private Shared Function MultiplyDecimal(ByVal left As Object, ByVal right As Object) As Object
            Dim leftValue As Decimal = Convert.ToDecimal(left)
            Dim rightValue As Decimal = Convert.ToDecimal(right)

            Try
                Return leftValue * rightValue
            Catch ex As OverflowException
                'Converting to Double is inconsistent with Division, where we convert to Single.
                Return CDbl(leftValue) * CDbl(rightValue)
            End Try
        End Function

        Private Shared Function MultiplySingle(ByVal left As Single, ByVal right As Single) As Object
            Dim result As Double = CDbl(left) * CDbl(right)

            If ((result <= Single.MaxValue AndAlso result >= Single.MinValue)) Then
                Return CSng(result)
            ElseIf Double.IsInfinity(result) AndAlso (Single.IsInfinity(left) OrElse Single.IsInfinity(right)) Then
                Return CSng(result)
            Else
                Return result
            End If
        End Function

        Private Shared Function MultiplyDouble(ByVal left As Double, ByVal right As Double) As Object
            Return left * right
        End Function

#End Region

#Region " Operator Divide / "

        Public Shared Function DivideObject(ByVal left As Object, ByVal right As Object) As Object

            Dim tc1 As TypeCode = GetTypeCode(left)
            Dim tc2 As TypeCode = GetTypeCode(right)

            Select Case tc1 * s_TCMAX + tc2

                Case TypeCode.Empty * s_TCMAX + TypeCode.Empty
                    Return DivideDouble(Nothing, Nothing)

                Case TypeCode.Empty * s_TCMAX + TypeCode.Boolean
                    Return DivideDouble(Nothing, ToVBBool(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.SByte,
                     TypeCode.Empty * s_TCMAX + TypeCode.Byte,
                     TypeCode.Empty * s_TCMAX + TypeCode.Int16,
                     TypeCode.Empty * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Empty * s_TCMAX + TypeCode.Int32,
                     TypeCode.Empty * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Empty * s_TCMAX + TypeCode.Int64,
                     TypeCode.Empty * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Empty * s_TCMAX + TypeCode.Double
                    Return DivideDouble(Nothing, Convert.ToDouble(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Decimal
                    Return DivideDecimal(0D, right)

                Case TypeCode.Empty * s_TCMAX + TypeCode.Single
                    Return DivideSingle(Nothing, Convert.ToSingle(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.String
                    Return DivideDouble(Nothing, CDbl(Convert.ToString(right)))


                Case TypeCode.Boolean * s_TCMAX + TypeCode.Empty
                    Return DivideDouble(ToVBBool(left), Nothing)

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Boolean
                    Return DivideDouble(ToVBBool(left), ToVBBool(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.SByte,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Byte,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int16,
                     TypeCode.Boolean * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int32,
                     TypeCode.Boolean * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Double
                    Return DivideDouble(ToVBBool(left), Convert.ToDouble(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Decimal
                    Return DivideDecimal(ToVBBool(left), right)

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Single
                    Return DivideSingle(ToVBBool(left), Convert.ToSingle(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.String
                    Return DivideDouble(ToVBBool(left), CDbl(Convert.ToString(right)))


                Case TypeCode.SByte * s_TCMAX + TypeCode.Empty,
                     TypeCode.Byte * s_TCMAX + TypeCode.Empty,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Empty,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Empty,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Empty,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Empty,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Empty,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Empty,
                     TypeCode.Double * s_TCMAX + TypeCode.Empty
                    Return DivideDouble(Convert.ToDouble(left), Nothing)

                Case TypeCode.SByte * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Byte * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Double * s_TCMAX + TypeCode.Boolean
                    Return DivideDouble(Convert.ToDouble(left), ToVBBool(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.SByte,
                     TypeCode.SByte * s_TCMAX + TypeCode.Byte,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int16,
                     TypeCode.SByte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int32,
                     TypeCode.SByte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int64,
                     TypeCode.SByte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.SByte * s_TCMAX + TypeCode.Double,
                     TypeCode.Byte * s_TCMAX + TypeCode.SByte,
                     TypeCode.Byte * s_TCMAX + TypeCode.Byte,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Byte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int32,
                     TypeCode.Byte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int64,
                     TypeCode.Byte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Byte * s_TCMAX + TypeCode.Double,
                     TypeCode.Int16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Double,
                     TypeCode.Int32 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Double,
                     TypeCode.Int64 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Double,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Double,
                     TypeCode.Single * s_TCMAX + TypeCode.Double,
                     TypeCode.Double * s_TCMAX + TypeCode.SByte,
                     TypeCode.Double * s_TCMAX + TypeCode.Byte,
                     TypeCode.Double * s_TCMAX + TypeCode.Int16,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Double * s_TCMAX + TypeCode.Int32,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Double * s_TCMAX + TypeCode.Int64,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Double * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Double * s_TCMAX + TypeCode.Single,
                     TypeCode.Double * s_TCMAX + TypeCode.Double
                    Return DivideDouble(Convert.ToDouble(left), Convert.ToDouble(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Byte * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Decimal * s_TCMAX + TypeCode.SByte,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Byte,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int16,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int32,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int64,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Decimal
                    Return DivideDecimal(left, right)

                Case TypeCode.SByte * s_TCMAX + TypeCode.Single,
                     TypeCode.Byte * s_TCMAX + TypeCode.Single,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Single,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Single,
                     TypeCode.Single * s_TCMAX + TypeCode.SByte,
                     TypeCode.Single * s_TCMAX + TypeCode.Byte,
                     TypeCode.Single * s_TCMAX + TypeCode.Int16,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Single * s_TCMAX + TypeCode.Int32,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Single * s_TCMAX + TypeCode.Int64,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Single * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Single * s_TCMAX + TypeCode.Single
                    Return DivideSingle(Convert.ToSingle(left), Convert.ToSingle(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.String,
                     TypeCode.Byte * s_TCMAX + TypeCode.String,
                     TypeCode.Int16 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.String,
                     TypeCode.Int32 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.String,
                     TypeCode.Int64 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.String,
                     TypeCode.Decimal * s_TCMAX + TypeCode.String,
                     TypeCode.Single * s_TCMAX + TypeCode.String,
                     TypeCode.Double * s_TCMAX + TypeCode.String
                    Return DivideDouble(Convert.ToDouble(left), CDbl(Convert.ToString(right)))


                Case TypeCode.Decimal * s_TCMAX + TypeCode.Empty
                    Return DivideDecimal(left, 0D)

                Case TypeCode.Decimal * s_TCMAX + TypeCode.Boolean
                    Return DivideDecimal(left, ToVBBool(right))


                Case TypeCode.Single * s_TCMAX + TypeCode.Empty
                    Return DivideSingle(Convert.ToSingle(left), Nothing)

                Case TypeCode.Single * s_TCMAX + TypeCode.Boolean
                    Return DivideSingle(Convert.ToSingle(left), ToVBBool(right))


                Case TypeCode.String * s_TCMAX + TypeCode.Empty
                    Return DivideDouble(CDbl(Convert.ToString(left)), Nothing)

                Case TypeCode.String * s_TCMAX + TypeCode.Boolean
                    Return DivideDouble(CDbl(Convert.ToString(left)), ToVBBool(right))

                Case TypeCode.String * s_TCMAX + TypeCode.SByte,
                     TypeCode.String * s_TCMAX + TypeCode.Byte,
                     TypeCode.String * s_TCMAX + TypeCode.Int16,
                     TypeCode.String * s_TCMAX + TypeCode.UInt16,
                     TypeCode.String * s_TCMAX + TypeCode.Int32,
                     TypeCode.String * s_TCMAX + TypeCode.UInt32,
                     TypeCode.String * s_TCMAX + TypeCode.Int64,
                     TypeCode.String * s_TCMAX + TypeCode.UInt64,
                     TypeCode.String * s_TCMAX + TypeCode.Decimal,
                     TypeCode.String * s_TCMAX + TypeCode.Single,
                     TypeCode.String * s_TCMAX + TypeCode.Double
                    Return DivideDouble(CDbl(Convert.ToString(left)), Convert.ToDouble(right))

                Case TypeCode.String * s_TCMAX + TypeCode.String
                    Return DivideDouble(CDbl(Convert.ToString(left)), CDbl(Convert.ToString(right)))

                Case Else

            End Select

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return InvokeUserDefinedOperator(UserDefinedOperator.Divide, left, right)
            End If

            Throw GetNoValidOperatorException(UserDefinedOperator.Divide, left, right)

        End Function

        Private Shared Function DivideDecimal(ByVal left As Object, ByVal right As Object) As Object
            Dim leftValue As Decimal = Convert.ToDecimal(left)
            Dim rightValue As Decimal = Convert.ToDecimal(right)

            Try
                Return leftValue / rightValue
            Catch ex As OverflowException
                'Converting to Single is inconsistent with Multiplication, where we convert to Double.
                Return CSng(leftValue) / CSng(rightValue)
            End Try
        End Function

        Private Shared Function DivideSingle(ByVal left As Single, ByVal right As Single) As Object
            Dim result As Single = left / right

            If Single.IsInfinity(result) Then
                If Single.IsInfinity(left) OrElse Single.IsInfinity(right) Then
                    Return result
                End If
                Return CDbl(left) / CDbl(right)
            Else
                Return result
            End If

        End Function

        Private Shared Function DivideDouble(ByVal left As Double, ByVal right As Double) As Object
            Return left / right
        End Function

#End Region

#Region " Operator Power ^ "

        Public Shared Function ExponentObject(ByVal left As Object, ByVal right As Object) As Object

            Dim tc1 As TypeCode = GetTypeCode(left)
            Dim tc2 As TypeCode = GetTypeCode(right)

            Dim leftValue As Double
            Dim rightValue As Double


            Select Case tc1
                Case TypeCode.Empty
                    leftValue = 0.0R

                Case TypeCode.Boolean
                    leftValue = ToVBBool(left)

                Case TypeCode.SByte,
                     TypeCode.Byte,
                     TypeCode.Int16,
                     TypeCode.UInt16,
                     TypeCode.Int32,
                     TypeCode.UInt32,
                     TypeCode.Int64,
                     TypeCode.UInt64,
                     TypeCode.Decimal,
                     TypeCode.Single,
                     TypeCode.Double
                    leftValue = Convert.ToDouble(left)

                Case TypeCode.String
                    leftValue = CDbl(Convert.ToString(left))

                Case TypeCode.Object
                    Return InvokeUserDefinedOperator(UserDefinedOperator.Power, left, right)

                Case Else
                    'DateTime
                    'Char
                    Throw GetNoValidOperatorException(UserDefinedOperator.Power, left, right)
            End Select

            Select Case tc2
                Case TypeCode.Empty
                    rightValue = 0.0R

                Case TypeCode.Boolean
                    rightValue = ToVBBool(right)

                Case TypeCode.SByte,
                     TypeCode.Byte,
                     TypeCode.Int16,
                     TypeCode.UInt16,
                     TypeCode.Int32,
                     TypeCode.UInt32,
                     TypeCode.Int64,
                     TypeCode.UInt64,
                     TypeCode.Decimal,
                     TypeCode.Single,
                     TypeCode.Double
                    rightValue = Convert.ToDouble(right)

                Case TypeCode.String
                    rightValue = CDbl(Convert.ToString(right))

                Case TypeCode.Object
                    Return InvokeUserDefinedOperator(UserDefinedOperator.Power, left, right)

                Case Else
                    'DateTime
                    'Char
                    Throw GetNoValidOperatorException(UserDefinedOperator.Power, left, right)
            End Select

            Return leftValue ^ rightValue

        End Function

#End Region

#Region " Operator Mod "

        Public Shared Function ModObject(ByVal left As Object, ByVal right As Object) As Object

            Dim tc1 As TypeCode = GetTypeCode(left)
            Dim tc2 As TypeCode = GetTypeCode(right)

            Select Case tc1 * s_TCMAX + tc2

                Case TypeCode.Empty * s_TCMAX + TypeCode.Empty
                    Return ModInt32(Nothing, Nothing)

                Case TypeCode.Empty * s_TCMAX + TypeCode.Boolean
                    Return ModInt16(Nothing, ToVBBool(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.SByte
                    Return ModSByte(Nothing, Convert.ToSByte(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Byte
                    Return ModByte(Nothing, Convert.ToByte(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int16
                    Return ModInt16(Nothing, Convert.ToInt16(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt16
                    Return ModUInt16(Nothing, Convert.ToUInt16(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int32
                    Return ModInt32(Nothing, Convert.ToInt32(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt32
                    Return ModUInt32(Nothing, Convert.ToUInt32(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int64
                    Return ModInt64(Nothing, Convert.ToInt64(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt64
                    Return ModUInt64(Nothing, Convert.ToUInt64(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Decimal
                    Return ModDecimal(0D, Convert.ToDecimal(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Single
                    Return ModSingle(Nothing, Convert.ToSingle(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Double
                    Return ModDouble(Nothing, Convert.ToDouble(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.String
                    Return ModDouble(Nothing, CDbl(Convert.ToString(right)))


                Case TypeCode.Boolean * s_TCMAX + TypeCode.Empty
                    Return ModInt16(ToVBBool(left), Nothing)

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Boolean
                    Return ModInt16(ToVBBool(left), ToVBBool(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.SByte
                    Return ModSByte(ToVBBool(left), Convert.ToSByte(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Byte,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int16
                    Return ModInt16(ToVBBool(left), Convert.ToInt16(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int32
                    Return ModInt32(ToVBBool(left), Convert.ToInt32(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int64
                    Return ModInt64(ToVBBool(left), Convert.ToInt64(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Decimal
                    Return ModDecimal(ToVBBool(left), Convert.ToDecimal(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Single
                    Return ModSingle(ToVBBool(left), Convert.ToSingle(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Double
                    Return ModDouble(ToVBBool(left), Convert.ToDouble(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.String
                    Return ModDouble(ToVBBool(left), CDbl(Convert.ToString(right)))


                Case TypeCode.SByte * s_TCMAX + TypeCode.Empty
                    Return ModSByte(Convert.ToSByte(left), Nothing)

                Case TypeCode.SByte * s_TCMAX + TypeCode.Boolean
                    Return ModSByte(Convert.ToSByte(left), ToVBBool(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.SByte
                    Return ModSByte(Convert.ToSByte(left), Convert.ToSByte(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Byte,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Byte * s_TCMAX + TypeCode.SByte,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int16

                    Return ModInt16(Convert.ToInt16(left), Convert.ToInt16(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int32,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int32 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int32

                    Return ModInt32(Convert.ToInt32(left), Convert.ToInt32(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int64,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int64 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int64

                    Return ModInt64(Convert.ToInt64(left), Convert.ToInt64(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.SByte * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Byte * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Decimal * s_TCMAX + TypeCode.SByte,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Byte,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int16,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int32,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int64,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Decimal

                    Return ModDecimal(left, right)

                Case TypeCode.SByte * s_TCMAX + TypeCode.Single,
                     TypeCode.Byte * s_TCMAX + TypeCode.Single,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Single,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Single,
                     TypeCode.Single * s_TCMAX + TypeCode.SByte,
                     TypeCode.Single * s_TCMAX + TypeCode.Byte,
                     TypeCode.Single * s_TCMAX + TypeCode.Int16,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Single * s_TCMAX + TypeCode.Int32,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Single * s_TCMAX + TypeCode.Int64,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Single * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Single * s_TCMAX + TypeCode.Single

                    Return ModSingle(Convert.ToSingle(left), Convert.ToSingle(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Double,
                     TypeCode.Byte * s_TCMAX + TypeCode.Double,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Double,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Double,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Double,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Double,
                     TypeCode.Single * s_TCMAX + TypeCode.Double,
                     TypeCode.Double * s_TCMAX + TypeCode.SByte,
                     TypeCode.Double * s_TCMAX + TypeCode.Byte,
                     TypeCode.Double * s_TCMAX + TypeCode.Int16,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Double * s_TCMAX + TypeCode.Int32,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Double * s_TCMAX + TypeCode.Int64,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Double * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Double * s_TCMAX + TypeCode.Single,
                     TypeCode.Double * s_TCMAX + TypeCode.Double

                    Return ModDouble(Convert.ToDouble(left), Convert.ToDouble(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.String,
                     TypeCode.Byte * s_TCMAX + TypeCode.String,
                     TypeCode.Int16 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.String,
                     TypeCode.Int32 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.String,
                     TypeCode.Int64 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.String,
                     TypeCode.Decimal * s_TCMAX + TypeCode.String,
                     TypeCode.Single * s_TCMAX + TypeCode.String,
                     TypeCode.Double * s_TCMAX + TypeCode.String

                    Return ModDouble(Convert.ToDouble(left), CDbl(Convert.ToString(right)))


                Case TypeCode.Byte * s_TCMAX + TypeCode.Empty
                    Return ModByte(Convert.ToByte(left), Nothing)

                Case TypeCode.Byte * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Boolean
                    Return ModInt16(Convert.ToInt16(left), ToVBBool(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.Byte
                    Return ModByte(Convert.ToByte(left), Convert.ToByte(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt16
                    Return ModUInt16(Convert.ToUInt16(left), Convert.ToUInt16(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt32
                    Return ModUInt32(Convert.ToUInt32(left), Convert.ToUInt32(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt64

                    Return ModUInt64(Convert.ToUInt64(left), Convert.ToUInt64(right))


                Case TypeCode.Int16 * s_TCMAX + TypeCode.Empty
                    Return ModInt16(Convert.ToInt16(left), Nothing)


                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Empty
                    Return ModUInt16(Convert.ToUInt16(left), Nothing)

                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Boolean
                    Return ModInt32(Convert.ToInt32(left), ToVBBool(right))


                Case TypeCode.Int32 * s_TCMAX + TypeCode.Empty
                    Return ModInt32(Convert.ToInt32(left), Nothing)

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Empty
                    Return ModUInt32(Convert.ToUInt32(left), Nothing)

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Boolean
                    Return ModInt64(Convert.ToInt64(left), ToVBBool(right))

                Case TypeCode.Int64 * s_TCMAX + TypeCode.Empty
                    Return ModInt64(Convert.ToInt64(left), Nothing)


                Case TypeCode.UInt64 * s_TCMAX + TypeCode.Empty
                    Return ModUInt64(Convert.ToUInt64(left), Nothing)

                Case TypeCode.UInt64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Boolean
                    Return ModDecimal(left, ToVBBool(right))


                Case TypeCode.Decimal * s_TCMAX + TypeCode.Empty
                    Return ModDecimal(left, 0D)


                Case TypeCode.Single * s_TCMAX + TypeCode.Empty
                    Return ModSingle(Convert.ToSingle(left), Nothing)

                Case TypeCode.Single * s_TCMAX + TypeCode.Boolean
                    Return ModSingle(Convert.ToSingle(left), ToVBBool(right))


                Case TypeCode.Double * s_TCMAX + TypeCode.Empty
                    Return ModDouble(Convert.ToDouble(left), Nothing)

                Case TypeCode.Double * s_TCMAX + TypeCode.Boolean
                    Return ModDouble(Convert.ToDouble(left), ToVBBool(right))


                Case TypeCode.String * s_TCMAX + TypeCode.Empty
                    Return ModDouble(CDbl(Convert.ToString(left)), Nothing)

                Case TypeCode.String * s_TCMAX + TypeCode.Boolean
                    Return ModDouble(CDbl(Convert.ToString(left)), ToVBBool(right))

                Case TypeCode.String * s_TCMAX + TypeCode.SByte,
                     TypeCode.String * s_TCMAX + TypeCode.Byte,
                     TypeCode.String * s_TCMAX + TypeCode.Int16,
                     TypeCode.String * s_TCMAX + TypeCode.UInt16,
                     TypeCode.String * s_TCMAX + TypeCode.Int32,
                     TypeCode.String * s_TCMAX + TypeCode.UInt32,
                     TypeCode.String * s_TCMAX + TypeCode.Int64,
                     TypeCode.String * s_TCMAX + TypeCode.UInt64,
                     TypeCode.String * s_TCMAX + TypeCode.Decimal,
                     TypeCode.String * s_TCMAX + TypeCode.Single,
                     TypeCode.String * s_TCMAX + TypeCode.Double

                    Return ModDouble(CDbl(Convert.ToString(left)), Convert.ToDouble(right))

                Case TypeCode.String * s_TCMAX + TypeCode.String
                    Return ModDouble(CDbl(Convert.ToString(left)), CDbl(Convert.ToString(right)))

                Case Else

            End Select

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return InvokeUserDefinedOperator(UserDefinedOperator.Modulus, left, right)
            End If

            Throw GetNoValidOperatorException(UserDefinedOperator.Modulus, left, right)
        End Function

        Private Shared Function ModSByte(ByVal left As SByte, ByVal right As SByte) As Object
            Return left Mod right
        End Function

        Private Shared Function ModByte(ByVal left As Byte, ByVal right As Byte) As Object
            Return left Mod right
        End Function

        Private Shared Function ModInt16(ByVal left As Int16, ByVal right As Int16) As Object
            Dim result As Integer = CInt(left) Mod CInt(right)

            If result < Int16.MinValue OrElse result > Int16.MaxValue Then
                Return result
            Else
                Return CShort(result)
            End If
        End Function

        Private Shared Function ModUInt16(ByVal left As UInt16, ByVal right As UInt16) As Object
            Return left Mod right
        End Function

        Private Shared Function ModInt32(ByVal left As Integer, ByVal right As Integer) As Object
            'Do operation with Int64 to avoid OverflowException with Int32.MinValue and -1
            Dim result As Long = CLng(left) Mod CLng(right)

            If result < Int32.MinValue OrElse result > Int32.MaxValue Then
                Return result
            Else
                Return CInt(result)
            End If
        End Function

        Private Shared Function ModUInt32(ByVal left As UInt32, ByVal right As UInt32) As Object
            Return left Mod right
        End Function

        Private Shared Function ModInt64(ByVal left As Int64, ByVal right As Int64) As Object

            If left = Int64.MinValue AndAlso right = -1 Then
                Return 0L
            Else
                Return left Mod right
            End If

        End Function

        Private Shared Function ModUInt64(ByVal left As UInt64, ByVal right As UInt64) As Object
            Return left Mod right
        End Function

        Private Shared Function ModDecimal(ByVal left As Object, ByVal right As Object) As Object
            Dim leftValue As Decimal = Convert.ToDecimal(left)
            Dim rightValue As Decimal = Convert.ToDecimal(right)

            Return leftValue Mod rightValue
        End Function

        Private Shared Function ModSingle(ByVal left As Single, ByVal right As Single) As Object
            Return left Mod right
        End Function

        Private Shared Function ModDouble(ByVal left As Double, ByVal right As Double) As Object
            Return left Mod right
        End Function

#End Region

#Region " Operator Integral Divide \ "

        Public Shared Function IntDivideObject(ByVal left As Object, ByVal right As Object) As Object

            Dim tc1 As TypeCode = GetTypeCode(left)
            Dim tc2 As TypeCode = GetTypeCode(right)

            Select Case tc1 * s_TCMAX + tc2

                Case TypeCode.Empty * s_TCMAX + TypeCode.Empty
                    Return IntDivideInt32(Nothing, Nothing)

                Case TypeCode.Empty * s_TCMAX + TypeCode.Boolean
                    Return IntDivideInt16(Nothing, ToVBBool(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.SByte
                    Return IntDivideSByte(Nothing, Convert.ToSByte(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Byte
                    Return IntDivideByte(Nothing, Convert.ToByte(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int16
                    Return IntDivideInt16(Nothing, Convert.ToInt16(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt16
                    Return IntDivideUInt16(Nothing, Convert.ToUInt16(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int32
                    Return IntDivideInt32(Nothing, Convert.ToInt32(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt32
                    Return IntDivideUInt32(Nothing, Convert.ToUInt32(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int64
                    Return IntDivideInt64(Nothing, Convert.ToInt64(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt64
                    Return IntDivideUInt64(Nothing, Convert.ToUInt64(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Empty * s_TCMAX + TypeCode.Single,
                     TypeCode.Empty * s_TCMAX + TypeCode.Double
                    Return IntDivideInt64(Nothing, Convert.ToInt64(right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.String
                    Return IntDivideInt64(Nothing, CLng(Convert.ToString(right)))


                Case TypeCode.Boolean * s_TCMAX + TypeCode.Empty
                    Return IntDivideInt16(ToVBBool(left), Nothing)

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Boolean
                    Return IntDivideInt16(ToVBBool(left), ToVBBool(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.SByte
                    Return IntDivideSByte(ToVBBool(left), Convert.ToSByte(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Byte,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int16
                    Return IntDivideInt16(ToVBBool(left), Convert.ToInt16(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int32
                    Return IntDivideInt32(ToVBBool(left), Convert.ToInt32(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Single,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Double

                    Return IntDivideInt64(ToVBBool(left), Convert.ToInt64(right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.String
                    Return IntDivideInt64(ToVBBool(left), CLng(Convert.ToString(right)))


                Case TypeCode.SByte * s_TCMAX + TypeCode.Empty
                    Return IntDivideSByte(Convert.ToSByte(left), Nothing)

                Case TypeCode.SByte * s_TCMAX + TypeCode.Boolean
                    Return IntDivideSByte(Convert.ToSByte(left), ToVBBool(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.SByte
                    Return IntDivideSByte(Convert.ToSByte(left), Convert.ToSByte(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Byte,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Byte * s_TCMAX + TypeCode.SByte,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int16

                    Return IntDivideInt16(Convert.ToInt16(left), Convert.ToInt16(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int32,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int32 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int32

                    Return IntDivideInt32(Convert.ToInt32(left), Convert.ToInt32(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int64,
                     TypeCode.SByte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.SByte * s_TCMAX + TypeCode.Decimal,
                     TypeCode.SByte * s_TCMAX + TypeCode.Single,
                     TypeCode.SByte * s_TCMAX + TypeCode.Double,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int64,
                     TypeCode.Byte * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Byte * s_TCMAX + TypeCode.Single,
                     TypeCode.Byte * s_TCMAX + TypeCode.Double,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Double,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Double,
                     TypeCode.Int64 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int64 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Single,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Double,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.SByte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int32,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Int64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Decimal,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Single,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Double,
                     TypeCode.Decimal * s_TCMAX + TypeCode.SByte,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Byte,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int16,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int32,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Int64,
                     TypeCode.Decimal * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Single,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Double,
                     TypeCode.Single * s_TCMAX + TypeCode.SByte,
                     TypeCode.Single * s_TCMAX + TypeCode.Byte,
                     TypeCode.Single * s_TCMAX + TypeCode.Int16,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Single * s_TCMAX + TypeCode.Int32,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Single * s_TCMAX + TypeCode.Int64,
                     TypeCode.Single * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Single * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Single * s_TCMAX + TypeCode.Single,
                     TypeCode.Single * s_TCMAX + TypeCode.Double,
                     TypeCode.Double * s_TCMAX + TypeCode.SByte,
                     TypeCode.Double * s_TCMAX + TypeCode.Byte,
                     TypeCode.Double * s_TCMAX + TypeCode.Int16,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Double * s_TCMAX + TypeCode.Int32,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Double * s_TCMAX + TypeCode.Int64,
                     TypeCode.Double * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Double * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Double * s_TCMAX + TypeCode.Single,
                     TypeCode.Double * s_TCMAX + TypeCode.Double

                    Return IntDivideInt64(Convert.ToInt64(left), Convert.ToInt64(right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.String,
                     TypeCode.Byte * s_TCMAX + TypeCode.String,
                     TypeCode.Int16 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.String,
                     TypeCode.Int32 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.String,
                     TypeCode.Int64 * s_TCMAX + TypeCode.String,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.String,
                     TypeCode.Decimal * s_TCMAX + TypeCode.String,
                     TypeCode.Single * s_TCMAX + TypeCode.String,
                     TypeCode.Double * s_TCMAX + TypeCode.String

                    Return IntDivideInt64(Convert.ToInt64(left), CLng(Convert.ToString(right)))


                Case TypeCode.Byte * s_TCMAX + TypeCode.Empty
                    Return IntDivideByte(Convert.ToByte(left), Nothing)

                Case TypeCode.Byte * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Boolean
                    Return IntDivideInt16(Convert.ToInt16(left), ToVBBool(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.Byte
                    Return IntDivideByte(Convert.ToByte(left), Convert.ToByte(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt16
                    Return IntDivideUInt16(Convert.ToUInt16(left), Convert.ToUInt16(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt32

                    Return IntDivideUInt32(Convert.ToUInt32(left), Convert.ToUInt32(right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt64

                    Return IntDivideUInt64(Convert.ToUInt64(left), Convert.ToUInt64(right))


                Case TypeCode.Int16 * s_TCMAX + TypeCode.Empty
                    Return IntDivideInt16(Convert.ToInt16(left), Nothing)


                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Empty
                    Return IntDivideUInt16(Convert.ToUInt16(left), Nothing)

                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Boolean
                    Return IntDivideInt32(Convert.ToInt32(left), ToVBBool(right))


                Case TypeCode.Int32 * s_TCMAX + TypeCode.Empty
                    Return IntDivideInt32(Convert.ToInt32(left), Nothing)


                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Empty
                    Return IntDivideUInt32(Convert.ToUInt32(left), Nothing)

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Single * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Double * s_TCMAX + TypeCode.Boolean

                    Return IntDivideInt64(Convert.ToInt64(left), ToVBBool(right))


                Case TypeCode.Int64 * s_TCMAX + TypeCode.Empty
                    Return IntDivideInt64(Convert.ToInt64(left), Nothing)


                Case TypeCode.UInt64 * s_TCMAX + TypeCode.Empty
                    Return IntDivideUInt64(Convert.ToUInt64(left), Nothing)


                Case TypeCode.Decimal * s_TCMAX + TypeCode.Empty,
                     TypeCode.Single * s_TCMAX + TypeCode.Empty,
                     TypeCode.Double * s_TCMAX + TypeCode.Empty
                    Return IntDivideInt64(Convert.ToInt64(left), Nothing)


                Case TypeCode.String * s_TCMAX + TypeCode.Empty
                    Return IntDivideInt64(CLng(Convert.ToString(left)), Nothing)

                Case TypeCode.String * s_TCMAX + TypeCode.Boolean
                    Return IntDivideInt64(CLng(Convert.ToString(left)), ToVBBool(right))

                Case TypeCode.String * s_TCMAX + TypeCode.SByte,
                     TypeCode.String * s_TCMAX + TypeCode.Byte,
                     TypeCode.String * s_TCMAX + TypeCode.Int16,
                     TypeCode.String * s_TCMAX + TypeCode.UInt16,
                     TypeCode.String * s_TCMAX + TypeCode.Int32,
                     TypeCode.String * s_TCMAX + TypeCode.UInt32,
                     TypeCode.String * s_TCMAX + TypeCode.Int64,
                     TypeCode.String * s_TCMAX + TypeCode.UInt64,
                     TypeCode.String * s_TCMAX + TypeCode.Decimal,
                     TypeCode.String * s_TCMAX + TypeCode.Single,
                     TypeCode.String * s_TCMAX + TypeCode.Double

                    Return IntDivideInt64(CLng(Convert.ToString(left)), Convert.ToInt64(right))

                Case TypeCode.String * s_TCMAX + TypeCode.String
                    Return IntDivideInt64(CLng(Convert.ToString(left)), CLng(Convert.ToString(right)))

            End Select

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return InvokeUserDefinedOperator(UserDefinedOperator.IntegralDivide, left, right)
            End If

            Throw GetNoValidOperatorException(UserDefinedOperator.IntegralDivide, left, right)
        End Function

        Private Shared Function IntDivideSByte(ByVal left As SByte, ByVal right As SByte) As Object
            If left = SByte.MinValue AndAlso right = -1 Then
                Return -CShort(SByte.MinValue)
            End If

            Return left \ right
        End Function

        Private Shared Function IntDivideByte(ByVal left As Byte, ByVal right As Byte) As Object
            Return left \ right
        End Function

        Private Shared Function IntDivideInt16(ByVal left As Int16, ByVal right As Int16) As Object
            If left = Short.MinValue AndAlso right = -1 Then
                Return -CInt(Short.MinValue)
            End If

            Return left \ right
        End Function

        Private Shared Function IntDivideUInt16(ByVal left As UInt16, ByVal right As UInt16) As Object
            Return left \ right
        End Function

        Private Shared Function IntDivideInt32(ByVal left As Int32, ByVal right As Int32) As Object
            If left = Integer.MinValue AndAlso right = -1 Then
                Return -CLng(Integer.MinValue)
            End If

            Return left \ right
        End Function

        Private Shared Function IntDivideUInt32(ByVal left As UInt32, ByVal right As UInt32) As Object
            Return left \ right
        End Function

        Private Shared Function IntDivideInt64(ByVal left As Int64, ByVal right As Int64) As Object
            Return left \ right
        End Function

        Private Shared Function IntDivideUInt64(ByVal left As UInt64, ByVal right As UInt64) As Object
            Return left \ right
        End Function

#End Region

#Region " Operator Shift Left << "

        Public Shared Function LeftShiftObject(ByVal operand As Object, ByVal amount As Object) As Object

            Dim tc1 As TypeCode = GetTypeCode(operand)
            Dim tc2 As TypeCode = GetTypeCode(amount)


            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return InvokeUserDefinedOperator(UserDefinedOperator.ShiftLeft, operand, amount)
            End If

            Select Case tc1
                Case TypeCode.Empty
                    Return Nothing << CInt(amount)
                Case TypeCode.Boolean
                    Return CShort(Convert.ToBoolean(operand)) << CInt(amount)
                Case TypeCode.SByte
                    Return Convert.ToSByte(operand) << CInt(amount)
                Case TypeCode.Byte
                    Return Convert.ToByte(operand) << CInt(amount)
                Case TypeCode.Int16
                    Return Convert.ToInt16(operand) << CInt(amount)
                Case TypeCode.UInt16
                    Return Convert.ToUInt16(operand) << CInt(amount)
                Case TypeCode.Int32
                    Return Convert.ToInt32(operand) << CInt(amount)
                Case TypeCode.UInt32
                    Return Convert.ToUInt32(operand) << CInt(amount)
                Case TypeCode.Int64,
                     TypeCode.Single,
                     TypeCode.Double,
                     TypeCode.Decimal
                    Return Convert.ToInt64(operand) << CInt(amount)
                Case TypeCode.UInt64
                    Return Convert.ToUInt64(operand) << CInt(amount)
                Case TypeCode.String
                    Return CLng(Convert.ToString(operand)) << CInt(amount)
            End Select

            Throw GetNoValidOperatorException(UserDefinedOperator.ShiftLeft, operand)
        End Function

#End Region

#Region " Operator Shift Right >> "

        Public Shared Function RightShiftObject(ByVal operand As Object, ByVal amount As Object) As Object

            Dim tc1 As TypeCode = GetTypeCode(operand)
            Dim tc2 As TypeCode = GetTypeCode(amount)

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return InvokeUserDefinedOperator(UserDefinedOperator.ShiftRight, operand, amount)
            End If

            Select Case tc1
                Case TypeCode.Empty
                    Return Nothing >> CInt(amount)
                Case TypeCode.Boolean
                    Return CShort(Convert.ToBoolean(operand)) >> CInt(amount)
                Case TypeCode.SByte
                    Return Convert.ToSByte(operand) >> CInt(amount)
                Case TypeCode.Byte
                    Return Convert.ToByte(operand) >> CInt(amount)
                Case TypeCode.Int16
                    Return Convert.ToInt16(operand) >> CInt(amount)
                Case TypeCode.UInt16
                    Return Convert.ToUInt16(operand) >> CInt(amount)
                Case TypeCode.Int32
                    Return Convert.ToInt32(operand) >> CInt(amount)
                Case TypeCode.UInt32
                    Return Convert.ToUInt32(operand) >> CInt(amount)
                Case TypeCode.Int64,
                     TypeCode.Single,
                     TypeCode.Double,
                     TypeCode.Decimal
                    Return Convert.ToInt64(operand) >> CInt(amount)
                Case TypeCode.UInt64
                    Return Convert.ToUInt64(operand) >> CInt(amount)
                Case TypeCode.String
                    Return CLng(Convert.ToString(operand)) >> CInt(amount)
            End Select

            Throw GetNoValidOperatorException(UserDefinedOperator.ShiftRight, operand)
        End Function

#End Region


#Region " Operator Concatenate & "

        Public Shared Function ConcatenateObject(ByVal left As Object, ByVal right As Object) As Object

            Dim tc1 As TypeCode = GetTypeCode(left)
            Dim tc2 As TypeCode = GetTypeCode(right)

            'Special cases for Char()
            If (tc1 = TypeCode.Object) AndAlso (TypeOf left Is Char()) Then
                tc1 = TypeCode.String
            End If

            If (tc2 = TypeCode.Object) AndAlso (TypeOf right Is Char()) Then
                tc2 = TypeCode.String
            End If

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return InvokeUserDefinedOperator(UserDefinedOperator.Concatenate, left, right)
            End If

            Return CStr(left) & CStr(right)
        End Function

#End Region

    End Class

End Namespace

