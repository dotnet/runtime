' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Option Strict On

Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Globalization
Imports System.Reflection
Imports System.Runtime.CompilerServices

Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils
Imports Microsoft.VisualBasic.CompilerServices.Symbols
Imports Microsoft.VisualBasic.CompilerServices.Utils


Namespace Microsoft.VisualBasic.CompilerServices

    ' Implements Object operators
    <EditorBrowsable(EditorBrowsableState.Never)>
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

        Public Shared Function CompareObjectEqual(ByVal Left As Object, ByVal Right As Object, ByVal TextCompare As Boolean) As Object
            Dim comparison As CompareClass = CompareObject2(Left, Right, TextCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return False
                Case CompareClass.UserDefined
                    Return InvokeUserDefinedOperator(UserDefinedOperator.Equal, Left, Right)
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.Equal, Left, Right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison = 0
            End Select
        End Function

        Public Shared Function ConditionalCompareObjectEqual(ByVal Left As Object, ByVal Right As Object, ByVal TextCompare As Boolean) As Boolean
            Dim comparison As CompareClass = CompareObject2(Left, Right, TextCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return False
                Case CompareClass.UserDefined
                    Return CBool(InvokeUserDefinedOperator(UserDefinedOperator.Equal, Left, Right))
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.Equal, Left, Right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison = 0
            End Select
        End Function

        Public Shared Function CompareObjectNotEqual(ByVal Left As Object, ByVal Right As Object, ByVal TextCompare As Boolean) As Object
            Dim comparison As CompareClass = CompareObject2(Left, Right, TextCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return True
                Case CompareClass.UserDefined
                    Return InvokeUserDefinedOperator(UserDefinedOperator.NotEqual, Left, Right)
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.NotEqual, Left, Right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison <> 0
            End Select
        End Function

        Public Shared Function ConditionalCompareObjectNotEqual(ByVal Left As Object, ByVal Right As Object, ByVal TextCompare As Boolean) As Boolean
            Dim comparison As CompareClass = CompareObject2(Left, Right, TextCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return True
                Case CompareClass.UserDefined
                    Return CBool(InvokeUserDefinedOperator(UserDefinedOperator.NotEqual, Left, Right))
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.NotEqual, Left, Right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison <> 0
            End Select
        End Function

        Public Shared Function CompareObjectLess(ByVal Left As Object, ByVal Right As Object, ByVal TextCompare As Boolean) As Object
            Dim comparison As CompareClass = CompareObject2(Left, Right, TextCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return False
                Case CompareClass.UserDefined
                    Return InvokeUserDefinedOperator(UserDefinedOperator.Less, Left, Right)
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.Less, Left, Right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison < 0
            End Select
        End Function

        Public Shared Function ConditionalCompareObjectLess(ByVal Left As Object, ByVal Right As Object, ByVal TextCompare As Boolean) As Boolean
            Dim comparison As CompareClass = CompareObject2(Left, Right, TextCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return False
                Case CompareClass.UserDefined
                    Return CBool(InvokeUserDefinedOperator(UserDefinedOperator.Less, Left, Right))
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.Less, Left, Right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison < 0
            End Select
        End Function

        Public Shared Function CompareObjectLessEqual(ByVal Left As Object, ByVal Right As Object, ByVal TextCompare As Boolean) As Object
            Dim comparison As CompareClass = CompareObject2(Left, Right, TextCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return False
                Case CompareClass.UserDefined
                    Return InvokeUserDefinedOperator(UserDefinedOperator.LessEqual, Left, Right)
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.LessEqual, Left, Right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison <= 0
            End Select
        End Function

        Public Shared Function ConditionalCompareObjectLessEqual(ByVal Left As Object, ByVal Right As Object, ByVal TextCompare As Boolean) As Boolean
            Dim comparison As CompareClass = CompareObject2(Left, Right, TextCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return False
                Case CompareClass.UserDefined
                    Return CBool(InvokeUserDefinedOperator(UserDefinedOperator.LessEqual, Left, Right))
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.LessEqual, Left, Right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison <= 0
            End Select
        End Function

        Public Shared Function CompareObjectGreaterEqual(ByVal Left As Object, ByVal Right As Object, ByVal TextCompare As Boolean) As Object
            Dim comparison As CompareClass = CompareObject2(Left, Right, TextCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return False
                Case CompareClass.UserDefined
                    Return InvokeUserDefinedOperator(UserDefinedOperator.GreaterEqual, Left, Right)
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.GreaterEqual, Left, Right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison >= 0
            End Select
        End Function

        Public Shared Function ConditionalCompareObjectGreaterEqual(ByVal Left As Object, ByVal Right As Object, ByVal TextCompare As Boolean) As Boolean
            Dim comparison As CompareClass = CompareObject2(Left, Right, TextCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return False
                Case CompareClass.UserDefined
                    Return CBool(InvokeUserDefinedOperator(UserDefinedOperator.GreaterEqual, Left, Right))
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.GreaterEqual, Left, Right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison >= 0
            End Select
        End Function

        Public Shared Function CompareObjectGreater(ByVal Left As Object, ByVal Right As Object, ByVal TextCompare As Boolean) As Object
            Dim comparison As CompareClass = CompareObject2(Left, Right, TextCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return False
                Case CompareClass.UserDefined
                    Return InvokeUserDefinedOperator(UserDefinedOperator.Greater, Left, Right)
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.Greater, Left, Right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison > 0
            End Select
        End Function

        Public Shared Function ConditionalCompareObjectGreater(ByVal Left As Object, ByVal Right As Object, ByVal TextCompare As Boolean) As Boolean
            Dim comparison As CompareClass = CompareObject2(Left, Right, TextCompare)

            Select Case comparison
                Case CompareClass.Unordered
                    Return False
                Case CompareClass.UserDefined
                    Return CBool(InvokeUserDefinedOperator(UserDefinedOperator.Greater, Left, Right))
                Case CompareClass.Undefined
                    Throw GetNoValidOperatorException(UserDefinedOperator.Greater, Left, Right)
                Case Else
                    Debug.Assert(comparison = CompareClass.Less OrElse
                                 comparison = CompareClass.Equal OrElse
                                 comparison = CompareClass.Greater)
                    Return comparison > 0
            End Select
        End Function

        Friend Shared Function CompareObject(ByVal left As Object, ByVal right As Object, ByVal textCompare As Boolean) As Integer
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
        Public Shared Function CompareString(ByVal Left As String, ByVal Right As String, ByVal TextCompare As Boolean) As Integer
            If Left Is Right Then
                Return CompareClass.Equal
            End If

            If Left Is Nothing Then
                If Right.Length() = 0 Then
                    Return CompareClass.Equal
                End If

                Return CompareClass.Less
            End If

            If Right Is Nothing Then
                If Left.Length() = 0 Then
                    Return CompareClass.Equal
                End If

                Return CompareClass.Greater
            End If

            Dim result As Integer

            If TextCompare Then
                result = GetCultureInfo().CompareInfo.Compare(Left, Right, OptionCompareTextFlags)
            Else
                result = System.String.CompareOrdinal(Left, Right)
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

        Public Shared Function PlusObject(ByVal Operand As Object) As Object

            Dim typ As TypeCode = GetTypeCode(Operand)
            Select Case typ

                Case TypeCode.Empty
                    Return Boxed_ZeroInteger

                Case TypeCode.Boolean
                    Return CShort(Convert.ToBoolean(Operand))

                Case TypeCode.SByte
                    Return Convert.ToSByte(Operand)

                Case TypeCode.Byte
                    Return Convert.ToByte(Operand)

                Case TypeCode.Int16
                    Return Convert.ToInt16(Operand)

                Case TypeCode.UInt16
                    Return Convert.ToUInt16(Operand)

                Case TypeCode.Int32
                    Return Convert.ToInt32(Operand)

                Case TypeCode.UInt32
                    Return Convert.ToUInt32(Operand)

                Case TypeCode.Int64
                    Return Convert.ToInt64(Operand)

                Case TypeCode.UInt64
                    Return Convert.ToUInt64(Operand)

                Case TypeCode.Decimal,
                     TypeCode.Single,
                     TypeCode.Double
                    Return Operand

                Case TypeCode.DateTime,
                     TypeCode.Char
                ' Fall through to error

                Case TypeCode.String
                    Return CDbl(Convert.ToString(Operand))

                Case TypeCode.Object
                    Return InvokeUserDefinedOperator(UserDefinedOperator.UnaryPlus, Operand)

                Case Else
                    ' Fall through to error
            End Select

            Throw GetNoValidOperatorException(UserDefinedOperator.UnaryPlus, Operand)
        End Function

#End Region

#Region " Operator Negate - "

        Public Shared Function NegateObject(ByVal Operand As Object) As Object

            Dim tc As TypeCode = GetTypeCode(Operand)
            Select Case tc

                Case TypeCode.Empty
                    Return Boxed_ZeroInteger

                Case TypeCode.Boolean
                    If TypeOf Operand Is Boolean Then
                        Return NegateBoolean(DirectCast(Operand, Boolean))
                    Else
                        Return NegateBoolean(Convert.ToBoolean(Operand))
                    End If

                Case TypeCode.SByte
                    If TypeOf Operand Is SByte Then
                        Return NegateSByte(DirectCast(Operand, SByte))
                    Else
                        Return NegateSByte(Convert.ToSByte(Operand))
                    End If

                Case TypeCode.Byte
                    If TypeOf Operand Is Byte Then
                        Return NegateByte(DirectCast(Operand, Byte))
                    Else
                        Return NegateByte(Convert.ToByte(Operand))
                    End If

                Case TypeCode.Int16
                    If TypeOf Operand Is Int16 Then
                        Return NegateInt16(DirectCast(Operand, Int16))
                    Else
                        Return NegateInt16(Convert.ToInt16(Operand))
                    End If

                Case TypeCode.UInt16
                    If TypeOf Operand Is UInt16 Then
                        Return NegateUInt16(DirectCast(Operand, UInt16))
                    Else
                        Return NegateUInt16(Convert.ToUInt16(Operand))
                    End If

                Case TypeCode.Int32
                    If TypeOf Operand Is Int32 Then
                        Return NegateInt32(DirectCast(Operand, Int32))
                    Else
                        Return NegateInt32(Convert.ToInt32(Operand))
                    End If

                Case TypeCode.UInt32
                    If TypeOf Operand Is UInt32 Then
                        Return NegateUInt32(DirectCast(Operand, UInt32))
                    Else
                        Return NegateUInt32(Convert.ToUInt32(Operand))
                    End If

                Case TypeCode.Int64
                    If TypeOf Operand Is Int64 Then
                        Return NegateInt64(DirectCast(Operand, Int64))
                    Else
                        Return NegateInt64(Convert.ToInt64(Operand))
                    End If

                Case TypeCode.UInt64
                    If TypeOf Operand Is UInt64 Then
                        Return NegateUInt64(DirectCast(Operand, UInt64))
                    Else
                        Return NegateUInt64(Convert.ToUInt64(Operand))
                    End If

                Case TypeCode.Decimal
                    If TypeOf Operand Is Decimal Then
                        Return NegateDecimal(DirectCast(Operand, Decimal))
                    Else
                        Return NegateDecimal(Convert.ToDecimal(Operand))
                    End If

                Case TypeCode.Single
                    If TypeOf Operand Is Single Then
                        Return NegateSingle(DirectCast(Operand, Single))
                    Else
                        Return NegateSingle(Convert.ToSingle(Operand))
                    End If

                Case TypeCode.Double
                    If TypeOf Operand Is Double Then
                        Return NegateDouble(DirectCast(Operand, Double))
                    Else
                        Return NegateDouble(Convert.ToDouble(Operand))
                    End If

                Case TypeCode.DateTime,
                     TypeCode.Char
                'Fall through to error.

                Case TypeCode.String
                    Dim stringOperand As String = TryCast(Operand, String)

                    If stringOperand IsNot Nothing Then
                        Return NegateString(stringOperand)
                    Else
                        Return NegateString(Convert.ToString(Operand))
                    End If

                Case TypeCode.Object
                    Return InvokeUserDefinedOperator(UserDefinedOperator.Negate, Operand)

                Case Else
                    'Fall through to error.
            End Select

            Throw GetNoValidOperatorException(UserDefinedOperator.Negate, Operand)
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
            Return -operand
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

        Public Shared Function NotObject(ByVal Operand As Object) As Object

            Dim tc As TypeCode = GetTypeCode(Operand)

            Select Case tc

                Case TypeCode.Empty
                    Return Not 0I

                Case TypeCode.Boolean
                    Return NotBoolean(Convert.ToBoolean(Operand))

                Case TypeCode.SByte
                    Return NotSByte(Convert.ToSByte(Operand), Operand.GetType())

                Case TypeCode.Byte
                    Return NotByte(Convert.ToByte(Operand), Operand.GetType())

                Case TypeCode.Int16
                    Return NotInt16(Convert.ToInt16(Operand), Operand.GetType())

                Case TypeCode.UInt16
                    Return NotUInt16(Convert.ToUInt16(Operand), Operand.GetType())

                Case TypeCode.Int32
                    Return NotInt32(Convert.ToInt32(Operand), Operand.GetType())

                Case TypeCode.UInt32
                    Return NotUInt32(Convert.ToUInt32(Operand), Operand.GetType())

                Case TypeCode.Int64
                    Return NotInt64(Convert.ToInt64(Operand), Operand.GetType())

                Case TypeCode.UInt64
                    Return NotUInt64(Convert.ToUInt64(Operand), Operand.GetType())

                Case TypeCode.Decimal,
                     TypeCode.Single,
                     TypeCode.Double
                    Return NotInt64(Convert.ToInt64(Operand))

                Case TypeCode.DateTime,
                     TypeCode.Char
                'Fall through to error.

                Case TypeCode.String
                    Return NotInt64(CLng(Convert.ToString(Operand)))

                Case TypeCode.Object
                    Return InvokeUserDefinedOperator(UserDefinedOperator.Not, Operand)

                Case Else
                    'Fall through to error.
            End Select

            Throw GetNoValidOperatorException(UserDefinedOperator.Not, Operand)
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

        Public Shared Function AndObject(ByVal Left As Object, ByVal Right As Object) As Object

            Dim tc1 As TypeCode = GetTypeCode(Left)
            Dim tc2 As TypeCode = GetTypeCode(Right)


            Select Case tc1 * s_TCMAX + tc2

                Case TypeCode.Empty * s_TCMAX + TypeCode.Empty
                    Return Boxed_ZeroInteger

                Case TypeCode.Empty * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Empty
                    Return False

                Case TypeCode.Empty * s_TCMAX + TypeCode.SByte,
                     TypeCode.SByte * s_TCMAX + TypeCode.Empty
                    Return AndSByte(CSByte(0), CSByte(0), GetEnumResult(Left, Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Byte,
                     TypeCode.Byte * s_TCMAX + TypeCode.Empty
                    Return AndByte(CByte(0), CByte(0), GetEnumResult(Left, Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Empty
                    Return AndInt16(0S, 0S, GetEnumResult(Left, Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Empty
                    Return AndUInt16(0US, 0US, GetEnumResult(Left, Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int32,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Empty
                    Return AndInt32(0I, 0I, GetEnumResult(Left, Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Empty
                    Return AndUInt32(0UI, 0UI, GetEnumResult(Left, Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int64,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Empty
                    Return AndInt64(0L, 0L, GetEnumResult(Left, Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Empty
                    Return AndUInt64(0UL, 0UL, GetEnumResult(Left, Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Empty * s_TCMAX + TypeCode.Single,
                     TypeCode.Empty * s_TCMAX + TypeCode.Double
                    Return AndInt64(Nothing, Convert.ToInt64(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.String
                    Return AndInt64(Nothing, CLng(Convert.ToString(Right)))


                Case TypeCode.Boolean * s_TCMAX + TypeCode.Boolean
                    Return AndBoolean(Convert.ToBoolean(Left), Convert.ToBoolean(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.SByte
                    Return AndSByte(ToVBBool(Left), Convert.ToSByte(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Byte,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int16
                    Return AndInt16(ToVBBool(Left), Convert.ToInt16(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int32
                    Return AndInt32(ToVBBool(Left), Convert.ToInt32(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Single,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Double

                    Return AndInt64(ToVBBool(Left), Convert.ToInt64(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.String
                    Return AndBoolean(Convert.ToBoolean(Left), CBool(Convert.ToString(Right)))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Boolean
                    Return AndSByte(Convert.ToSByte(Left), ToVBBool(Right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.SByte
                    Return AndSByte(Convert.ToSByte(Left), Convert.ToSByte(Right), GetEnumResult(Left, Right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Byte,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Byte * s_TCMAX + TypeCode.SByte,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Byte

                    Return AndInt16(Convert.ToInt16(Left), Convert.ToInt16(Right))

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

                    Return AndInt32(Convert.ToInt32(Left), Convert.ToInt32(Right))

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

                    Return AndInt64(Convert.ToInt64(Left), Convert.ToInt64(Right))

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

                    Return AndInt64(Convert.ToInt64(Left), CLng(Convert.ToString(Right)))


                Case TypeCode.Byte * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Boolean
                    Return AndInt16(Convert.ToInt16(Left), ToVBBool(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.Byte
                    Return AndByte(Convert.ToByte(Left), Convert.ToByte(Right), GetEnumResult(Left, Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Byte
                    Return AndUInt16(Convert.ToUInt16(Left), Convert.ToUInt16(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt16

                    Return AndUInt32(Convert.ToUInt32(Left), Convert.ToUInt32(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt32

                    Return AndUInt64(Convert.ToUInt64(Left), Convert.ToUInt64(Right))


                Case TypeCode.Int16 * s_TCMAX + TypeCode.Int16
                    Return AndInt16(Convert.ToInt16(Left), Convert.ToInt16(Right), GetEnumResult(Left, Right))


                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Boolean
                    Return AndInt32(Convert.ToInt32(Left), ToVBBool(Right))

                Case TypeCode.UInt16 * s_TCMAX + TypeCode.UInt16
                    Return AndUInt16(Convert.ToUInt16(Left), Convert.ToUInt16(Right), GetEnumResult(Left, Right))


                Case TypeCode.Int32 * s_TCMAX + TypeCode.Int32
                    Return AndInt32(Convert.ToInt32(Left), Convert.ToInt32(Right), GetEnumResult(Left, Right))


                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Single * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Double * s_TCMAX + TypeCode.Boolean
                    Return AndInt64(Convert.ToInt64(Left), ToVBBool(Right))

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.UInt32
                    Return AndUInt32(Convert.ToUInt32(Left), Convert.ToUInt32(Right), GetEnumResult(Left, Right))


                Case TypeCode.Int64 * s_TCMAX + TypeCode.Int64
                    Return AndInt64(Convert.ToInt64(Left), Convert.ToInt64(Right), GetEnumResult(Left, Right))


                Case TypeCode.UInt64 * s_TCMAX + TypeCode.UInt64
                    Return AndUInt64(Convert.ToUInt64(Left), Convert.ToUInt64(Right), GetEnumResult(Left, Right))


                Case TypeCode.Decimal * s_TCMAX + TypeCode.Empty,
                     TypeCode.Single * s_TCMAX + TypeCode.Empty,
                     TypeCode.Double * s_TCMAX + TypeCode.Empty
                    Return AndInt64(Convert.ToInt64(Left), Nothing)


                Case TypeCode.String * s_TCMAX + TypeCode.Empty
                    Return AndInt64(CLng(Convert.ToString(Left)), Nothing)

                Case TypeCode.String * s_TCMAX + TypeCode.Boolean
                    Return AndBoolean(CBool(Convert.ToString(Left)), Convert.ToBoolean(Right))

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

                    Return AndInt64(CLng(Convert.ToString(Left)), Convert.ToInt64(Right))

                Case TypeCode.String * s_TCMAX + TypeCode.String
                    Return AndInt64(CLng(Convert.ToString(Left)), CLng(Convert.ToString(Right)))

            End Select

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return InvokeUserDefinedOperator(UserDefinedOperator.And, Left, Right)
            End If

            Throw GetNoValidOperatorException(UserDefinedOperator.And, Left, Right)
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

        Public Shared Function OrObject(ByVal Left As Object, ByVal Right As Object) As Object

            Dim tc1 As TypeCode = GetTypeCode(Left)
            Dim tc2 As TypeCode = GetTypeCode(Right)

            Select Case tc1 * s_TCMAX + tc2

                Case TypeCode.Empty * s_TCMAX + TypeCode.Empty
                    Return Boxed_ZeroInteger

                Case TypeCode.Empty * s_TCMAX + TypeCode.Boolean
                    Return OrBoolean(Nothing, Convert.ToBoolean(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.SByte,
                     TypeCode.Empty * s_TCMAX + TypeCode.Byte,
                     TypeCode.Empty * s_TCMAX + TypeCode.Int16,
                     TypeCode.Empty * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Empty * s_TCMAX + TypeCode.Int32,
                     TypeCode.Empty * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Empty * s_TCMAX + TypeCode.Int64,
                     TypeCode.Empty * s_TCMAX + TypeCode.UInt64

                    Return Right

                Case TypeCode.Empty * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Empty * s_TCMAX + TypeCode.Single,
                     TypeCode.Empty * s_TCMAX + TypeCode.Double
                    Return OrInt64(Nothing, Convert.ToInt64(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.String
                    Return OrInt64(Nothing, CLng(Convert.ToString(Right)))


                Case TypeCode.Boolean * s_TCMAX + TypeCode.Empty
                    Return OrBoolean(Convert.ToBoolean(Left), Nothing)

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Boolean
                    Return OrBoolean(Convert.ToBoolean(Left), Convert.ToBoolean(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.SByte
                    Return OrSByte(ToVBBool(Left), Convert.ToSByte(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Byte,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int16
                    Return OrInt16(ToVBBool(Left), Convert.ToInt16(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int32
                    Return OrInt32(ToVBBool(Left), Convert.ToInt32(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Single,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Double

                    Return OrInt64(ToVBBool(Left), Convert.ToInt64(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.String
                    Return OrBoolean(Convert.ToBoolean(Left), CBool(Convert.ToString(Right)))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Empty,
                     TypeCode.Byte * s_TCMAX + TypeCode.Empty,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Empty,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Empty,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Empty,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Empty,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Empty,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Empty

                    Return Left

                Case TypeCode.SByte * s_TCMAX + TypeCode.Boolean
                    Return OrSByte(Convert.ToSByte(Left), ToVBBool(Right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.SByte
                    Return OrSByte(Convert.ToSByte(Left), Convert.ToSByte(Right), GetEnumResult(Left, Right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Byte,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Byte * s_TCMAX + TypeCode.SByte,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Byte

                    Return OrInt16(Convert.ToInt16(Left), Convert.ToInt16(Right))

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

                    Return OrInt32(Convert.ToInt32(Left), Convert.ToInt32(Right))

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

                    Return OrInt64(Convert.ToInt64(Left), Convert.ToInt64(Right))

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

                    Return OrInt64(Convert.ToInt64(Left), CLng(Convert.ToString(Right)))


                Case TypeCode.Byte * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Boolean
                    Return OrInt16(Convert.ToInt16(Left), ToVBBool(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.Byte
                    Return OrByte(Convert.ToByte(Left), Convert.ToByte(Right), GetEnumResult(Left, Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Byte
                    Return OrUInt16(Convert.ToUInt16(Left), Convert.ToUInt16(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt16

                    Return OrUInt32(Convert.ToUInt32(Left), Convert.ToUInt32(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt32

                    Return OrUInt64(Convert.ToUInt64(Left), Convert.ToUInt64(Right))


                Case TypeCode.Int16 * s_TCMAX + TypeCode.Int16
                    Return OrInt16(Convert.ToInt16(Left), Convert.ToInt16(Right), GetEnumResult(Left, Right))


                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Boolean
                    Return OrInt32(Convert.ToInt32(Left), ToVBBool(Right))

                Case TypeCode.UInt16 * s_TCMAX + TypeCode.UInt16
                    Return OrUInt16(Convert.ToUInt16(Left), Convert.ToUInt16(Right), GetEnumResult(Left, Right))


                Case TypeCode.Int32 * s_TCMAX + TypeCode.Int32
                    Return OrInt32(Convert.ToInt32(Left), Convert.ToInt32(Right), GetEnumResult(Left, Right))


                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Single * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Double * s_TCMAX + TypeCode.Boolean

                    Return OrInt64(Convert.ToInt64(Left), ToVBBool(Right))

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.UInt32
                    Return OrUInt32(Convert.ToUInt32(Left), Convert.ToUInt32(Right), GetEnumResult(Left, Right))


                Case TypeCode.Int64 * s_TCMAX + TypeCode.Int64
                    Return OrInt64(Convert.ToInt64(Left), Convert.ToInt64(Right), GetEnumResult(Left, Right))


                Case TypeCode.UInt64 * s_TCMAX + TypeCode.UInt64
                    Return OrUInt64(Convert.ToUInt64(Left), Convert.ToUInt64(Right), GetEnumResult(Left, Right))


                Case TypeCode.Decimal * s_TCMAX + TypeCode.Empty,
                     TypeCode.Single * s_TCMAX + TypeCode.Empty,
                     TypeCode.Double * s_TCMAX + TypeCode.Empty
                    Return OrInt64(Convert.ToInt64(Left), Nothing)


                Case TypeCode.String * s_TCMAX + TypeCode.Empty
                    Return OrInt64(CLng(Convert.ToString(Left)), Nothing)

                Case TypeCode.String * s_TCMAX + TypeCode.Boolean
                    Return OrBoolean(CBool(Convert.ToString(Left)), Convert.ToBoolean(Right))

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

                    Return OrInt64(CLng(Convert.ToString(Left)), Convert.ToInt64(Right))

                Case TypeCode.String * s_TCMAX + TypeCode.String
                    Return OrInt64(CLng(Convert.ToString(Left)), CLng(Convert.ToString(Right)))

            End Select

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return InvokeUserDefinedOperator(UserDefinedOperator.Or, Left, Right)
            End If

            Throw GetNoValidOperatorException(UserDefinedOperator.Or, Left, Right)
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

        Public Shared Function XorObject(ByVal Left As Object, ByVal Right As Object) As Object

            Dim tc1 As TypeCode = GetTypeCode(Left)
            Dim tc2 As TypeCode = GetTypeCode(Right)

            Select Case tc1 * s_TCMAX + tc2

                Case TypeCode.Empty * s_TCMAX + TypeCode.Empty
                    Return Boxed_ZeroInteger

                Case TypeCode.Empty * s_TCMAX + TypeCode.Boolean
                    Return XorBoolean(Nothing, Convert.ToBoolean(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.SByte
                    Return XorSByte(Nothing, Convert.ToSByte(Right), GetEnumResult(Left, Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Byte
                    Return XorByte(Nothing, Convert.ToByte(Right), GetEnumResult(Left, Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int16
                    Return XorInt16(Nothing, Convert.ToInt16(Right), GetEnumResult(Left, Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt16
                    Return XorUInt16(Nothing, Convert.ToUInt16(Right), GetEnumResult(Left, Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int32
                    Return XorInt32(Nothing, Convert.ToInt32(Right), GetEnumResult(Left, Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt32
                    Return XorUInt32(Nothing, Convert.ToUInt32(Right), GetEnumResult(Left, Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int64
                    Return XorInt64(Nothing, Convert.ToInt64(Right), GetEnumResult(Left, Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt64
                    Return XorUInt64(Nothing, Convert.ToUInt64(Right), GetEnumResult(Left, Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Empty * s_TCMAX + TypeCode.Single,
                     TypeCode.Empty * s_TCMAX + TypeCode.Double
                    Return XorInt64(Nothing, Convert.ToInt64(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.String
                    Return XorInt64(Nothing, CLng(Convert.ToString(Right)))


                Case TypeCode.Boolean * s_TCMAX + TypeCode.Empty
                    Return XorBoolean(Convert.ToBoolean(Left), Nothing)

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Boolean
                    Return XorBoolean(Convert.ToBoolean(Left), Convert.ToBoolean(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.SByte
                    Return XorSByte(ToVBBool(Left), Convert.ToSByte(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Byte,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int16
                    Return XorInt16(ToVBBool(Left), Convert.ToInt16(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int32
                    Return XorInt32(ToVBBool(Left), Convert.ToInt32(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Single,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Double

                    Return XorInt64(ToVBBool(Left), Convert.ToInt64(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.String
                    Return XorBoolean(Convert.ToBoolean(Left), CBool(Convert.ToString(Right)))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Empty
                    Return XorSByte(Convert.ToSByte(Left), Nothing, GetEnumResult(Left, Right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Boolean
                    Return XorSByte(Convert.ToSByte(Left), ToVBBool(Right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.SByte
                    Return XorSByte(Convert.ToSByte(Left), Convert.ToSByte(Right), GetEnumResult(Left, Right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Byte,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Byte * s_TCMAX + TypeCode.SByte,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Byte

                    Return XorInt16(Convert.ToInt16(Left), Convert.ToInt16(Right))

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

                    Return XorInt32(Convert.ToInt32(Left), Convert.ToInt32(Right))

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

                    Return XorInt64(Convert.ToInt64(Left), Convert.ToInt64(Right))

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

                    Return XorInt64(Convert.ToInt64(Left), CLng(Convert.ToString(Right)))


                Case TypeCode.Byte * s_TCMAX + TypeCode.Empty
                    Return XorByte(Convert.ToByte(Left), Nothing, GetEnumResult(Left, Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Boolean
                    Return XorInt16(Convert.ToInt16(Left), ToVBBool(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.Byte
                    Return XorByte(Convert.ToByte(Left), Convert.ToByte(Right), GetEnumResult(Left, Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Byte
                    Return XorUInt16(Convert.ToUInt16(Left), Convert.ToUInt16(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt16

                    Return XorUInt32(Convert.ToUInt32(Left), Convert.ToUInt32(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt32

                    Return XorUInt64(Convert.ToUInt64(Left), Convert.ToUInt64(Right))


                Case TypeCode.Int16 * s_TCMAX + TypeCode.Empty
                    Return XorInt16(Convert.ToInt16(Left), Nothing, GetEnumResult(Left, Right))

                Case TypeCode.Int16 * s_TCMAX + TypeCode.Int16
                    Return XorInt16(Convert.ToInt16(Left), Convert.ToInt16(Right), GetEnumResult(Left, Right))

                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Empty
                    Return XorUInt16(Convert.ToUInt16(Left), Nothing, GetEnumResult(Left, Right))

                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Boolean
                    Return XorInt32(Convert.ToInt32(Left), ToVBBool(Right))

                Case TypeCode.UInt16 * s_TCMAX + TypeCode.UInt16
                    Return XorUInt16(Convert.ToUInt16(Left), Convert.ToUInt16(Right), GetEnumResult(Left, Right))

                Case TypeCode.Int32 * s_TCMAX + TypeCode.Empty
                    Return XorInt32(Convert.ToInt32(Left), Nothing, GetEnumResult(Left, Right))

                Case TypeCode.Int32 * s_TCMAX + TypeCode.Int32
                    Return XorInt32(Convert.ToInt32(Left), Convert.ToInt32(Right), GetEnumResult(Left, Right))

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Empty
                    Return XorUInt32(Convert.ToUInt32(Left), Nothing, GetEnumResult(Left, Right))

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Single * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Double * s_TCMAX + TypeCode.Boolean

                    Return XorInt64(Convert.ToInt64(Left), ToVBBool(Right))

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.UInt32
                    Return XorUInt32(Convert.ToUInt32(Left), Convert.ToUInt32(Right), GetEnumResult(Left, Right))

                Case TypeCode.Int64 * s_TCMAX + TypeCode.Empty
                    Return XorInt64(Convert.ToInt64(Left), Nothing, GetEnumResult(Left, Right))

                Case TypeCode.Int64 * s_TCMAX + TypeCode.Int64
                    Return XorInt64(Convert.ToInt64(Left), Convert.ToInt64(Right), GetEnumResult(Left, Right))

                Case TypeCode.UInt64 * s_TCMAX + TypeCode.Empty
                    Return XorUInt64(Convert.ToUInt64(Left), Nothing, GetEnumResult(Left, Right))

                Case TypeCode.UInt64 * s_TCMAX + TypeCode.UInt64
                    Return XorUInt64(Convert.ToUInt64(Left), Convert.ToUInt64(Right), GetEnumResult(Left, Right))


                Case TypeCode.Decimal * s_TCMAX + TypeCode.Empty,
                     TypeCode.Single * s_TCMAX + TypeCode.Empty,
                     TypeCode.Double * s_TCMAX + TypeCode.Empty
                    Return XorInt64(Convert.ToInt64(Left), Nothing)


                Case TypeCode.String * s_TCMAX + TypeCode.Empty
                    Return XorInt64(CLng(Convert.ToString(Left)), Nothing)

                Case TypeCode.String * s_TCMAX + TypeCode.Boolean
                    Return XorBoolean(CBool(Convert.ToString(Left)), Convert.ToBoolean(Right))

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

                    Return XorInt64(CLng(Convert.ToString(Left)), Convert.ToInt64(Right))

                Case TypeCode.String * s_TCMAX + TypeCode.String
                    Return XorInt64(CLng(Convert.ToString(Left)), CLng(Convert.ToString(Right)))

            End Select

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return InvokeUserDefinedOperator(UserDefinedOperator.Xor, Left, Right)
            End If

            Throw GetNoValidOperatorException(UserDefinedOperator.Xor, Left, Right)
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

        Public Shared Function AddObject(ByVal Left As Object, ByVal Right As Object) As Object

            Dim tc1 As TypeCode = GetTypeCode(Left)
            Dim tc2 As TypeCode = GetTypeCode(Right)


            'Special cases for Char()
            If tc1 = TypeCode.Object Then
                Dim leftCharArray As Char() = TryCast(Left, Char())

                If leftCharArray IsNot Nothing Then
                    If tc2 = TypeCode.String OrElse tc2 = TypeCode.Empty OrElse ((tc2 = TypeCode.Object) AndAlso (TypeOf Right Is Char())) Then
                        'Treat Char() as String for these cases
                        Left = CStr(leftCharArray)
                        tc1 = TypeCode.String
                    End If
                End If
            End If

            If (tc2 = TypeCode.Object) Then
                Dim rightCharArray As Char() = TryCast(Right, Char())

                If rightCharArray IsNot Nothing Then
                    If tc1 = TypeCode.String OrElse tc1 = TypeCode.Empty Then
                        Right = CStr(rightCharArray)
                        tc2 = TypeCode.String
                    End If
                End If
            End If


            Select Case tc1 * s_TCMAX + tc2

                Case TypeCode.Empty * s_TCMAX + TypeCode.Empty
                    Return Boxed_ZeroInteger

                Case TypeCode.Empty * s_TCMAX + TypeCode.Boolean
                    Return AddInt16(Nothing, ToVBBool(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.SByte
                    Return Convert.ToSByte(Right)

                Case TypeCode.Empty * s_TCMAX + TypeCode.Byte
                    Return Convert.ToByte(Right)

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int16
                    Return Convert.ToInt16(Right)

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt16
                    Return Convert.ToUInt16(Right)

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int32
                    Return Convert.ToInt32(Right)

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt32
                    Return Convert.ToUInt32(Right)

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int64
                    Return Convert.ToInt64(Right)

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt64
                    Return Convert.ToUInt64(Right)

                Case TypeCode.Empty * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Empty * s_TCMAX + TypeCode.Single,
                     TypeCode.Empty * s_TCMAX + TypeCode.Double,
                     TypeCode.Empty * s_TCMAX + TypeCode.String,
                     TypeCode.DBNull * s_TCMAX + TypeCode.String

                    Return Right

                Case TypeCode.Empty * s_TCMAX + TypeCode.DateTime
                    Return AddString(CStr(CDate(Nothing)), CStr(Convert.ToDateTime(Right)))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Char
                    Return AddString(ControlChars.NullChar, Convert.ToString(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Empty
                    Return AddInt16(ToVBBool(Left), Nothing)

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Boolean
                    Return AddInt16(ToVBBool(Left), ToVBBool(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.SByte
                    Return AddSByte(ToVBBool(Left), Convert.ToSByte(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Byte,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int16
                    Return AddInt16(ToVBBool(Left), Convert.ToInt16(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int32
                    Return AddInt32(ToVBBool(Left), Convert.ToInt32(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int64
                    Return AddInt64(ToVBBool(Left), Convert.ToInt64(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Decimal
                    Return AddDecimal(ToVBBool(Left), Convert.ToDecimal(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Single
                    Return AddSingle(ToVBBool(Left), Convert.ToSingle(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Double
                    Return AddDouble(ToVBBool(Left), Convert.ToDouble(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.String
                    Return AddDouble(ToVBBool(Left), CDbl(Convert.ToString(Right)))


                Case TypeCode.SByte * s_TCMAX + TypeCode.Empty
                    Return Convert.ToSByte(Left)

                Case TypeCode.SByte * s_TCMAX + TypeCode.Boolean
                    Return AddSByte(Convert.ToSByte(Left), ToVBBool(Right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.SByte
                    Return AddSByte(Convert.ToSByte(Left), Convert.ToSByte(Right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Byte,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Byte * s_TCMAX + TypeCode.SByte,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int16

                    Return AddInt16(Convert.ToInt16(Left), Convert.ToInt16(Right))

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

                    Return AddInt32(Convert.ToInt32(Left), Convert.ToInt32(Right))

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

                    Return AddInt64(Convert.ToInt64(Left), Convert.ToInt64(Right))

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

                    Return AddDecimal(Left, Right)

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

                    Return AddSingle(Convert.ToSingle(Left), Convert.ToSingle(Right))

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

                    Return AddDouble(Convert.ToDouble(Left), Convert.ToDouble(Right))

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

                    Return AddDouble(Convert.ToDouble(Left), CDbl(Convert.ToString(Right)))

                Case TypeCode.Byte * s_TCMAX + TypeCode.Empty
                    Return Convert.ToByte(Left)

                Case TypeCode.Byte * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Boolean
                    Return AddInt16(Convert.ToInt16(Left), ToVBBool(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.Byte
                    Return AddByte(Convert.ToByte(Left), Convert.ToByte(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt16
                    Return AddUInt16(Convert.ToUInt16(Left), Convert.ToUInt16(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt32
                    Return AddUInt32(Convert.ToUInt32(Left), Convert.ToUInt32(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt64

                    Return AddUInt64(Convert.ToUInt64(Left), Convert.ToUInt64(Right))


                Case TypeCode.Int16 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToInt16(Left)


                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToUInt16(Left)

                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Boolean
                    Return AddInt32(Convert.ToInt32(Left), ToVBBool(Right))


                Case TypeCode.Int32 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToInt32(Left)


                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToUInt32(Left)

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Boolean
                    Return AddInt64(Convert.ToInt64(Left), ToVBBool(Right))


                Case TypeCode.Int64 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToInt64(Left)


                Case TypeCode.UInt64 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToUInt64(Left)

                Case TypeCode.UInt64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Boolean
                    Return AddDecimal(Left, ToVBBool(Right))


                Case TypeCode.Decimal * s_TCMAX + TypeCode.Empty,
                     TypeCode.Single * s_TCMAX + TypeCode.Empty,
                     TypeCode.Double * s_TCMAX + TypeCode.Empty,
                     TypeCode.String * s_TCMAX + TypeCode.Empty,
                     TypeCode.String * s_TCMAX + TypeCode.DBNull

                    Return Left


                Case TypeCode.Single * s_TCMAX + TypeCode.Boolean
                    Return AddSingle(Convert.ToSingle(Left), ToVBBool(Right))


                Case TypeCode.Double * s_TCMAX + TypeCode.Boolean
                    Return AddDouble(Convert.ToDouble(Left), ToVBBool(Right))


                Case TypeCode.DateTime * s_TCMAX + TypeCode.Empty
                    Return AddString(CStr(CDate(Nothing)), CStr(CDate(Left)))

                Case TypeCode.DateTime * s_TCMAX + TypeCode.DateTime
                    Return AddString(CStr(Convert.ToDateTime(Left)), CStr(Convert.ToDateTime(Right)))

                Case TypeCode.DateTime * s_TCMAX + TypeCode.String
                    Return AddString(CStr(Convert.ToDateTime(Left)), Convert.ToString(Right))


                Case TypeCode.Char * s_TCMAX + TypeCode.Empty
                    Return AddString(Convert.ToString(Left), ControlChars.NullChar)

                Case TypeCode.Char * s_TCMAX + TypeCode.Char,
                     TypeCode.Char * s_TCMAX + TypeCode.String,
                     TypeCode.String * s_TCMAX + TypeCode.Char
                    Return AddString(Convert.ToString(Left), Convert.ToString(Right))


                Case TypeCode.String * s_TCMAX + TypeCode.Boolean
                    Return AddDouble(CDbl(Convert.ToString(Left)), ToVBBool(Right))

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

                    Return AddDouble(CDbl(Convert.ToString(Left)), Convert.ToDouble(Right))

                Case TypeCode.String * s_TCMAX + TypeCode.DateTime
                    Return AddString(Convert.ToString(Left), CStr(Convert.ToDateTime(Right)))

                Case TypeCode.String * s_TCMAX + TypeCode.String
                    Return AddString(Convert.ToString(Left), Convert.ToString(Right))

                Case Else

            End Select

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return InvokeUserDefinedOperator(UserDefinedOperator.Plus, Left, Right)
            End If

            Throw GetNoValidOperatorException(UserDefinedOperator.Plus, Left, Right)
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

        Public Shared Function SubtractObject(ByVal Left As Object, ByVal Right As Object) As Object
            Dim tc1 As TypeCode = GetTypeCode(Left)
            Dim tc2 As TypeCode = GetTypeCode(Right)

            Select Case tc1 * s_TCMAX + tc2

                Case TypeCode.Empty * s_TCMAX + TypeCode.Empty
                    Return Boxed_ZeroInteger

                Case TypeCode.Empty * s_TCMAX + TypeCode.Boolean
                    Return SubtractInt16(Nothing, ToVBBool(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.SByte
                    Return SubtractSByte(Nothing, Convert.ToSByte(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Byte
                    Return SubtractByte(Nothing, Convert.ToByte(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int16
                    Return SubtractInt16(Nothing, Convert.ToInt16(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt16
                    Return SubtractUInt16(Nothing, Convert.ToUInt16(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int32
                    Return SubtractInt32(Nothing, Convert.ToInt32(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt32
                    Return SubtractUInt32(Nothing, Convert.ToUInt32(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int64
                    Return SubtractInt64(Nothing, Convert.ToInt64(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt64
                    Return SubtractUInt64(Nothing, Convert.ToUInt64(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Decimal
                    Return SubtractDecimal(0D, Right)

                Case TypeCode.Empty * s_TCMAX + TypeCode.Single
                    Return SubtractSingle(Nothing, Convert.ToSingle(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Double
                    Return SubtractDouble(Nothing, Convert.ToDouble(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.String
                    Return SubtractDouble(Nothing, CDbl(Convert.ToString(Right)))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Empty
                    Return SubtractInt16(ToVBBool(Left), Nothing)

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Boolean
                    Return SubtractInt16(ToVBBool(Left), ToVBBool(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.SByte
                    Return SubtractSByte(ToVBBool(Left), Convert.ToSByte(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Byte,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int16
                    Return SubtractInt16(ToVBBool(Left), Convert.ToInt16(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int32
                    Return SubtractInt32(ToVBBool(Left), Convert.ToInt32(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int64
                    Return SubtractInt64(ToVBBool(Left), Convert.ToInt64(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Decimal
                    Return SubtractDecimal(ToVBBool(Left), Convert.ToDecimal(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Single
                    Return SubtractSingle(ToVBBool(Left), Convert.ToSingle(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Double
                    Return SubtractDouble(ToVBBool(Left), Convert.ToDouble(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.String
                    Return SubtractDouble(ToVBBool(Left), CDbl(Convert.ToString(Right)))


                Case TypeCode.SByte * s_TCMAX + TypeCode.Empty
                    Return Convert.ToSByte(Left)

                Case TypeCode.SByte * s_TCMAX + TypeCode.Boolean
                    Return SubtractSByte(Convert.ToSByte(Left), ToVBBool(Right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.SByte
                    Return SubtractSByte(Convert.ToSByte(Left), Convert.ToSByte(Right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Byte,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Byte * s_TCMAX + TypeCode.SByte,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int16

                    Return SubtractInt16(Convert.ToInt16(Left), Convert.ToInt16(Right))

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

                    Return SubtractInt32(Convert.ToInt32(Left), Convert.ToInt32(Right))

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

                    Return SubtractInt64(Convert.ToInt64(Left), Convert.ToInt64(Right))

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

                    Return SubtractDecimal(Left, Right)

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

                    Return SubtractSingle(Convert.ToSingle(Left), Convert.ToSingle(Right))

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

                    Return SubtractDouble(Convert.ToDouble(Left), Convert.ToDouble(Right))

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

                    Return SubtractDouble(Convert.ToDouble(Left), CDbl(Convert.ToString(Right)))


                Case TypeCode.Byte * s_TCMAX + TypeCode.Empty
                    Return Convert.ToByte(Left)

                Case TypeCode.Byte * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Boolean
                    Return SubtractInt16(Convert.ToInt16(Left), ToVBBool(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.Byte
                    Return SubtractByte(Convert.ToByte(Left), Convert.ToByte(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt16
                    Return SubtractUInt16(Convert.ToUInt16(Left), Convert.ToUInt16(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt32
                    Return SubtractUInt32(Convert.ToUInt32(Left), Convert.ToUInt32(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt64

                    Return SubtractUInt64(Convert.ToUInt64(Left), Convert.ToUInt64(Right))


                Case TypeCode.Int16 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToInt16(Left)

                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToUInt16(Left)

                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Boolean
                    Return SubtractInt32(Convert.ToInt32(Left), ToVBBool(Right))


                Case TypeCode.Int32 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToInt32(Left)

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToUInt32(Left)

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Boolean
                    Return SubtractInt64(Convert.ToInt64(Left), ToVBBool(Right))


                Case TypeCode.Int64 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToInt64(Left)

                Case TypeCode.UInt64 * s_TCMAX + TypeCode.Empty
                    Return Convert.ToUInt64(Left)

                Case TypeCode.UInt64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Boolean
                    Return SubtractDecimal(Left, ToVBBool(Right))


                Case TypeCode.Decimal * s_TCMAX + TypeCode.Empty,
                     TypeCode.Single * s_TCMAX + TypeCode.Empty,
                     TypeCode.Double * s_TCMAX + TypeCode.Empty
                    Return Left


                Case TypeCode.Single * s_TCMAX + TypeCode.Boolean
                    Return SubtractSingle(Convert.ToSingle(Left), ToVBBool(Right))

                Case TypeCode.Double * s_TCMAX + TypeCode.Boolean
                    Return SubtractDouble(Convert.ToDouble(Left), ToVBBool(Right))

                Case TypeCode.String * s_TCMAX + TypeCode.Empty
                    Return CDbl(Convert.ToString(Left))

                Case TypeCode.String * s_TCMAX + TypeCode.Boolean
                    Return SubtractDouble(CDbl(Convert.ToString(Left)), ToVBBool(Right))

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

                    Return SubtractDouble(CDbl(Convert.ToString(Left)), Convert.ToDouble(Right))

                Case TypeCode.String * s_TCMAX + TypeCode.String
                    Return SubtractDouble(CDbl(Convert.ToString(Left)), CDbl(Convert.ToString(Right)))

                Case Else

            End Select

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object OrElse
               (tc1 = TypeCode.DateTime AndAlso tc2 = TypeCode.DateTime) OrElse
               (tc1 = TypeCode.DateTime AndAlso tc2 = TypeCode.Empty) OrElse
               (tc1 = TypeCode.Empty AndAlso tc2 = TypeCode.DateTime) Then

                Return InvokeUserDefinedOperator(UserDefinedOperator.Minus, Left, Right)
            End If

            Throw GetNoValidOperatorException(UserDefinedOperator.Minus, Left, Right)

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

        Public Shared Function MultiplyObject(ByVal Left As Object, ByVal Right As Object) As Object

            Dim tc1 As TypeCode = GetTypeCode(Left)
            Dim tc2 As TypeCode = GetTypeCode(Right)

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
                    Return MultiplyDouble(Nothing, CDbl(Convert.ToString(Right)))


                Case TypeCode.Boolean * s_TCMAX + TypeCode.Boolean
                    Return MultiplyInt16(ToVBBool(Left), ToVBBool(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.SByte
                    Return MultiplySByte(ToVBBool(Left), Convert.ToSByte(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Byte,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int16
                    Return MultiplyInt16(ToVBBool(Left), Convert.ToInt16(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int32
                    Return MultiplyInt32(ToVBBool(Left), Convert.ToInt32(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int64
                    Return MultiplyInt64(ToVBBool(Left), Convert.ToInt64(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Decimal
                    Return MultiplyDecimal(ToVBBool(Left), Convert.ToDecimal(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Single
                    Return MultiplySingle(ToVBBool(Left), Convert.ToSingle(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Double
                    Return MultiplyDouble(ToVBBool(Left), Convert.ToDouble(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.String
                    Return MultiplyDouble(ToVBBool(Left), CDbl(Convert.ToString(Right)))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Boolean
                    Return MultiplySByte(Convert.ToSByte(Left), ToVBBool(Right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.SByte
                    Return MultiplySByte(Convert.ToSByte(Left), Convert.ToSByte(Right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Byte,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Byte * s_TCMAX + TypeCode.SByte,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int16

                    Return MultiplyInt16(Convert.ToInt16(Left), Convert.ToInt16(Right))

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

                    Return MultiplyInt32(Convert.ToInt32(Left), Convert.ToInt32(Right))

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

                    Return MultiplyInt64(Convert.ToInt64(Left), Convert.ToInt64(Right))

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

                    Return MultiplyDecimal(Left, Right)

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

                    Return MultiplySingle(Convert.ToSingle(Left), Convert.ToSingle(Right))

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

                    Return MultiplyDouble(Convert.ToDouble(Left), Convert.ToDouble(Right))

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

                    Return MultiplyDouble(Convert.ToDouble(Left), CDbl(Convert.ToString(Right)))


                Case TypeCode.Byte * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Boolean
                    Return MultiplyInt16(Convert.ToInt16(Left), ToVBBool(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.Byte
                    Return MultiplyByte(Convert.ToByte(Left), Convert.ToByte(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt16
                    Return MultiplyUInt16(Convert.ToUInt16(Left), Convert.ToUInt16(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt32
                    Return MultiplyUInt32(Convert.ToUInt32(Left), Convert.ToUInt32(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt64

                    Return MultiplyUInt64(Convert.ToUInt64(Left), Convert.ToUInt64(Right))


                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Boolean
                    Return MultiplyInt32(Convert.ToInt32(Left), ToVBBool(Right))

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Boolean
                    Return MultiplyInt64(Convert.ToInt64(Left), ToVBBool(Right))


                Case TypeCode.UInt64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Boolean
                    Return MultiplyDecimal(Left, ToVBBool(Right))

                Case TypeCode.Single * s_TCMAX + TypeCode.Boolean
                    Return MultiplySingle(Convert.ToSingle(Left), ToVBBool(Right))


                Case TypeCode.Double * s_TCMAX + TypeCode.Boolean
                    Return MultiplyDouble(Convert.ToDouble(Left), ToVBBool(Right))


                Case TypeCode.String * s_TCMAX + TypeCode.Empty
                    Return MultiplyDouble(CDbl(Convert.ToString(Left)), Nothing)

                Case TypeCode.String * s_TCMAX + TypeCode.Boolean
                    Return MultiplyDouble(CDbl(Convert.ToString(Left)), ToVBBool(Right))

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

                    Return MultiplyDouble(CDbl(Convert.ToString(Left)), Convert.ToDouble(Right))

                Case TypeCode.String * s_TCMAX + TypeCode.String
                    Return MultiplyDouble(CDbl(Convert.ToString(Left)), CDbl(Convert.ToString(Right)))

                Case Else

            End Select

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return InvokeUserDefinedOperator(UserDefinedOperator.Multiply, Left, Right)
            End If

            Throw GetNoValidOperatorException(UserDefinedOperator.Multiply, Left, Right)

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

        Public Shared Function DivideObject(ByVal Left As Object, ByVal Right As Object) As Object

            Dim tc1 As TypeCode = GetTypeCode(Left)
            Dim tc2 As TypeCode = GetTypeCode(Right)

            Select Case tc1 * s_TCMAX + tc2

                Case TypeCode.Empty * s_TCMAX + TypeCode.Empty
                    Return DivideDouble(Nothing, Nothing)

                Case TypeCode.Empty * s_TCMAX + TypeCode.Boolean
                    Return DivideDouble(Nothing, ToVBBool(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.SByte,
                     TypeCode.Empty * s_TCMAX + TypeCode.Byte,
                     TypeCode.Empty * s_TCMAX + TypeCode.Int16,
                     TypeCode.Empty * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Empty * s_TCMAX + TypeCode.Int32,
                     TypeCode.Empty * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Empty * s_TCMAX + TypeCode.Int64,
                     TypeCode.Empty * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Empty * s_TCMAX + TypeCode.Double
                    Return DivideDouble(Nothing, Convert.ToDouble(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Decimal
                    Return DivideDecimal(0D, Right)

                Case TypeCode.Empty * s_TCMAX + TypeCode.Single
                    Return DivideSingle(Nothing, Convert.ToSingle(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.String
                    Return DivideDouble(Nothing, CDbl(Convert.ToString(Right)))


                Case TypeCode.Boolean * s_TCMAX + TypeCode.Empty
                    Return DivideDouble(ToVBBool(Left), Nothing)

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Boolean
                    Return DivideDouble(ToVBBool(Left), ToVBBool(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.SByte,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Byte,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int16,
                     TypeCode.Boolean * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int32,
                     TypeCode.Boolean * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Double
                    Return DivideDouble(ToVBBool(Left), Convert.ToDouble(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Decimal
                    Return DivideDecimal(ToVBBool(Left), Right)

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Single
                    Return DivideSingle(ToVBBool(Left), Convert.ToSingle(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.String
                    Return DivideDouble(ToVBBool(Left), CDbl(Convert.ToString(Right)))


                Case TypeCode.SByte * s_TCMAX + TypeCode.Empty,
                     TypeCode.Byte * s_TCMAX + TypeCode.Empty,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Empty,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Empty,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Empty,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Empty,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Empty,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Empty,
                     TypeCode.Double * s_TCMAX + TypeCode.Empty
                    Return DivideDouble(Convert.ToDouble(Left), Nothing)

                Case TypeCode.SByte * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Byte * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Double * s_TCMAX + TypeCode.Boolean
                    Return DivideDouble(Convert.ToDouble(Left), ToVBBool(Right))

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
                    Return DivideDouble(Convert.ToDouble(Left), Convert.ToDouble(Right))

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
                    Return DivideDecimal(Left, Right)

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
                    Return DivideSingle(Convert.ToSingle(Left), Convert.ToSingle(Right))

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
                    Return DivideDouble(Convert.ToDouble(Left), CDbl(Convert.ToString(Right)))


                Case TypeCode.Decimal * s_TCMAX + TypeCode.Empty
                    Return DivideDecimal(Left, 0D)

                Case TypeCode.Decimal * s_TCMAX + TypeCode.Boolean
                    Return DivideDecimal(Left, ToVBBool(Right))


                Case TypeCode.Single * s_TCMAX + TypeCode.Empty
                    Return DivideSingle(Convert.ToSingle(Left), Nothing)

                Case TypeCode.Single * s_TCMAX + TypeCode.Boolean
                    Return DivideSingle(Convert.ToSingle(Left), ToVBBool(Right))


                Case TypeCode.String * s_TCMAX + TypeCode.Empty
                    Return DivideDouble(CDbl(Convert.ToString(Left)), Nothing)

                Case TypeCode.String * s_TCMAX + TypeCode.Boolean
                    Return DivideDouble(CDbl(Convert.ToString(Left)), ToVBBool(Right))

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
                    Return DivideDouble(CDbl(Convert.ToString(Left)), Convert.ToDouble(Right))

                Case TypeCode.String * s_TCMAX + TypeCode.String
                    Return DivideDouble(CDbl(Convert.ToString(Left)), CDbl(Convert.ToString(Right)))

                Case Else

            End Select

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return InvokeUserDefinedOperator(UserDefinedOperator.Divide, Left, Right)
            End If

            Throw GetNoValidOperatorException(UserDefinedOperator.Divide, Left, Right)

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

        Public Shared Function ExponentObject(ByVal Left As Object, ByVal Right As Object) As Object

            Dim tc1 As TypeCode = GetTypeCode(Left)
            Dim tc2 As TypeCode = GetTypeCode(Right)

            Dim leftValue As Double
            Dim rightValue As Double


            Select Case tc1
                Case TypeCode.Empty
                    leftValue = 0.0R

                Case TypeCode.Boolean
                    leftValue = ToVBBool(Left)

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
                    leftValue = Convert.ToDouble(Left)

                Case TypeCode.String
                    leftValue = CDbl(Convert.ToString(Left))

                Case TypeCode.Object
                    Return InvokeUserDefinedOperator(UserDefinedOperator.Power, Left, Right)

                Case Else
                    'DateTime
                    'Char
                    Throw GetNoValidOperatorException(UserDefinedOperator.Power, Left, Right)
            End Select

            Select Case tc2
                Case TypeCode.Empty
                    rightValue = 0.0R

                Case TypeCode.Boolean
                    rightValue = ToVBBool(Right)

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
                    rightValue = Convert.ToDouble(Right)

                Case TypeCode.String
                    rightValue = CDbl(Convert.ToString(Right))

                Case TypeCode.Object
                    Return InvokeUserDefinedOperator(UserDefinedOperator.Power, Left, Right)

                Case Else
                    'DateTime
                    'Char
                    Throw GetNoValidOperatorException(UserDefinedOperator.Power, Left, Right)
            End Select

            Return leftValue ^ rightValue

        End Function

#End Region

#Region " Operator Mod "

        Public Shared Function ModObject(ByVal Left As Object, ByVal Right As Object) As Object

            Dim tc1 As TypeCode = GetTypeCode(Left)
            Dim tc2 As TypeCode = GetTypeCode(Right)

            Select Case tc1 * s_TCMAX + tc2

                Case TypeCode.Empty * s_TCMAX + TypeCode.Empty
                    Return ModInt32(Nothing, Nothing)

                Case TypeCode.Empty * s_TCMAX + TypeCode.Boolean
                    Return ModInt16(Nothing, ToVBBool(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.SByte
                    Return ModSByte(Nothing, Convert.ToSByte(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Byte
                    Return ModByte(Nothing, Convert.ToByte(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int16
                    Return ModInt16(Nothing, Convert.ToInt16(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt16
                    Return ModUInt16(Nothing, Convert.ToUInt16(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int32
                    Return ModInt32(Nothing, Convert.ToInt32(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt32
                    Return ModUInt32(Nothing, Convert.ToUInt32(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int64
                    Return ModInt64(Nothing, Convert.ToInt64(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt64
                    Return ModUInt64(Nothing, Convert.ToUInt64(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Decimal
                    Return ModDecimal(0D, Convert.ToDecimal(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Single
                    Return ModSingle(Nothing, Convert.ToSingle(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Double
                    Return ModDouble(Nothing, Convert.ToDouble(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.String
                    Return ModDouble(Nothing, CDbl(Convert.ToString(Right)))


                Case TypeCode.Boolean * s_TCMAX + TypeCode.Empty
                    Return ModInt16(ToVBBool(Left), Nothing)

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Boolean
                    Return ModInt16(ToVBBool(Left), ToVBBool(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.SByte
                    Return ModSByte(ToVBBool(Left), Convert.ToSByte(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Byte,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int16
                    Return ModInt16(ToVBBool(Left), Convert.ToInt16(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int32
                    Return ModInt32(ToVBBool(Left), Convert.ToInt32(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int64
                    Return ModInt64(ToVBBool(Left), Convert.ToInt64(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Decimal
                    Return ModDecimal(ToVBBool(Left), Convert.ToDecimal(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Single
                    Return ModSingle(ToVBBool(Left), Convert.ToSingle(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Double
                    Return ModDouble(ToVBBool(Left), Convert.ToDouble(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.String
                    Return ModDouble(ToVBBool(Left), CDbl(Convert.ToString(Right)))


                Case TypeCode.SByte * s_TCMAX + TypeCode.Empty
                    Return ModSByte(Convert.ToSByte(Left), Nothing)

                Case TypeCode.SByte * s_TCMAX + TypeCode.Boolean
                    Return ModSByte(Convert.ToSByte(Left), ToVBBool(Right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.SByte
                    Return ModSByte(Convert.ToSByte(Left), Convert.ToSByte(Right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Byte,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Byte * s_TCMAX + TypeCode.SByte,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int16

                    Return ModInt16(Convert.ToInt16(Left), Convert.ToInt16(Right))

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

                    Return ModInt32(Convert.ToInt32(Left), Convert.ToInt32(Right))

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

                    Return ModInt64(Convert.ToInt64(Left), Convert.ToInt64(Right))

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

                    Return ModDecimal(Left, Right)

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

                    Return ModSingle(Convert.ToSingle(Left), Convert.ToSingle(Right))

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

                    Return ModDouble(Convert.ToDouble(Left), Convert.ToDouble(Right))

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

                    Return ModDouble(Convert.ToDouble(Left), CDbl(Convert.ToString(Right)))


                Case TypeCode.Byte * s_TCMAX + TypeCode.Empty
                    Return ModByte(Convert.ToByte(Left), Nothing)

                Case TypeCode.Byte * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Boolean
                    Return ModInt16(Convert.ToInt16(Left), ToVBBool(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.Byte
                    Return ModByte(Convert.ToByte(Left), Convert.ToByte(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt16
                    Return ModUInt16(Convert.ToUInt16(Left), Convert.ToUInt16(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt32
                    Return ModUInt32(Convert.ToUInt32(Left), Convert.ToUInt32(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt64

                    Return ModUInt64(Convert.ToUInt64(Left), Convert.ToUInt64(Right))


                Case TypeCode.Int16 * s_TCMAX + TypeCode.Empty
                    Return ModInt16(Convert.ToInt16(Left), Nothing)


                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Empty
                    Return ModUInt16(Convert.ToUInt16(Left), Nothing)

                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Boolean
                    Return ModInt32(Convert.ToInt32(Left), ToVBBool(Right))


                Case TypeCode.Int32 * s_TCMAX + TypeCode.Empty
                    Return ModInt32(Convert.ToInt32(Left), Nothing)

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Empty
                    Return ModUInt32(Convert.ToUInt32(Left), Nothing)

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Boolean
                    Return ModInt64(Convert.ToInt64(Left), ToVBBool(Right))

                Case TypeCode.Int64 * s_TCMAX + TypeCode.Empty
                    Return ModInt64(Convert.ToInt64(Left), Nothing)


                Case TypeCode.UInt64 * s_TCMAX + TypeCode.Empty
                    Return ModUInt64(Convert.ToUInt64(Left), Nothing)

                Case TypeCode.UInt64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Boolean
                    Return ModDecimal(Left, ToVBBool(Right))


                Case TypeCode.Decimal * s_TCMAX + TypeCode.Empty
                    Return ModDecimal(Left, 0D)


                Case TypeCode.Single * s_TCMAX + TypeCode.Empty
                    Return ModSingle(Convert.ToSingle(Left), Nothing)

                Case TypeCode.Single * s_TCMAX + TypeCode.Boolean
                    Return ModSingle(Convert.ToSingle(Left), ToVBBool(Right))


                Case TypeCode.Double * s_TCMAX + TypeCode.Empty
                    Return ModDouble(Convert.ToDouble(Left), Nothing)

                Case TypeCode.Double * s_TCMAX + TypeCode.Boolean
                    Return ModDouble(Convert.ToDouble(Left), ToVBBool(Right))


                Case TypeCode.String * s_TCMAX + TypeCode.Empty
                    Return ModDouble(CDbl(Convert.ToString(Left)), Nothing)

                Case TypeCode.String * s_TCMAX + TypeCode.Boolean
                    Return ModDouble(CDbl(Convert.ToString(Left)), ToVBBool(Right))

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

                    Return ModDouble(CDbl(Convert.ToString(Left)), Convert.ToDouble(Right))

                Case TypeCode.String * s_TCMAX + TypeCode.String
                    Return ModDouble(CDbl(Convert.ToString(Left)), CDbl(Convert.ToString(Right)))

                Case Else

            End Select

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return InvokeUserDefinedOperator(UserDefinedOperator.Modulus, Left, Right)
            End If

            Throw GetNoValidOperatorException(UserDefinedOperator.Modulus, Left, Right)
        End Function

        <MethodImpl(MethodImplOptions.NoInlining)> ' To work around https://github.com/dotnet/coreclr/issues/8648
        Private Shared Function ModSByte(ByVal left As SByte, ByVal right As SByte) As Object
            Return left Mod right
        End Function

        Private Shared Function ModByte(ByVal left As Byte, ByVal right As Byte) As Object
            Return left Mod right
        End Function

        Private Shared Function ModInt16(ByVal left As Int16, ByVal right As Int16) As Object
            Return left Mod right
        End Function

        Private Shared Function ModUInt16(ByVal left As UInt16, ByVal right As UInt16) As Object
            Return left Mod right
        End Function

        Private Shared Function ModInt32(ByVal left As Integer, ByVal right As Integer) As Object
            If left = Integer.MinValue AndAlso right = -1 Then
                Return 0
            Else
                Return left Mod right
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

        Public Shared Function IntDivideObject(ByVal Left As Object, ByVal Right As Object) As Object

            Dim tc1 As TypeCode = GetTypeCode(Left)
            Dim tc2 As TypeCode = GetTypeCode(Right)

            Select Case tc1 * s_TCMAX + tc2

                Case TypeCode.Empty * s_TCMAX + TypeCode.Empty
                    Return IntDivideInt32(Nothing, Nothing)

                Case TypeCode.Empty * s_TCMAX + TypeCode.Boolean
                    Return IntDivideInt16(Nothing, ToVBBool(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.SByte
                    Return IntDivideSByte(Nothing, Convert.ToSByte(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Byte
                    Return IntDivideByte(Nothing, Convert.ToByte(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int16
                    Return IntDivideInt16(Nothing, Convert.ToInt16(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt16
                    Return IntDivideUInt16(Nothing, Convert.ToUInt16(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int32
                    Return IntDivideInt32(Nothing, Convert.ToInt32(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt32
                    Return IntDivideUInt32(Nothing, Convert.ToUInt32(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Int64
                    Return IntDivideInt64(Nothing, Convert.ToInt64(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.UInt64
                    Return IntDivideUInt64(Nothing, Convert.ToUInt64(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Empty * s_TCMAX + TypeCode.Single,
                     TypeCode.Empty * s_TCMAX + TypeCode.Double
                    Return IntDivideInt64(Nothing, Convert.ToInt64(Right))

                Case TypeCode.Empty * s_TCMAX + TypeCode.String
                    Return IntDivideInt64(Nothing, CLng(Convert.ToString(Right)))


                Case TypeCode.Boolean * s_TCMAX + TypeCode.Empty
                    Return IntDivideInt16(ToVBBool(Left), Nothing)

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Boolean
                    Return IntDivideInt16(ToVBBool(Left), ToVBBool(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.SByte
                    Return IntDivideSByte(ToVBBool(Left), Convert.ToSByte(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.Byte,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int16
                    Return IntDivideInt16(ToVBBool(Left), Convert.ToInt16(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt16,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int32
                    Return IntDivideInt32(ToVBBool(Left), Convert.ToInt32(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.UInt32,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Int64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.UInt64,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Decimal,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Single,
                     TypeCode.Boolean * s_TCMAX + TypeCode.Double

                    Return IntDivideInt64(ToVBBool(Left), Convert.ToInt64(Right))

                Case TypeCode.Boolean * s_TCMAX + TypeCode.String
                    Return IntDivideInt64(ToVBBool(Left), CLng(Convert.ToString(Right)))


                Case TypeCode.SByte * s_TCMAX + TypeCode.Empty
                    Return IntDivideSByte(Convert.ToSByte(Left), Nothing)

                Case TypeCode.SByte * s_TCMAX + TypeCode.Boolean
                    Return IntDivideSByte(Convert.ToSByte(Left), ToVBBool(Right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.SByte
                    Return IntDivideSByte(Convert.ToSByte(Left), Convert.ToSByte(Right))

                Case TypeCode.SByte * s_TCMAX + TypeCode.Byte,
                     TypeCode.SByte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Byte * s_TCMAX + TypeCode.SByte,
                     TypeCode.Byte * s_TCMAX + TypeCode.Int16,
                     TypeCode.Int16 * s_TCMAX + TypeCode.SByte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Int16

                    Return IntDivideInt16(Convert.ToInt16(Left), Convert.ToInt16(Right))

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

                    Return IntDivideInt32(Convert.ToInt32(Left), Convert.ToInt32(Right))

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

                    Return IntDivideInt64(Convert.ToInt64(Left), Convert.ToInt64(Right))

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

                    Return IntDivideInt64(Convert.ToInt64(Left), CLng(Convert.ToString(Right)))


                Case TypeCode.Byte * s_TCMAX + TypeCode.Empty
                    Return IntDivideByte(Convert.ToByte(Left), Nothing)

                Case TypeCode.Byte * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int16 * s_TCMAX + TypeCode.Boolean
                    Return IntDivideInt16(Convert.ToInt16(Left), ToVBBool(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.Byte
                    Return IntDivideByte(Convert.ToByte(Left), Convert.ToByte(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt16
                    Return IntDivideUInt16(Convert.ToUInt16(Left), Convert.ToUInt16(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt32

                    Return IntDivideUInt32(Convert.ToUInt32(Left), Convert.ToUInt32(Right))

                Case TypeCode.Byte * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt16 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt32 * s_TCMAX + TypeCode.UInt64,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Byte,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt16,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt32,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.UInt64

                    Return IntDivideUInt64(Convert.ToUInt64(Left), Convert.ToUInt64(Right))


                Case TypeCode.Int16 * s_TCMAX + TypeCode.Empty
                    Return IntDivideInt16(Convert.ToInt16(Left), Nothing)


                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Empty
                    Return IntDivideUInt16(Convert.ToUInt16(Left), Nothing)

                Case TypeCode.UInt16 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int32 * s_TCMAX + TypeCode.Boolean
                    Return IntDivideInt32(Convert.ToInt32(Left), ToVBBool(Right))


                Case TypeCode.Int32 * s_TCMAX + TypeCode.Empty
                    Return IntDivideInt32(Convert.ToInt32(Left), Nothing)


                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Empty
                    Return IntDivideUInt32(Convert.ToUInt32(Left), Nothing)

                Case TypeCode.UInt32 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Int64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.UInt64 * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Decimal * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Single * s_TCMAX + TypeCode.Boolean,
                     TypeCode.Double * s_TCMAX + TypeCode.Boolean

                    Return IntDivideInt64(Convert.ToInt64(Left), ToVBBool(Right))


                Case TypeCode.Int64 * s_TCMAX + TypeCode.Empty
                    Return IntDivideInt64(Convert.ToInt64(Left), Nothing)


                Case TypeCode.UInt64 * s_TCMAX + TypeCode.Empty
                    Return IntDivideUInt64(Convert.ToUInt64(Left), Nothing)


                Case TypeCode.Decimal * s_TCMAX + TypeCode.Empty,
                     TypeCode.Single * s_TCMAX + TypeCode.Empty,
                     TypeCode.Double * s_TCMAX + TypeCode.Empty
                    Return IntDivideInt64(Convert.ToInt64(Left), Nothing)


                Case TypeCode.String * s_TCMAX + TypeCode.Empty
                    Return IntDivideInt64(CLng(Convert.ToString(Left)), Nothing)

                Case TypeCode.String * s_TCMAX + TypeCode.Boolean
                    Return IntDivideInt64(CLng(Convert.ToString(Left)), ToVBBool(Right))

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

                    Return IntDivideInt64(CLng(Convert.ToString(Left)), Convert.ToInt64(Right))

                Case TypeCode.String * s_TCMAX + TypeCode.String
                    Return IntDivideInt64(CLng(Convert.ToString(Left)), CLng(Convert.ToString(Right)))

            End Select

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return InvokeUserDefinedOperator(UserDefinedOperator.IntegralDivide, Left, Right)
            End If

            Throw GetNoValidOperatorException(UserDefinedOperator.IntegralDivide, Left, Right)
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

        Public Shared Function LeftShiftObject(ByVal Operand As Object, ByVal Amount As Object) As Object

            Dim tc1 As TypeCode = GetTypeCode(Operand)
            Dim tc2 As TypeCode = GetTypeCode(Amount)


            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return InvokeUserDefinedOperator(UserDefinedOperator.ShiftLeft, Operand, Amount)
            End If

            Select Case tc1
                Case TypeCode.Empty
                    Return Nothing << CInt(Amount)
                Case TypeCode.Boolean
                    Return CShort(Convert.ToBoolean(Operand)) << CInt(Amount)
                Case TypeCode.SByte
                    Return Convert.ToSByte(Operand) << CInt(Amount)
                Case TypeCode.Byte
                    Return Convert.ToByte(Operand) << CInt(Amount)
                Case TypeCode.Int16
                    Return Convert.ToInt16(Operand) << CInt(Amount)
                Case TypeCode.UInt16
                    Return Convert.ToUInt16(Operand) << CInt(Amount)
                Case TypeCode.Int32
                    Return Convert.ToInt32(Operand) << CInt(Amount)
                Case TypeCode.UInt32
                    Return Convert.ToUInt32(Operand) << CInt(Amount)
                Case TypeCode.Int64,
                     TypeCode.Single,
                     TypeCode.Double,
                     TypeCode.Decimal
                    Return Convert.ToInt64(Operand) << CInt(Amount)
                Case TypeCode.UInt64
                    Return Convert.ToUInt64(Operand) << CInt(Amount)
                Case TypeCode.String
                    Return CLng(Convert.ToString(Operand)) << CInt(Amount)
            End Select

            Throw GetNoValidOperatorException(UserDefinedOperator.ShiftLeft, Operand)
        End Function

#End Region

#Region " Operator Shift Right >> "

        Public Shared Function RightShiftObject(ByVal Operand As Object, ByVal Amount As Object) As Object

            Dim tc1 As TypeCode = GetTypeCode(Operand)
            Dim tc2 As TypeCode = GetTypeCode(Amount)

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return InvokeUserDefinedOperator(UserDefinedOperator.ShiftRight, Operand, Amount)
            End If

            Select Case tc1
                Case TypeCode.Empty
                    Return Nothing >> CInt(Amount)
                Case TypeCode.Boolean
                    Return CShort(Convert.ToBoolean(Operand)) >> CInt(Amount)
                Case TypeCode.SByte
                    Return Convert.ToSByte(Operand) >> CInt(Amount)
                Case TypeCode.Byte
                    Return Convert.ToByte(Operand) >> CInt(Amount)
                Case TypeCode.Int16
                    Return Convert.ToInt16(Operand) >> CInt(Amount)
                Case TypeCode.UInt16
                    Return Convert.ToUInt16(Operand) >> CInt(Amount)
                Case TypeCode.Int32
                    Return Convert.ToInt32(Operand) >> CInt(Amount)
                Case TypeCode.UInt32
                    Return Convert.ToUInt32(Operand) >> CInt(Amount)
                Case TypeCode.Int64,
                     TypeCode.Single,
                     TypeCode.Double,
                     TypeCode.Decimal
                    Return Convert.ToInt64(Operand) >> CInt(Amount)
                Case TypeCode.UInt64
                    Return Convert.ToUInt64(Operand) >> CInt(Amount)
                Case TypeCode.String
                    Return CLng(Convert.ToString(Operand)) >> CInt(Amount)
            End Select

            Throw GetNoValidOperatorException(UserDefinedOperator.ShiftRight, Operand)
        End Function

#End Region

#Region " Operator Concatenate & "

        Public Shared Function ConcatenateObject(ByVal Left As Object, ByVal Right As Object) As Object
            Dim conv1, conv2 As IConvertible
            Dim tc1, tc2 As TypeCode

            conv1 = TryCast(Left, IConvertible)
            If conv1 Is Nothing Then
                If Left Is Nothing Then
                    tc1 = TypeCode.Empty
                Else
                    tc1 = TypeCode.Object
                End If
            Else
                tc1 = conv1.GetTypeCode()
            End If

            conv2 = TryCast(Right, IConvertible)
            If conv2 Is Nothing Then
                If Right Is Nothing Then
                    tc2 = TypeCode.Empty
                Else
                    tc2 = TypeCode.Object
                End If
            Else
                tc2 = conv2.GetTypeCode()
            End If

            'Special cases for Char()
            If (tc1 = TypeCode.Object) AndAlso (TypeOf Left Is Char()) Then
                tc1 = TypeCode.String
            End If

            If (tc2 = TypeCode.Object) AndAlso (TypeOf Right Is Char()) Then
                tc2 = TypeCode.String
            End If

            If tc1 = TypeCode.Object OrElse tc2 = TypeCode.Object Then
                Return InvokeUserDefinedOperator(UserDefinedOperator.Concatenate, Left, Right)
            End If

            Dim LeftIsNull As Boolean = (tc1 = TypeCode.DBNull)
            Dim RightIsNull As Boolean = (tc2 = TypeCode.DBNull)

            If LeftIsNull And RightIsNull Then
                Return Left
            ElseIf LeftIsNull And Not RightIsNull Then
                Left = ""
            ElseIf RightIsNull And Not LeftIsNull Then
                Right = ""
            End If

            Return CStr(Left) & CStr(Right)
        End Function

#End Region

    End Class

End Namespace

