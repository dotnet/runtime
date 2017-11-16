' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Diagnostics
Imports System.Linq
Imports System.Linq.Expressions
Imports System.Reflection

Namespace Global.Microsoft.VisualBasic.CompilerServices
    <Global.System.Diagnostics.DebuggerNonUserCode()>
    <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)>
    Partial Public Class Utils
        Private Sub New()
        End Sub
        Public Shared Function CopyArray(arySrc As Global.System.Array, aryDest As Global.System.Array) As Global.System.Array
            If arySrc Is Nothing Then
                Return aryDest
            End If
            Dim lLength As Integer
            lLength = arySrc.Length
            If lLength = 0 Then
                Return aryDest
            End If
            If aryDest.Rank() <> arySrc.Rank() Then
                Throw New Global.System.InvalidCastException()
            End If
            Dim iDim As Integer
            For iDim = 0 To aryDest.Rank() - 2
                If aryDest.GetUpperBound(iDim) <> arySrc.GetUpperBound(iDim) Then
                    Throw New Global.System.ArrayTypeMismatchException()
                End If
            Next iDim
            If lLength > aryDest.Length Then
                lLength = aryDest.Length
            End If
            If arySrc.Rank > 1 Then
                Dim lastRank As Integer = arySrc.Rank
                Dim lenSrcLastRank As Integer = arySrc.GetLength(lastRank - 1)
                Dim lenDestLastRank As Integer = aryDest.GetLength(lastRank - 1)
                If lenDestLastRank = 0 Then
                    Return aryDest
                End If
                Dim lenCopy As Integer = If(lenSrcLastRank > lenDestLastRank, lenDestLastRank, lenSrcLastRank)
                Dim i As Integer
                For i = 0 To (arySrc.Length \ lenSrcLastRank) - 1
                    Global.System.Array.Copy(arySrc, i * lenSrcLastRank, aryDest, i * lenDestLastRank, lenCopy)
                Next i
            Else
                Global.System.Array.Copy(arySrc, aryDest, lLength)
            End If
            Return aryDest
        End Function
    End Class

    Friend Module ReflectionExtensions

        <System.Runtime.CompilerServices.ExtensionAttribute()>
        Public Function GetTypeCode(type As Type) As TypeCode
            Return Type.GetTypeCode(type)
        End Function

        Public ReadOnly Property BindingFlagsInvokeMethod As BindingFlags
            Get
                Return CType(256, BindingFlags) ' BindingFlags.InvokeMethod
            End Get
        End Property

        Public ReadOnly Property BindingFlagsGetProperty As BindingFlags
            Get
                Return CType(4096, BindingFlags) ' BindingFlags.GetProperty
            End Get
        End Property

        Public ReadOnly Property BindingFlagsSetProperty As BindingFlags
            Get
                Return CType(8192, BindingFlags) ' BindingFlags.SetProperty
            End Get
        End Property

        Public ReadOnly Property BindingFlagsIgnoreReturn As BindingFlags
            Get
                Return CType(16777216, BindingFlags) ' BindingFlags.IgnoreReturn
            End Get
        End Property

        <System.Runtime.CompilerServices.ExtensionAttribute()>
        Public Function IsEquivalentTo(mi1 As MethodBase, mi2 As MethodBase) As Boolean

            If mi1 Is Nothing OrElse mi2 Is Nothing Then
                Return mi1 Is Nothing AndAlso mi2 Is Nothing
            End If

            If mi1.Equals(mi2) Then
                Return True
            End If

            If TypeOf mi1 Is MethodInfo AndAlso TypeOf mi2 Is MethodInfo Then
                Dim method1 As MethodInfo = DirectCast(mi1, MethodInfo)
                Dim method2 As MethodInfo = DirectCast(mi2, MethodInfo)

                If method1.IsGenericMethod <> method2.IsGenericMethod Then
                    Return False
                End If

                If method1.IsGenericMethod Then
                    method1 = method1.GetGenericMethodDefinition()
                    method2 = method2.GetGenericMethodDefinition()

                    If method1.GetGenericArguments().Length <> method2.GetGenericArguments().Length Then
                        Return False ' Methods of different arity are not equivalent.
                    End If
                End If

                If Not method1.Equals(method2) AndAlso
                   method1.Name.Equals(method2.Name) AndAlso
                   method1.DeclaringType.IsGenericallyEqual(method2.DeclaringType) AndAlso
                   method1.ReturnType.IsGenericallyEquivalentTo(method2.ReturnType, method1, method2) Then

                    Dim pis1 As ParameterInfo() = method1.GetParameters()
                    Dim pis2 As ParameterInfo() = method2.GetParameters()

                    Return pis1.Length = pis2.Length AndAlso
                           Enumerable.All(Enumerable.Zip(pis1,
                                                         pis2,
                                                         Function(pi1, pi2) pi1.IsEquivalentTo(pi2, method1, method2)),
                                                     Function(x) x)
                End If

                Return False
            End If

            If TypeOf mi1 Is ConstructorInfo AndAlso TypeOf mi2 Is ConstructorInfo Then
                Dim ctor1 As ConstructorInfo = DirectCast(mi1, ConstructorInfo)
                Dim ctor2 As ConstructorInfo = DirectCast(mi2, ConstructorInfo)

                If Not ctor1.Equals(ctor2) AndAlso
                   ctor1.DeclaringType.IsGenericallyEqual(ctor2.DeclaringType) Then

                    Dim pis1 As ParameterInfo() = ctor1.GetParameters()
                    Dim pis2 As ParameterInfo() = ctor2.GetParameters()

                    Return pis1.Length = pis2.Length AndAlso
                           Enumerable.All(Enumerable.Zip(pis1,
                                                         pis2,
                                                         Function(pi1, pi2) pi1.IsEquivalentTo(pi2, ctor1, ctor2)),
                                                     Function(x) x)
                End If

                Return False
            End If

            Return False
        End Function

        <System.Runtime.CompilerServices.ExtensionAttribute()>
        Private Function IsEquivalentTo(pi1 As ParameterInfo, pi2 As ParameterInfo, method1 As MethodBase, method2 As MethodBase) As Boolean

            If pi1 Is Nothing OrElse pi2 Is Nothing Then
                Return pi1 Is Nothing AndAlso pi2 Is Nothing
            End If

            If pi1.Equals(pi2) Then
                Return True
            End If

            Return pi1.ParameterType.IsGenericallyEquivalentTo(pi2.ParameterType, method1, method2)
        End Function

        <System.Runtime.CompilerServices.ExtensionAttribute()>
        Private Function IsGenericallyEqual(t1 As Type, t2 As Type) As Boolean

            If t1 Is Nothing OrElse t2 Is Nothing Then
                Return t1 Is Nothing AndAlso t2 Is Nothing
            End If

            If t1.Equals(t2) Then
                Return True
            End If

            If t1.IsConstructedGenericType OrElse t2.IsConstructedGenericType Then
                Dim t1def As Type = If(t1.IsConstructedGenericType, t1.GetGenericTypeDefinition(), t1)
                Dim t2def As Type = If(t2.IsConstructedGenericType, t2.GetGenericTypeDefinition(), t2)

                Return t1def.Equals(t2def)
            End If

            Return False
        End Function

        ' Compares two types and calls them equivalent if a type parameter equals a type argument.
        ' i.e if the inputs are (T, int, C(Of T), C(Of Integer)) then this will return true.
        <System.Runtime.CompilerServices.ExtensionAttribute()>
        Private Function IsGenericallyEquivalentTo(t1 As Type, t2 As Type, member1 As MemberInfo, member2 As MemberInfo) As Boolean

            Debug.Assert(Not (TypeOf member1 Is MethodBase) OrElse
                         Not DirectCast(member1, MethodBase).IsGenericMethod OrElse
                         (DirectCast(member1, MethodBase).IsGenericMethodDefinition AndAlso DirectCast(member2, MethodBase).IsGenericMethodDefinition))

            If t1.Equals(t2) Then
                Return True
            End If

            ' If one of them is a type param and then the other is a real type, then get the type argument in the member
            ' or it's declaring type that corresponds to the type param and compare that to the other type.
            If t1.IsGenericParameter Then
                If t2.IsGenericParameter Then
                    ' If member's declaring type is not type parameter's declaring type, we assume that it is used as a type argument
                    If t1.DeclaringMethod Is Nothing AndAlso member1.DeclaringType.Equals(t1.DeclaringType) Then
                        If Not (t2.DeclaringMethod Is Nothing AndAlso member2.DeclaringType.Equals(t2.DeclaringType)) Then
                            Return t1.IsTypeParameterEquivalentToTypeInst(t2, member2)
                        End If
                    ElseIf t2.DeclaringMethod Is Nothing AndAlso member2.DeclaringType.Equals(t2.DeclaringType) Then
                        Return t2.IsTypeParameterEquivalentToTypeInst(t1, member1)
                    End If

                    ' If both of these are type params but didn't compare to be equal then one of them is likely bound to another
                    ' open type. Simply disallow such cases.
                    Return False
                End If

                Return t1.IsTypeParameterEquivalentToTypeInst(t2, member2)

            ElseIf t2.IsGenericParameter Then
                Return t2.IsTypeParameterEquivalentToTypeInst(t1, member1)
            End If

            ' Recurse in for generic types arrays, byref and pointer types.
            If t1.IsGenericType AndAlso t2.IsGenericType Then
                Dim args1 As Type() = t1.GetGenericArguments()
                Dim args2 As Type() = t2.GetGenericArguments()

                If args1.Length = args2.Length Then
                    Return t1.IsGenericallyEqual(t2) AndAlso
                           Enumerable.All(Enumerable.Zip(args1,
                                                         args2,
                                                         Function(ta1, ta2) ta1.IsGenericallyEquivalentTo(ta2, member1, member2)),
                                                     Function(x) x)
                End If
            End If

            If t1.IsArray AndAlso t2.IsArray Then
                Return t1.GetArrayRank() = t2.GetArrayRank() AndAlso
                       t1.GetElementType().IsGenericallyEquivalentTo(t2.GetElementType(), member1, member2)
            End If

            If (t1.IsByRef AndAlso t2.IsByRef) OrElse
               (t1.IsPointer AndAlso t2.IsPointer) Then
                Return t1.GetElementType().IsGenericallyEquivalentTo(t2.GetElementType(), member1, member2)
            End If

            Return False
        End Function

        <System.Runtime.CompilerServices.ExtensionAttribute()>
        Private Function IsTypeParameterEquivalentToTypeInst(typeParam As Type, typeInst As Type, member As MemberInfo) As Boolean

            Debug.Assert(typeParam.IsGenericParameter)

            If typeParam.DeclaringMethod IsNot Nothing Then
                ' The type param is from a generic method. Since only methods can be generic, anything else
                ' here means they are not equivalent.
                If Not (TypeOf member Is MethodBase) Then
                    Return False
                End If

                Dim method As MethodBase = DirectCast(member, MethodBase)
                Dim position As Integer = typeParam.GenericParameterPosition
                Dim args As Type() = If(method.IsGenericMethod, method.GetGenericArguments(), Nothing)

                Return args IsNot Nothing AndAlso
                       args.Length > position AndAlso
                       args(position).Equals(typeInst)
            Else
                Return member.DeclaringType.GetGenericArguments()(typeParam.GenericParameterPosition).Equals(typeInst)
            End If
        End Function

        ' s_MemberEquivalence will replace itself with one version or another
        ' depending on what works at run time
        Private s_MemberEquivalence As Func(Of MethodBase, MethodBase, Boolean) =
            Function(m1, m2)
                Try
                    ' See if MetadataToken property is available.
                    Dim MemberInfo As Type = GetType(MethodBase)
                    Dim [property] As PropertyInfo = MemberInfo.GetProperty("MetadataToken", GetType(Integer), Array.Empty(Of Type)())

                    If ([property] IsNot Nothing AndAlso [property].CanRead) Then
                        ' Function(parameter1, parameter2) parameter1.MetadataToken = parameter2.MetadataToken
                        Dim parameter1 As ParameterExpression = Expression.Parameter(MemberInfo)
                        Dim parameter2 As ParameterExpression = Expression.Parameter(MemberInfo)
                        Dim memberEquivalence As Func(Of MethodBase, MethodBase, Boolean) = Expression.Lambda(Of Func(Of MethodBase, MethodBase, Boolean))(
                                            Expression.Equal(
                                                Expression.Property(parameter1, [property]),
                                                Expression.Property(parameter2, [property])),
                                                {parameter1, parameter2}).Compile()

                        Dim result As Boolean = memberEquivalence(m1, m2)
                        ' it worked, so publish it
                        s_MemberEquivalence = memberEquivalence

                        Return result
                    End If
                Catch
                    ' Platform might not allow access to the property
                End Try

                ' MetadataToken is not available in some contexts. Looks like this is one of those cases.
                ' fallback to "IsEquivalentTo"
                Dim fallbackMemberEquivalence As Func(Of MethodBase, MethodBase, Boolean) = Function(m1param, m2param) m1param.IsEquivalentTo(m2param)

                ' fallback must work
                s_MemberEquivalence = fallbackMemberEquivalence
                Return fallbackMemberEquivalence(m1, m2)
            End Function


        <System.Runtime.CompilerServices.ExtensionAttribute()>
        Public Function HasSameMetadataDefinitionAs(mi1 As MethodBase, mi2 As MethodBase) As Boolean
#If UNSUPPORTEDAPI Then
            return (mi1.MetadataToken = mi2.MetadataToken) AndAlso mi1.Module.Equals(mi2.Module)
#Else
            Return mi1.Module.Equals(mi2.Module) AndAlso s_MemberEquivalence(mi1, mi2)
#End If
        End Function

    End Module
End Namespace
