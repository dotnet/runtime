' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Option Strict On

Imports System
Imports System.Reflection
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Text

Imports Microsoft.VisualBasic.CompilerServices.Symbols
Imports Microsoft.VisualBasic.CompilerServices.ConversionResolution
Imports Microsoft.VisualBasic.CompilerServices.Utils
Imports Microsoft.VisualBasic.CompilerServices.ReflectionExtensions

#Const BINDING_LOG = False
#Const GENERICITY_LOG = False

Namespace Microsoft.VisualBasic.CompilerServices

    ' Implements VB method overloading semantics.
    Friend Class OverloadResolution
        ' Prevent creation.
        Private Sub New()
        End Sub

        Friend Enum ResolutionFailure
            None
            MissingMember
            InvalidArgument
            AmbiguousMatch
            InvalidTarget
        End Enum


        'perhaps this could go into the Symbols utility module
        Private Shared Function IsExactSignatureMatch(
            ByVal leftSignature As ParameterInfo(),
            ByVal leftTypeParameterCount As Integer,
            ByVal rightSignature As ParameterInfo(),
            ByVal rightTypeParameterCount As Integer) As Boolean

            Dim longerSignature As ParameterInfo()
            Dim shorterSignature As ParameterInfo()

            If leftSignature.Length >= rightSignature.Length Then
                longerSignature = leftSignature
                shorterSignature = rightSignature
            Else
                longerSignature = rightSignature
                shorterSignature = leftSignature
            End If

            'If the signatures differ in length, then the extra parameters of the
            'longer signature must all be optional to be an exact match.

            For index As Integer = shorterSignature.Length To longerSignature.Length - 1
                If Not longerSignature(index).IsOptional Then
                    Return False
                End If
            Next

            For i As Integer = 0 To shorterSignature.Length - 1

                Dim type1 As Type = shorterSignature(i).ParameterType
                Dim type2 As Type = longerSignature(i).ParameterType

                If type1.IsByRef Then type1 = type1.GetElementType
                If type2.IsByRef Then type2 = type2.GetElementType

                If type1 IsNot type2 AndAlso
                   (Not shorterSignature(i).IsOptional OrElse
                    Not longerSignature(i).IsOptional) Then
                    Return False
                End If
            Next

            Return True
        End Function

        Private Enum ComparisonType
            ParameterSpecificty
            GenericSpecificityBasedOnMethodGenericParams
            GenericSpecificityBasedOnTypeGenericParams
        End Enum

        Private Shared Sub CompareNumericTypeSpecificity(
            ByVal leftType As Type,
            ByVal rightType As Type,
            ByRef leftWins As Boolean,
            ByRef rightWins As Boolean)

            'This function implements the notion that signed types are
            'preferred over unsigned types during overload resolution.

            Debug.Assert(IsNumericType(leftType) AndAlso Not IsEnum(leftType) AndAlso
                         IsNumericType(rightType) AndAlso Not IsEnum(rightType),
                         "expected only numerics here. : #12/10/2003#")

            If leftType Is rightType Then
                'Do nothing since neither wins.
            Else
                Debug.Assert(GetTypeCode(leftType) <> GetTypeCode(rightType),
                             "this should have been caught above")

                If NumericSpecificityRank(GetTypeCode(leftType)) <
                        NumericSpecificityRank(GetTypeCode(rightType)) Then
                    leftWins = True
                Else
                    rightWins = True
                End If
            End If

            Return
        End Sub

        Private Shared Sub CompareParameterSpecificity(
            ByVal argumentType As Type,
            ByVal leftParameter As ParameterInfo,
            ByVal leftProcedure As MethodBase,
            ByVal expandLeftParamArray As Boolean,
            ByVal rightParameter As ParameterInfo,
            ByVal rightProcedure As MethodBase,
            ByVal expandRightParamArray As Boolean,
            ByRef leftWins As Boolean,
            ByRef rightWins As Boolean,
            ByRef bothLose As Boolean)


            bothLose = False
            Dim leftType As Type = leftParameter.ParameterType
            Dim rightType As Type = rightParameter.ParameterType

            If leftType.IsByRef Then leftType = GetElementType(leftType)
            If rightType.IsByRef Then rightType = GetElementType(rightType)

            If expandLeftParamArray AndAlso IsParamArray(leftParameter) Then
                leftType = GetElementType(leftType)
            End If

            If expandRightParamArray AndAlso IsParamArray(rightParameter) Then
                rightType = GetElementType(rightType)
            End If

            If IsNumericType(leftType) AndAlso IsNumericType(rightType) AndAlso
               Not IsEnum(leftType) AndAlso Not IsEnum(rightType) Then

                CompareNumericTypeSpecificity(leftType, rightType, leftWins, rightWins)
                Return
            End If

            'If both the types are different only by generic method type parameters
            'with the same index position, then treat as identity.

            If leftProcedure IsNot Nothing AndAlso
               rightProcedure IsNot Nothing AndAlso
               IsRawGeneric(leftProcedure) AndAlso
               IsRawGeneric(rightProcedure) Then

                If leftType Is rightType Then Return 'Check this first--shortcut.

                Dim leftIndex As Integer = IndexIn(leftType, leftProcedure)
                Dim rightIndex As Integer = IndexIn(rightType, rightProcedure)

                If leftIndex = rightIndex AndAlso leftIndex >= 0 Then Return
            End If

            Dim operatorMethod As Method = Nothing
            Dim leftToRight As ConversionClass = ClassifyConversion(rightType, leftType, operatorMethod)

            If leftToRight = ConversionClass.Identity Then Return

            If leftToRight = ConversionClass.Widening Then

                If operatorMethod IsNot Nothing AndAlso
                   ClassifyConversion(leftType, rightType, operatorMethod) = ConversionClass.Widening Then

                    ' Although W<-->W conversions don't exist in the set of predefined conversions,
                    ' it can occur with user-defined conversions.  If the two param types widen to each other
                    ' (necessarily by using user-defined conversions), and the argument type is known and
                    ' is identical to one of the parameter types, then that parameter wins.  Otherwise,
                    ' if the arugment type is not specified, we can't make a decision and both lose.

                    If argumentType IsNot Nothing AndAlso argumentType Is leftType Then
                        leftWins = True
                        Return
                    End If

                    If argumentType IsNot Nothing AndAlso argumentType Is rightType Then
                        rightWins = True
                        Return
                    End If

                    bothLose = True
                    Return

                End If

                leftWins = True
                Return
            End If

            Dim rightToLeft As ConversionClass = ClassifyConversion(leftType, rightType, operatorMethod)

            If rightToLeft = ConversionClass.Widening Then
                rightWins = True
                Return
            End If

            bothLose = True
            Return

        End Sub

        Private Shared Sub CompareGenericityBasedOnMethodGenericParams(
            ByVal leftParameter As ParameterInfo,
            ByVal rawLeftParameter As ParameterInfo,
            ByVal leftMember As Method,
            ByVal expandLeftParamArray As Boolean,
            ByVal rightParameter As ParameterInfo,
            ByVal rawRightParameter As ParameterInfo,
            ByVal rightMember As Method,
            ByVal expandRightParamArray As Boolean,
            ByRef leftIsLessGeneric As Boolean,
            ByRef rightIsLessGeneric As Boolean,
            ByRef signatureMismatch As Boolean)

            If Not leftMember.IsMethod OrElse Not rightMember.IsMethod Then
                Return
            End If

            Dim leftType As Type = leftParameter.ParameterType
            Dim rightType As Type = rightParameter.ParameterType

            'Since generic methods are instantiated by this point, the parameter
            'types are bound. However, we need to compare against the unbound types.
            Dim rawLeftType As Type = rawLeftParameter.ParameterType
            Dim rawRightType As Type = rawRightParameter.ParameterType

            If leftType.IsByRef Then
                leftType = GetElementType(leftType)
                rawLeftType = GetElementType(rawLeftType)
            End If

            If rightType.IsByRef Then
                rightType = GetElementType(rightType)
                rawRightType = GetElementType(rawRightType)
            End If

            If expandLeftParamArray AndAlso IsParamArray(leftParameter) Then
                leftType = GetElementType(leftType)
                rawLeftType = GetElementType(rawLeftType)
            End If

            If expandRightParamArray AndAlso IsParamArray(rightParameter) Then
                rightType = GetElementType(rightType)
                rawRightType = GetElementType(rawRightType)
            End If
            If leftType IsNot rightType Then
                'The signatures of the two methods are not identical and so the "least generic" rule
                'does not apply.
                signatureMismatch = True
                Return
            End If

            Dim leftProcedure As MethodBase = leftMember.AsMethod
            Dim rightProcedure As MethodBase = rightMember.AsMethod

            If IsGeneric(leftProcedure) Then leftProcedure = DirectCast(leftProcedure, MethodInfo).GetGenericMethodDefinition
            If IsGeneric(rightProcedure) Then rightProcedure = DirectCast(rightProcedure, MethodInfo).GetGenericMethodDefinition

            ' Only references to generic parameters of the procedures count. For the purpose of this
            ' function, references to generic parameters of a type do not make a procedure more generic.

            If RefersToGenericParameter(rawLeftType, leftProcedure) Then
                If Not RefersToGenericParameter(rawRightType, rightProcedure) Then
                    rightIsLessGeneric = True
                End If
            ElseIf RefersToGenericParameter(rawRightType, rightProcedure) Then
                If Not RefersToGenericParameter(rawLeftType, leftProcedure) Then
                    leftIsLessGeneric = True
                End If
            End If

        End Sub

        Private Shared Sub CompareGenericityBasedOnTypeGenericParams(
            ByVal leftParameter As ParameterInfo,
            ByVal rawLeftParameter As ParameterInfo,
            ByVal leftMember As Method,
            ByVal expandLeftParamArray As Boolean,
            ByVal rightParameter As ParameterInfo,
            ByVal rawRightParameter As ParameterInfo,
            ByVal rightMember As Method,
            ByVal expandRightParamArray As Boolean,
            ByRef leftIsLessGeneric As Boolean,
            ByRef rightIsLessGeneric As Boolean,
            ByRef signatureMismatch As Boolean)

            Dim leftType As Type = leftParameter.ParameterType
            Dim rightType As Type = rightParameter.ParameterType

            'Since generic methods are instantiated by this point, the parameter
            'types are bound. However, we need to compare against the unbound types.
            Dim rawLeftType As Type = rawLeftParameter.ParameterType
            Dim rawRightType As Type = rawRightParameter.ParameterType

            If leftType.IsByRef Then
                leftType = GetElementType(leftType)
                rawLeftType = GetElementType(rawLeftType)
            End If

            If rightType.IsByRef Then
                rightType = GetElementType(rightType)
                rawRightType = GetElementType(rawRightType)
            End If

            If expandLeftParamArray AndAlso IsParamArray(leftParameter) Then
                leftType = GetElementType(leftType)
                rawLeftType = GetElementType(rawLeftType)
            End If

            If expandRightParamArray AndAlso IsParamArray(rightParameter) Then
                rightType = GetElementType(rightType)
                rawRightType = GetElementType(rawRightType)
            End If

            'Need to check type equivalency for the NoPIA case
            If leftType IsNot rightType Then
                'The signatures of the two methods are not identical and so the "least generic" rule
                'does not apply.
                signatureMismatch = True
                Return
            End If

            ' Only references to generic parameters of the generic types count. For the purpose of this
            ' function, references to generic parameters of a method do not make a procedure more generic.
            '
            Dim leftDeclaringType As Type = leftMember.RawDeclaringType
            Dim rightDeclaringType As Type = rightMember.RawDeclaringType

#If GENERICITY_LOG Then
            Console.Writeline("----------CompareGenericityBasedOnTypeGenericParams---------")
            Console.Writeline("LeftType: " & LeftType.MetaDataToken & " - " & LeftType.ToString())
            Console.Writeline("LeftRawType: " & RawLeftType.MetaDataToken & " - " & RawLeftType.ToString())
            Console.Writeline("LeftDeclaringType: " & LeftDeclaringType.MetaDataToken & " - " & LeftDeclaringType.ToString())
            Console.Writeline("RightType: " & RightType.MetaDataToken & " - " & RightType.ToString())
            Console.Writeline("RightRawType: " & RawRightType.MetaDataToken & " - " & RawRightType.ToString())
            Console.Writeline("RightDeclaringType: " & RightDeclaringType.MetaDataToken & " - " & RightDeclaringType.ToString())
#End If

            If RefersToGenericParameterCLRSemantics(rawLeftType, leftDeclaringType) Then
                If Not RefersToGenericParameterCLRSemantics(rawRightType, rightDeclaringType) Then
                    rightIsLessGeneric = True
                End If
            ElseIf RefersToGenericParameterCLRSemantics(rawRightType, rightDeclaringType) Then
                leftIsLessGeneric = True
            End If
        End Sub

        Private Shared Function LeastGenericProcedure(
            ByVal left As Method,
            ByVal right As Method,
            ByVal compareGenericity As ComparisonType,
            ByRef signatureMismatch As Boolean) As Method

            Dim leftWinsAtLeastOnce As Boolean = False
            Dim rightWinsAtLeastOnce As Boolean = False
            signatureMismatch = False

            If Not left.IsMethod OrElse Not right.IsMethod Then Return Nothing

            Dim paramIndex As Integer = 0
            Dim leftParamsLen As Integer = left.Parameters.Length
            Dim rightParamsLen As Integer = right.Parameters.Length

            Do While paramIndex < leftParamsLen AndAlso paramIndex < rightParamsLen

                Select Case compareGenericity
                    Case ComparisonType.GenericSpecificityBasedOnMethodGenericParams

                        CompareGenericityBasedOnMethodGenericParams(
                            left.Parameters(paramIndex),
                            left.RawParameters(paramIndex),
                            left,
                            left.ParamArrayExpanded,
                            right.Parameters(paramIndex),
                            right.RawParameters(paramIndex),
                            right,
                            False,
                            leftWinsAtLeastOnce,
                            rightWinsAtLeastOnce,
                            signatureMismatch)

                    Case ComparisonType.GenericSpecificityBasedOnTypeGenericParams

                        CompareGenericityBasedOnTypeGenericParams(
                            left.Parameters(paramIndex),
                            left.RawParameters(paramIndex),
                            left,
                            left.ParamArrayExpanded,
                            right.Parameters(paramIndex),
                            right.RawParameters(paramIndex),
                            right,
                            False,
                            leftWinsAtLeastOnce,
                            rightWinsAtLeastOnce,
                            signatureMismatch)

                    Case Else
                        Debug.Assert(False, "Unexpected comparison type!!!")
                End Select

                If signatureMismatch OrElse (leftWinsAtLeastOnce AndAlso rightWinsAtLeastOnce) Then
                    Return Nothing
                End If

                paramIndex += 1
            Loop

            Debug.Assert(Not (leftWinsAtLeastOnce AndAlso rightWinsAtLeastOnce),
                         "Least generic method logic is confused.")

            If paramIndex < leftParamsLen OrElse paramIndex < rightParamsLen Then
                'The procedures have different numbers of parameters, and so don't have matching signatures.
                Return Nothing
            End If

            If leftWinsAtLeastOnce Then
                Return left
            End If

            If rightWinsAtLeastOnce Then
                Return right
            End If

            Return Nothing
        End Function

        Friend Shared Function LeastGenericProcedure(
            ByVal left As Method,
            ByVal right As Method) As Method

            If Not (left.IsGeneric OrElse
                    right.IsGeneric OrElse
                    IsGeneric(left.DeclaringType) OrElse
                    IsGeneric(right.DeclaringType)) Then
                Return Nothing
            End If

            Dim signatureMismatch As Boolean = False

            Dim leastGeneric As Method =
                LeastGenericProcedure(
                    left,
                    right,
                    ComparisonType.GenericSpecificityBasedOnMethodGenericParams,
                    signatureMismatch)

            If leastGeneric Is Nothing AndAlso Not signatureMismatch Then

                leastGeneric =
                    LeastGenericProcedure(
                        left,
                        right,
                        ComparisonType.GenericSpecificityBasedOnTypeGenericParams,
                        signatureMismatch)
            End If

            Return leastGeneric

        End Function

        Private Shared Sub InsertIfMethodAvailable(
            ByVal newCandidate As MemberInfo,
            ByVal newCandidateSignature As ParameterInfo(),
            ByVal newCandidateParamArrayIndex As Integer,
            ByVal expandNewCandidateParamArray As Boolean,
            ByVal arguments As Object(),
            ByVal argumentCount As Integer,
            ByVal argumentNames As String(),
            ByVal typeArguments As Type(),
            ByVal collectOnlyOperators As Boolean,
            ByVal candidates As List(Of Method),
            ByVal baseReference As Container)

            'Note that Arguments, ArgumentNames and TypeNames will be nothing when collecting operators.
            '
            Debug.Assert(arguments Is Nothing OrElse arguments.Length = argumentCount, "Inconsistency in arguments!!!")

            Dim newCandidateNode As Method = Nothing

            'If we're collecting only operators, then hiding by name and signature doesn't apply (neither do
            'ParamArrays), so skip all of this logic.
            If Not collectOnlyOperators Then

                Dim newCandidateMethod As MethodBase = TryCast(newCandidate, MethodBase)
                Dim inferenceFailedForNewCandidate As Boolean = False

                ' Note that operators cannot be generic methods
                '
                ' Need to complete type argument inference for generic methods when no type arguments
                ' have been supplied and when type arguments are supplied, the generic method needs to
                ' to be instantiated. Need to complete this so early in the overload process so that
                ' hid-by-sig, paramarray disambiguation etc. are done using the substitued signature.
                '
                If newCandidate.MemberType = MemberTypes.Method AndAlso IsRawGeneric(newCandidateMethod) Then

                    newCandidateNode =
                        New Method(
                            newCandidateMethod,
                            newCandidateSignature,
                            newCandidateParamArrayIndex,
                            expandNewCandidateParamArray)

                    ' Inferring of type arguments is done when when determining the callability of this
                    ' procedure with these arguments by comparing them against the corresponding parameters.
                    '
                    ' Note that although RejectUncallableProcedure needs to be invoked on the non-generics
                    ' candidates too, it is not done here because some of the candidates might be rejected
                    ' for various reasons like hide-by-sig, paramarray disambiguation, etc. and RejectUncall-
                    ' -ableProcedure would not have to be invoked for them. This is especially important
                    ' because the RejectUncallableProcedure task is expensive.
                    '

                    RejectUncallableProcedure(
                        newCandidateNode,
                        arguments,
                        argumentNames,
                        typeArguments)


                    ' Get the instantiated method for this candidate
                    '
                    newCandidate = newCandidateNode.AsMethod
                    newCandidateSignature = newCandidateNode.Parameters

                End If

                ' Verify if TypeInference succeeded. This should only happen for Methods
                If newCandidate IsNot Nothing AndAlso
                    newCandidate.MemberType = MemberTypes.Method AndAlso
                    IsRawGeneric(TryCast(newCandidate, MethodBase)) Then
                    inferenceFailedForNewCandidate = True
                End If

                For index As Integer = 0 To candidates.Count - 1

                    Dim existing As Method = candidates.Item(index)
                    If existing Is Nothing Then Continue For 'This item was killed earlier, so skip it.

                    Dim existingCandidateSignature As ParameterInfo() = existing.Parameters
                    Dim existingCandidate As MethodBase
                    If existing.IsMethod Then existingCandidate = existing.AsMethod Else existingCandidate = Nothing

                    If newCandidate = existing Then Continue For

                    Dim newCandidateParameterIndex As Integer = 0
                    Dim existingCandidateParameterIndex As Integer = 0

                    For currentArgument As Integer = 1 To argumentCount

                        Dim bothLose As Boolean = False
                        Dim newCandidateWins As Boolean = False
                        Dim existingCandidateWins As Boolean = False

                        CompareParameterSpecificity(
                            Nothing,
                            newCandidateSignature(newCandidateParameterIndex),
                            newCandidateMethod,
                            expandNewCandidateParamArray,
                            existingCandidateSignature(existingCandidateParameterIndex),
                            existingCandidate,
                            existing.ParamArrayExpanded,
                            newCandidateWins,
                            existingCandidateWins,
                            bothLose)

                        If bothLose Or newCandidateWins Or existingCandidateWins Then
                            GoTo continueloop
                        End If

                        'If a parameter is a param array, there is no next parameter and so advancing
                        'through the parameter list is bad.

                        If newCandidateParameterIndex <> newCandidateParamArrayIndex OrElse Not expandNewCandidateParamArray Then
                            newCandidateParameterIndex += 1
                        End If

                        If existingCandidateParameterIndex <> existing.ParamArrayIndex OrElse Not existing.ParamArrayExpanded Then
                            existingCandidateParameterIndex += 1
                        End If

                    Next

                    Dim exactSignature As Boolean =
                        IsExactSignatureMatch(
                            newCandidateSignature,
                            GetTypeParameters(newCandidate).Length,
                            existing.Parameters,
                            existing.TypeParameters.Length)

                    If Not exactSignature Then

                        ' If inference failed for any of the candidates, then don't compare them.
                        '
                        ' This simple strategy besides fixing the problems associated with an inference
                        ' failed candidate beating a inference passing candidate also helps with better
                        ' error reporting by showing the inference failed candidates too.
                        '
                        If inferenceFailedForNewCandidate OrElse
                           (existingCandidate IsNot Nothing AndAlso IsRawGeneric(existingCandidate)) Then
                            Continue For
                        End If


                        If Not expandNewCandidateParamArray AndAlso existing.ParamArrayExpanded Then
                            'Delete current item from list and continue.
                            candidates.Item(index) = Nothing
                            Continue For

                        ElseIf expandNewCandidateParamArray AndAlso Not existing.ParamArrayExpanded Then
                            Return

                        ElseIf Not expandNewCandidateParamArray AndAlso Not existing.ParamArrayExpanded Then
                            'In theory, this shouldn't happen, but another language could
                            'theoretically define two methods with optional arguments that
                            'end up being equivalent. So don't prefer one over the other.
                            Continue For

                        Else
                            'If both are expanded, then see if one uses more on actual
                            'parameters than the other. If so, we prefer the one that uses
                            'more on actual parameters.

                            If (newCandidateParameterIndex > existingCandidateParameterIndex) Then
                                'Delete current item from list and continue.
                                candidates.Item(index) = Nothing
                                Continue For

                            ElseIf existingCandidateParameterIndex > newCandidateParameterIndex Then
                                Return

                            End If

                            Continue For

                        End If
                    Else
                        Debug.Assert((baseReference IsNot Nothing AndAlso baseReference.IsWindowsRuntimeObject) OrElse
                                     IsOrInheritsFrom(existing.DeclaringType, newCandidate.DeclaringType),
                                     "expected inheritance or WinRT collection types here")

                        If newCandidate.DeclaringType Is existing.DeclaringType Then
                            'If the two members are declared in the same container, both should be added to the set
                            'of overloads.  This results in intelligent ambiguity error messages.
                            Exit For

                        End If

                        ' If the base container is a WinRT object implementing collection interfaces they could have
                        ' the same method name with the same signature. We need to add them to the set to throw 
                        ' ambiguity errors.
                        If baseReference IsNot Nothing AndAlso baseReference.IsWindowsRuntimeObject() AndAlso
                           Symbols.IsCollectionInterface(newCandidate.DeclaringType) AndAlso
                           Symbols.IsCollectionInterface(existing.DeclaringType) Then
                            Exit For
                        End If

                        'If inference did not fail for the base candidate, but failed for the derived candidate, then
                        'the derived candidate cannot hide the base candidate. VSWhidbey Bug 369042.
                        '
                        If Not inferenceFailedForNewCandidate AndAlso
                           (existingCandidate IsNot Nothing AndAlso IsRawGeneric(existingCandidate)) Then
                            Continue For
                        End If

                        Return

                    End If

                    Debug.Assert(False, "unexpected code path")

continueloop:
                Next
            End If

            If newCandidateNode IsNot Nothing Then
                candidates.Add(newCandidateNode)
            ElseIf newCandidate.MemberType = MemberTypes.Property Then
                candidates.Add(
                    New Method(
                        DirectCast(newCandidate, PropertyInfo),
                        newCandidateSignature,
                        newCandidateParamArrayIndex,
                        expandNewCandidateParamArray))
            Else
                candidates.Add(
                    New Method(
                        DirectCast(newCandidate, MethodBase),
                        newCandidateSignature,
                        newCandidateParamArrayIndex,
                        expandNewCandidateParamArray))
            End If

        End Sub

        Friend Shared Function CollectOverloadCandidates(
            ByVal members As MemberInfo(),
            ByVal arguments As Object(),
            ByVal argumentCount As Integer,
            ByVal argumentNames As String(),
            ByVal typeArguments As Type(),
            ByVal collectOnlyOperators As Boolean,
            ByVal terminatingScope As System.Type,
            ByRef rejectedForArgumentCount As Integer,
            ByRef rejectedForTypeArgumentCount As Integer,
            ByVal baseReference As Container) As List(Of Method)


            'Note that Arguments, ArgumentNames and TypeNames will be nothing when collecting operators.
            '
            Debug.Assert(arguments Is Nothing OrElse arguments.Length = argumentCount, "Inconsistency in arguments!!!")

            Dim typeArgumentCount As Integer = 0
            If typeArguments IsNot Nothing Then
                typeArgumentCount = typeArguments.Length
            End If

            Dim candidates As List(Of Method) = New List(Of Method)(members.Length)

            If members.Length = 0 Then
                Return candidates
            End If

            Dim keepSearching As Boolean = True
            Dim index As Integer = 0

            Do
                Dim currentScope As Type = members(index).DeclaringType

                'The terminating scope parameter controls at which point candidate collection
                'will stop. This is useful for overloaded operator resolution where the left
                'and right operands may have a common ancestor and we wish to collect the common
                'candidates only once.
                If terminatingScope IsNot Nothing AndAlso IsOrInheritsFrom(terminatingScope, currentScope) Then Exit Do
                Do
                    Dim candidate As MemberInfo = members(index)
                    Dim candidateSignature As ParameterInfo() = Nothing
                    Dim typeParameterCount As Integer = 0

                    Select Case candidate.MemberType

                        Case MemberTypes.Constructor,
                             MemberTypes.Method

                            Dim currentMethod As MethodBase = DirectCast(candidate, MethodBase)

                            If collectOnlyOperators AndAlso Not IsUserDefinedOperator(currentMethod) Then
                                GoTo nextcandidate
                            End If

                            candidateSignature = currentMethod.GetParameters
                            typeParameterCount = GetTypeParameters(currentMethod).Length

                            If IsShadows(currentMethod) Then keepSearching = False

                        Case MemberTypes.Property

                            If collectOnlyOperators Then GoTo nextcandidate

                            Dim propertyBlock As PropertyInfo = DirectCast(candidate, PropertyInfo)
                            Dim getMethod As MethodInfo = propertyBlock.GetGetMethod

                            If getMethod IsNot Nothing Then
                                candidateSignature = getMethod.GetParameters

                                Debug.Assert(propertyBlock.GetSetMethod Is Nothing OrElse
                                             IsShadows(propertyBlock.GetSetMethod) = IsShadows(getMethod),
                                             "unexpected mismatched shadows on accessors")
                                If IsShadows(getMethod) Then keepSearching = False

                            Else
                                Dim setMethod As MethodInfo = propertyBlock.GetSetMethod
                                Debug.Assert(setMethod IsNot Nothing, "must have set here")

                                Dim setParameters As ParameterInfo() = setMethod.GetParameters
                                candidateSignature = New ParameterInfo(setParameters.Length - 2) {}
                                System.Array.Copy(setParameters, candidateSignature, candidateSignature.Length)

                                If IsShadows(setMethod) Then keepSearching = False

                            End If

                        Case MemberTypes.Custom,
                             MemberTypes.Event,
                             MemberTypes.Field,
                             MemberTypes.TypeInfo,
                             MemberTypes.NestedType

                            'All of these items automatically shadow.
                            If Not collectOnlyOperators Then
                                keepSearching = False
                            End If
                            GoTo nextcandidate

                        Case Else
                            Debug.Assert(False, "what is this?  just ignore it.")
                            GoTo nextcandidate

                    End Select


                    'We have a possible candidate method if we make it this far.  Insert it into the
                    'list if it qualifies.

                    Debug.Assert(candidateSignature IsNot Nothing, "must have signature if we have a method")

                    Dim requiredParameterCount As Integer = 0
                    Dim maximumParameterCount As Integer = 0
                    Dim paramArrayIndex As Integer = -1

                    'Weed out procedures that cannot accept the number of supplied arguments.

                    GetAllParameterCounts(candidateSignature, requiredParameterCount, maximumParameterCount, paramArrayIndex)

                    Dim hasParamArray As Boolean = paramArrayIndex >= 0
                    If argumentCount < requiredParameterCount OrElse
                       (Not hasParamArray AndAlso argumentCount > maximumParameterCount) Then
                        rejectedForArgumentCount += 1
                        GoTo nextcandidate
                    End If

                    'If type arguments have been supplied, weed out procedures that don't have an
                    'appropriate number of type parameters.

                    If typeArgumentCount > 0 AndAlso typeArgumentCount <> typeParameterCount Then
                        rejectedForTypeArgumentCount += 1
                        GoTo nextcandidate
                    End If

                    ' A method with a paramarray can be considered in two forms: in an
                    ' expanded form or in an unexpanded form (i.e. as if the paramarray
                    ' decoration was not specified). Weirdly, it can apply in both forms, as 
                    ' in the case of passing Object() to ParamArray x As Object() (because
                    ' Object() converts to both Object() and Object).

                    ' Does the method apply in its unexpanded form? This can only happen if
                    ' either there is no paramarray or if the argument count matches exactly
                    ' (if it's less, then the paramarray is expanded to nothing, if it's more, 
                    ' it's expanded to one or more parameters).

                    If Not hasParamArray OrElse argumentCount = maximumParameterCount Then
                        InsertIfMethodAvailable(
                            candidate,
                            candidateSignature,
                            paramArrayIndex,
                            False,
                            arguments,
                            argumentCount,
                            argumentNames,
                            typeArguments,
                            collectOnlyOperators,
                            candidates,
                            baseReference)
                    End If

                    'How about it's expanded form? It always applies if there's a paramarray.

                    If hasParamArray Then
                        Debug.Assert(Not collectOnlyOperators, "didn't expect operator with paramarray")
                        InsertIfMethodAvailable(
                            candidate,
                            candidateSignature,
                            paramArrayIndex,
                            True,
                            arguments,
                            argumentCount,
                            argumentNames,
                            typeArguments,
                            collectOnlyOperators,
                            candidates,
                            baseReference)
                    End If

nextcandidate:
                    index += 1

                Loop While index < members.Length AndAlso members(index).DeclaringType Is currentScope

            Loop While keepSearching AndAlso index < members.Length

#If BINDING_LOG Then
            Console.WriteLine("== COLLECTION AND SHADOWING ==")
            For Each item As Method In Candidates
                If item Is Nothing Then
                    Console.WriteLine("dead")
                Else
                    Console.WriteLine(item.DumpContents)
                End If
            Next
#End If

            'Remove the dead entries from the list--simplifies code later on.
            index = 0
            While index < candidates.Count
                If candidates(index) Is Nothing Then
                    Dim span As Integer = index + 1
                    While span < candidates.Count AndAlso candidates(span) Is Nothing
                        span += 1
                    End While
                    candidates.RemoveRange(index, span - index)
                End If
                index += 1
            End While

            Return candidates
        End Function

        Private Shared Function CanConvert(
            ByVal targetType As Type,
            ByVal sourceType As Type,
            ByVal rejectNarrowingConversion As Boolean,
            ByVal errors As List(Of String),
            ByVal parameterName As String,
            ByVal isByRefCopyBackContext As Boolean,
            ByRef requiresNarrowingConversion As Boolean,
            ByRef allNarrowingIsFromObject As Boolean) As Boolean

            Dim conversionResult As ConversionClass = ClassifyConversion(targetType, sourceType, Nothing)

            Select Case conversionResult

                Case ConversionClass.Identity, ConversionClass.Widening
                    Return True

                Case ConversionClass.Narrowing

                    If rejectNarrowingConversion Then
                        If errors IsNot Nothing Then
                            ReportError(
                                errors,
                                IIf(isByRefCopyBackContext, SR.ArgumentNarrowingCopyBack3, SR.ArgumentNarrowing3),
                                parameterName,
                                sourceType,
                                targetType)
                        End If

                        Return False
                    Else
                        requiresNarrowingConversion = True
                        If sourceType IsNot GetType(Object) Then allNarrowingIsFromObject = False

                        Return True
                    End If

            End Select

            If errors IsNot Nothing Then
                ReportError(
                    errors,
                    IIf(conversionResult = ConversionClass.Ambiguous,
                        IIf(isByRefCopyBackContext,
                            SR.ArgumentMismatchAmbiguousCopyBack3,
                            SR.ArgumentMismatchAmbiguous3),
                        IIf(isByRefCopyBackContext,
                            SR.ArgumentMismatchCopyBack3,
                            SR.ArgumentMismatch3)),
                    parameterName,
                    sourceType,
                    targetType)
            End If

            Return False
        End Function

        Private Shared Function InferTypeArgumentsFromArgument(
            ByVal argumentType As Type,
            ByVal parameterType As Type,
            ByVal typeInferenceArguments As Type(),
            ByVal targetProcedure As MethodBase,
            ByVal digThroughToBasesAndImplements As Boolean) As Boolean

            Dim inferred As Boolean =
                InferTypeArgumentsFromArgumentDirectly(
                    argumentType,
                    parameterType,
                    typeInferenceArguments,
                    targetProcedure,
                    digThroughToBasesAndImplements)


            If (inferred OrElse
                Not digThroughToBasesAndImplements OrElse
                Not IsInstantiatedGeneric(parameterType) OrElse
                (Not parameterType.IsClass AndAlso Not parameterType.IsInterface)) Then

                'can only inherit from classes or interfaces.
                'can ignore generic parameters here because it
                'were a generic parameter, inference would
                'definitely have succeeded.

                Return inferred
            End If


            Dim rawGenericParameterType As Type = parameterType.GetGenericTypeDefinition

            If (IsArrayType(argumentType)) Then

                '1. Generic IList is implemented only by one dimensional arrays
                '
                '2. If parameter type is a class, then no other inference from
                '   array is possible.
                '
                If (argumentType.GetArrayRank > 1 OrElse
                    parameterType.IsClass) Then

                    Return False
                End If
                'For arrays, change the argument type to be IList(Of Array element type)

                argumentType =
                    GetType(System.Collections.Generic.IList(Of )).MakeGenericType(New Type() {argumentType.GetElementType})

                If (GetType(System.Collections.Generic.IList(Of )) Is rawGenericParameterType) Then
                    GoTo RetryInference
                End If

            ElseIf (Not argumentType.IsClass AndAlso
                     Not argumentType.IsInterface) Then

                Debug.Assert(Not IsGenericParameter(argumentType), "Generic parameter unexpected!!!")

                Return False

            ElseIf (IsInstantiatedGeneric(argumentType) AndAlso
                     argumentType.GetGenericTypeDefinition Is rawGenericParameterType) Then

                Return False
            End If


            If (parameterType.IsClass) Then

                If (Not argumentType.IsClass) Then
                    Return False
                End If

                Dim base As Type = argumentType.BaseType

                While (base IsNot Nothing)

                    If (IsInstantiatedGeneric(base) AndAlso
                         base.GetGenericTypeDefinition Is rawGenericParameterType) Then

                        Exit While
                    End If

                    base = base.BaseType
                End While

                argumentType = base
            Else

                Dim implementedMatch As Type = Nothing
                For Each implemented As Type In argumentType.GetInterfaces

                    If (IsInstantiatedGeneric(implemented) AndAlso
                        implemented.GetGenericTypeDefinition Is rawGenericParameterType) Then

                        If (implementedMatch IsNot Nothing) Then
                            'Ambiguous
                            '
                            Return False
                        End If

                        implementedMatch = implemented
                    End If
                Next

                argumentType = implementedMatch
            End If

            If (argumentType Is Nothing) Then
                Return False
            End If

RetryInference:

            Return InferTypeArgumentsFromArgumentDirectly(
                    argumentType,
                    parameterType,
                    typeInferenceArguments,
                    targetProcedure,
                    digThroughToBasesAndImplements)

        End Function


        Private Shared Function InferTypeArgumentsFromArgumentDirectly(
            ByVal argumentType As Type,
            ByVal parameterType As Type,
            ByVal typeInferenceArguments As Type(),
            ByVal targetProcedure As MethodBase,
            ByVal digThroughToBasesAndImplements As Boolean) As Boolean

            Debug.Assert(Not parameterType.IsByRef, "didn't expect byref parameter type here")
            Debug.Assert(IsRawGeneric(targetProcedure), "Type inference for instantiated generic unexpected!!!")

            If Not RefersToGenericParameter(parameterType, targetProcedure) Then
                Return True
            End If

            'If a generic method is parameterized by T, an argument of type A matching a parameter of type
            'P can be used to infer a type for T by these patterns:
            '
            '  -- If P is T, then infer A for T
            '  -- If P is G(Of T) and A is G(Of X), then infer X for T
            '  -- If P is or implements G(Of T) and A is G(Of X), then infer X for T
            '  -- If P is Array Of T, and A is Array Of X, then infer X for T

            If IsGenericParameter(parameterType) Then
                If AreGenericMethodDefsEqual(parameterType.DeclaringMethod, targetProcedure) Then
                    Dim parameterIndex As Integer = parameterType.GenericParameterPosition
                    If typeInferenceArguments(parameterIndex) Is Nothing Then
                        typeInferenceArguments(parameterIndex) = argumentType

                    ElseIf typeInferenceArguments(parameterIndex) IsNot argumentType Then
                        Return False

                    End If
                End If

            ElseIf IsInstantiatedGeneric(parameterType) Then

                Dim bestMatchType As Type = Nothing

                If IsInstantiatedGeneric(argumentType) AndAlso
                    argumentType.GetGenericTypeDefinition Is parameterType.GetGenericTypeDefinition Then
                    bestMatchType = argumentType
                End If

                If bestMatchType Is Nothing AndAlso digThroughToBasesAndImplements Then
                    For Each possibleGenericType As Type In argumentType.GetInterfaces
                        If IsInstantiatedGeneric(possibleGenericType) AndAlso
                            possibleGenericType.GetGenericTypeDefinition Is parameterType.GetGenericTypeDefinition Then

                            If bestMatchType Is Nothing Then
                                bestMatchType = possibleGenericType
                            Else
                                ' Multiple generic interfaces match the parameter type
                                Return False
                            End If
                        End If
                    Next
                End If

                If bestMatchType IsNot Nothing Then
                    Dim parameterTypeParameters As Type() = GetTypeArguments(parameterType)
                    Dim argumentTypeArguments As Type() = GetTypeArguments(bestMatchType)

                    Debug.Assert(parameterTypeParameters.Length = argumentTypeArguments.Length,
                                 "inconsistent parameter counts")

                    For index As Integer = 0 To argumentTypeArguments.Length - 1
                        If Not InferTypeArgumentsFromArgument(
                                    argumentTypeArguments(index),
                                    parameterTypeParameters(index),
                                    typeInferenceArguments,
                                    targetProcedure,
                                    False) Then     'Don't dig through because generics covariance is not allowed
                            Return False
                        End If
                    Next

                    Return True
                End If

                Return False

            ElseIf IsArrayType(parameterType) Then

                If IsArrayType(argumentType) Then
                    If parameterType.GetArrayRank = argumentType.GetArrayRank Then
                        Return InferTypeArgumentsFromArgument(
                                GetElementType(argumentType),
                                GetElementType(parameterType),
                                typeInferenceArguments,
                                targetProcedure,
                                digThroughToBasesAndImplements)
                    End If
                End If

                Return False
            End If

            Return True

        End Function

        Private Shared Function CanPassToParamArray(
            ByVal targetProcedure As Method,
            ByVal argument As Object,
            ByVal parameter As ParameterInfo) As Boolean

            'This method generates no errors because errors are reported only on the expanded form and
            'the unexpanded form is always accompanied by the expanded form.

            Debug.Assert(IsParamArray(parameter), "expected ParamArray parameter")

            'A Nothing argument can be passed as an unexpanded ParamArray.
            If argument Is Nothing Then Return True

            Dim parameterType As Type = parameter.ParameterType
            Dim argumentType As Type = GetArgumentType(argument)
            Dim conversionResult As ConversionClass = ClassifyConversion(parameterType, argumentType, Nothing)
            Return conversionResult = ConversionClass.Widening OrElse conversionResult = ConversionClass.Identity
        End Function

        Friend Shared Function CanPassToParameter(
            ByVal targetProcedure As Method,
            ByVal argument As Object,
            ByVal parameter As ParameterInfo,
            ByVal isExpandedParamArray As Boolean,
            ByVal rejectNarrowingConversions As Boolean,
            ByVal errors As List(Of String),
            ByRef requiresNarrowingConversion As Boolean,
            ByRef allNarrowingIsFromObject As Boolean) As Boolean

            'A Nothing argument always matches a parameter. Also, it doesn't contribute to type inferencing.
            If argument Is Nothing Then Return True

            Dim parameterType As Type = parameter.ParameterType
            Dim isByRef As Boolean = parameterType.IsByRef

            If isByRef OrElse isExpandedParamArray Then
                parameterType = GetElementType(parameterType)
            End If
            Dim argumentType As Type = GetArgumentType(argument)
            'A Missing argument always matches an optional parameter.
            If argument Is System.Reflection.Missing.Value Then
                If parameter.IsOptional Then
                    Return True
                ElseIf Not IsRootObjectType(parameterType) OrElse Not isExpandedParamArray Then
                    'Trying to pass a Missing argument to a non-optional parameter.
                    'CLR throws if that's the case, so we disallow it here.
                    If errors IsNot Nothing Then
                        If isExpandedParamArray Then
                            'Trying to pass a Missing argument to an expanded ParamArray.
                            ReportError(errors, SR.OmittedParamArrayArgument)
                        Else
                            ReportError(errors, SR.OmittedArgument1, parameter.Name)
                        End If
                    End If
                    Return False
                End If
            End If

            'Check if the conversion from the argument type to the
            'parameter type can succeed.
            Dim canCopyIn As Boolean =
                CanConvert(
                    parameterType,
                    argumentType,
                    rejectNarrowingConversions,
                    errors,
                    parameter.Name,
                    False,
                    requiresNarrowingConversion,
                    allNarrowingIsFromObject)

            If Not isByRef OrElse Not canCopyIn Then
                Return canCopyIn
            End If

            'If the parameter is ByRef, check if the conversion from
            'the parameter type to the argument type can succeed.
            Return CanConvert(
                    argumentType,
                    parameterType,
                    rejectNarrowingConversions,
                    errors,
                    parameter.Name,
                    True,
                    requiresNarrowingConversion,
                    allNarrowingIsFromObject)

        End Function

        Friend Shared Function InferTypeArgumentsFromArgument(
            ByVal targetProcedure As Method,
            ByVal argument As Object,
            ByVal parameter As ParameterInfo,
            ByVal isExpandedParamArray As Boolean,
            ByVal errors As List(Of String)) As Boolean

            'A Nothing argument doesn't contribute to type inferencing.
            If argument Is Nothing Then Return True

            Dim parameterType As Type = parameter.ParameterType
            Dim isByRef As Boolean = parameterType.IsByRef

            If isByRef OrElse isExpandedParamArray Then
                parameterType = GetElementType(parameterType)
            End If
            Dim argumentType As Type = GetArgumentType(argument)
            Debug.Assert(targetProcedure.IsMethod, "we shouldn't be infering type arguments for non-methods")
            If Not InferTypeArgumentsFromArgument(
                        argumentType,
                        parameterType,
                        targetProcedure.TypeArguments,
                        targetProcedure.AsMethod,
                        True) Then

                If errors IsNot Nothing Then
                    ReportError(errors, SR.TypeInferenceFails1, parameter.Name)
                End If
                Return False
            End If

            Return True
        End Function

        Friend Shared Function PassToParameter(
            ByVal argument As Object,
            ByVal parameter As ParameterInfo,
            ByVal parameterType As Type) As Object

            'This function takes an object and modifies it so it can be passed
            'as the parameter described by the ParameterInfo.  This involves casting it
            'to the parameter type and/or substituting optional values for Missing
            'arguments.

            Debug.Assert(parameter IsNot Nothing AndAlso parameterType IsNot Nothing)

            Dim isByRef As Boolean = parameterType.IsByRef
            If isByRef Then
                parameterType = parameterType.GetElementType
            End If

            'An argument represented by a TypedNothing is actually a Nothing
            'reference with a type. This argument passes to the parameter as Nothing.
            If TypeOf argument Is TypedNothing Then
                argument = Nothing
            End If

            'A Missing argument loads the parameter's optional value.
            If argument Is System.Reflection.Missing.Value AndAlso parameter.IsOptional Then
                argument = parameter.DefaultValue
            End If

            'If the argument is a boxed ValueType and we're passing it to a
            'ByRef parameter, then we must forcefully copy it to avoid
            'aliasing since the invocation will modify the boxed ValueType
            'in place. 
            If isByRef Then
                Dim argumentType As Type = GetArgumentType(argument)
                If argumentType IsNot Nothing AndAlso IsValueType(argumentType) Then
                    argument = Conversions.ForceValueCopy(argument, argumentType)
                End If
            End If

            'Peform the conversion to the parameter type and return the result.
            Return Conversions.ChangeType(argument, parameterType)
        End Function

        Private Shared Function FindParameterByName(ByVal parameters As ParameterInfo(), ByVal name As String, ByRef index As Integer) As Boolean
            'Find the Index of the parameter in Parameters which matches Name. Return True if such a parameter is found.

            Dim paramIndex As Integer = 0
            Do While paramIndex < parameters.Length
                If Operators.CompareString(name, parameters(paramIndex).Name, True) = 0 Then
                    index = paramIndex
                    Return True
                End If
                paramIndex += 1
            Loop
            Return False
        End Function

        Private Shared Function CreateMatchTable(ByVal size As Integer, ByVal lastPositionalMatchIndex As Integer) As Boolean()
            'Create a table for keeping track of which parameters have been matched with
            'an argument. Used for detecting multiple matches during named argument matching,
            'and also for loading the optional values of unmatched parameters.

            Dim result As Boolean() = New Boolean(size - 1) {}
            For index As Integer = 0 To lastPositionalMatchIndex
                result(index) = True
            Next
            Return result
        End Function

        Friend Shared Function CanMatchArguments(
            ByVal targetProcedure As Method,
            ByVal arguments As Object(),
            ByVal argumentNames As String(),
            ByVal typeArguments As Type(),
            ByVal rejectNarrowingConversions As Boolean,
            ByVal errors As List(Of String)) As Boolean

            Dim reportErrors As Boolean = errors IsNot Nothing

            targetProcedure.ArgumentsValidated = True

            'First instantiate the generic method.  If type arguments aren't supplied, 
            'we need to infer them first.
            'In error cases, the method might already be instantiated, so need to use the
            'passed in type params only if the method has not yet been instantiated.
            'In the non-error case, the method is always uninstantiated at this time.
            '
            Debug.Assert(Not (errors Is Nothing AndAlso
                              targetProcedure.IsMethod AndAlso
                              targetProcedure.AsMethod.IsGenericMethod AndAlso (Not targetProcedure.AsMethod.IsGenericMethodDefinition)),
                                "Instantiated generic method unexpected!!!")

            If targetProcedure.IsMethod AndAlso IsRawGeneric(targetProcedure.AsMethod) Then
                If typeArguments.Length = 0 Then
                    typeArguments = New Type(targetProcedure.TypeParameters.Length - 1) {}
                    targetProcedure.TypeArguments = typeArguments

                    If Not InferTypeArguments(targetProcedure, arguments, argumentNames, typeArguments, errors) Then
                        Return False
                    End If
                Else
                    targetProcedure.TypeArguments = typeArguments
                End If

                If Not InstantiateGenericMethod(targetProcedure, typeArguments, errors) Then
                    Return False
                End If
            End If

            Dim parameters As ParameterInfo() = targetProcedure.Parameters
            Debug.Assert(arguments.Length <= parameters.Length OrElse
                            (targetProcedure.ParamArrayExpanded AndAlso targetProcedure.ParamArrayIndex >= 0),
                         "argument count mismatch -- this method should have been rejected already")

            Dim argIndex As Integer = argumentNames.Length
            Dim paramIndex As Integer = 0

            'STEP 1
            'Match all positional arguments until we encounter a ParamArray or run out of positional arguments.
            Do While argIndex < arguments.Length

                'The loop is finished if we encounter a ParamArray.
                If paramIndex = targetProcedure.ParamArrayIndex Then Exit Do

                If Not CanPassToParameter(
                            targetProcedure,
                            arguments(argIndex),
                            parameters(paramIndex),
                            False,
                            rejectNarrowingConversions,
                            errors,
                            targetProcedure.RequiresNarrowingConversion,
                            targetProcedure.AllNarrowingIsFromObject) Then

                    'If errors are needed, keep going to catch them all.
                    If Not reportErrors Then Return False

                End If

                argIndex += 1
                paramIndex += 1
            Loop

            'STEP 2
            'Match all remaining positional arguments to the ParamArray.
            If targetProcedure.HasParamArray Then

                Debug.Assert(paramIndex = targetProcedure.ParamArrayIndex,
                             "current parameter must be param array by this point")

                If targetProcedure.ParamArrayExpanded Then
                    'Treat the ParamArray in its expanded form. Match remaining arguments to the
                    'ParamArray's element type.

                    'Nothing passed to a ParamArray will widen to both the type of the ParamArray and the
                    'array type of the ParamArray. In that case, we explicitly disallow matching an
                    'expanded ParamArray. If one argument remains and it is Nothing, reject the match.

                    If argIndex = arguments.Length - 1 AndAlso arguments(argIndex) Is Nothing Then
                        'No need to generate an error for this case since Nothing will always match
                        'the associated unexpanded form.
                        Return False
                    End If

                    Do While argIndex < arguments.Length

                        If Not CanPassToParameter(
                                    targetProcedure,
                                    arguments(argIndex),
                                    parameters(paramIndex),
                                    True,
                                    rejectNarrowingConversions,
                                    errors,
                                    targetProcedure.RequiresNarrowingConversion,
                                    targetProcedure.AllNarrowingIsFromObject) Then

                            'If errors are needed, keep going to catch them all.
                            If Not reportErrors Then Return False

                        End If

                        argIndex += 1
                    Loop

                Else
                    'Treat the ParamArray in its unexpanded form. Determine if the argument can
                    'be passed directly as a ParamArray.

                    Debug.Assert(arguments.Length - argIndex <= 1,
                                 "must have zero or one arg left to match the unexpanded paramarray")  'Candidate collection guarantees this.

                    'Need one argument left over for the unexpanded form to be applicable.
                    If arguments.Length - argIndex <> 1 Then
                        'No need to generate an error for this case because the error
                        'reporting will be done on the expanded form. All we need to do is
                        'disqualify the unexpanded form.
                        Return False
                    End If

                    If Not CanPassToParamArray(
                                targetProcedure,
                                arguments(argIndex),
                                parameters(paramIndex)) Then

                        ' We do need to report errors when only the
                        ' unexpanded form is being considered.
                        If reportErrors Then
                            ReportError(
                                errors,
                                SR.ArgumentMismatch3,
                                parameters(paramIndex).Name,
                                GetArgumentType(arguments(argIndex)),
                                parameters(paramIndex).ParameterType)
                        End If

                        Return False
                    End If

                End If

                'Matching the ParamArray consumes this parameter. Increment the parameter index.
                paramIndex += 1
            End If

            'If needed, create the table which keeps track of matched Parameters.
            'Initialize it using the positional matches we've found thus far.
            'This table is needed if we potentially have unmatched Optional parameters.
            'This can happen when named arguments exist or when the number of positional
            'arguments is less than the number of parameters.
            Dim matchedParameters As Boolean() = Nothing

            If argumentNames.Length > 0 OrElse paramIndex < parameters.Length Then
                matchedParameters = CreateMatchTable(parameters.Length, paramIndex - 1)
            End If

            'STEP 3
            'Match all named arguments.
            If argumentNames.Length > 0 Then

                Debug.Assert(parameters.Length > 0, "expected some parameters here")  'Candidate collection guarantees this.

                'The named argument mapping table contains indices into the
                'parameters array to describe the association between arguments
                'and parameters.
                '
                'Given an array of arguments and an array of argument names, the
                'index n into each of these arrays represents the nth named argument
                'and its assocated name. If argument n matches the name of the
                'parameter at index m in the array of parameters, then the named
                'argument mapping table will contain the value m at index n.

                Dim namedArgumentMapping As Integer() = New Integer(argumentNames.Length - 1) {}

                argIndex = 0
                Do While argIndex < argumentNames.Length

                    If Not FindParameterByName(parameters, argumentNames(argIndex), paramIndex) Then
                        'This named argument does not match the name of any parameter.
                        'If errors are needed, keep going to catch them all.
                        If Not reportErrors Then Return False
                        ReportError(errors, SR.NamedParamNotFound2, argumentNames(argIndex), targetProcedure)
                        GoTo skipargument
                    End If

                    If paramIndex = targetProcedure.ParamArrayIndex Then
                        'This named argument matches a ParamArray parameter.
                        'If errors are needed, keep going to catch them all.
                        If Not reportErrors Then Return False
                        ReportError(errors, SR.NamedParamArrayArgument1, argumentNames(argIndex))
                        GoTo skipargument
                    End If

                    If matchedParameters(paramIndex) Then
                        'This named argument matches a parameter which has already been specified.
                        'If errors are needed, keep going to catch them all.
                        If Not reportErrors Then Return False
                        ReportError(errors, SR.NamedArgUsedTwice2, argumentNames(argIndex), targetProcedure)
                        GoTo skipargument
                    End If

                    If Not CanPassToParameter(
                                targetProcedure,
                                arguments(argIndex),
                                parameters(paramIndex),
                                False,
                                rejectNarrowingConversions,
                                errors,
                                targetProcedure.RequiresNarrowingConversion,
                                targetProcedure.AllNarrowingIsFromObject) Then

                        'If errors are needed, keep going to catch them all.
                        If Not reportErrors Then Return False

                    End If

                    matchedParameters(paramIndex) = True
                    namedArgumentMapping(argIndex) = paramIndex
skipargument:
                    argIndex += 1
                Loop

                'Store this away for use when/if we invoke this method.
                targetProcedure.NamedArgumentMapping = namedArgumentMapping
            End If

            'All remaining unmatched parameters must be Optional.
            If matchedParameters IsNot Nothing Then
                For index As Integer = 0 To matchedParameters.Length - 1
                    If matchedParameters(index) = False AndAlso Not parameters(index).IsOptional Then
                        'This parameter is not optional.
                        'If errors are needed, keep going to catch them all.
                        If Not reportErrors Then Return False
                        ReportError(errors, SR.OmittedArgument1, parameters(index).Name)
                    End If
                Next
            End If

            'If errors were generated, the arguments failed to match the target procedure.
            If errors IsNot Nothing AndAlso errors.Count > 0 Then
                Return False
            End If

            Return True

        End Function

        Private Shared Function InstantiateGenericMethod(
            ByVal targetProcedure As Method,
            ByVal typeArguments As Type(),
            ByVal errors As List(Of String)) As Boolean

            'Verify that all type arguments have been supplied.
            Debug.Assert(typeArguments.Length = targetProcedure.TypeParameters.Length, "expected length match")

            Dim reportErrors As Boolean = errors IsNot Nothing

            For typeArgumentIndex As Integer = 0 To typeArguments.Length - 1

                If typeArguments(typeArgumentIndex) Is Nothing Then
                    If Not reportErrors Then Return False
                    ReportError(
                            errors,
                            SR.UnboundTypeParam1,
                            targetProcedure.TypeParameters(typeArgumentIndex).Name)
                End If

            Next

            If errors Is Nothing OrElse errors.Count = 0 Then
                'Create the instantiated form of the generic method using the type arguments
                'inferred during argument matching.
                If Not targetProcedure.BindGenericArguments Then
                    If Not reportErrors Then Return False
                    ReportError(errors, SR.FailedTypeArgumentBinding)
                End If
            End If

            'If errors were generated, the instantiation failed.
            If errors IsNot Nothing AndAlso errors.Count > 0 Then
                Return False
            End If

            Return True
        End Function

        'may not want Method as TargetProcedure - may instead want to pass the required information in separately.
        'this means that for the simple case of only one method we do not need to allocate a Method object.
        Friend Shared Sub MatchArguments(
            ByVal targetProcedure As Method,
            ByVal arguments As Object(),
            ByVal matchedArguments As Object())

            Dim parameters As ParameterInfo() = targetProcedure.Parameters

            Debug.Assert(targetProcedure.ArgumentsValidated,
                         "expected validation of arguments to be made before matching")
            Debug.Assert(matchedArguments.Length = parameters.Length OrElse
                         matchedArguments.Length = parameters.Length + 1,
                         "size of matched arguments array must equal number of parameters")
            Debug.Assert(arguments.Length <= parameters.Length OrElse
                            (targetProcedure.ParamArrayExpanded AndAlso targetProcedure.ParamArrayIndex >= 0),
                         "argument count mismatch -- this method should have been rejected already")

            Dim namedArgumentMapping As Integer() = targetProcedure.NamedArgumentMapping

            Dim argIndex As Integer = 0
            If namedArgumentMapping IsNot Nothing Then argIndex = namedArgumentMapping.Length
            Dim paramIndex As Integer = 0

            'STEP 1
            'Match all positional arguments until we encounter a ParamArray or run out of positional arguments.
            Do While argIndex < arguments.Length

                'The loop is finished if we encounter a ParamArray.
                If paramIndex = targetProcedure.ParamArrayIndex Then Exit Do

                matchedArguments(paramIndex) =
                    PassToParameter(arguments(argIndex), parameters(paramIndex), parameters(paramIndex).ParameterType)

                argIndex += 1
                paramIndex += 1
            Loop

            'STEP 2
            'Match all remaining positional arguments to the ParamArray.
            If targetProcedure.HasParamArray Then
                Debug.Assert(paramIndex = targetProcedure.ParamArrayIndex,
                             "current parameter must be param array by this point")

                If targetProcedure.ParamArrayExpanded Then
                    'Treat the ParamArray in its expanded form. Pass the remaining arguments into
                    'the ParamArray.

                    Dim remainingArgumentCount As Integer = arguments.Length - argIndex
                    Dim paramArrayParameter As ParameterInfo = parameters(paramIndex)
                    Dim paramArrayElementType As System.Type = paramArrayParameter.ParameterType.GetElementType

                    Dim paramArrayArgument As System.Array =
                        System.Array.CreateInstance(paramArrayElementType, remainingArgumentCount)

                    Dim paramArrayIndex As Integer = 0
                    Do While argIndex < arguments.Length

                        paramArrayArgument.SetValue(
                            PassToParameter(arguments(argIndex), paramArrayParameter, paramArrayElementType),
                            paramArrayIndex)

                        argIndex += 1
                        paramArrayIndex += 1
                    Loop

                    matchedArguments(paramIndex) = paramArrayArgument

                Else
                    Debug.Assert(arguments.Length - argIndex = 1,
                                 "must have one arg left to match the unexpanded paramarray")

                    'Treat the ParamArray in its unexpanded form. Pass the one remaining argument
                    'directly as a ParamArray.
                    matchedArguments(paramIndex) =
                        PassToParameter(arguments(argIndex), parameters(paramIndex), parameters(paramIndex).ParameterType)
                End If

                'Matching the ParamArray consumes this parameter. Increment the parameter index.
                paramIndex += 1
            End If

            'If needed, create the table which keeps track of matched Parameters.
            'Initialize it using the positional matches we've found thus far.
            'This table is needed if we potentially have unmatched Optional parameters.
            'This can happen when named arguments exist or when the number of positional
            'arguments is less than the number of parameters.
            Dim matchedParameters As Boolean() = Nothing

            If namedArgumentMapping IsNot Nothing OrElse paramIndex < parameters.Length Then
                matchedParameters = CreateMatchTable(parameters.Length, paramIndex - 1)
            End If

            'STEP 3
            'Match all named arguments.
            If namedArgumentMapping IsNot Nothing Then

                Debug.Assert(parameters.Length > 0, "expected some parameters here")  'Candidate collection guarantees this.

                'The named argument mapping table contains indices into the
                'parameters array to describe the association between arguments
                'and parameters.
                '
                'Given an array of arguments and an array of argument names, the
                'index n into each of these arrays represents the nth named argument
                'and its assocated name. If argument n matches the name of the
                'parameter at index m in the array of parameters, then the named
                'argument mapping table will contain the value m at index n.

                argIndex = 0
                Do While argIndex < namedArgumentMapping.Length
                    paramIndex = namedArgumentMapping(argIndex)

                    matchedArguments(paramIndex) =
                        PassToParameter(arguments(argIndex), parameters(paramIndex), parameters(paramIndex).ParameterType)

                    Debug.Assert(Not matchedParameters(paramIndex), "named argument match collision")
                    matchedParameters(paramIndex) = True
                    argIndex += 1
                Loop

            End If

            'If all has gone well, by this point any unmatched parameters are Optional.
            'Fill in unmatched parameters with their optional values.
            If matchedParameters IsNot Nothing Then
                For index As Integer = 0 To matchedParameters.Length - 1
                    If matchedParameters(index) = False Then
                        Debug.Assert(parameters(index).IsOptional,
                                     "unmatched, non-optional parameter. How did we get this far?")
                        matchedArguments(index) =
                            PassToParameter(System.Reflection.Missing.Value, parameters(index), parameters(index).ParameterType)
                    End If
                Next
            End If

            Return
        End Sub

        Private Shared Function InferTypeArguments(
            ByVal targetProcedure As Method,
            ByVal arguments As Object(),
            ByVal argumentNames As String(),
            ByVal typeArguments As Type(),
            ByVal errors As List(Of String)) As Boolean

            Dim reportErrors As Boolean = errors IsNot Nothing

            Dim parameters As ParameterInfo() = targetProcedure.RawParameters

            Debug.Assert(arguments.Length <= parameters.Length OrElse
                            (targetProcedure.ParamArrayExpanded AndAlso targetProcedure.ParamArrayIndex >= 0),
                         "argument count mismatch -- this method should have been rejected already")

            Dim argIndex As Integer = argumentNames.Length
            Dim paramIndex As Integer = 0

            'STEP 1
            'Infer from all positional arguments until we encounter a ParamArray or run out of positional arguments.
            Do While argIndex < arguments.Length

                'The loop is finished if we encounter a ParamArray.
                If paramIndex = targetProcedure.ParamArrayIndex Then Exit Do

                If Not InferTypeArgumentsFromArgument(
                            targetProcedure,
                            arguments(argIndex),
                            parameters(paramIndex),
                            False,
                            errors) Then

                    'If errors are needed, keep going to catch them all.
                    If Not reportErrors Then Return False
                End If

                argIndex += 1
                paramIndex += 1
            Loop

            'STEP 2
            'Infer from all remaining positional arguments matching a ParamArray.
            If targetProcedure.HasParamArray Then

                Debug.Assert(paramIndex = targetProcedure.ParamArrayIndex,
                             "current parameter must be param array by this point")

                If targetProcedure.ParamArrayExpanded Then
                    'Treat the ParamArray in its expanded form. Infer the element type from the remaining arguments.
                    Do While argIndex < arguments.Length

                        If Not InferTypeArgumentsFromArgument(
                                    targetProcedure,
                                    arguments(argIndex),
                                    parameters(paramIndex),
                                    True,
                                    errors) Then

                            'If errors are needed, keep going to catch them all.
                            If Not reportErrors Then Return False

                        End If

                        argIndex += 1
                    Loop

                Else
                    'Treat the ParamArray in its unexpanded form. Infer the ParamArray type from the argument.

                    Debug.Assert(arguments.Length - argIndex <= 1,
                                 "must have zero or one arg left to match the unexpanded paramarray")  'Candidate collection guarantees this.

                    If arguments.Length - argIndex <> 1 Then
                        'Type inferencing not possible here.
                        Return True
                    End If

                    If Not InferTypeArgumentsFromArgument(
                                targetProcedure,
                                arguments(argIndex),
                                parameters(paramIndex),
                                False,
                                errors) Then
                        Return False
                    End If

                End If

                'Matching the ParamArray consumes this parameter. Increment the parameter index.
                paramIndex += 1
            End If

            'STEP 3
            'Infer from named arguments.
            If argumentNames.Length > 0 Then

                Debug.Assert(parameters.Length > 0, "expected some parameters here")  'Candidate collection guarantees this.

                argIndex = 0
                Do While argIndex < argumentNames.Length

                    If Not FindParameterByName(parameters, argumentNames(argIndex), paramIndex) Then
                        GoTo skipargument
                    End If

                    If paramIndex = targetProcedure.ParamArrayIndex Then
                        GoTo skipargument
                    End If

                    If Not InferTypeArgumentsFromArgument(
                                targetProcedure,
                                arguments(argIndex),
                                parameters(paramIndex),
                                False,
                                errors) Then

                        'If errors are needed, keep going to catch them all.
                        If Not reportErrors Then Return False

                    End If
skipargument:
                    argIndex += 1
                Loop

            End If

            'If errors were generated, inference of type arguments failed.
            If errors IsNot Nothing AndAlso errors.Count > 0 Then
                Return False
            End If

            Return True
        End Function

        Friend Shared Sub ReorderArgumentArray(
            ByVal targetProcedure As Method,
            ByVal parameterResults As Object(),
            ByVal arguments As Object(),
            ByVal copyBack As Boolean(),
            ByVal lookupFlags As BindingFlags)

            'No need to copy back if there are no valid targets .
            'The copy back array will be be Nothing if the compiler determined that all
            'arguments are Rvalues.
            If copyBack Is Nothing Then
                Return
            End If

            'Initialize the copy back array to all ByVal.
            For index As Integer = 0 To copyBack.Length - 1
                copyBack(index) = False
            Next

            'No need to copy back if there are no byref parameters. Properties can't have
            'ByRef arguments, so skip these as well.
            If HasFlag(lookupFlags, BindingFlagsSetProperty) OrElse
               Not targetProcedure.HasByRefParameter Then
                Return
            End If

            Debug.Assert(copyBack.Length = arguments.Length, "array sizes must match")
            Debug.Assert(parameterResults.Length = targetProcedure.Parameters.Length, "parameter arrays must match")

            Dim parameters As ParameterInfo() = targetProcedure.Parameters
            Dim namedArgumentMapping As Integer() = targetProcedure.NamedArgumentMapping

            Dim argIndex As Integer = 0
            If namedArgumentMapping IsNot Nothing Then argIndex = namedArgumentMapping.Length
            Dim paramIndex As Integer = 0

            'STEP 1
            'Copy back all positional parameters until we encounter a ParamArray or run out of positional arguments.
            Do While argIndex < arguments.Length

                'The loop is finished if we encounter a ParamArray.
                If paramIndex = targetProcedure.ParamArrayIndex Then Exit Do

                If parameters(paramIndex).ParameterType.IsByRef Then
                    arguments(argIndex) = parameterResults(paramIndex)
                    copyBack(argIndex) = True
                End If

                argIndex += 1
                paramIndex += 1
            Loop

            'STEP 2
            'No need to copy back from the ParamArray because they can't be ByRef. Skip it.

            'STEP 3
            'Copy back all named arguments.
            If namedArgumentMapping IsNot Nothing Then
                argIndex = 0
                Do While argIndex < namedArgumentMapping.Length
                    paramIndex = namedArgumentMapping(argIndex)

                    If parameters(paramIndex).ParameterType.IsByRef Then
                        arguments(argIndex) = parameterResults(paramIndex)
                        copyBack(argIndex) = True
                    End If

                    argIndex += 1
                Loop
            End If

            Return
        End Sub

        Private Shared Function RejectUncallableProcedures(
            ByVal candidates As List(Of Method),
            ByVal arguments As Object(),
            ByVal argumentNames As String(),
            ByVal typeArguments As Type(),
            ByRef candidateCount As Integer,
            ByRef someCandidatesAreGeneric As Boolean) As Method

            Dim bestCandidate As Method = Nothing

            For index As Integer = 0 To candidates.Count - 1

                Dim candidateProcedure As Method = candidates(index)

                If Not candidateProcedure.ArgumentMatchingDone Then

                    RejectUncallableProcedure(
                        candidateProcedure,
                        arguments,
                        argumentNames,
                        typeArguments)
                End If

                If candidateProcedure.NotCallable Then
                    candidateCount -= 1
                Else
                    bestCandidate = candidateProcedure

                    If candidateProcedure.IsGeneric OrElse IsGeneric(candidateProcedure.DeclaringType) Then
                        someCandidatesAreGeneric = True
                    End If

                End If

            Next


#If BINDING_LOG Then
            Console.WriteLine("== REJECT UNCALLABLE ==")
            For Each item As Method In Candidates
                If item Is Nothing Then
                    Console.WriteLine("dead ** didn't expect this here.")
                Else
                    Console.WriteLine(item.DumpContents)
                End If
            Next
#End If
            Return bestCandidate

        End Function

        Private Shared Sub RejectUncallableProcedure(
            ByVal candidate As Method,
            ByVal arguments As Object(),
            ByVal argumentNames As String(),
            ByVal typeArguments As Type())

            Debug.Assert(candidate.ArgumentMatchingDone = False, "Argument matching being done multiple times!!!")

            If Not CanMatchArguments(
                        candidate,
                        arguments,
                        argumentNames,
                        typeArguments,
                        False,
                        Nothing) Then

                candidate.NotCallable = True
            End If

            candidate.ArgumentMatchingDone = True

        End Sub

        ' Type.IsEquivalentTo is not supported in .Net 4.0
        Private Shared Function GetArgumentType(ByVal argument As Object) As Type
            'A Nothing object has no type.
            If argument Is Nothing Then Return Nothing
            'A typed Nothing object stores the type that Nothing should be considered as.
            Dim typedNothingArgument As TypedNothing = TryCast(argument, TypedNothing)

            If typedNothingArgument IsNot Nothing Then Return typedNothingArgument.Type
            'Otherwise, just return the type of the object.
            Return argument.GetType
        End Function

        Private Shared Function MoreSpecificProcedure(
            ByVal left As Method,
            ByVal right As Method,
            ByVal arguments As Object(),
            ByVal argumentNames As String(),
            ByVal compareGenericity As ComparisonType,
            Optional ByRef bothLose As Boolean = False,
            Optional ByVal continueWhenBothLose As Boolean = False) As Method

            bothLose = False
            Dim leftWinsAtLeastOnce As Boolean = False
            Dim rightWinsAtLeastOnce As Boolean = False

            'Compare the parameters that match the supplied positional arguments.

            Dim leftMethod As MethodBase
            Dim rightMethod As MethodBase
            If left.IsMethod Then leftMethod = left.AsMethod Else leftMethod = Nothing
            If right.IsMethod Then rightMethod = right.AsMethod Else rightMethod = Nothing

            Dim leftParamIndex As Integer = 0
            Dim rightParamIndex As Integer = 0

            Dim argIndex As Integer = argumentNames.Length
            Do While argIndex < arguments.Length

                Dim argumentType As Type = GetArgumentType(arguments(argIndex))

                Select Case compareGenericity
                    Case ComparisonType.GenericSpecificityBasedOnMethodGenericParams
                        ' Compare GenericSpecificity
                        CompareGenericityBasedOnMethodGenericParams(
                            left.Parameters(leftParamIndex),
                            left.RawParameters(leftParamIndex),
                            left,
                            left.ParamArrayExpanded,
                            right.Parameters(rightParamIndex),
                            right.RawParameters(rightParamIndex),
                            right,
                            right.ParamArrayExpanded,
                            leftWinsAtLeastOnce,
                            rightWinsAtLeastOnce,
                            bothLose)

                    Case ComparisonType.GenericSpecificityBasedOnTypeGenericParams
                        ' Compare GenericSpecificity
                        CompareGenericityBasedOnTypeGenericParams(
                            left.Parameters(leftParamIndex),
                            left.RawParametersFromType(leftParamIndex),
                            left,
                            left.ParamArrayExpanded,
                            right.Parameters(rightParamIndex),
                            right.RawParametersFromType(rightParamIndex),
                            right,
                            right.ParamArrayExpanded,
                            leftWinsAtLeastOnce,
                            rightWinsAtLeastOnce,
                            bothLose)

                    Case ComparisonType.ParameterSpecificty
                        ' Compare ParameterSpecificity
                        CompareParameterSpecificity(
                                argumentType,
                                left.Parameters(leftParamIndex),
                                leftMethod,
                                left.ParamArrayExpanded,
                                right.Parameters(rightParamIndex),
                                rightMethod,
                                right.ParamArrayExpanded,
                                leftWinsAtLeastOnce,
                                rightWinsAtLeastOnce,
                                bothLose)

                    Case Else
                        Debug.Assert(False, "Unexpected comparison type!!!")
                End Select

                If (bothLose AndAlso (Not continueWhenBothLose)) OrElse
                   (leftWinsAtLeastOnce AndAlso rightWinsAtLeastOnce) Then
                    Return Nothing
                End If

                If leftParamIndex <> left.ParamArrayIndex Then leftParamIndex += 1
                If rightParamIndex <> right.ParamArrayIndex Then rightParamIndex += 1
                argIndex += 1
            Loop

            argIndex = 0
            Do While argIndex < argumentNames.Length

                Dim leftParameterFound As Boolean = FindParameterByName(left.Parameters, argumentNames(argIndex), leftParamIndex)
                Dim rightParameterFound As Boolean = FindParameterByName(right.Parameters, argumentNames(argIndex), rightParamIndex)

                If Not leftParameterFound OrElse Not rightParameterFound Then
                    Throw New InternalErrorException()
                End If

                Dim argumentType As Type = GetArgumentType(arguments(argIndex))

                Select Case compareGenericity
                    Case ComparisonType.GenericSpecificityBasedOnMethodGenericParams
                        ' Compare GenericSpecificity
                        CompareGenericityBasedOnMethodGenericParams(
                            left.Parameters(leftParamIndex),
                            left.RawParameters(leftParamIndex),
                            left,
                            True,
                            right.Parameters(rightParamIndex),
                            right.RawParameters(rightParamIndex),
                            right,
                            True,
                            leftWinsAtLeastOnce,
                            rightWinsAtLeastOnce,
                            bothLose)

                    Case ComparisonType.GenericSpecificityBasedOnTypeGenericParams
                        ' Compare GenericSpecificity
                        CompareGenericityBasedOnTypeGenericParams(
                            left.Parameters(leftParamIndex),
                            left.RawParameters(leftParamIndex),
                            left,
                            True,
                            right.Parameters(rightParamIndex),
                            right.RawParameters(rightParamIndex),
                            right,
                            True,
                            leftWinsAtLeastOnce,
                            rightWinsAtLeastOnce,
                            bothLose)

                    Case ComparisonType.ParameterSpecificty
                        ' Compare ParameterSpecificity
                        CompareParameterSpecificity(
                            argumentType,
                            left.Parameters(leftParamIndex),
                            leftMethod,
                            True,
                            right.Parameters(rightParamIndex),
                            rightMethod,
                            True,
                            leftWinsAtLeastOnce,
                            rightWinsAtLeastOnce,
                            bothLose)
                End Select

                If (bothLose AndAlso (Not continueWhenBothLose)) OrElse
                   (leftWinsAtLeastOnce AndAlso rightWinsAtLeastOnce) Then
                    Return Nothing
                End If

                argIndex += 1
            Loop

            Debug.Assert(Not (leftWinsAtLeastOnce AndAlso rightWinsAtLeastOnce),
                         "Most specific method logic is confused.")

            If leftWinsAtLeastOnce Then Return left
            If rightWinsAtLeastOnce Then Return right

            Return Nothing
        End Function

        Private Shared Function MostSpecificProcedure(
            ByVal candidates As List(Of Method),
            ByRef candidateCount As Integer,
            ByVal arguments As Object(),
            ByVal argumentNames As String()) As Method


            For Each currentCandidate As Method In candidates

                If currentCandidate.NotCallable OrElse currentCandidate.RequiresNarrowingConversion Then
                    Continue For
                End If

                Dim currentCandidateIsBest As Boolean = True

                For Each contender As Method In candidates

                    If contender.NotCallable OrElse
                       contender.RequiresNarrowingConversion OrElse
                       (contender = currentCandidate AndAlso
                            contender.ParamArrayExpanded = currentCandidate.ParamArrayExpanded) Then

                        Continue For
                    End If

                    Dim bestOfTheTwo As Method =
                        MoreSpecificProcedure(
                            currentCandidate,
                            contender,
                            arguments,
                            argumentNames,
                            ComparisonType.ParameterSpecificty,
                            continueWhenBothLose:=True)

                    If bestOfTheTwo Is currentCandidate Then
                        If Not contender.LessSpecific Then
                            contender.LessSpecific = True
                            candidateCount -= 1
                        End If
                    Else
                        'The current candidate can't be the most specific.
                        currentCandidateIsBest = False

                        If bestOfTheTwo Is contender AndAlso Not currentCandidate.LessSpecific Then
                            currentCandidate.LessSpecific = True
                            candidateCount -= 1
                        End If
                    End If
                Next

                If currentCandidateIsBest Then
                    Debug.Assert(candidateCount = 1, "Surprising overload candidate remains.")
                    Return currentCandidate
                End If

            Next

            Return Nothing
        End Function

        Private Shared Function RemoveRedundantGenericProcedures(
            ByVal candidates As List(Of Method),
            ByRef candidateCount As Integer,
            ByVal arguments As Object(),
            ByVal argumentNames As String()) As Method


            For leftIndex As Integer = 0 To candidates.Count - 1
                Dim left As Method = candidates(leftIndex)

                If Not left.NotCallable Then

                    For rightIndex As Integer = leftIndex + 1 To candidates.Count - 1
                        Dim right As Method = candidates(rightIndex)

                        If Not right.NotCallable AndAlso
                           left.RequiresNarrowingConversion = right.RequiresNarrowingConversion Then

                            Dim leastGeneric As Method = Nothing
                            Dim signatureMismatch As Boolean = False

                            ' Least generic based on generic method's type parameters

                            If left.IsGeneric() OrElse right.IsGeneric() Then

                                leastGeneric =
                                    MoreSpecificProcedure(
                                        left,
                                        right,
                                        arguments,
                                        argumentNames,
                                        ComparisonType.GenericSpecificityBasedOnMethodGenericParams,
                                        signatureMismatch)

                                If leastGeneric IsNot Nothing Then
                                    candidateCount -= 1
                                    If candidateCount = 1 Then
                                        Return leastGeneric
                                    End If
                                    If leastGeneric Is left Then
                                        right.NotCallable = True
                                    Else
                                        left.NotCallable = True
                                        Exit For
                                    End If
                                End If
                            End If


                            ' Least generic based on method's generic parent's type parameters

                            If Not signatureMismatch AndAlso
                               leastGeneric Is Nothing AndAlso
                               (IsGeneric(left.DeclaringType) OrElse IsGeneric(right.DeclaringType)) Then

                                leastGeneric =
                                    MoreSpecificProcedure(
                                        left,
                                        right,
                                        arguments,
                                        argumentNames,
                                        ComparisonType.GenericSpecificityBasedOnTypeGenericParams,
                                        signatureMismatch)

                                If leastGeneric IsNot Nothing Then
                                    candidateCount -= 1
                                    If candidateCount = 1 Then
                                        Return leastGeneric
                                    End If
                                    If leastGeneric Is left Then
                                        right.NotCallable = True
                                    Else
                                        left.NotCallable = True
                                        Exit For
                                    End If
                                End If
                            End If
                        End If

                    Next

                End If
            Next

            Return Nothing
        End Function


        Private Shared Sub ReportError(
            ByVal errors As List(Of String),
            ByVal resourceID As String,
            ByVal substitution1 As String,
            ByVal substitution2 As Type,
            ByVal substitution3 As Type)

            Debug.Assert(errors IsNot Nothing, "expected error table")
            errors.Add(
                GetResourceString(
                    resourceID,
                    substitution1,
                    VBFriendlyName(substitution2),
                    VBFriendlyName(substitution3)))
        End Sub

        Private Shared Sub ReportError(
            ByVal errors As List(Of String),
            ByVal resourceID As String,
            ByVal substitution1 As String,
            ByVal substitution2 As Method)

            Debug.Assert(errors IsNot Nothing, "expected error table")
            errors.Add(
                GetResourceString(
                    resourceID,
                    substitution1,
                    substitution2.ToString))
        End Sub

        Private Shared Sub ReportError(
            ByVal errors As List(Of String),
            ByVal resourceID As String,
            ByVal substitution1 As String)

            Debug.Assert(errors IsNot Nothing, "expected error table")
            errors.Add(
                GetResourceString(
                    resourceID,
                    substitution1))
        End Sub

        Private Shared Sub ReportError(ByVal errors As List(Of String), ByVal resourceID As String)

            Debug.Assert(errors IsNot Nothing, "expected error table")
            errors.Add(
                GetResourceString(resourceID))
        End Sub

        Private Delegate Function ArgumentDetector(
            ByVal targetProcedure As Method,
            ByVal arguments As Object(),
            ByVal argumentNames As String(),
            ByVal typeArguments As Type(),
            ByVal errors As List(Of String)) As Boolean

        Private Delegate Function CandidateProperty(ByVal candidate As Method) As Boolean

        Private Shared Function ReportOverloadResolutionFailure(
            ByVal overloadedProcedureName As String,
            ByVal candidates As List(Of Method),
            ByVal arguments As Object(),
            ByVal argumentNames As String(),
            ByVal typeArguments As Type(),
            ByVal errorID As String,
            ByVal failure As ResolutionFailure,
            ByVal detector As ArgumentDetector,
            ByVal candidateFilter As CandidateProperty) As Exception

            Dim errorMessage As StringBuilder = New StringBuilder
            Dim errors As New List(Of String)
            Dim candidateReportCount As Integer = 0

            For index As Integer = 0 To candidates.Count - 1

                Dim candidateProcedure As Method = candidates(index)

                If candidateFilter(candidateProcedure) Then

                    If candidateProcedure.HasParamArray Then
                        'We may have two versions of paramarray methods in the list. So skip the first
                        'one (the unexpanded one). However, we don't want to skip the unexpanded form
                        'if the expanded form will fail the filter.
                        Dim indexAhead As Integer = index + 1
                        While indexAhead < candidates.Count
                            If candidateFilter(candidates(indexAhead)) AndAlso
                               candidates(indexAhead) = candidateProcedure Then
                                Continue For
                            End If
                            indexAhead += 1
                        End While
                    End If

                    candidateReportCount += 1

                    errors.Clear()
                    Dim result As Boolean =
                        detector(candidateProcedure, arguments, argumentNames, typeArguments, errors)
                    Debug.Assert(result = False AndAlso errors.Count > 0, "expected this candidate to fail")

                    errorMessage.Append(vbCrLf & "    '")
                    errorMessage.Append(candidateProcedure.ToString)
                    errorMessage.Append("':")
                    For Each errorString As String In errors
                        errorMessage.Append(vbCrLf & "        ")
                        errorMessage.Append(errorString)
                    Next
                End If

            Next

            Debug.Assert(candidateReportCount > 0, "expected at least one candidate")

            Dim message As String = GetResourceString(errorID, overloadedProcedureName, errorMessage.ToString)
            If candidateReportCount = 1 Then
                'ParamArrays may cause only one candidate to get reported. In this case, reporting an
                'ambiguity is misleading.
                'Using the same error message for the single-candidate case
                'is also misleading, but the benefit is not high enough for constructing a better message.
                'InvalidCastException is thrown only for back compat.  It would
                'be nice if the latebinder had its own set of exceptions to throw.
                Return New InvalidCastException(message)
            Else
                Return New AmbiguousMatchException(message)
            End If
        End Function

        Private Shared Function DetectArgumentErrors(
            ByVal targetProcedure As Method,
            ByVal arguments As Object(),
            ByVal argumentNames As String(),
            ByVal typeArguments As Type(),
            ByVal errors As List(Of String)) As Boolean

            Return CanMatchArguments(
                    targetProcedure,
                    arguments,
                    argumentNames,
                    typeArguments,
                    False,
                    errors)
        End Function

        Private Shared Function CandidateIsNotCallable(ByVal candidate As Method) As Boolean
            Return candidate.NotCallable
        End Function

        Private Shared Function ReportUncallableProcedures(
            ByVal overloadedProcedureName As String,
            ByVal candidates As List(Of Method),
            ByVal arguments As Object(),
            ByVal argumentNames As String(),
            ByVal typeArguments As Type(),
            ByVal failure As ResolutionFailure) As Exception

            Return ReportOverloadResolutionFailure(
                    overloadedProcedureName,
                    candidates,
                    arguments,
                    argumentNames,
                    typeArguments,
                    SR.NoCallableOverloadCandidates2,
                    failure,
                    AddressOf DetectArgumentErrors,
                    AddressOf CandidateIsNotCallable)
        End Function

        Private Shared Function DetectArgumentNarrowing(
            ByVal targetProcedure As Method,
            ByVal arguments As Object(),
            ByVal argumentNames As String(),
            ByVal typeArguments As Type(),
            ByVal errors As List(Of String)) As Boolean

            Return CanMatchArguments(
                    targetProcedure,
                    arguments,
                    argumentNames,
                    typeArguments,
                    True,
                    errors)
        End Function

        Private Shared Function CandidateIsNarrowing(ByVal candidate As Method) As Boolean
            Return Not candidate.NotCallable AndAlso candidate.RequiresNarrowingConversion
        End Function

        Private Shared Function ReportNarrowingProcedures(
            ByVal overloadedProcedureName As String,
            ByVal candidates As List(Of Method),
            ByVal arguments As Object(),
            ByVal argumentNames As String(),
            ByVal typeArguments As Type(),
            ByVal failure As ResolutionFailure) As Exception

            Return ReportOverloadResolutionFailure(
                    overloadedProcedureName,
                    candidates,
                    arguments,
                    argumentNames,
                    typeArguments,
                    SR.NoNonNarrowingOverloadCandidates2,
                    failure,
                    AddressOf DetectArgumentNarrowing,
                    AddressOf CandidateIsNarrowing)
        End Function

        Private Shared Function DetectUnspecificity(
            ByVal targetProcedure As Method,
            ByVal arguments As Object(),
            ByVal argumentNames As String(),
            ByVal typeArguments As Type(),
            ByVal errors As List(Of String)) As Boolean

            ReportError(errors, SR.NotMostSpecificOverload)
            Return False
        End Function

        Private Shared Function CandidateIsUnspecific(ByVal candidate As Method) As Boolean
            Return Not candidate.NotCallable AndAlso Not candidate.RequiresNarrowingConversion AndAlso Not candidate.LessSpecific
        End Function

        Private Shared Function ReportUnspecificProcedures(
            ByVal overloadedProcedureName As String,
            ByVal candidates As List(Of Method),
            ByVal failure As ResolutionFailure) As Exception

            Return ReportOverloadResolutionFailure(
                    overloadedProcedureName,
                    candidates,
                    Nothing,
                    Nothing,
                    Nothing,
                    SR.NoMostSpecificOverload2,
                    failure,
                    AddressOf DetectUnspecificity,
                    AddressOf CandidateIsUnspecific)
        End Function

        Friend Shared Function ResolveOverloadedCall(
                ByVal methodName As String,
                ByVal candidates As List(Of Method),
                ByVal arguments As Object(),
                ByVal argumentNames As String(),
                ByVal typeArguments As Type(),
                ByVal lookupFlags As BindingFlags,
                ByVal reportErrors As Boolean,
                ByRef failure As ResolutionFailure) As Method

            'Optimistically hope to succeed.
            failure = ResolutionFailure.None

            'From here on, CandidateCount will be used to keep track of the
            'number of remaining viable Candidates in the list.
            Dim candidateCount As Integer = candidates.Count
            Dim someCandidatesAreGeneric As Boolean = False

            Dim best As Method =
                RejectUncallableProcedures(
                    candidates,
                    arguments,
                    argumentNames,
                    typeArguments,
                    candidateCount,
                    someCandidatesAreGeneric)

            If candidateCount = 1 Then
                Return best
            End If

            If candidateCount = 0 Then
                failure = ResolutionFailure.InvalidArgument
                If reportErrors Then
                    Throw ReportUncallableProcedures(methodName, candidates, arguments, argumentNames, typeArguments, failure)
                End If
                Return Nothing
            End If

            If someCandidatesAreGeneric Then
                best = RemoveRedundantGenericProcedures(candidates, candidateCount, arguments, argumentNames)
                If candidateCount = 1 Then
                    Return best
                End If
            End If

            'See if only one does not require narrowing.  If all candidates require narrowing,
            'but one does so only from Object, pick that candidate.

            Dim narrowOnlyFromObjectCount As Integer = 0
            Dim bestNarrowingCandidate As Method = Nothing

            For Each candidate As Method In candidates

                If Not candidate.NotCallable Then
                    If candidate.RequiresNarrowingConversion Then

                        candidateCount -= 1

                        If candidate.AllNarrowingIsFromObject Then
                            narrowOnlyFromObjectCount += 1
                            bestNarrowingCandidate = candidate
                        End If
                    Else
                        best = candidate
                    End If
                End If

            Next

            If candidateCount = 1 Then
                Return best
            End If

            If candidateCount = 0 Then
                If narrowOnlyFromObjectCount = 1 Then
                    Return bestNarrowingCandidate
                End If

                failure = ResolutionFailure.AmbiguousMatch
                If reportErrors Then
                    Throw ReportNarrowingProcedures(methodName, candidates, arguments, argumentNames, typeArguments, failure)
                End If
                Return Nothing
            End If

            best = MostSpecificProcedure(candidates, candidateCount, arguments, argumentNames)

            If best IsNot Nothing Then
                Return best
            End If

            failure = ResolutionFailure.AmbiguousMatch
            If reportErrors Then
                Throw ReportUnspecificProcedures(methodName, candidates, failure)
            End If
            Return Nothing
        End Function

        Friend Shared Function ResolveOverloadedCall(
                ByVal methodName As String,
                ByVal members As MemberInfo(),
                ByVal arguments As Object(),
                ByVal argumentNames As String(),
                ByVal typeArguments As Type(),
                ByVal lookupFlags As BindingFlags,
                ByVal reportErrors As Boolean,
                ByRef failure As ResolutionFailure,
                ByVal baseReference As Container) As Method

#If BINDING_LOG Then
            Console.WriteLine("== MEMBERS ==")
            For Each m As MemberInfo In Members
                Console.WriteLine(MemberToString(m))
            Next
#End If

            'Build the list of candidate Methods, one of which overload resolution will
            'select.
            Dim rejectedForArgumentCount As Integer = 0
            Dim rejectedForTypeArgumentCount As Integer = 0

            Dim candidates As List(Of Method) =
                CollectOverloadCandidates(
                    members,
                    arguments,
                    arguments.Length,
                    argumentNames,
                    typeArguments,
                    False,
                    Nothing,
                    rejectedForArgumentCount,
                    rejectedForTypeArgumentCount,
                    baseReference)

            ' If there is only one candidate and it is NotCallable, let ResolveOverloadedCall
            ' figure out the error message and exception.
            If candidates.Count = 1 AndAlso Not candidates.Item(0).NotCallable Then
                Return candidates.Item(0)
            End If

            If candidates.Count = 0 Then
                failure = ResolutionFailure.MissingMember

                If reportErrors Then
                    Dim errorID As String = SR.NoViableOverloadCandidates1

                    If rejectedForArgumentCount > 0 Then
                        errorID = SR.NoArgumentCountOverloadCandidates1
                    ElseIf rejectedForTypeArgumentCount > 0 Then
                        errorID = SR.NoTypeArgumentCountOverloadCandidates1
                    End If
                    Throw New MissingMemberException(GetResourceString(errorID, methodName))
                End If
                Return Nothing
            End If

            Return ResolveOverloadedCall(
                    methodName,
                    candidates,
                    arguments,
                    argumentNames,
                    typeArguments,
                    lookupFlags,
                    reportErrors,
                    failure)

        End Function

    End Class

End Namespace
