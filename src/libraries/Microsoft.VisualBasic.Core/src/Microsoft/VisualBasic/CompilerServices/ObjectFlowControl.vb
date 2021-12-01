' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.

Imports System
Imports System.ComponentModel
Imports System.Diagnostics.CodeAnalysis
Imports System.Reflection

Imports Microsoft.VisualBasic.CompilerServices.ConversionResolution
Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils
Imports Microsoft.VisualBasic.CompilerServices.Symbols
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.CompilerServices

    '  Provides runtime support for loop iterators on various types such as 
    '  object, decimal, etc.
    <EditorBrowsable(EditorBrowsableState.Never)>
    Public NotInheritable Class ObjectFlowControl

        Private Sub New()
        End Sub

        Public Shared Sub CheckForSyncLockOnValueType(ByVal Expression As Object)
            If Expression IsNot Nothing AndAlso Expression.GetType.IsValueType() Then
                Throw New ArgumentException(
                    SR.Format(SR.SyncLockRequiresReferenceType1, VBFriendlyName(Expression.GetType)))
            End If
        End Sub



        <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
        Public NotInheritable Class ForLoopControl
            Private _counter As Object
            Private _limit As Object
            Private _stepValue As Object
            Private _positiveStep As Boolean
            Private _enumType As Type
            Private _widestType As Type
            Private _widestTypeCode As TypeCode
            Private _useUserDefinedOperators As Boolean
            Private _operatorPlus As Method
            Private _operatorGreaterEqual As Method
            Private _operatorLessEqual As Method


            Private Sub New()
            End Sub

            <RequiresUnreferencedCode("Calls ClassifyConversion")>
            Private Shared Function GetWidestType(ByVal type1 As System.Type, ByVal type2 As System.Type) As Type
                If type1 Is Nothing OrElse type2 Is Nothing Then Return Nothing

                If Not type1.IsEnum AndAlso Not type2.IsEnum Then
                    Dim tc1 As TypeCode = GetTypeCode(type1)
                    Dim tc2 As TypeCode = GetTypeCode(type2)

                    If IsNumericType(tc1) AndAlso IsNumericType(tc2) Then
                        Return MapTypeCodeToType(ForLoopWidestTypeCode(tc1)(tc2))
                    End If
                End If

                Dim leftToRight As ConversionClass = ClassifyConversion(type2, type1, Nothing)
                If leftToRight = ConversionClass.Identity OrElse leftToRight = ConversionClass.Widening Then
                    Return type2
                End If

                Dim rightToLeft As ConversionClass = ClassifyConversion(type1, type2, Nothing)
                If rightToLeft = ConversionClass.Widening Then
                    Return type1
                End If

                Return Nothing
            End Function

            <RequiresUnreferencedCode("Calls GetWidestType")>
            Private Shared Function GetWidestType(ByVal type1 As System.Type, ByVal type2 As System.Type, ByVal type3 As System.Type) As Type
                Return GetWidestType(type1, GetWidestType(type2, type3))
            End Function

            <RequiresUnreferencedCode("Calls Conversions.ChangeType")>
            Private Shared Function ConvertLoopElement(
                    ByVal elementName As String,
                    ByVal value As Object,
                    ByVal sourceType As Type,
                    <DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)>
                    ByVal targetType As Type) As Object
                Try
                    Return Conversions.ChangeType(value, targetType)
                Catch ex As OutOfMemoryException
                    Throw ex
                Catch
                    Throw New ArgumentException(SR.Format(SR.ForLoop_ConvertToType3, elementName, VBFriendlyName(sourceType), VBFriendlyName(targetType)))
                End Try
            End Function

            <RequiresUnreferencedCode("Calls Operators.GetCallableUserDefinedOperator")>
            Private Shared Function VerifyForLoopOperator(
                ByVal op As UserDefinedOperator,
                ByVal forLoopArgument As Object,
                ByVal forLoopArgumentType As Type) As Method

                Dim operatorMethod As Method = Operators.GetCallableUserDefinedOperator(op, forLoopArgument, forLoopArgument)

                If operatorMethod Is Nothing Then
                    Throw New ArgumentException(SR.Format(
                        SR.ForLoop_OperatorRequired2,
                        VBFriendlyNameOfType(forLoopArgumentType, fullName:=True),
                        Symbols.OperatorNames(op)))
                End If

                Dim operatorInfo As MethodInfo = TryCast(operatorMethod.AsMethod, MethodInfo)
                Dim parameters As ParameterInfo() = operatorInfo.GetParameters

                ' Validate the types
                Select Case op
                    Case UserDefinedOperator.Plus, UserDefinedOperator.Minus
                        If parameters.Length <> 2 OrElse
                            parameters(0).ParameterType IsNot forLoopArgumentType OrElse
                            parameters(1).ParameterType IsNot forLoopArgumentType OrElse
                            operatorInfo.ReturnType IsNot forLoopArgumentType Then
                            Throw New ArgumentException(SR.Format(
                                SR.ForLoop_UnacceptableOperator2,
                                operatorMethod.ToString,
                                VBFriendlyNameOfType(forLoopArgumentType, fullName:=True)))
                        End If

                    Case UserDefinedOperator.LessEqual, UserDefinedOperator.GreaterEqual
                        If parameters.Length <> 2 OrElse
                            parameters(0).ParameterType IsNot forLoopArgumentType OrElse
                            parameters(1).ParameterType IsNot forLoopArgumentType Then
                            Throw New ArgumentException(SR.Format(
                                SR.ForLoop_UnacceptableRelOperator2,
                                operatorMethod.ToString,
                                VBFriendlyNameOfType(forLoopArgumentType, fullName:=True)))
                        End If
                End Select

                Return operatorMethod
            End Function

            <RequiresUnreferencedCode("The types of the parameters cannot be statically analyzed and may be trimmed")>
            Public Shared Function ForLoopInitObj(ByVal Counter As Object, ByVal Start As Object, ByVal Limit As Object, ByVal StepValue As Object, ByRef LoopForResult As Object, ByRef CounterResult As Object) As Boolean
                Dim loopFor As ForLoopControl

                If (Start Is Nothing) Then
                    Throw New ArgumentException(SR.Format(SR.Argument_InvalidNullValue1, "Start"))
                ElseIf (Limit Is Nothing) Then
                    Throw New ArgumentException(SR.Format(SR.Argument_InvalidNullValue1, "Limit"))
                ElseIf (StepValue Is Nothing) Then
                    Throw New ArgumentException(SR.Format(SR.Argument_InvalidNullValue1, "Step"))
                End If

                Dim startType As Type = Start.GetType()
                Dim limitType As Type = Limit.GetType()
                Dim stepType As Type = StepValue.GetType()

                Dim widestType As Type = GetWidestType(stepType, startType, limitType)

                If widestType Is Nothing Then
                    Throw New ArgumentException(SR.Format(SR.ForLoop_CommonType3, VBFriendlyName(startType), VBFriendlyName(limitType), VBFriendlyName(StepValue)))
                End If

                loopFor = New ForLoopControl

                Dim widestTypeCode As TypeCode = GetTypeCode(widestType)

                ' If the widest typecode is Object, try to use user defined conversions.
                If widestTypeCode = TypeCode.Object Then
                    loopFor._useUserDefinedOperators = True
                End If

                If widestTypeCode = TypeCode.String Then
                    widestTypeCode = TypeCode.Double
                End If

                Dim startTypeCode As TypeCode = startType.GetTypeCode
                Dim limitTypeCode As TypeCode = limitType.GetTypeCode
                Dim stepTypeCode As TypeCode = stepType.GetTypeCode

                ' If one or more of the three values is an enum of the same underlying
                ' type as the loop, and all of the enum types are the same, then make the type
                ' of the loop the enum.
                Dim currentEnumType As Type = Nothing

                If (startTypeCode = widestTypeCode) AndAlso startType.IsEnum Then
                    currentEnumType = startType
                End If

                If (limitTypeCode = widestTypeCode) AndAlso limitType.IsEnum Then
                    If (Not currentEnumType Is Nothing) AndAlso
                       (Not currentEnumType Is limitType) Then
                        currentEnumType = Nothing
                        GoTo NotEnumType
                    End If

                    currentEnumType = limitType
                End If

                If (stepTypeCode = widestTypeCode) AndAlso stepType.IsEnum Then
                    If (Not currentEnumType Is Nothing) AndAlso
                       (Not currentEnumType Is stepType) Then
                        currentEnumType = Nothing
                        GoTo NotEnumType
                    End If

                    currentEnumType = stepType
                End If
NotEnumType:
                loopFor._enumType = currentEnumType

                If Not loopFor._useUserDefinedOperators Then
                    loopFor._widestType = MapTypeCodeToType(widestTypeCode)
                Else
                    loopFor._widestType = widestType
                End If

                loopFor._widestTypeCode = widestTypeCode

                loopFor._counter = ConvertLoopElement("Start", Start, startType, loopFor._widestType)
                loopFor._limit = ConvertLoopElement("Limit", Limit, limitType, loopFor._widestType)
                loopFor._stepValue = ConvertLoopElement("Step", StepValue, stepType, loopFor._widestType)

                ' Verify that the required operators are present.
                If loopFor._useUserDefinedOperators Then
                    loopFor._operatorPlus = VerifyForLoopOperator(UserDefinedOperator.Plus, loopFor._counter, loopFor._widestType)
                    VerifyForLoopOperator(UserDefinedOperator.Minus, loopFor._counter, loopFor._widestType)
                    loopFor._operatorLessEqual = VerifyForLoopOperator(UserDefinedOperator.LessEqual, loopFor._counter, loopFor._widestType)
                    loopFor._operatorGreaterEqual = VerifyForLoopOperator(UserDefinedOperator.GreaterEqual, loopFor._counter, loopFor._widestType)
                End If

                'Important: a Zero step is considered Positive. This is consistent with the early-bound behavior.
                loopFor._positiveStep = Operators.ConditionalCompareObjectGreaterEqual(
                        loopFor._stepValue,
                        Operators.SubtractObject(loopFor._stepValue, loopFor._stepValue),
                        False)

                LoopForResult = loopFor

                If Not loopFor._enumType Is Nothing Then
                    CounterResult = System.Enum.ToObject(loopFor._enumType, loopFor._counter)
                Else
                    CounterResult = loopFor._counter
                End If

                Return CheckContinueLoop(loopFor)
            End Function

            <RequiresUnreferencedCode("The types of the parameters cannot be statically analyzed and may be trimmed")>
            Public Shared Function ForNextCheckObj(ByVal Counter As Object, ByVal LoopObj As Object, ByRef CounterResult As Object) As Boolean

                Dim loopFor As ForLoopControl

                If LoopObj Is Nothing Then
                    Throw VbMakeIllegalForException()
                End If

                If Counter Is Nothing Then
                    Throw New NullReferenceException(SR.Format(SR.Argument_InvalidNullValue1, "Counter"))
                End If

                loopFor = CType(LoopObj, ForLoopControl)

                Dim needToChangeType As Boolean = False

                If Not loopFor._useUserDefinedOperators Then
                    ' At this point, we know it's IConvertible
                    Dim counterTypeCode As TypeCode = Counter.GetType.GetTypeCode

                    If counterTypeCode <> loopFor._widestTypeCode OrElse counterTypeCode = TypeCode.String Then
                        If counterTypeCode = TypeCode.Object Then
                            Throw New ArgumentException(SR.Format(SR.ForLoop_CommonType2, VBFriendlyName(MapTypeCodeToType(counterTypeCode)), VBFriendlyName(loopFor._widestType)))
                        Else
                            Dim widestType As Type = GetWidestType(MapTypeCodeToType(counterTypeCode), loopFor._widestType)
                            Dim widestTypeCode As TypeCode = GetTypeCode(widestType)

                            If widestTypeCode = TypeCode.String Then
                                widestTypeCode = TypeCode.Double
                            End If

                            loopFor._widestTypeCode = widestTypeCode
                            loopFor._widestType = MapTypeCodeToType(widestTypeCode)
                            needToChangeType = True
                        End If
                    End If
                End If

                If needToChangeType OrElse loopFor._useUserDefinedOperators Then
                    Counter = ConvertLoopElement("Start", Counter, Counter.GetType(), loopFor._widestType)

                    If Not loopFor._useUserDefinedOperators Then
                        loopFor._limit = ConvertLoopElement("Limit", loopFor._limit, loopFor._limit.GetType(), loopFor._widestType)
                        loopFor._stepValue = ConvertLoopElement("Step", loopFor._stepValue, loopFor._stepValue.GetType(), loopFor._widestType)
                    End If
                End If

                If Not loopFor._useUserDefinedOperators Then
                    loopFor._counter = Operators.AddObject(Counter, loopFor._stepValue)

                    Dim resultTypeCode As TypeCode = loopFor._counter.GetType.GetTypeCode ' CType(LoopFor.Counter, IConvertible).GetTypeCode()

                    If Not loopFor._enumType Is Nothing Then
                        CounterResult = System.Enum.ToObject(loopFor._enumType, loopFor._counter)
                    Else
                        CounterResult = loopFor._counter
                    End If

                    If resultTypeCode <> loopFor._widestTypeCode Then
                        'Overflow to bigger type occurred
                        loopFor._limit = Conversions.ChangeType(loopFor._limit, MapTypeCodeToType(resultTypeCode))
                        loopFor._stepValue = Conversions.ChangeType(loopFor._stepValue, MapTypeCodeToType(resultTypeCode))
                        'If we overflow, then we should always be at the end of the loop
                        Return False
                    End If
                Else
                    ' Execute addition.
                    loopFor._counter = Operators.InvokeUserDefinedOperator(
                        loopFor._operatorPlus,
                        True,
                        Counter,
                        loopFor._stepValue)

                    If loopFor._counter.GetType() IsNot loopFor._widestType Then
                        loopFor._counter = ConvertLoopElement("Start", loopFor._counter, loopFor._counter.GetType(), loopFor._widestType)
                    End If

                    CounterResult = loopFor._counter
                End If

                Return CheckContinueLoop(loopFor)
            End Function

            Public Shared Function ForNextCheckR4(ByVal count As Single, ByVal limit As Single, ByVal StepValue As Single) As Boolean
                'Important: a Zero step is considered Positive. This is consistent with integral For loops.
                If StepValue >= 0 Then
                    Return count <= limit
                Else
                    Return count >= limit
                End If
            End Function

            Public Shared Function ForNextCheckR8(ByVal count As Double, ByVal limit As Double, ByVal StepValue As Double) As Boolean
                'Important: a Zero step is considered Positive. This is consistent with integral For loops.
                If StepValue >= 0 Then
                    Return count <= limit
                Else
                    Return count >= limit
                End If
            End Function

            Public Shared Function ForNextCheckDec(ByVal count As Decimal, ByVal limit As Decimal, ByVal StepValue As Decimal) As Boolean
                'Important: a Zero step is considered Positive. This is consistent with integral For loops.
                If StepValue >= 0 Then
                    Return count <= limit
                Else
                    Return count >= limit
                End If
            End Function

            <RequiresUnreferencedCode("Calls Operators.InvokeUserDefinedOperator")>
            Private Shared Function CheckContinueLoop(ByVal loopFor As ForLoopControl) As Boolean

                If Not loopFor._useUserDefinedOperators Then
                    Dim icompare As IComparable
                    Dim compareResult As Integer

                    Try
                        icompare = CType(loopFor._counter, IComparable)
                        compareResult = icompare.CompareTo(loopFor._limit)

                        If loopFor._positiveStep Then
                            Return compareResult <= 0
                        Else
                            Return compareResult >= 0
                        End If

                    Catch ex As InvalidCastException
                        Throw New ArgumentException(SR.Format(SR.Argument_IComparable2, "loop control variable", VBFriendlyName(loopFor._counter)))
                    End Try
                Else
                    If loopFor._positiveStep Then
                        Return CBool(Operators.InvokeUserDefinedOperator(
                            loopFor._operatorLessEqual,
                            True,
                            loopFor._counter,
                            loopFor._limit))
                    Else
                        Return CBool(Operators.InvokeUserDefinedOperator(
                            loopFor._operatorGreaterEqual,
                            True,
                            loopFor._counter,
                            loopFor._limit))
                    End If
                End If
            End Function

        End Class
    End Class
End Namespace
