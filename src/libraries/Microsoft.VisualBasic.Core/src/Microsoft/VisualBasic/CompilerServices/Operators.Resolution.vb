' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Dynamic
Imports System.Linq.Expressions
Imports System.Reflection

Imports Microsoft.VisualBasic.CompilerServices.Symbols
Imports Microsoft.VisualBasic.CompilerServices.OverloadResolution
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.CompilerServices

    ' Implements VB operator resolution semantics.
    Partial Public NotInheritable Class Operators

        Friend Shared Function CollectOperators(
            ByVal op As UserDefinedOperator,
            ByVal type1 As System.Type,
            ByVal type2 As System.Type,
            ByRef foundType1Operators As Boolean,
            ByRef foundType2Operators As Boolean) As List(Of Method)

            'Given an operator kind and two types to scan, construct a list of operators by
            'collecting operators from both types.
            '
            'The second type can be NULL, in which case operators are collected from only the first
            'type.

            Dim searchBothTypes As Boolean = type2 IsNot Nothing
            Dim result As List(Of Method)

            If Not IsRootObjectType(type1) AndAlso IsClassOrValueType(type1) Then

                Dim container As Container = New Container(type1)
                Dim members As MemberInfo() = container.LookupNamedMembers(OperatorCLSNames(op))

                result =
                    CollectOverloadCandidates(
                        members,
                        Nothing,
                        IIf(IsUnaryOperator(op), 1, 2),
                        Nothing,
                        Nothing,
                        True,
                        Nothing,
                        Nothing,
                        Nothing,
                        Nothing)

                If result.Count > 0 Then foundType1Operators = True

            Else
                result = New List(Of Method)
            End If

            If searchBothTypes AndAlso Not IsRootObjectType(type2) AndAlso IsClassOrValueType(type2) Then

                Dim commonAncestor As Type = type1
                While commonAncestor IsNot Nothing
                    If IsOrInheritsFrom(type2, commonAncestor) Then
                        Exit While
                    End If
                    commonAncestor = commonAncestor.BaseType
                End While

                Dim container As Container = New Container(type2)
                Dim members As MemberInfo() = container.LookupNamedMembers(OperatorCLSNames(op))
                Dim secondResult As List(Of Method)

                'Collect operators up until the common ancestor because we don't want
                'duplicate operators in the result list.
                secondResult =
                    CollectOverloadCandidates(
                        members,
                        Nothing,
                        IIf(IsUnaryOperator(op), 1, 2),
                        Nothing,
                        Nothing,
                        True,
                        commonAncestor,
                        Nothing,
                        Nothing,
                        Nothing)

                If secondResult.Count > 0 Then foundType2Operators = True

                'Merge the second result into the main result.
                result.AddRange(secondResult)
            End If

            Return result

        End Function

        Friend Shared Function ResolveUserDefinedOperator(
            ByVal op As UserDefinedOperator,
            ByVal arguments As Object(),
            ByVal reportErrors As Boolean) As Method

            'Given an operation to perform with operands, select the appropriate
            'user-defined operator.  If one exists, it will be supplied as an out parameter.  This
            'function will generate compile errors if the resolution is ambiguous.
            '
            'Unary operators will have only one operand.
            '
            'To select the appropriate operator, first collect all applicable operators.  If only one
            'exists, resolution is complete. If more than one exists, perform standard method overload
            'resolution to select the correct operator.  If none exist, report an error.
            '
            'See the language specification for an in-depth discussion of the algorithm.

            Debug.Assert((IsBinaryOperator(op) AndAlso arguments.Length = 2) OrElse
                         (IsUnaryOperator(op) AndAlso arguments.Length = 1),
                         "second operand supplied for a unary operator?")

            'The value Nothing is treated as the default value of the type of the other operand in a binary operator expression.
            'If one of the operands is Nothing, find the other operand's type now.  In a unary operator expression, or if both
            'operands are Nothing in a binary operator expression, the type of operation is Integer.  However, these cases
            '(necessarily involving intrinsic types) should not reach this far.

            'During normal overload resolution, Nothing matches any type.  In the context of operator overload resolution,
            'Nothing must match only the type of the other operand.  To do this, we introduce the notion of a typed Nothing.
            'We represent a typed Nothing with an instance of a special object which overload resolution uses to understand that
            'Nothing should match only one type.

            'Make a copy of the arguments so that typed Nothings don't escape from this function.
            arguments = DirectCast(arguments.Clone, Object())

            Dim leftType As Type
            Dim rightType As Type = Nothing

            If arguments(0) Is Nothing Then
                Debug.Assert(arguments.Length > 1, "unary op on Nothing unexpected here")
                Debug.Assert(arguments(1) IsNot Nothing, "binary op on Nothing operands unexpected here")

                rightType = arguments(1).GetType
                leftType = rightType
                arguments(0) = New TypedNothing(leftType)
            Else
                leftType = arguments(0).GetType

                If arguments.Length > 1 Then
                    If arguments(1) IsNot Nothing Then
                        rightType = arguments(1).GetType
                    Else
                        rightType = leftType
                        arguments(1) = New TypedNothing(rightType)
                    End If
                End If
            End If

            'First construct the list of operators we will consider.
            Dim foundLeftOperators As Boolean
            Dim foundRightOperators As Boolean
            Dim candidates As List(Of Method) =
                CollectOperators(
                    op,
                    leftType,
                    rightType,
                    foundLeftOperators,
                    foundRightOperators)

            If candidates.Count > 0 Then
                'There are operators available, so use standard method overload resolution
                'to choose the correct one.

                Dim failure As ResolutionFailure

                Return ResolveOverloadedCall(
                        OperatorNames(op),
                        candidates,
                        arguments,
                        NoArgumentNames,
                        NoTypeArguments,
                        BindingFlagsInvokeMethod,
                        reportErrors,
                        failure)
            End If

            Return Nothing
        End Function

        Friend Shared Function InvokeUserDefinedOperator(
            ByVal operatorMethod As Method,
            ByVal forceArgumentValidation As Boolean,
            ByVal ParamArray arguments As Object()) As Object

            Debug.Assert(operatorMethod IsNot Nothing, "Operator can't be nothing at this point")

            'Overload resolution will potentially select one method before validating arguments.
            'Validate those arguments now.
            If Not operatorMethod.ArgumentsValidated OrElse forceArgumentValidation Then

                If Not CanMatchArguments(operatorMethod, arguments, NoArgumentNames, NoTypeArguments, False, Nothing) Then

                    Const reportErrors As Boolean = True
                    If reportErrors Then
                        Dim errorMessage As String = ""
                        Dim errors As New List(Of String)

                        Dim result As Boolean =
                            CanMatchArguments(operatorMethod, arguments, NoArgumentNames, NoTypeArguments, False, errors)

                        Debug.Assert(result = False AndAlso errors.Count > 0, "expected this candidate to fail")

                        For Each errorString As String In errors
                            errorMessage &= vbCrLf & "    " & errorString
                        Next

                        errorMessage = GetResourceString(SR.MatchArgumentFailure2, operatorMethod.ToString, errorMessage)
                        'We are missing a member which can match the arguments, so throw a missing member exception.
                        Throw New InvalidCastException(errorMessage)
                    End If

                    Return Nothing
                End If

            End If

            Dim baseReference As Container = New Container(operatorMethod.DeclaringType)
            Return baseReference.InvokeMethod(
                    operatorMethod,
                    arguments,
                    Nothing,
                    BindingFlagsInvokeMethod)
        End Function

        Friend Shared Function InvokeUserDefinedOperator(
            ByVal op As UserDefinedOperator,
            ByVal ParamArray arguments As Object()) As Object

            If IDOUtils.TryCastToIDMOP(arguments(0)) IsNot Nothing Then
                Return IDOBinder.InvokeUserDefinedOperator(op, arguments)
            Else
                Return InvokeObjectUserDefinedOperator(op, arguments)
            End If
        End Function

        <ObsoleteAttribute("do not use this method", True)>
        <DebuggerHiddenAttribute()> <DebuggerStepThroughAttribute()>
        Public Shared Function FallbackInvokeUserDefinedOperator(
                ByVal vbOp As Object,
                ByVal arguments As Object()) As Object

            Return InvokeObjectUserDefinedOperator(CType(vbOp, UserDefinedOperator), arguments)
        End Function

        Friend Shared Function InvokeObjectUserDefinedOperator(
            ByVal op As UserDefinedOperator,
            ByVal arguments As Object()) As Object

            Dim operatorMethod As Method = ResolveUserDefinedOperator(op, arguments, True)

            If operatorMethod IsNot Nothing Then
                Return InvokeUserDefinedOperator(operatorMethod, False, arguments)
            End If

            'There are no results, so the operation is not defined for the operands.
            If arguments.Length > 1 Then
                Throw GetNoValidOperatorException(op, arguments(0), arguments(1))
            Else
                Throw GetNoValidOperatorException(op, arguments(0))
            End If
        End Function

        Friend Shared Function GetCallableUserDefinedOperator(
            ByVal op As UserDefinedOperator,
            ByVal ParamArray arguments As Object()) As Method

            Dim operatorMethod As Method = ResolveUserDefinedOperator(op, arguments, False)

            If operatorMethod IsNot Nothing Then
                'Overload resolution will potentially select one method before validating arguments.
                'Validate those arguments now.
                If Not operatorMethod.ArgumentsValidated Then
                    If Not CanMatchArguments(operatorMethod, arguments, NoArgumentNames, NoTypeArguments, False, Nothing) Then
                        Return Nothing
                    End If
                End If
            End If

            Return operatorMethod
        End Function

    End Class

End Namespace



