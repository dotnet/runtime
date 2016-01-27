' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Dynamic
Imports System.Globalization
Imports System.Reflection
Imports System.Runtime.InteropServices

Imports Microsoft.VisualBasic.CompilerServices.Symbols
Imports Microsoft.VisualBasic.CompilerServices.OverloadResolution
Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils
Imports Microsoft.VisualBasic.CompilerServices.Utils
Imports Microsoft.VisualBasic.CompilerServices.ReflectionExtensions
Imports System.Runtime.Versioning

#Const NEW_BINDER = True
#Const BINDING_LOG = False

Namespace Microsoft.VisualBasic.CompilerServices

    ' Implements VB late binder.
    Public NotInheritable Class NewLateBinding
        ' Prevent creation.
        Private Sub New()
        End Sub

        <DebuggerHiddenAttribute()> <DebuggerStepThroughAttribute()>
        Public Shared Function LateCanEvaluate(
                ByVal instance As Object,
                ByVal type As System.Type,
                ByVal memberName As String,
                ByVal arguments As Object(),
                ByVal allowFunctionEvaluation As Boolean,
                ByVal allowPropertyEvaluation As Boolean) As Boolean

            Dim baseReference As Container
            If type IsNot Nothing Then
                baseReference = New Container(type)
            Else
                baseReference = New Container(instance)
            End If

            Dim members As MemberInfo() = baseReference.GetMembers(memberName, False)

            If members.Length = 0 Then
                Return True
            End If

            ' This is a field access
            If members(0).MemberType = MemberTypes.Field Then
                If arguments.Length = 0 Then
                    Return True
                Else
                    Dim fieldValue As Object = baseReference.GetFieldValue(DirectCast(members(0), FieldInfo))
                    baseReference = New Container(fieldValue)
                    If baseReference.IsArray Then
                        Return True
                    End If
                    Return allowPropertyEvaluation
                End If
            End If

            ' This is a method invocation
            If members(0).MemberType = MemberTypes.Method Then
                Return allowFunctionEvaluation
            End If

            ' This is a property access
            If members(0).MemberType = MemberTypes.Property Then
                Return allowPropertyEvaluation
            End If

            Return True
        End Function


        <DebuggerHiddenAttribute()> <DebuggerStepThroughAttribute()>
        Public Shared Function LateCall(
                ByVal instance As Object,
                ByVal type As System.Type,
                ByVal memberName As String,
                ByVal arguments As Object(),
                ByVal argumentNames As String(),
                ByVal typeArguments As System.Type(),
                ByVal copyBack As Boolean(),
                ByVal ignoreReturn As Boolean) As Object

            If arguments Is Nothing Then arguments = NoArguments
            If argumentNames Is Nothing Then argumentNames = NoArgumentNames
            If typeArguments Is Nothing Then typeArguments = NoTypeArguments

            Dim baseReference As Container
            If type IsNot Nothing Then
                baseReference = New Container(type)
            Else
                baseReference = New Container(instance)
            End If

            Dim idmop As IDynamicMetaObjectProvider = IDOUtils.TryCastToIDMOP(instance)
            If idmop IsNot Nothing AndAlso typeArguments Is NoTypeArguments Then
                Return IDOBinder.IDOCall(idmop, memberName, arguments, argumentNames, copyBack, ignoreReturn)
            Else
                Return ObjectLateCall(instance, type, memberName, arguments,
                    argumentNames, typeArguments, copyBack, ignoreReturn)
            End If
        End Function

        'This method is only called from DynamicMethods generated at runtime
        <Obsolete("do not use this method", True)>
        <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
        <DebuggerHiddenAttribute()> <DebuggerStepThroughAttribute()>
        Public Shared Function FallbackCall(
                ByVal instance As Object,
                ByVal memberName As String,
                ByVal arguments As Object(),
                ByVal argumentNames As String(),
                ByVal ignoreReturn As Boolean) As Object

            Return ObjectLateCall(instance, Nothing, memberName, arguments,
                argumentNames, NoTypeArguments, IDOBinder.GetCopyBack(), ignoreReturn)
        End Function

        <DebuggerHiddenAttribute()> <DebuggerStepThroughAttribute()>
        Private Shared Function ObjectLateCall(
                ByVal instance As Object,
                ByVal type As System.Type,
                ByVal memberName As String,
                ByVal arguments As Object(),
                ByVal argumentNames As String(),
                ByVal typeArguments As System.Type(),
                ByVal copyBack As Boolean(),
                ByVal ignoreReturn As Boolean) As Object

            Dim baseReference As Container
            If type IsNot Nothing Then
                baseReference = New Container(type)
            Else
                baseReference = New Container(instance)
            End If

            Dim invocationFlags As BindingFlags = BindingFlagsInvokeMethod Or BindingFlagsGetProperty
            If ignoreReturn Then invocationFlags = invocationFlags Or BindingFlagsIgnoreReturn

            Dim failure As ResolutionFailure

            Return CallMethod(
                       baseReference,
                       memberName,
                       arguments,
                       argumentNames,
                       typeArguments,
                       copyBack,
                       invocationFlags,
                       True,
                       failure)
        End Function

        'Quick check to determine if FallbackCall will succeed
        Friend Shared Function CanBindCall(ByVal instance As Object, ByVal memberName As String, ByVal arguments As Object(), ByVal argumentNames As String(), ByVal ignoreReturn As Boolean) As Boolean
            Dim baseReference As New Container(instance)
            Dim invocationFlags As BindingFlags = BindingFlagsInvokeMethod Or BindingFlagsGetProperty
            If ignoreReturn Then invocationFlags = invocationFlags Or BindingFlagsIgnoreReturn

            Dim failure As ResolutionFailure
            Dim members As MemberInfo() = baseReference.GetMembers(memberName, False)
            If members Is Nothing OrElse members.Length = 0 Then
                Return False
            End If

            Dim targetProcedure As Method =
                ResolveCall(
                    baseReference,
                    memberName,
                    members,
                    arguments,
                    argumentNames,
                    NoTypeArguments,
                    invocationFlags,
                    False,
                    failure)

            Return failure = ResolutionFailure.None
        End Function

        ' LateCallInvokeDefault is used to optionally invoke the default action on a call target.
        ' If the arguemnts are non-empty, then it isn't optional, and is treated
        ' as an error if there is no default action.
        ' Currently we can get here only in the process of execution of NewLateBinding.LateCall. 
        <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
        <DebuggerHiddenAttribute()> <DebuggerStepThroughAttribute()>
        Public Shared Function LateCallInvokeDefault(
                ByVal instance As Object,
                ByVal arguments As Object(),
                ByVal argumentNames As String(),
                ByVal reportErrors As Boolean) As Object

            Return InternalLateInvokeDefault(instance, arguments, argumentNames, reportErrors, IDOBinder.GetCopyBack())
        End Function

        ' LateGetInvokeDefault is used to optionally invoke the default action.
        ' If the arguemnts are non-empty, then it isn't optional, and is treated
        ' as an error if there is no default action.
        ' Currently we can get here only in the process of execution of NewLateBinding.LateGet. 
        <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
        <DebuggerHiddenAttribute()> <DebuggerStepThroughAttribute()>
        Public Shared Function LateGetInvokeDefault(
                ByVal instance As Object,
                ByVal arguments As Object(),
                ByVal argumentNames As String(),
                ByVal reportErrors As Boolean) As Object

            ' According to a comment in VBGetBinder.FallbackInvoke, this function is called when
            ' "The DLR was able to resolve o.member, but not o.member(args)"
            ' When NewLateBinding.LateGet is evaluating similar expression itself, it never tries to invoke default action 
            ' if arguments are not empty. It simply returns result of evaluating o.member. I believe, it makes sense
            ' to follow the same logic here. I.e., if there are no arguments, simply return the instance unless it is an IDO.

            If IDOUtils.TryCastToIDMOP(instance) IsNot Nothing OrElse
                (arguments IsNot Nothing AndAlso arguments.Length > 0) _
            Then
                Return InternalLateInvokeDefault(instance, arguments, argumentNames, reportErrors, IDOBinder.GetCopyBack())
            Else
                Return instance
            End If
        End Function

        Private Shared Function InternalLateInvokeDefault(
                ByVal instance As Object,
                ByVal arguments As Object(),
                ByVal argumentNames As String(),
                ByVal reportErrors As Boolean,
                ByVal copyBack As Boolean()) As Object

            Dim idmop As IDynamicMetaObjectProvider = IDOUtils.TryCastToIDMOP(instance)
            If idmop IsNot Nothing Then
                Return IDOBinder.IDOInvokeDefault(idmop, arguments, argumentNames, reportErrors, copyBack)
            Else
                Return ObjectLateInvokeDefault(instance, arguments, argumentNames, reportErrors, copyBack)
            End If
        End Function

        'This method is only called from DynamicMethods generated at runtime
        <Obsolete("do not use this method", True)>
        <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
        <DebuggerHiddenAttribute()> <DebuggerStepThroughAttribute()>
        Public Shared Function FallbackInvokeDefault1(
                ByVal instance As Object,
                ByVal arguments As Object(),
                ByVal argumentNames As String(),
                ByVal reportErrors As Boolean) As Object

            ' Try using the IDO index operation (in case it's an IDO array)
            Return IDOBinder.IDOFallbackInvokeDefault(DirectCast(instance, IDynamicMetaObjectProvider), arguments, argumentNames, reportErrors, IDOBinder.GetCopyBack())
        End Function

        'This method is only called from DynamicMethods generated at runtime
        <Obsolete("do not use this method", True)>
        <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
        <DebuggerHiddenAttribute()> <DebuggerStepThroughAttribute()>
        Public Shared Function FallbackInvokeDefault2(
                ByVal instance As Object,
                ByVal arguments As Object(),
                ByVal argumentNames As String(),
                ByVal reportErrors As Boolean) As Object

            Return ObjectLateInvokeDefault(instance, arguments, argumentNames, reportErrors, IDOBinder.GetCopyBack())
        End Function

        <DebuggerHiddenAttribute()> <DebuggerStepThroughAttribute()>
        Private Shared Function ObjectLateInvokeDefault(
                ByVal instance As Object,
                ByVal arguments As Object(),
                ByVal argumentNames As String(),
                ByVal reportErrors As Boolean,
                ByVal copyBack As Boolean()) As Object

            Dim baseReference As Container = New Container(instance)
            Dim failure As ResolutionFailure
            Dim result As Object = InternalLateIndexGet(
                instance, arguments, argumentNames,
                reportErrors OrElse arguments.Length <> 0 OrElse baseReference.IsArray,
                failure, copyBack)
            Return If(failure = ResolutionFailure.None, result, instance)
        End Function

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Public Shared Function LateIndexGet(
                ByVal instance As Object,
                ByVal arguments() As Object,
                ByVal argumentNames() As String) As Object

            Return InternalLateInvokeDefault(instance, arguments, argumentNames, True, Nothing)
        End Function

        Private Shared Function LateIndexGet(
                ByVal instance As Object,
                ByVal arguments() As Object,
                ByVal argumentNames() As String,
                ByVal copyBack As Boolean()) As Object

            Return InternalLateInvokeDefault(instance, arguments, argumentNames, True, copyBack)
        End Function

        Private Shared Function InternalLateIndexGet(
                ByVal instance As Object,
                ByVal arguments() As Object,
                ByVal argumentNames() As String,
                ByVal reportErrors As Boolean,
                ByRef failure As ResolutionFailure,
                ByVal copyBack As Boolean()) As Object

            failure = ResolutionFailure.None

            If arguments Is Nothing Then arguments = NoArguments
            If argumentNames Is Nothing Then argumentNames = NoArgumentNames

            Dim baseReference As Container = New Container(instance)

            'An r-value expression o(a) has two possible forms:
            '    1: o(a)    array lookup--where o is an array object and a is a set of indices
            '    2: o.d(a)  default member access--where o has default method/property d

            If baseReference.IsArray Then
                'This is an array lookup o(a).

                If argumentNames.Length > 0 Then
                    failure = ResolutionFailure.InvalidArgument

                    If reportErrors Then
                        Throw New ArgumentException(GetResourceString(SR.Argument_InvalidNamedArgs))
                    End If

                    Return Nothing
                End If

                ' Initialize the copy back array to all ByVal
                ResetCopyback(copyBack)
                Return baseReference.GetArrayValue(arguments)
            End If

            'This is a default member access o.d(a), which is a call to method "".
            Return CallMethod(
                       baseReference,
                       "",
                       arguments,
                       argumentNames,
                       NoTypeArguments,
                       copyBack,
                       BindingFlagsInvokeMethod Or BindingFlagsGetProperty,
                       reportErrors,
                       failure)
        End Function

        Friend Shared Function CanBindInvokeDefault(
                ByVal instance As Object,
                ByVal arguments As Object(),
                ByVal argumentNames As String(),
                ByVal reportErrors As Boolean) As Boolean

            Dim baseReference As Container = New Container(instance)
            reportErrors = reportErrors OrElse arguments.Length <> 0 OrElse baseReference.IsArray

            If Not reportErrors Then
                Return True
            End If

            'An r-value expression o(a) has two possible forms:
            '    1: o(a)    array lookup--where o is an array object and a is a set of indices
            '    2: o.d(a)  default member access--where o has default method/property d

            If baseReference.IsArray Then
                'This is an array lookup o(a).
                Return argumentNames.Length = 0
            End If

            'This is a default member access o.d(a), which is a call to method "".
            Return CanBindCall(instance, "", arguments, argumentNames, False)
        End Function

        Friend Shared Sub ResetCopyback(ByVal copyBack As Boolean())
            If copyBack IsNot Nothing Then
                ' Initialize the copy back array to all ByVal.
                For index As Integer = 0 To copyBack.Length - 1
                    copyBack(index) = False
                Next
            End If
        End Sub

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Public Shared Function LateGet(
                ByVal instance As Object,
                ByVal type As System.Type,
                ByVal memberName As String,
                ByVal arguments As Object(),
                ByVal argumentNames As String(),
                ByVal typeArguments As Type(),
                ByVal copyBack As Boolean()) As Object

            If arguments Is Nothing Then arguments = NoArguments
            If argumentNames Is Nothing Then argumentNames = NoArgumentNames
            If typeArguments Is Nothing Then typeArguments = NoTypeArguments

            Dim baseReference As Container
            If type IsNot Nothing Then
                baseReference = New Container(type)
            Else
                baseReference = New Container(instance)
            End If

            Dim invocationFlags As BindingFlags = BindingFlagsInvokeMethod Or BindingFlagsGetProperty

            Dim idmop As IDynamicMetaObjectProvider = IDOUtils.TryCastToIDMOP(instance)
            If idmop IsNot Nothing AndAlso typeArguments Is NoTypeArguments Then
                Return IDOBinder.IDOGet(idmop, memberName, arguments, argumentNames, copyBack)
            Else
                Return ObjectLateGet(instance, type, memberName, arguments, argumentNames, typeArguments, copyBack)
            End If
        End Function 'LateGet

        'This method is only called from DynamicMethods generated at runtime
        <Obsolete("do not use this method", True)>
        <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
        <DebuggerHiddenAttribute()> <DebuggerStepThroughAttribute()>
        Public Shared Function FallbackGet(
                ByVal instance As Object,
                ByVal memberName As String,
                ByVal arguments As Object(),
                ByVal argumentNames As String()) As Object

            Return ObjectLateGet(instance, Nothing, memberName, arguments, argumentNames, NoTypeArguments, IDOBinder.GetCopyBack())
        End Function 'FallbackGet

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Private Shared Function ObjectLateGet(
                ByVal instance As Object,
                ByVal type As System.Type,
                ByVal memberName As String,
                ByVal arguments As Object(),
                ByVal argumentNames As String(),
                ByVal typeArguments As Type(),
                ByVal copyBack As Boolean()) As Object

            Dim baseReference As Container
            If type IsNot Nothing Then
                baseReference = New Container(type)
            Else
                baseReference = New Container(instance)
            End If

            Dim invocationFlags As BindingFlags = BindingFlagsInvokeMethod Or BindingFlagsGetProperty

            Dim members As MemberInfo() = baseReference.GetMembers(memberName, True)

            If members(0).MemberType = MemberTypes.Field Then
                If typeArguments.Length > 0 Then
                    Throw New ArgumentException(GetResourceString(SR.Argument_InvalidValue))
                End If

                Dim fieldValue As Object = baseReference.GetFieldValue(DirectCast(members(0), FieldInfo))
                If arguments.Length = 0 Then
                    'This is a simple field access.
                    Return fieldValue
                Else
                    'This is an indexed field access.
                    Return LateIndexGet(fieldValue, arguments, argumentNames, copyBack)
                End If
            End If

            If argumentNames.Length > arguments.Length OrElse
               (copyBack IsNot Nothing AndAlso copyBack.Length <> arguments.Length) Then
                Throw New ArgumentException(GetResourceString(SR.Argument_InvalidValue))
            End If

            Dim failure As OverloadResolution.ResolutionFailure
            Dim targetProcedure As Method =
                ResolveCall(
                    baseReference,
                    memberName,
                    members,
                    arguments,
                    argumentNames,
                    typeArguments,
                    invocationFlags,
                    False,
                    failure)

            If failure = OverloadResolution.ResolutionFailure.None Then
                Return baseReference.InvokeMethod(targetProcedure, arguments, copyBack, invocationFlags)

            ElseIf arguments.Length > 0 AndAlso members.Length = 1 AndAlso IsZeroArgumentCall(members(0)) Then
                ' Dev10 #579405: For default property transformation the group should contain just 1 item
                '                and that item should take no arguments.

                targetProcedure =
                    ResolveCall(
                        baseReference,
                        memberName,
                        members,
                        NoArguments,
                        NoArgumentNames,
                        typeArguments,
                        invocationFlags,
                        False,
                        failure)

                If failure = OverloadResolution.ResolutionFailure.None Then
                    Dim result As Object = baseReference.InvokeMethod(targetProcedure, NoArguments, Nothing, invocationFlags)

                    'For backwards compatibility, throw a missing member exception if the intermediate result is Nothing.
                    If result Is Nothing Then
                        Throw New MissingMemberException(
                                GetResourceString(
                                    SR.IntermediateLateBoundNothingResult1,
                                    targetProcedure.ToString,
                                    baseReference.VBFriendlyName))
                    End If

                    result = InternalLateIndexGet(
                                result,
                                arguments,
                                argumentNames,
                                False,
                                failure,
                                copyBack)

                    If failure = ResolutionFailure.None Then
                        Return result
                    End If
                End If

            End If

            'Every attempt to make this work failed.  Redo the original call resolution to generate errors.
            ResolveCall(
                baseReference,
                memberName,
                members,
                arguments,
                argumentNames,
                typeArguments,
                invocationFlags,
                True,
                failure)
            Debug.Assert(False, "the resolution should have thrown an exception")
            Throw New InternalErrorException()
        End Function 'ObjectLateGet

        'Quick check to determine if FallbackGet will succeed
        Friend Shared Function CanBindGet(ByVal instance As Object, ByVal memberName As String, ByVal arguments As Object(), ByVal argumentNames As String()) As Boolean
            Dim baseReference As New Container(instance)
            Dim invocationFlags As BindingFlags = BindingFlagsInvokeMethod Or BindingFlagsGetProperty

            Dim failure As ResolutionFailure
            Dim members As MemberInfo() = baseReference.GetMembers(memberName, False)
            If members Is Nothing OrElse members.Length = 0 Then
                Return False
            End If

            If members(0).MemberType = MemberTypes.Field Then
                'There may be additional work after the field get, but as far
                'as we're concerned the binding succeeded
                Return True
            End If

            Dim targetProcedure As Method =
                ResolveCall(
                    baseReference,
                    memberName,
                    members,
                    arguments,
                    argumentNames,
                    NoTypeArguments,
                    invocationFlags,
                    False,
                    failure)

            If failure = OverloadResolution.ResolutionFailure.None Then
                Return True
            End If

            If arguments.Length > 0 AndAlso members.Length = 1 AndAlso IsZeroArgumentCall(members(0)) Then
                ' Dev10 #579405: For default property transformation the group should contain just 1 item
                '                and that item should take no arguments.

                targetProcedure =
                    ResolveCall(
                        baseReference,
                        memberName,
                        members,
                        NoArguments,
                        NoArgumentNames,
                        NoTypeArguments,
                        invocationFlags,
                        False,
                        failure)

                If failure = OverloadResolution.ResolutionFailure.None Then
                    'There will be additional work after the first call, but as
                    'far as we're concerned the binding succeeded.
                    Return True
                End If
            End If

            'Every attempt at binding failed, return false so we use the IDO's error
            Return False
        End Function

        ' Determines if the member is a zero argument method or property
        Friend Shared Function IsZeroArgumentCall(ByVal member As MemberInfo) As Boolean
            Return ((member.MemberType = MemberTypes.Method AndAlso
                        DirectCast(member, MethodInfo).GetParameters().Length = 0) OrElse
                    (member.MemberType = MemberTypes.Property AndAlso
                        DirectCast(member, PropertyInfo).GetIndexParameters().Length = 0))
        End Function

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Public Shared Sub LateIndexSetComplex(
                ByVal instance As Object,
                ByVal arguments As Object(),
                ByVal argumentNames As String(),
                ByVal optimisticSet As Boolean,
                ByVal rValueBase As Boolean)

            Dim idmop As IDynamicMetaObjectProvider = IDOUtils.TryCastToIDMOP(instance)
            If idmop IsNot Nothing Then
                Call IDOBinder.IDOIndexSetComplex(idmop, arguments, argumentNames, optimisticSet, rValueBase)
            Else
                Call ObjectLateIndexSetComplex(instance, arguments, argumentNames, optimisticSet, rValueBase)
                Return
            End If
        End Sub

        'This method is only called from DynamicMethods generated at runtime
        <Obsolete("do not use this method", True)>
        <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Public Shared Sub FallbackIndexSetComplex(
                ByVal instance As Object,
                ByVal arguments As Object(),
                ByVal argumentNames As String(),
                ByVal optimisticSet As Boolean,
                ByVal rValueBase As Boolean)

            ObjectLateIndexSetComplex(instance, arguments, argumentNames, optimisticSet, rValueBase)
        End Sub 'FallbackIndexSetComplex

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Friend Shared Sub ObjectLateIndexSetComplex(
                ByVal instance As Object,
                ByVal arguments As Object(),
                ByVal argumentNames As String(),
                ByVal optimisticSet As Boolean,
                ByVal rValueBase As Boolean)

            If arguments Is Nothing Then arguments = NoArguments
            If argumentNames Is Nothing Then argumentNames = NoArgumentNames

            Dim baseReference As Container = New Container(instance)

            'An l-value expression o(a) has two possible forms:
            '    1: o(a) = v    array lookup--where o is an array object and a is a set of indices
            '    2: o.d(a) = v  default member access--where o has default method/property d

            If baseReference.IsArray Then
                'This is an array lookup and assignment o(a) = v.

                If argumentNames.Length > 0 Then
                    Throw New ArgumentException(GetResourceString(SR.Argument_InvalidNamedArgs))
                End If

                baseReference.SetArrayValue(arguments)
                Return
            End If

            If argumentNames.Length > arguments.Length Then
                Throw New ArgumentException(GetResourceString(SR.Argument_InvalidValue))
            End If

            If arguments.Length < 1 Then
                'We're binding to a Set, we must have at least the Value argument.
                Throw New ArgumentException(GetResourceString(SR.Argument_InvalidValue))
            End If

            Dim methodName As String = ""

            Dim invocationFlags As BindingFlags = BindingFlagsSetProperty

            Dim members As MemberInfo() = baseReference.GetMembers(methodName, True) 'MethodName is set during this call.

            Dim failure As OverloadResolution.ResolutionFailure
            Dim targetProcedure As Method =
                ResolveCall(
                    baseReference,
                    methodName,
                    members,
                    arguments,
                    argumentNames,
                    NoTypeArguments,
                    invocationFlags,
                    False,
                    failure)

            If failure = OverloadResolution.ResolutionFailure.None Then

                If rValueBase AndAlso baseReference.IsValueType Then
                    Throw New Exception(
                            GetResourceString(
                                SR.RValueBaseForValueType,
                                baseReference.VBFriendlyName,
                                baseReference.VBFriendlyName))
                End If

                baseReference.InvokeMethod(targetProcedure, arguments, Nothing, invocationFlags)
                Return

            ElseIf optimisticSet Then
                Return

            Else
                'Redo the resolution to generate errors.
                ResolveCall(
                    baseReference,
                    methodName,
                    members,
                    arguments,
                    argumentNames,
                    NoTypeArguments,
                    invocationFlags,
                    True,
                    failure)
            End If


            Debug.Assert(False, "the resolution should have thrown an exception - should never reach here")
            Throw New InternalErrorException()

        End Sub

        'Determines if ObjectLateIndexSetComplex can succeed
        'Used by IDOBinder
        Friend Shared Function CanIndexSetComplex(
                ByVal instance As Object,
                ByVal arguments As Object(),
                ByVal argumentNames As String(),
                ByVal optimisticSet As Boolean,
                ByVal rValueBase As Boolean) As Boolean

            Dim baseReference As Container = New Container(instance)

            'An l-value expression o(a) has two possible forms:
            '    1: o(a) = v    array lookup--where o is an array object and a is a set of indices
            '    2: o.d(a) = v  default member access--where o has default method/property d

            If baseReference.IsArray Then
                'This is an array lookup and assignment o(a) = v.
                Return argumentNames.Length = 0
            End If

            Dim methodName As String = ""
            Dim invocationFlags As BindingFlags = BindingFlagsSetProperty

            Dim members As MemberInfo() = baseReference.GetMembers(methodName, False) 'MethodName is set during this call.
            If members Is Nothing OrElse members.Length = 0 Then
                Return False
            End If

            Dim failure As OverloadResolution.ResolutionFailure
            Dim targetProcedure As Method =
                ResolveCall(
                    baseReference,
                    methodName,
                    members,
                    arguments,
                    argumentNames,
                    NoTypeArguments,
                    invocationFlags,
                    False,
                    failure)

            If failure = OverloadResolution.ResolutionFailure.None Then
                If rValueBase AndAlso baseReference.IsValueType Then
                    Return False
                End If

                Return True
            End If

            Return optimisticSet
        End Function

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Public Shared Sub LateIndexSet(
                ByVal instance As Object,
                ByVal arguments() As Object,
                ByVal argumentNames() As String)

            Dim idmop As IDynamicMetaObjectProvider = IDOUtils.TryCastToIDMOP(instance)
            If idmop IsNot Nothing Then
                IDOBinder.IDOIndexSet(idmop, arguments, argumentNames)
                Return
            Else
                ObjectLateIndexSet(instance, arguments, argumentNames)
                Return
            End If
        End Sub 'LateIndexSet

        'This method is only called from DynamicMethods generated at runtime
        <Obsolete("do not use this method", True)>
        <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
        <DebuggerHiddenAttribute()> <DebuggerStepThroughAttribute()>
        Public Shared Sub FallbackIndexSet(
                ByVal instance As Object,
                ByVal arguments() As Object,
                ByVal argumentNames() As String)

            ObjectLateIndexSet(instance, arguments, argumentNames)
        End Sub 'FallbackIndexSet

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Private Shared Sub ObjectLateIndexSet(
                ByVal instance As Object,
                ByVal arguments() As Object,
                ByVal argumentNames() As String)

            ObjectLateIndexSetComplex(instance, arguments, argumentNames, False, False)
            Return
        End Sub 'ObjectLateIndexSet

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Public Shared Sub LateSetComplex(
                ByVal instance As Object,
                ByVal type As Type,
                ByVal memberName As String,
                ByVal arguments() As Object,
                ByVal argumentNames() As String,
                ByVal typeArguments() As Type,
                ByVal optimisticSet As Boolean,
                ByVal rValueBase As Boolean)

            Dim idmop As IDynamicMetaObjectProvider = IDOUtils.TryCastToIDMOP(instance)
            If idmop IsNot Nothing AndAlso typeArguments Is Nothing Then
                IDOBinder.IDOSetComplex(idmop, memberName, arguments, argumentNames, optimisticSet, rValueBase)
            Else
                ObjectLateSetComplex(instance, type,
                    memberName, arguments, argumentNames, typeArguments, optimisticSet, rValueBase)
                Return
            End If
        End Sub

        'This method is only called from DynamicMethods generated at runtime
        <Obsolete("do not use this method", True)>
        <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Public Shared Sub FallbackSetComplex(
                ByVal instance As Object,
                ByVal memberName As String,
                ByVal arguments() As Object,
                ByVal optimisticSet As Boolean,
                ByVal rValueBase As Boolean)

            ObjectLateSetComplex(
                instance, Nothing, memberName, arguments, New String() {},
                NoTypeArguments, optimisticSet, rValueBase)
        End Sub 'FallbackSetComplex

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Friend Shared Sub ObjectLateSetComplex(
                ByVal instance As Object,
                ByVal type As Type,
                ByVal memberName As String,
                ByVal arguments() As Object,
                ByVal argumentNames() As String,
                ByVal typeArguments() As Type,
                ByVal optimisticSet As Boolean,
                ByVal rValueBase As Boolean)

            Const defaultCallType As CallType = CType(0, CallType)
            LateSet(instance, type, memberName, arguments, argumentNames, typeArguments, optimisticSet, rValueBase, defaultCallType)
        End Sub

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Public Shared Sub LateSet(
                ByVal instance As Object,
                ByVal type As Type,
                ByVal memberName As String,
                ByVal arguments() As Object,
                ByVal argumentNames() As String,
                ByVal typeArguments As Type())

            Dim idmop As IDynamicMetaObjectProvider = IDOUtils.TryCastToIDMOP(instance)
            If idmop IsNot Nothing AndAlso typeArguments Is Nothing Then
                IDOBinder.IDOSet(idmop, memberName, argumentNames, arguments)
            Else
                ObjectLateSet(instance, type, memberName, arguments, argumentNames, typeArguments)
                Return
            End If
        End Sub

        'This method is only called from DynamicMethods generated at runtime
        <Obsolete("do not use this method", True)>
        <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Public Shared Sub FallbackSet(
                ByVal instance As Object,
                ByVal memberName As String,
                ByVal arguments() As Object)

            ObjectLateSet(instance, Nothing, memberName, arguments, NoArgumentNames, NoTypeArguments)
        End Sub

        Friend Shared Sub ObjectLateSet(
                ByVal instance As Object,
                ByVal type As Type,
                ByVal memberName As String,
                ByVal arguments() As Object,
                ByVal argumentNames() As String,
                ByVal typeArguments As Type())

            Const defaultCallType As CallType = CType(0, CallType)
            LateSet(instance, type, memberName, arguments, argumentNames,
                typeArguments, False, False, defaultCallType)
            Return
        End Sub

        <DebuggerHiddenAttribute(), DebuggerStepThroughAttribute()>
        Public Shared Sub LateSet(
                ByVal instance As Object,
                ByVal type As Type,
                ByVal memberName As String,
                ByVal arguments As Object(),
                ByVal argumentNames As String(),
                ByVal typeArguments As Type(),
                ByVal optimisticSet As Boolean,
                ByVal rValueBase As Boolean,
                ByVal callType As CallType)

            If arguments Is Nothing Then arguments = NoArguments
            If argumentNames Is Nothing Then argumentNames = NoArgumentNames
            If typeArguments Is Nothing Then typeArguments = NoTypeArguments

            Dim baseReference As Container
            If type IsNot Nothing Then
                baseReference = New Container(type)
            Else
                baseReference = New Container(instance)
            End If

            Dim invocationFlags As BindingFlags

            ' If we have a IDO that implements TryGetMember for a property but not TrySetMember then we could land up
            ' here and with an optimistic set but we don't want to throw an exception if the property is not found.
            ' Swallow the exception and return quietly if this is a readonly IDO property doing an optimistic set.
            Dim members As MemberInfo() = baseReference.GetMembers(memberName, Not optimisticSet)

            If members.Length = 0 And optimisticSet Then
                Return
            End If

            If members(0).MemberType = MemberTypes.Field Then

                If typeArguments.Length > 0 Then
                    Throw New ArgumentException(GetResourceString(SR.Argument_InvalidValue))
                End If

                If arguments.Length = 1 Then
                    If rValueBase AndAlso baseReference.IsValueType Then
                        Throw New Exception(
                                GetResourceString(
                                    SR.RValueBaseForValueType,
                                    baseReference.VBFriendlyName,
                                    baseReference.VBFriendlyName))
                    End If
                    'This is a simple field set.
                    baseReference.SetFieldValue(DirectCast(members(0), FieldInfo), arguments(0))
                    Return
                Else
                    'This is an indexed field set.
                    Dim fieldValue As Object = baseReference.GetFieldValue(DirectCast(members(0), FieldInfo))
                    LateIndexSetComplex(fieldValue, arguments, argumentNames, optimisticSet, True)
                    Return
                End If
            End If

            invocationFlags = BindingFlagsSetProperty

            If argumentNames.Length > arguments.Length Then
                Throw New ArgumentException(GetResourceString(SR.Argument_InvalidValue))
            End If

            Dim failure As OverloadResolution.ResolutionFailure
            Dim targetProcedure As Method

            If typeArguments.Length = 0 Then

                targetProcedure =
                    ResolveCall(
                        baseReference,
                        memberName,
                        members,
                        arguments,
                        argumentNames,
                        NoTypeArguments,
                        invocationFlags,
                        False,
                        failure)

                If failure = OverloadResolution.ResolutionFailure.None Then
                    If rValueBase AndAlso baseReference.IsValueType Then
                        Throw New Exception(
                                GetResourceString(
                                    SR.RValueBaseForValueType,
                                    baseReference.VBFriendlyName,
                                    baseReference.VBFriendlyName))
                    End If

                    baseReference.InvokeMethod(targetProcedure, arguments, Nothing, invocationFlags)
                    Return
                End If

            End If

            Dim secondaryInvocationFlags As BindingFlags =
                    BindingFlagsInvokeMethod Or BindingFlagsGetProperty

            If failure = OverloadResolution.ResolutionFailure.None OrElse failure = OverloadResolution.ResolutionFailure.MissingMember Then

                targetProcedure =
                    ResolveCall(
                        baseReference,
                        memberName,
                        members,
                        NoArguments,
                        NoArgumentNames,
                        typeArguments,
                        secondaryInvocationFlags,
                        False,
                        failure)

                If failure = OverloadResolution.ResolutionFailure.None Then
                    Dim result As Object =
                        baseReference.InvokeMethod(targetProcedure, NoArguments, Nothing, secondaryInvocationFlags)

                    'For backwards compatibility, throw a missing member exception if the intermediate result is Nothing.
                    If result Is Nothing Then
                        Throw New MissingMemberException(
                                GetResourceString(
                                    SR.IntermediateLateBoundNothingResult1,
                                    targetProcedure.ToString,
                                    baseReference.VBFriendlyName))
                    End If

                    LateIndexSetComplex(result, arguments, argumentNames, optimisticSet, True)
                    Return
                End If
            End If

            If optimisticSet Then
                Return
            End If

            'Everything failed, so give errors. Redo the first attempt to generate the errors.
            If typeArguments.Length = 0 Then
                ResolveCall(
                    baseReference,
                    memberName,
                    members,
                    arguments,
                    argumentNames,
                    typeArguments,
                    invocationFlags,
                    True,
                    failure)

            Else
                ResolveCall(
                    baseReference,
                    memberName,
                    members,
                    NoArguments,
                    NoArgumentNames,
                    typeArguments,
                    secondaryInvocationFlags,
                    True,
                    failure)
            End If

            Debug.Assert(False, "the resolution should have thrown an exception")
            Throw New InternalErrorException()
            Return
        End Sub

        'Determines if LateSet will succeed. Used by IDOBinder.
        Friend Shared Function CanBindSet(ByVal instance As Object, ByVal memberName As String, ByVal value As Object, ByVal optimisticSet As Boolean, ByVal rValueBase As Boolean) As Boolean
            Dim baseReference As New Container(instance)
            Dim arguments As Object() = {value}

            Dim members As MemberInfo() = baseReference.GetMembers(memberName, False)
            If members Is Nothing OrElse members.Length = 0 Then
                Return False
            End If

            If members(0).MemberType = MemberTypes.Field Then
                If arguments.Length = 1 AndAlso rValueBase AndAlso baseReference.IsValueType Then
                    Return False
                End If

                'There may be more work (for indexed fields), but if we got
                'this far we consider it success.
                Return True
            End If

            Dim failure As OverloadResolution.ResolutionFailure
            Dim targetProcedure As Method =
                ResolveCall(
                    baseReference,
                    memberName,
                    members,
                    arguments,
                    NoArgumentNames,
                    NoTypeArguments,
                    BindingFlagsSetProperty,
                    False,
                    failure)

            If failure = OverloadResolution.ResolutionFailure.None Then
                If rValueBase AndAlso baseReference.IsValueType Then
                    Return False
                End If

                Return True
            End If

            Dim secondaryInvocationFlags As BindingFlags = BindingFlagsInvokeMethod Or BindingFlagsGetProperty

            If failure = OverloadResolution.ResolutionFailure.MissingMember Then
                targetProcedure =
                    ResolveCall(
                        baseReference,
                        memberName,
                        members,
                        NoArguments,
                        NoArgumentNames,
                        NoTypeArguments,
                        secondaryInvocationFlags,
                        False,
                        failure)

                If failure = OverloadResolution.ResolutionFailure.None Then
                    'There is work (to call the method/prop), but if we got
                    'this far we consider it success.
                    Return True
                End If
            End If

            'Everything failed, so use the IDO's error if any
            'Unless we're doing an optimistic set, in which case this is considered success.
            Return optimisticSet
        End Function

        Private Shared Function CallMethod(
                ByVal baseReference As Container,
                ByVal methodName As String,
                ByVal arguments As Object(),
                ByVal argumentNames As String(),
                ByVal typeArguments As System.Type(),
                ByVal copyBack As Boolean(),
                ByVal invocationFlags As BindingFlags,
                ByVal reportErrors As Boolean,
                ByRef failure As ResolutionFailure) As Object

            Debug.Assert(baseReference IsNot Nothing, "Nothing unexpected")
            Debug.Assert(arguments IsNot Nothing, "Nothing unexpected")
            Debug.Assert(argumentNames IsNot Nothing, "Nothing unexpected")
            Debug.Assert(typeArguments IsNot Nothing, "Nothing unexpected")

            failure = ResolutionFailure.None

            If argumentNames.Length > arguments.Length OrElse
               (copyBack IsNot Nothing AndAlso copyBack.Length <> arguments.Length) Then
                failure = ResolutionFailure.InvalidArgument

                If reportErrors Then
                    Throw New ArgumentException(GetResourceString(SR.Argument_InvalidValue))
                End If

                Return Nothing
            End If

            If HasFlag(invocationFlags, BindingFlagsSetProperty) AndAlso arguments.Length < 1 Then
                failure = ResolutionFailure.InvalidArgument

                If reportErrors Then
                    'If we're binding to a Set, we must have at least the Value argument.
                    Throw New ArgumentException(GetResourceString(SR.Argument_InvalidValue))
                End If

                Return Nothing
            End If

            Dim members As MemberInfo() = baseReference.GetMembers(methodName, reportErrors)

            If members Is Nothing OrElse members.Length = 0 Then
                failure = ResolutionFailure.MissingMember

                If reportErrors Then
                    Debug.Assert(False, "If ReportErrors is True, GetMembers should have thrown above")
                    members = baseReference.GetMembers(methodName, True)
                End If

                Return Nothing
            End If

            Dim targetProcedure As Method =
                ResolveCall(
                    baseReference,
                    methodName,
                    members,
                    arguments,
                    argumentNames,
                    typeArguments,
                    invocationFlags,
                    reportErrors,
                    failure)

            If failure = ResolutionFailure.None Then
                Return baseReference.InvokeMethod(targetProcedure, arguments, copyBack, invocationFlags)
            End If

            Return Nothing
        End Function

        Friend Shared Function MatchesPropertyRequirements(ByVal targetProcedure As Method, ByVal flags As BindingFlags) As MethodInfo
            Debug.Assert(targetProcedure.IsProperty, "advertised property method isn't.")

            If HasFlag(flags, BindingFlagsSetProperty) Then
                Return targetProcedure.AsProperty.GetSetMethod
            Else
                Return targetProcedure.AsProperty.GetGetMethod
            End If
        End Function

        Friend Shared Function ReportPropertyMismatch(ByVal targetProcedure As Method, ByVal flags As BindingFlags) As Exception
            Debug.Assert(targetProcedure.IsProperty, "advertised property method isn't.")

            If HasFlag(flags, BindingFlagsSetProperty) Then
                Debug.Assert(targetProcedure.AsProperty.GetSetMethod Is Nothing, "expected error condition")
                Return New MissingMemberException(
                    GetResourceString(SR.NoSetProperty1, targetProcedure.AsProperty.Name))
            Else
                Debug.Assert(targetProcedure.AsProperty.GetGetMethod Is Nothing, "expected error condition")
                Return New MissingMemberException(
                    GetResourceString(SR.NoGetProperty1, targetProcedure.AsProperty.Name))
            End If
        End Function

        Friend Shared Function ResolveCall(
                ByVal baseReference As Container,
                ByVal methodName As String,
                ByVal members As MemberInfo(),
                ByVal arguments As Object(),
                ByVal argumentNames As String(),
                ByVal typeArguments As Type(),
                ByVal lookupFlags As BindingFlags,
                ByVal reportErrors As Boolean,
                ByRef failure As OverloadResolution.ResolutionFailure) As Method

            Debug.Assert(baseReference IsNot Nothing, "expected a base reference")
            Debug.Assert(methodName IsNot Nothing, "expected method name")
            Debug.Assert(members IsNot Nothing AndAlso members.Length > 0, "expected members")
            Debug.Assert(arguments IsNot Nothing AndAlso
                         argumentNames IsNot Nothing AndAlso
                         typeArguments IsNot Nothing AndAlso
                         argumentNames.Length <= arguments.Length,
                         "expected valid argument arrays")

            failure = OverloadResolution.ResolutionFailure.None

            If members(0).MemberType <> MemberTypes.Method AndAlso
               members(0).MemberType <> MemberTypes.Property Then

                failure = OverloadResolution.ResolutionFailure.InvalidTarget
                If reportErrors Then
                    'This expression is not a procedure, but occurs as the target of a procedure call.
                    Throw New ArgumentException(
                        GetResourceString(SR.ExpressionNotProcedure, methodName, baseReference.VBFriendlyName))
                End If
                Return Nothing
            End If

            'When binding to Property Set accessors, strip off the last Value argument
            'because it does not participate in overload resolution.

            Dim savedArguments As Object()
            Dim argumentCount As Integer = arguments.Length
            Dim lastArgument As Object = Nothing

            If HasFlag(lookupFlags, BindingFlagsSetProperty) Then
                If arguments.Length = 0 Then
                    failure = OverloadResolution.ResolutionFailure.InvalidArgument

                    If reportErrors Then
                        Throw New InvalidCastException(
                            GetResourceString(SR.PropertySetMissingArgument1, methodName))
                    End If

                    Return Nothing
                End If

                savedArguments = arguments
                arguments = New Object(argumentCount - 2) {}
                System.Array.Copy(savedArguments, arguments, arguments.Length)
                lastArgument = savedArguments(argumentCount - 1)
            End If

            Dim resolutionResult As Method =
                ResolveOverloadedCall(
                    methodName,
                    members,
                    arguments,
                    argumentNames,
                    typeArguments,
                    lookupFlags,
                    reportErrors,
                    failure,
                    baseReference)

            Debug.Assert(failure = OverloadResolution.ResolutionFailure.None OrElse Not reportErrors,
                         "if resolution failed, an exception should have been thrown")

            If failure <> OverloadResolution.ResolutionFailure.None Then
                Debug.Assert(resolutionResult Is Nothing, "resolution failed so should have no result")
                Return Nothing
            End If

            Debug.Assert(resolutionResult IsNot Nothing, "resolution didn't fail, so should have result")

#If BINDING_LOG Then
            Console.WriteLine("== RESULT ==")
            Console.WriteLine(ResolutionResult.DeclaringType.Name & "::" & ResolutionResult.ToString)
            Console.WriteLine()
#End If

            'Overload resolution will potentially select one method before validating arguments.
            'Validate those arguments now.
            If Not resolutionResult.ArgumentsValidated Then

                If Not CanMatchArguments(resolutionResult, arguments, argumentNames, typeArguments, False, Nothing) Then

                    failure = OverloadResolution.ResolutionFailure.InvalidArgument

                    If reportErrors Then
                        Dim errorMessage As String = ""
                        Dim errors As New List(Of String)

                        Dim result As Boolean =
                            CanMatchArguments(resolutionResult, arguments, argumentNames, typeArguments, False, errors)

                        Debug.Assert(result = False AndAlso errors.Count > 0, "expected this candidate to fail")

                        For Each errorString As String In errors
                            errorMessage &= vbCrLf & "    " & errorString
                        Next

                        errorMessage = GetResourceString(SR.MatchArgumentFailure2, resolutionResult.ToString, errorMessage)
                        'We are missing a member which can match the arguments, so throw a missing member exception.
                        'The InvalidCastException is thrown only for back compat.  It would
                        'be nice if the latebinder had its own set of exceptions to throw.
                        Throw New InvalidCastException(errorMessage)
                    End If

                    Return Nothing
                End If

            End If

            'Once we've gotten this far, we've selected a member. From this point on, we determine
            'if the member can be called given the context.

            'Check that the resulting binding makes sense in the current context.
            If resolutionResult.IsProperty Then
                If MatchesPropertyRequirements(resolutionResult, lookupFlags) Is Nothing Then
                    failure = OverloadResolution.ResolutionFailure.InvalidTarget
                    If reportErrors Then
                        Throw ReportPropertyMismatch(resolutionResult, lookupFlags)
                    End If
                    Return Nothing
                End If
            Else
                Debug.Assert(resolutionResult.IsMethod, "must be a method")
                If HasFlag(lookupFlags, BindingFlagsSetProperty) Then
                    failure = OverloadResolution.ResolutionFailure.InvalidTarget
                    If reportErrors Then
                        'Methods can't be targets of assignments.
                        Throw New MissingMemberException(
                            GetResourceString(SR.MethodAssignment1, resolutionResult.AsMethod.Name))
                    End If
                    Return Nothing
                End If
            End If

            If HasFlag(lookupFlags, BindingFlagsSetProperty) Then
                'Need to match the Value argument for the property set call.
                Debug.Assert(GetCallTarget(resolutionResult, lookupFlags).Name.StartsWith("set_"), "expected set accessor")

                Dim parameters As ParameterInfo() = GetCallTarget(resolutionResult, lookupFlags).GetParameters
                Dim lastParameter As ParameterInfo = parameters(parameters.Length - 1)
                If Not CanPassToParameter(
                            resolutionResult,
                            lastArgument,
                            lastParameter,
                            False,
                            False,
                            Nothing,
                            Nothing,
                            Nothing) Then

                    failure = OverloadResolution.ResolutionFailure.InvalidArgument

                    If reportErrors Then
                        Dim errorMessage As String = ""
                        Dim errors As New List(Of String)

                        Dim result As Boolean =
                            CanPassToParameter(
                                resolutionResult,
                                lastArgument,
                                lastParameter,
                                False,
                                False,
                                errors,
                                Nothing,
                                Nothing)

                        Debug.Assert(result = False AndAlso errors.Count > 0, "expected this candidate to fail")

                        For Each errorString As String In errors
                            errorMessage &= vbCrLf & "    " & errorString
                        Next

                        errorMessage = GetResourceString(SR.MatchArgumentFailure2, resolutionResult.ToString, errorMessage)
                        'The selected member can't handle the type of the Value argument, so this is an argument exception.
                        'The InvalidCastException is thrown only for back compat.  It would
                        'be nice if the latebinder had its own set of exceptions to throw.
                        Throw New InvalidCastException(errorMessage)
                    End If

                    Return Nothing
                End If
            End If

            Return resolutionResult
        End Function

        Friend Shared Function GetCallTarget(ByVal targetProcedure As Method, ByVal flags As BindingFlags) As MethodBase
            If targetProcedure.IsMethod Then Return targetProcedure.AsMethod
            If targetProcedure.IsProperty Then Return MatchesPropertyRequirements(targetProcedure, flags)
            Debug.Assert(False, "not a method or property??")
            Return Nothing
        End Function

        Friend Shared Function ConstructCallArguments(
            ByVal targetProcedure As Method,
            ByVal arguments As Object(),
            ByVal lookupFlags As BindingFlags) As Object()

            Debug.Assert(targetProcedure IsNot Nothing AndAlso arguments IsNot Nothing, "expected arguments")


            Dim parameters As ParameterInfo() = GetCallTarget(targetProcedure, lookupFlags).GetParameters
            Dim callArguments As Object() = New Object(parameters.Length - 1) {}

            Dim savedArguments As Object()
            Dim argumentCount As Integer = arguments.Length
            Dim lastArgument As Object = Nothing

            If HasFlag(lookupFlags, BindingFlagsSetProperty) Then
                Debug.Assert(arguments.Length > 0, "must have an argument for property set Value")
                savedArguments = arguments
                arguments = New Object(argumentCount - 2) {}
                System.Array.Copy(savedArguments, arguments, arguments.Length)
                lastArgument = savedArguments(argumentCount - 1)
            End If

            MatchArguments(targetProcedure, arguments, callArguments)

            If HasFlag(lookupFlags, BindingFlagsSetProperty) Then
                'Need to match the Value argument for the property set call.
                Debug.Assert(GetCallTarget(targetProcedure, lookupFlags).Name.StartsWith("set_"), "expected set accessor")

                Dim lastParameter As ParameterInfo = parameters(parameters.Length - 1)
                callArguments(parameters.Length - 1) =
                    PassToParameter(lastArgument, lastParameter, lastParameter.ParameterType)
            End If

            Return callArguments
        End Function

    End Class
End Namespace
