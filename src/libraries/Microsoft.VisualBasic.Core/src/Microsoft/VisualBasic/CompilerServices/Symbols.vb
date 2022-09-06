' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.

Imports System
Imports System.Reflection
Imports System.Diagnostics
Imports System.Collections.Generic
Imports System.Linq

Imports Microsoft.VisualBasic.CompilerServices.NewLateBinding
Imports Microsoft.VisualBasic.CompilerServices.OverloadResolution
Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils
Imports Microsoft.VisualBasic.CompilerServices.Utils
Imports Microsoft.VisualBasic.CompilerServices.ReflectionExtensions
Imports System.Diagnostics.CodeAnalysis

Namespace Microsoft.VisualBasic.CompilerServices

    ' The symbol table.  This consists of helper functions wrapping Reflection
    ' (which is the actual "symbol table").
    Friend NotInheritable Class Symbols
        ' Prevent creation.
        Private Sub New()
        End Sub

        Friend Enum UserDefinedOperator As SByte
            UNDEF
            Narrow
            Widen
            IsTrue
            IsFalse
            Negate
            [Not]
            UnaryPlus
            Plus
            Minus
            Multiply
            Divide
            Power
            IntegralDivide
            Concatenate
            ShiftLeft
            ShiftRight
            Modulus
            [Or]
            [Xor]
            [And]
            [Like]
            Equal
            NotEqual
            Less
            LessEqual
            GreaterEqual
            Greater
            MAX
        End Enum

        Friend Shared ReadOnly NoArguments As Object() = Array.Empty(Of Object)()
        Friend Shared ReadOnly NoArgumentNames As String() = Array.Empty(Of String)()
        Friend Shared ReadOnly NoTypeArguments As Type() = Array.Empty(Of Type)()
        Friend Shared ReadOnly NoTypeParameters As Type() = Array.Empty(Of Type)()

        Friend Shared ReadOnly OperatorCLSNames As String()
        Friend Shared ReadOnly OperatorNames As String()

        Shared Sub New()
            OperatorCLSNames = New String(UserDefinedOperator.MAX - 1) {}
            OperatorCLSNames(UserDefinedOperator.Narrow) = "op_Explicit"
            OperatorCLSNames(UserDefinedOperator.Widen) = "op_Implicit"
            OperatorCLSNames(UserDefinedOperator.IsTrue) = "op_True"
            OperatorCLSNames(UserDefinedOperator.IsFalse) = "op_False"
            OperatorCLSNames(UserDefinedOperator.Negate) = "op_UnaryNegation"
            OperatorCLSNames(UserDefinedOperator.Not) = "op_OnesComplement"
            OperatorCLSNames(UserDefinedOperator.UnaryPlus) = "op_UnaryPlus"
            OperatorCLSNames(UserDefinedOperator.Plus) = "op_Addition"
            OperatorCLSNames(UserDefinedOperator.Minus) = "op_Subtraction"
            OperatorCLSNames(UserDefinedOperator.Multiply) = "op_Multiply"
            OperatorCLSNames(UserDefinedOperator.Divide) = "op_Division"
            OperatorCLSNames(UserDefinedOperator.Power) = "op_Exponent"
            OperatorCLSNames(UserDefinedOperator.IntegralDivide) = "op_IntegerDivision"
            OperatorCLSNames(UserDefinedOperator.Concatenate) = "op_Concatenate"
            OperatorCLSNames(UserDefinedOperator.ShiftLeft) = "op_LeftShift"
            OperatorCLSNames(UserDefinedOperator.ShiftRight) = "op_RightShift"
            OperatorCLSNames(UserDefinedOperator.Modulus) = "op_Modulus"
            OperatorCLSNames(UserDefinedOperator.Or) = "op_BitwiseOr"
            OperatorCLSNames(UserDefinedOperator.Xor) = "op_ExclusiveOr"
            OperatorCLSNames(UserDefinedOperator.And) = "op_BitwiseAnd"
            OperatorCLSNames(UserDefinedOperator.Like) = "op_Like"
            OperatorCLSNames(UserDefinedOperator.Equal) = "op_Equality"
            OperatorCLSNames(UserDefinedOperator.NotEqual) = "op_Inequality"
            OperatorCLSNames(UserDefinedOperator.Less) = "op_LessThan"
            OperatorCLSNames(UserDefinedOperator.LessEqual) = "op_LessThanOrEqual"
            OperatorCLSNames(UserDefinedOperator.GreaterEqual) = "op_GreaterThanOrEqual"
            OperatorCLSNames(UserDefinedOperator.Greater) = "op_GreaterThan"


            OperatorNames = New String(UserDefinedOperator.MAX - 1) {}
            OperatorNames(UserDefinedOperator.Narrow) = "CType"
            OperatorNames(UserDefinedOperator.Widen) = "CType"
            OperatorNames(UserDefinedOperator.IsTrue) = "IsTrue"
            OperatorNames(UserDefinedOperator.IsFalse) = "IsFalse"
            OperatorNames(UserDefinedOperator.Negate) = "-"
            OperatorNames(UserDefinedOperator.Not) = "Not"
            OperatorNames(UserDefinedOperator.UnaryPlus) = "+"
            OperatorNames(UserDefinedOperator.Plus) = "+"
            OperatorNames(UserDefinedOperator.Minus) = "-"
            OperatorNames(UserDefinedOperator.Multiply) = "*"
            OperatorNames(UserDefinedOperator.Divide) = "/"
            OperatorNames(UserDefinedOperator.Power) = "^"
            OperatorNames(UserDefinedOperator.IntegralDivide) = "\"
            OperatorNames(UserDefinedOperator.Concatenate) = "&"
            OperatorNames(UserDefinedOperator.ShiftLeft) = "<<"
            OperatorNames(UserDefinedOperator.ShiftRight) = ">>"
            OperatorNames(UserDefinedOperator.Modulus) = "Mod"
            OperatorNames(UserDefinedOperator.Or) = "Or"
            OperatorNames(UserDefinedOperator.Xor) = "Xor"
            OperatorNames(UserDefinedOperator.And) = "And"
            OperatorNames(UserDefinedOperator.Like) = "Like"
            OperatorNames(UserDefinedOperator.Equal) = "="
            OperatorNames(UserDefinedOperator.NotEqual) = "<>"
            OperatorNames(UserDefinedOperator.Less) = "<"
            OperatorNames(UserDefinedOperator.LessEqual) = "<="
            OperatorNames(UserDefinedOperator.GreaterEqual) = ">="
            OperatorNames(UserDefinedOperator.Greater) = ">"
        End Sub

        Friend Shared Function IsUnaryOperator(ByVal op As UserDefinedOperator) As Boolean
            Select Case op
                Case UserDefinedOperator.Narrow,
                     UserDefinedOperator.Widen,
                     UserDefinedOperator.IsTrue,
                     UserDefinedOperator.IsFalse,
                     UserDefinedOperator.Negate,
                     UserDefinedOperator.Not,
                     UserDefinedOperator.UnaryPlus

                    Return True

            End Select
            Return False
        End Function

        Friend Shared Function IsBinaryOperator(ByVal op As UserDefinedOperator) As Boolean
            Select Case op
                Case UserDefinedOperator.Plus,
                     UserDefinedOperator.Minus,
                     UserDefinedOperator.Multiply,
                     UserDefinedOperator.Divide,
                     UserDefinedOperator.Power,
                     UserDefinedOperator.IntegralDivide,
                     UserDefinedOperator.Concatenate,
                     UserDefinedOperator.ShiftLeft,
                     UserDefinedOperator.ShiftRight,
                     UserDefinedOperator.Modulus,
                     UserDefinedOperator.Or,
                     UserDefinedOperator.Xor,
                     UserDefinedOperator.And,
                     UserDefinedOperator.Like,
                     UserDefinedOperator.Equal,
                     UserDefinedOperator.NotEqual,
                     UserDefinedOperator.Less,
                     UserDefinedOperator.LessEqual,
                     UserDefinedOperator.GreaterEqual,
                     UserDefinedOperator.Greater

                    Return True

            End Select
            Return False
        End Function

        Friend Shared Function IsUserDefinedOperator(ByVal method As MethodBase) As Boolean
            Return method.IsSpecialName AndAlso method.Name.StartsWith("op_", StringComparison.Ordinal)
        End Function

        Friend Shared Function IsNarrowingConversionOperator(ByVal method As MethodBase) As Boolean
            Return method.IsSpecialName AndAlso method.Name.Equals(OperatorCLSNames(UserDefinedOperator.Narrow))
        End Function

        Friend Shared Function MapToUserDefinedOperator(ByVal method As MethodBase) As UserDefinedOperator
            Debug.Assert(IsUserDefinedOperator(method), "expected operator here")

            For cursor As Integer = UserDefinedOperator.UNDEF + 1 To UserDefinedOperator.MAX - 1
                If method.Name.Equals(OperatorCLSNames(cursor)) Then

                    Dim paramCount As Integer = method.GetParameters.Length
                    Dim op As UserDefinedOperator = CType(cursor, UserDefinedOperator)

                    If (paramCount = 1 AndAlso IsUnaryOperator(op)) OrElse
                       (paramCount = 2 AndAlso IsBinaryOperator(op)) Then
                        'Match found, so quit loop early.
                        Return op
                    End If

                End If
            Next

            Return UserDefinedOperator.UNDEF
        End Function

        Friend Shared Function GetTypeCode(ByVal type As System.Type) As TypeCode
            Return type.GetTypeCode
        End Function

        Friend Shared Function MapTypeCodeToType(ByVal typeCode As TypeCode) As <DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)> Type

            Select Case typeCode

                Case TypeCode.Boolean : Return GetType(Boolean)
                Case TypeCode.SByte : Return GetType(SByte)
                Case TypeCode.Byte : Return GetType(Byte)
                Case TypeCode.Int16 : Return GetType(Short)
                Case TypeCode.UInt16 : Return GetType(UShort)
                Case TypeCode.Int32 : Return GetType(Integer)
                Case TypeCode.UInt32 : Return GetType(UInteger)
                Case TypeCode.Int64 : Return GetType(Long)
                Case TypeCode.UInt64 : Return GetType(ULong)
                Case TypeCode.Decimal : Return GetType(Decimal)
                Case TypeCode.Single : Return GetType(Single)
                Case TypeCode.Double : Return GetType(Double)
                Case TypeCode.DateTime : Return GetType(Date)
                Case TypeCode.Char : Return GetType(Char)
                Case TypeCode.String : Return GetType(String)
                Case TypeCode.Object : Return GetType(Object)
                Case TypeCode.DBNull : Return GetType(DBNull)

                Case TypeCode.Empty
                    'fall through

            End Select

            Return Nothing
        End Function

        Friend Shared Function IsRootObjectType(ByVal type As System.Type) As Boolean
            Return type Is GetType(Object)
        End Function

        Friend Shared Function IsRootEnumType(ByVal type As System.Type) As Boolean
            Return type Is GetType(System.Enum)
        End Function

        Friend Shared Function IsValueType(ByVal type As System.Type) As Boolean
            Return type.IsValueType
        End Function

        Friend Shared Function IsEnum(ByVal type As System.Type) As Boolean
            Return type.IsEnum
        End Function

        Friend Shared Function IsArrayType(ByVal type As System.Type) As Boolean
            Return type.IsArray
        End Function

        Friend Shared Function IsStringType(ByVal type As System.Type) As Boolean
            Return type Is GetType(String)
        End Function

        Friend Shared Function IsCharArrayRankOne(ByVal type As System.Type) As Boolean
            Return type Is GetType(Char())
        End Function

        Friend Shared Function IsIntegralType(ByVal typeCode As TypeCode) As Boolean
            Select Case typeCode
                Case TypeCode.SByte,
                     TypeCode.Byte,
                     TypeCode.Int16,
                     TypeCode.UInt16,
                     TypeCode.Int32,
                     TypeCode.UInt32,
                     TypeCode.Int64,
                     TypeCode.UInt64

                    Return True

                Case TypeCode.Empty,
                     TypeCode.Object,
                     TypeCode.Boolean,
                     TypeCode.Decimal,
                     TypeCode.Single,
                     TypeCode.Double,
                     TypeCode.DateTime,
                     TypeCode.Char,
                     TypeCode.String

                    'Fall through to end.
            End Select

            Return False
        End Function


        Friend Shared Function IsNumericType(ByVal typeCode As TypeCode) As Boolean
            Select Case typeCode
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

                    Return True

                Case TypeCode.Empty,
                     TypeCode.Object,
                     TypeCode.Boolean,
                     TypeCode.DateTime,
                     TypeCode.Char,
                     TypeCode.String

                    'Fall through to end.
            End Select

            Return False
        End Function

        Friend Shared Function IsNumericType(ByVal type As System.Type) As Boolean
            Return IsNumericType(GetTypeCode(type))
        End Function

        Friend Shared Function IsIntrinsicType(ByVal typeCode As TypeCode) As Boolean
            Select Case typeCode
                Case TypeCode.Boolean,
                     TypeCode.SByte,
                     TypeCode.Byte,
                     TypeCode.Int16,
                     TypeCode.UInt16,
                     TypeCode.Int32,
                     TypeCode.UInt32,
                     TypeCode.Int64,
                     TypeCode.UInt64,
                     TypeCode.Decimal,
                     TypeCode.Single,
                     TypeCode.Double,
                     TypeCode.DateTime,
                     TypeCode.Char,
                     TypeCode.String

                    Return True

                Case TypeCode.Empty,
                     TypeCode.Object

                    'Fall through to end.
            End Select

            Return False
        End Function

        Friend Shared Function IsIntrinsicType(ByVal type As System.Type) As Boolean
            Return IsIntrinsicType(GetTypeCode(type)) AndAlso Not IsEnum(type)
        End Function


        Friend Shared Function IsClass(ByVal type As System.Type) As Boolean
            Return type.IsClass OrElse IsRootEnumType(type)
        End Function

        Friend Shared Function IsClassOrValueType(ByVal type As System.Type) As Boolean
            Return IsValueType(type) OrElse IsClass(type)
        End Function

        Friend Shared Function IsInterface(ByVal type As System.Type) As Boolean
            Return type.IsInterface
        End Function

        Friend Shared Function IsClassOrInterface(ByVal type As System.Type) As Boolean
            Return IsClass(type) OrElse IsInterface(type)
        End Function

        Friend Shared Function IsReferenceType(ByVal type As System.Type) As Boolean
            Return IsClass(type) OrElse IsInterface(type)
        End Function

        Friend Shared Function IsGenericParameter(ByVal type As System.Type) As Boolean
            Return type.IsGenericParameter
        End Function

#If LATEBINDING Then
        Friend Shared Function IsCollectionInterface(ByVal type As System.Type) As Boolean
            If type.IsInterface AndAlso
               ((type.IsGenericType AndAlso
                   (type.GetGenericTypeDefinition() Is GetType(System.Collections.Generic.IList(Of )) OrElse
                    type.GetGenericTypeDefinition() Is GetType(System.Collections.Generic.ICollection(Of )) OrElse
                    type.GetGenericTypeDefinition() Is GetType(System.Collections.Generic.IEnumerable(Of )) OrElse
                    type.GetGenericTypeDefinition() Is GetType(System.Collections.Generic.IReadOnlyList(Of )) OrElse
                    type.GetGenericTypeDefinition() Is GetType(System.Collections.Generic.IReadOnlyCollection(Of )) OrElse
                    type.GetGenericTypeDefinition() Is GetType(System.Collections.Generic.IDictionary(Of ,)) OrElse
                    type.GetGenericTypeDefinition() Is GetType(System.Collections.Generic.IReadOnlyDictionary(Of ,)))) OrElse
                type Is GetType(System.Collections.IList) OrElse
                type Is GetType(System.Collections.ICollection) OrElse
                type Is GetType(System.Collections.IEnumerable) OrElse
                type Is GetType(System.ComponentModel.INotifyPropertyChanged) OrElse
                type Is GetType(System.Collections.Specialized.INotifyCollectionChanged)) Then
                Return True
            End If

            Return False
        End Function
#Else
        Friend Shared Function IsCollectionInterface(ByVal Type As System.Type) As Boolean
            If Type.IsInterface AndAlso
               ((Type.IsGenericType AndAlso
                   (Type.GetGenericTypeDefinition() = GetType(System.Collections.Generic.IList(Of )) OrElse
                    Type.GetGenericTypeDefinition() = GetType(System.Collections.Generic.ICollection(Of )) OrElse
                    Type.GetGenericTypeDefinition() = GetType(System.Collections.Generic.IEnumerable(Of )) OrElse
                    Type.GetGenericTypeDefinition() = GetType(System.Collections.Generic.IReadOnlyList(Of )) OrElse
                    Type.GetGenericTypeDefinition() = GetType(System.Collections.Generic.IReadOnlyCollection(Of )) OrElse
                    Type.GetGenericTypeDefinition() = GetType(System.Collections.Generic.IDictionary(Of ,)) OrElse
                    Type.GetGenericTypeDefinition() = GetType(System.Collections.Generic.IReadOnlyDictionary(Of ,)))) OrElse
                Type = GetType(System.Collections.IList) OrElse
                Type = GetType(System.Collections.ICollection) OrElse
                Type = GetType(System.Collections.IEnumerable) OrElse
                Type = GetType(System.ComponentModel.INotifyPropertyChanged) OrElse
                Type = GetType(System.Collections.Specialized.INotifyCollectionChanged)) Then
                Return True
            End If

            Return False
        End Function
#End If
        <UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification:="Calls GetInterfaces but only looks for existing interface type by reference equality. Trimmer guarantees that if the interface type is kept it is also kept on all the types which implement it.")>
        Friend Shared Function [Implements](ByVal implementor As System.Type, ByVal [interface] As System.Type) As Boolean

            Debug.Assert(Not IsInterface(implementor), "interfaces can't implement, so why call this?")
            Debug.Assert(IsInterface([interface]), "expected interface, not " & [interface].FullName)

            For Each implemented As Type In implementor.GetInterfaces
                If implemented Is [interface] Then
                    Return True
                End If
            Next

            Return False

        End Function

        <UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification:="Calls GetInterfaces but only looks for existing interface type by reference equality. Trimmer guarantees that if the interface type is kept it is also kept on all the types which implement it.")>
        Friend Shared Function IsOrInheritsFrom(ByVal derived As System.Type, ByVal base As System.Type) As Boolean
            Debug.Assert((Not derived.IsByRef) AndAlso (Not derived.IsPointer))
            Debug.Assert((Not base.IsByRef) AndAlso (Not base.IsPointer))

            If derived Is base Then Return True

            If derived.IsGenericParameter() Then
                If IsClass(base) AndAlso
                   (CBool(derived.GenericParameterAttributes() And GenericParameterAttributes.NotNullableValueTypeConstraint)) AndAlso
                   IsOrInheritsFrom(GetType(System.ValueType), base) Then
                    Return True
                End If

                For Each typeConstraint As Type In derived.GetGenericParameterConstraints
                    If IsOrInheritsFrom(typeConstraint, base) Then
                        Return True
                    End If
                Next

            ElseIf IsInterface(derived) Then
                If IsInterface(base) Then
                    For Each baseInterface As Type In derived.GetInterfaces
                        If baseInterface Is base Then
                            Return True
                        End If
                    Next
                End If

            ElseIf IsClass(base) AndAlso IsClassOrValueType(derived) Then
                Return derived.IsSubclassOf(base)
            End If

            Return False
        End Function

        Friend Shared Function IsGeneric(ByVal type As Type) As Boolean
            Return type.IsGenericType
        End Function

        Friend Shared Function IsInstantiatedGeneric(ByVal type As Type) As Boolean
            Return type.IsGenericType AndAlso (Not type.IsGenericTypeDefinition)
        End Function

        Friend Shared Function IsGeneric(ByVal method As MethodBase) As Boolean
            Return method.IsGenericMethod
        End Function

        Friend Shared Function IsGeneric(ByVal member As MemberInfo) As Boolean
            'Returns True whether Method is an instantiated or uninstantiated generic method.
            Dim method As MethodBase = TryCast(member, MethodBase)
            If method Is Nothing Then Return False
            Return IsGeneric(method)
        End Function

#If DEBUG Then
        Friend Shared Function IsInstantiatedGeneric(ByVal Method As MethodBase) As Boolean
            Return Method.IsGenericMethod AndAlso (Not Method.IsGenericMethodDefinition)
        End Function
#End If

        Friend Shared Function IsRawGeneric(ByVal method As MethodBase) As Boolean
            Return method.IsGenericMethod AndAlso method.IsGenericMethodDefinition
        End Function

        Friend Shared Function GetTypeParameters(ByVal member As MemberInfo) As Type()
            Dim method As MethodBase = TryCast(member, MethodBase)
            If method Is Nothing Then Return NoTypeParameters
            Return method.GetGenericArguments
        End Function

        Friend Shared Function GetTypeArguments(ByVal type As Type) As Type()
            Debug.Assert(type.GetGenericTypeDefinition IsNot Nothing, "expected bound generic type")
            Return type.GetGenericArguments
        End Function

        Friend Shared Function GetInterfaceConstraints(<DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)> ByVal genericParameter As Type) As Type()
            'Returns the interface constraints for the type parameter.
            Debug.Assert(IsGenericParameter(genericParameter), "expected type parameter")
            Return System.Linq.Enumerable.ToArray(genericParameter.GetInterfaces)
        End Function

        Friend Shared Function GetClassConstraint(ByVal genericParameter As Type) As Type
            'Returns the class constraint for the type parameter, Nothing if it has
            'no class constraint.
            Debug.Assert(IsGenericParameter(genericParameter), "expected type parameter")

            'Type parameters with no class constraint have System.Object as their base type.
            Dim classConstraint As Type = genericParameter.BaseType
            If IsRootObjectType(classConstraint) Then Return Nothing
            Return classConstraint
        End Function

        Friend Shared Function IndexIn(ByVal possibleGenericParameter As Type, ByVal genericMethodDef As MethodBase) As Integer
            'Returns the index of PossibleGenericParameter in Method.  If the generic param cannot be found,
            'returns -1

            Debug.Assert(genericMethodDef IsNot Nothing AndAlso IsRawGeneric(genericMethodDef), "Uninstantiated generic expected!!!")

            If IsGenericParameter(possibleGenericParameter) AndAlso
               possibleGenericParameter.DeclaringMethod IsNot Nothing AndAlso
               AreGenericMethodDefsEqual(possibleGenericParameter.DeclaringMethod, genericMethodDef) Then
                Return possibleGenericParameter.GenericParameterPosition
            End If
            Return -1
        End Function

        Friend Shared Function RefersToGenericParameter(ByVal referringType As Type, ByVal method As MethodBase) As Boolean
            'Given ReferringType, determine if it contains any usages of the generic parameters of Method.
            'For example, the referring types T and C1(Of T) and T() refer to a generic param of Sub F(Of T).

            If Not IsRawGeneric(method) Then Return False

            If referringType.IsByRef Then referringType = GetElementType(referringType)

            If IsGenericParameter(referringType) Then
                'Is T a generic parameter of Method?

                Debug.Assert(referringType.DeclaringMethod.IsGenericMethodDefinition, "Unexpected generic method instantiation!!!")

                If AreGenericMethodDefsEqual(referringType.DeclaringMethod, method) Then
                    Return True
                End If

            ElseIf IsGeneric(referringType) Then
                'For C1(Of T, U, V), recurse on T, U, and V.
                For Each param As Type In GetTypeArguments(referringType)
                    If RefersToGenericParameter(param, method) Then
                        Return True
                    End If
                Next

            ElseIf IsArrayType(referringType) Then
                'For T(), recurse on T.
                Return RefersToGenericParameter(referringType.GetElementType, method)

            End If

            Return False

        End Function

        'Is T a generic parameter of Type. Note that the clr way of representing type params will
        'cause us to return true for the copies of the type params of all the parent types that are
        'on the passed in Typ. Note that this clr behavior has been retained because in the run time
        'for the uses of this function, this functionality is desired.
        '
        Friend Shared Function RefersToGenericParameterCLRSemantics(ByVal referringType As Type, ByVal typ As Type) As Boolean
            'Given ReferringType, determine if it contains any usages of the generic parameters of Typ.
            'For example, the referring types T and C1(Of T) and T() refer to a generic param of Class Cls1(Of T).

            If referringType.IsByRef Then referringType = GetElementType(referringType)

            If IsGenericParameter(referringType) Then
                'Is T a generic parameter of Type. Note that the clr way of representing type params will
                'return true for the copies of the type params of all the parent types that are on the
                'passed in Typ.

                If referringType.DeclaringType Is typ Then
                    Return True
                End If

            ElseIf IsGeneric(referringType) Then
                'For C1(Of T, U, V), recurse on T, U, and V.
                For Each param As Type In GetTypeArguments(referringType)
                    If RefersToGenericParameterCLRSemantics(param, typ) Then
                        Return True
                    End If
                Next

            ElseIf IsArrayType(referringType) Then
                'For T(), recurse on T.
                Return RefersToGenericParameterCLRSemantics(referringType.GetElementType, typ)

            End If

            Return False

        End Function

        Friend Shared Function AreGenericMethodDefsEqual(ByVal method1 As MethodBase, ByVal method2 As MethodBase) As Boolean
            Debug.Assert(method1 IsNot Nothing AndAlso IsRawGeneric(method1), "Generic method def expected!!!")
            Debug.Assert(method2 IsNot Nothing AndAlso IsRawGeneric(method2), "Generic method def expected!!!")

            ' Need to do this kind of comparison because the MethodInfo obtained for a
            ' base method through type1 is not the same as that obtained from type2
            '
            Return method1 Is method2 OrElse
                   method1.HasSameMetadataDefinitionAs(method2)
        End Function

        Friend Shared Function IsShadows(ByVal method As MethodBase) As Boolean
            If method.IsHideBySig Then Return False
            If method.IsVirtual AndAlso (method.Attributes And MethodAttributes.NewSlot) = 0 Then

                'Only the most derived Overrides member shows up in the member list returned by reflection.
                'However, we have to check the most base (Overridable) member because the Shadowing information
                'is stored only there.
                If (DirectCast(method, MethodInfo).GetRuntimeBaseDefinition().Attributes And MethodAttributes.NewSlot) = 0 Then
                    Return False
                End If
            End If
            Return True
        End Function

        Friend Shared Function IsShared(ByVal member As MemberInfo) As Boolean

            Select Case member.MemberType
                Case MemberTypes.Method
                    Return DirectCast(member, MethodInfo).IsStatic

                Case MemberTypes.Field
                    Return DirectCast(member, FieldInfo).IsStatic

                Case MemberTypes.Constructor
                    Return DirectCast(member, ConstructorInfo).IsStatic

                Case MemberTypes.Property
                    Return DirectCast(member, PropertyInfo).GetGetMethod.IsStatic

                Case Else
                    Debug.Assert(False, "unexpected membertype")
            End Select

            Return False

        End Function

        Friend Shared Function IsParamArray(ByVal parameter As ParameterInfo) As Boolean
            Return IsArrayType(parameter.ParameterType) AndAlso parameter.IsDefined(GetType(ParamArrayAttribute), False)
        End Function

        Friend Shared Function GetElementType(ByVal type As System.Type) As Type
            Debug.Assert(type.HasElementType, "expected type with element type")
            Return type.GetElementType
        End Function

        Friend Shared Function AreParametersAndReturnTypesValid(
            ByVal parameters As ParameterInfo(),
            ByVal returnType As Type) As Boolean

            If returnType IsNot Nothing AndAlso (returnType.IsPointer OrElse returnType.IsByRef) Then
                Return False
            End If

            If parameters IsNot Nothing Then
                For Each parameter As ParameterInfo In parameters
                    If parameter.ParameterType.IsPointer Then
                        Return False
                    End If
                Next
            End If

            Return True
        End Function

        Friend Shared Sub GetAllParameterCounts(
            ByVal parameters As ParameterInfo(),
            ByRef requiredParameterCount As Integer,
            ByRef maximumParameterCount As Integer,
            ByRef paramArrayIndex As Integer)


            Debug.Assert(parameters IsNot Nothing, "expected parameter array")

            maximumParameterCount = parameters.Length

            'All optional parameters are grouped at the end, so the index of the
            'last non-optional (+1) gives us the count of required parameters.
            For index As Integer = maximumParameterCount - 1 To 0 Step -1
                If Not parameters(index).IsOptional Then
                    requiredParameterCount = index + 1
                    Exit For
                End If
            Next

            'Only the last parameter can be a ParamArray, so check it.
            If maximumParameterCount <> 0 AndAlso IsParamArray(parameters(maximumParameterCount - 1)) Then
                paramArrayIndex = maximumParameterCount - 1
                requiredParameterCount -= 1
            End If
        End Sub

        Friend Shared Function IsNonPublicRuntimeMember(ByVal member As MemberInfo) As Boolean

            'Disallow latebound calls to internal Microsoft.VisualBasic types
            Dim declaringType As System.Type = member.DeclaringType

            ' For nested types IsNotPublic doesn't return the right value so
            ' we need to use Not IsPublic.
            '
            ' The following code will only allow calls to members of top level public types
            ' in the runtime library. Read the reflection documentation and test with
            ' nested types before changing this code.

            Return Not declaringType.IsPublic AndAlso declaringType.Assembly Is Utils.VBRuntimeAssembly

        End Function

        'this is a utility function, so it doesn't really belong in Symbols, but...
        Friend Shared Function HasFlag(ByVal flags As BindingFlags, ByVal flagToTest As BindingFlags) As Boolean
            Return CBool(flags And flagToTest)
        End Function

        Friend NotInheritable Class Container

            Private NotInheritable Class InheritanceSorter : Implements IComparer(Of MemberInfo)

                Private Sub New()
                End Sub

                Private Function Compare(ByVal left As MemberInfo, ByVal right As MemberInfo) As Integer Implements IComparer(Of MemberInfo).Compare
                    Dim leftType As Type = left.DeclaringType
                    Dim rightType As Type = right.DeclaringType

#If BINDING_LOG Then
                'Console.WriteLine("compare: " & LeftType.Name & " " & RightType.Name)
#End If
                    If leftType Is rightType Then Return 0
                    If leftType.IsSubclassOf(rightType) Then Return -1

                    'Necessary to return 1 only for RightType.IsSubclassOf(LeftType)?  If no inheritance
                    'relationhip exists, which is possible when members come from IReflect, returning 1
                    'is still okay. Returning 1 in this IReflect case will not cause qsort to never terminate.
                    Return 1
                End Function

                Friend Shared ReadOnly Instance As InheritanceSorter = New InheritanceSorter

            End Class

            Private ReadOnly _instance As Object
            <DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)>
            Private ReadOnly _type As Type

            <RequiresUnreferencedCode("Calls Object.GetType() which cannot be statically analyzed")>
            Friend Sub New(ByVal instance As Object)

                If instance Is Nothing Then
                    Throw VbMakeObjNotSetException()
                End If

                _instance = instance
                _type = instance.GetType
            End Sub

            Friend Sub New(
                    <DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)>
                    ByVal type As Type)

                If type Is Nothing Then
                    Throw VbMakeObjNotSetException()
                End If

                _instance = Nothing
                _type = type
            End Sub

            ' Try to determine if this object represents a WindowsRuntime object - i.e. it either
            ' is coming from a WinMD file or is derived from a class coming from a WinMD.
            ' The logic here matches the CLR's logic of finding a WinRT object.

            Friend ReadOnly Property IsWindowsRuntimeObject() As Boolean
                Get
                    Dim curType As Type = _type
                    While curType IsNot Nothing
                        If (curType.Attributes And TypeAttributes.WindowsRuntime) = TypeAttributes.WindowsRuntime Then
                            ' Found a WinRT COM object
                            Return True
                        ElseIf (curType.Attributes And TypeAttributes.Import) = TypeAttributes.Import Then
                            ' Found a class that is actually imported from COM but not WinRT
                            ' this is definitely a non-WinRT COM object
                            Return False
                        End If
                        curType = curType.BaseType
                    End While
                    Return False

                End Get
            End Property

            Friend ReadOnly Property VBFriendlyName() As String
                Get
                    Return Utils.VBFriendlyName(_type, _instance)
                End Get
            End Property

            Friend ReadOnly Property IsArray() As Boolean
                Get
                    Return IsArrayType(_type) AndAlso _instance IsNot Nothing
                End Get
            End Property

            Friend ReadOnly Property IsValueType() As Boolean
                Get
                    Return Symbols.IsValueType(_type) AndAlso _instance IsNot Nothing
                End Get
            End Property

            Private Const s_defaultLookupFlags As BindingFlags =
                BindingFlags.IgnoreCase Or
                BindingFlags.FlattenHierarchy Or
                BindingFlags.Public Or
                BindingFlags.Static Or
                BindingFlags.Instance

            Private Shared ReadOnly s_noMembers As MemberInfo() = Array.Empty(Of MemberInfo)()

            Private Shared Function FilterInvalidMembers(ByVal members As MemberInfo()) As MemberInfo()

                If members Is Nothing OrElse members.Length = 0 Then
                    Return Nothing
                End If

                Dim validMemberCount As Integer = 0
                Dim memberIndex As Integer = 0

                For memberIndex = 0 To members.Length - 1
                    Dim parameters As ParameterInfo() = Nothing
                    Dim returnType As Type = Nothing

                    Select Case members(memberIndex).MemberType

                        Case MemberTypes.Constructor,
                             MemberTypes.Method

                            Dim currentMethod As MethodInfo = DirectCast(members(memberIndex), MethodInfo)

                            parameters = currentMethod.GetParameters
                            returnType = currentMethod.ReturnType

                        Case MemberTypes.Property

                            Dim propertyBlock As PropertyInfo = DirectCast(members(memberIndex), PropertyInfo)
                            Dim getMethod As MethodInfo = propertyBlock.GetGetMethod

                            If getMethod IsNot Nothing Then
                                parameters = getMethod.GetParameters
                            Else
                                Dim setMethod As MethodInfo = propertyBlock.GetSetMethod
                                Dim setParameters As ParameterInfo() = setMethod.GetParameters

                                parameters = New ParameterInfo(setParameters.Length - 2) {}
                                System.Array.Copy(setParameters, parameters, parameters.Length)
                            End If

                            returnType = propertyBlock.PropertyType

                        Case MemberTypes.Field
                            returnType = DirectCast(members(memberIndex), FieldInfo).FieldType

                    End Select

                    If AreParametersAndReturnTypesValid(parameters, returnType) Then
                        validMemberCount += 1
                    Else
                        members(memberIndex) = Nothing
                    End If
                Next

                If validMemberCount = members.Length Then
                    Return members
                ElseIf validMemberCount > 0 Then

                    Dim validMembers(validMemberCount - 1) As MemberInfo
                    Dim validMemberIndex As Integer = 0

                    For memberIndex = 0 To members.Length - 1
                        If members(memberIndex) IsNot Nothing Then
                            validMembers(validMemberIndex) = members(memberIndex)
                            validMemberIndex += 1
                        End If
                    Next

                    Return validMembers
                End If

                Return Nothing
            End Function

            ' For a WinRT object, we want to treat members of it's collection interfaces as members of the object
            ' itself. So GetMembers calls here to find the member in all the collection interfaces that this object
            ' implements.
            <UnconditionalSuppressMessage("ReflectionAnalysis", "IL2065:UnrecognizedReflectionPattern",
                Justification:="_type is annotated with .All, so it's Interfaces will be annotated as well and it is safe to call GetMember on the Interfaces.
                    We should be able to remove once https://github.com/mono/linker/issues/1731 is fixed.")>
            Friend Function LookupWinRTCollectionInterfaceMembers(ByVal memberName As String) As List(Of MemberInfo)
                Debug.Assert(Me.IsWindowsRuntimeObject(), "Expected a Windows Runtime Object")

                Dim result As New List(Of MemberInfo)
                For Each implemented As Type In _type.GetInterfaces()
                    If IsCollectionInterface(implemented) Then
                        Dim members As MemberInfo() = implemented.GetMember(memberName, s_defaultLookupFlags)
                        If (members IsNot Nothing) Then
                            result.AddRange(members)
                        End If
                    End If
                Next

                Return result
            End Function

            <UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
                Justification:="_type is annotated with .All, so it's BaseType will be annotated as well and it is safe to call GetMember on the BaseType.
                    We should be able to remove once https://github.com/mono/linker/issues/1731 is fixed.")>
            Friend Function LookupNamedMembers(ByVal memberName As String) As MemberInfo()
                'Returns an array of members matching MemberName sorted by inheritance (most derived first).
                'If no members match MemberName, returns an empty array.

                Dim result As MemberInfo()

                If IsGenericParameter(_type) Then
                    'Getting the members of a generic parameter follows a special rule.
                    'In a Latebound context, only members of the class constraint are
                    'applicable. We will ignore interface constraints. Also, custom
                    'Reflection can't be involved, so no need to use that.

                    Dim classConstraint As Type = GetClassConstraint(_type)
                    If classConstraint IsNot Nothing Then
                        result = Enumerable.ToArray(classConstraint.GetMember(memberName, s_defaultLookupFlags))
                    Else
                        result = Nothing
                    End If
                Else
                    result = Enumerable.ToArray(_type.GetMember(memberName, s_defaultLookupFlags))
                End If

                If Me.IsWindowsRuntimeObject() Then
                    Dim collectionMethods As List(Of MemberInfo) = LookupWinRTCollectionInterfaceMembers(memberName)
                    If result IsNot Nothing Then
                        collectionMethods.AddRange(result)
                    End If

                    result = collectionMethods.ToArray()
                End If

                result = FilterInvalidMembers(result)

                If result Is Nothing Then
                    result = s_noMembers
                ElseIf result.Length > 1 Then
                    Array.Sort(Of MemberInfo)(result, InheritanceSorter.Instance)
                End If

                Return result
            End Function

            ' For a WinRT object, we want to treat members of it's collection interfaces as members of the object
            ' itself. Search through all the collection interfaces for default members.
            <RequiresUnreferencedCode("Calls Container.LookupDefaultMembers")>
            Private Function LookupWinRTCollectionDefaultMembers(ByRef defaultMemberName As String) As List(Of MemberInfo)
                Debug.Assert(Me.IsWindowsRuntimeObject(), "Expected a Windows Runtime Object")

                Dim result As New List(Of MemberInfo)
                For Each implemented As Type In _type.GetInterfaces()
                    If IsCollectionInterface(implemented) Then
                        Dim members As MemberInfo() = LookupDefaultMembers(defaultMemberName, implemented)
                        If (members IsNot Nothing) Then
                            result.AddRange(members)
                        End If
                    End If
                Next

                Return result
            End Function

            <RequiresUnreferencedCode("Calls Type.GetMember() on a type that cannot be statically analyzed")>
            Private Function LookupDefaultMembers(ByRef defaultMemberName As String, ByVal searchType As Type) As MemberInfo()
                'Returns an array of default members sorted by inheritance (most derived first).
                'If no members match MemberName, returns an empty array.
                'The default member name is determined by walking up the inheritance hierarchy looking
                'for a DefaultMemberAttribute.

                Dim potentialDefaultMemberName As String = Nothing

                'Find the default member name.
                Dim current As Type = searchType
                Do
                    Dim attributes As Object() = Enumerable.ToArray(current.GetCustomAttributes(GetType(DefaultMemberAttribute), False))

                    If attributes IsNot Nothing AndAlso attributes.Length > 0 Then
                        potentialDefaultMemberName = DirectCast(attributes(0), DefaultMemberAttribute).MemberName
                        Exit Do
                    End If
                    current = current.BaseType

                Loop While current IsNot Nothing AndAlso Not IsRootObjectType(current)

                If potentialDefaultMemberName IsNot Nothing Then
                    Dim result As MemberInfo() = Enumerable.ToArray(current.GetMember(potentialDefaultMemberName, s_defaultLookupFlags))

                    result = FilterInvalidMembers(result)

                    If result IsNot Nothing Then
                        defaultMemberName = potentialDefaultMemberName
                        If result.Length > 1 Then
                            Array.Sort(result, InheritanceSorter.Instance)
                        End If
                        Return result
                    End If
                End If

                Return s_noMembers
            End Function

            <RequiresUnreferencedCode("Calls LookupDefaultMembers")>
            Friend Function GetMembers(
                ByRef memberName As String,
                ByVal reportErrors As Boolean) As MemberInfo()

                Dim result As MemberInfo()
                If memberName Is Nothing Then memberName = ""

                If memberName = "" Then

                    result = Me.LookupDefaultMembers(memberName, _type) 'MemberName is set during this call.

                    If Me.IsWindowsRuntimeObject() Then
                        Dim collectionMethods As List(Of MemberInfo) = LookupWinRTCollectionDefaultMembers(memberName)
                        If result IsNot Nothing Then
                            collectionMethods.AddRange(result)
                        End If

                        result = collectionMethods.ToArray()
                    End If

                    If result.Length = 0 Then
                        If reportErrors Then
                            Throw New MissingMemberException(
                                SR.Format(SR.MissingMember_NoDefaultMemberFound1, Me.VBFriendlyName))
                        End If

                        Return result
                    End If

                Else
                    result = Me.LookupNamedMembers(memberName)

                    If result.Length = 0 Then
                        If reportErrors Then
                            Throw New MissingMemberException(
                                SR.Format(SR.MissingMember_MemberNotFoundOnType2, memberName, Me.VBFriendlyName))
                        End If

                        Return result
                    End If
                End If

                Return result
            End Function

            Friend Function GetFieldValue(ByVal field As FieldInfo) As Object
                If _instance Is Nothing AndAlso Not IsShared(field) Then
                    'Reference to non-shared member '|1' requires an object reference.
                    Throw New NullReferenceException(
                        SR.Format(SR.NullReference_InstanceReqToAccessMember1, FieldToString(field)))
                End If
                '
                'BEGIN: SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY
                '
                If IsNonPublicRuntimeMember(field) Then
                    'No message text intentional - Default BCL message used
                    Throw New MissingMemberException
                End If
                '
                'END: SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY
                '
                Return field.GetValue(_instance)
            End Function

            <RequiresUnreferencedCode("Cannot statically analyze the type of FieldInfo.FieldType")>
            Friend Sub SetFieldValue(ByVal field As FieldInfo, ByVal value As Object)
                If field.IsInitOnly Then
                    Throw New MissingMemberException(
                        SR.Format(SR.MissingMember_ReadOnlyField2, field.Name, Me.VBFriendlyName))
                End If

                If _instance Is Nothing AndAlso Not IsShared(field) Then
                    'Reference to non-shared member '|1' requires an object reference.
                    Throw New NullReferenceException(
                        SR.Format(SR.NullReference_InstanceReqToAccessMember1, FieldToString(field)))
                End If
                '
                'BEGIN: SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY
                '
                If IsNonPublicRuntimeMember(field) Then
                    'No message text intentional - Default BCL message used
                    Throw New MissingMemberException
                End If
                '
                'END: SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY
                '
                field.SetValue(_instance, Conversions.ChangeType(value, field.FieldType))
                Return
            End Sub

            <RequiresUnreferencedCode("Calls Conversions.ChangeType")>
            Friend Function GetArrayValue(ByVal indices As Object()) As Object
                Debug.Assert(Me.IsArray, "expected array when getting array value")
                Debug.Assert(indices IsNot Nothing, "expected valid indices")


                Dim arrayInstance As Array = DirectCast(_instance, System.Array)
                Dim rank As Integer = arrayInstance.Rank

                If indices.Length <> rank Then
                    Throw New RankException
                End If

                'We use ChangeType to handle potential user-defined conversion operators.
                Dim zerothIndex As Integer =
                    DirectCast(Conversions.ChangeType(indices(0), GetType(Integer)), Integer)

                If rank = 1 Then
                    Return arrayInstance.GetValue(zerothIndex)
                Else
                    Dim firstIndex As Integer =
                        DirectCast(Conversions.ChangeType(indices(1), GetType(Integer)), Integer)

                    If rank = 2 Then
                        Return arrayInstance.GetValue(zerothIndex, firstIndex)
                    Else
                        Dim secondIndex As Integer =
                            DirectCast(Conversions.ChangeType(indices(2), GetType(Integer)), Integer)

                        If rank = 3 Then
                            Return arrayInstance.GetValue(zerothIndex, firstIndex, secondIndex)
                        Else
                            Dim indexArray As Integer() = New Integer(rank - 1) {}
                            indexArray(0) = zerothIndex : indexArray(1) = firstIndex : indexArray(2) = secondIndex

                            For i As Integer = 3 To rank - 1
                                indexArray(i) =
                                    DirectCast(Conversions.ChangeType(indices(i), GetType(Integer)), Integer)
                            Next

                            Return arrayInstance.GetValue(indexArray)
                        End If
                    End If
                End If

            End Function

            <RequiresUnreferencedCode("Uses _type.GetElementType which cannot be statically analyzed.")>
            Friend Sub SetArrayValue(ByVal arguments As Object())
                'The last argument is the Value to be stored into the array. The other arguments are
                'the indices into the array.
                Debug.Assert(Me.IsArray, "expected array when setting array value")
                Debug.Assert(arguments IsNot Nothing, "expected valid indices")


                Dim arrayInstance As Array = DirectCast(_instance, System.Array)
                Dim rank As Integer = arrayInstance.Rank

                If arguments.Length - 1 <> rank Then
                    Throw New RankException
                End If

                'To ensure order of evaulation, we must evaluate the Value argument after
                'evaluating each index argument.
                Dim value As Object = arguments(arguments.Length - 1)
                Dim elementType As Type = _type.GetElementType

                'We use ChangeType to handle potential user-defined conversion operators.
                Dim zerothIndex As Integer =
                    DirectCast(Conversions.ChangeType(arguments(0), GetType(Integer)), Integer)

                If rank = 1 Then
                    arrayInstance.SetValue(Conversions.ChangeType(value, elementType), zerothIndex)
                    Return
                Else
                    Dim firstIndex As Integer =
                        DirectCast(Conversions.ChangeType(arguments(1), GetType(Integer)), Integer)

                    If rank = 2 Then
                        arrayInstance.SetValue(Conversions.ChangeType(value, elementType), zerothIndex, firstIndex)
                        Return
                    Else
                        Dim secondIndex As Integer =
                            DirectCast(Conversions.ChangeType(arguments(2), GetType(Integer)), Integer)

                        If rank = 3 Then
                            arrayInstance.SetValue(Conversions.ChangeType(value, elementType), zerothIndex, firstIndex, secondIndex)
                            Return
                        Else
                            Dim indexArray As Integer() = New Integer(rank - 1) {}
                            indexArray(0) = zerothIndex : indexArray(1) = firstIndex : indexArray(2) = secondIndex

                            For i As Integer = 3 To rank - 1
                                indexArray(i) =
                                    DirectCast(Conversions.ChangeType(arguments(i), GetType(Integer)), Integer)
                            Next

                            arrayInstance.SetValue(Conversions.ChangeType(value, elementType), indexArray)
                            Return
                        End If
                    End If
                End If

            End Sub

            <RequiresUnreferencedCode("Calls ConstructCallArguments which is unsafe")>
            Friend Function InvokeMethod(
                ByVal targetProcedure As Method,
                ByVal arguments As Object(),
                ByVal copyBack As Boolean(),
                ByVal flags As BindingFlags) As Object


                Dim callTarget As MethodBase = GetCallTarget(targetProcedure, flags)
                Debug.Assert(callTarget IsNot Nothing, "must have valid MethodBase")

                Debug.Assert(Not targetProcedure.IsGeneric OrElse
                             DirectCast(targetProcedure.AsMethod, MethodInfo).GetGenericMethodDefinition IsNot Nothing,
                             "expected bound generic method by this point")

                Dim callArguments As Object() =
                    ConstructCallArguments(targetProcedure, arguments, flags)

                If _instance Is Nothing AndAlso Not IsShared(callTarget) Then
                    'Reference to non-shared member '|1' requires an object reference.
                    Throw New NullReferenceException(
                        SR.Format(SR.NullReference_InstanceReqToAccessMember1, targetProcedure.ToString))
                End If
                '
                'BEGIN: SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY
                '
                If IsNonPublicRuntimeMember(callTarget) Then
                    'No message text intentional - Default BCL message used
                    Throw New MissingMemberException
                End If
                '
                'END: SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY SECURITY
                '

                Dim result As Object
                Try
                    result = callTarget.Invoke(_instance, callArguments)

                Catch ex As TargetInvocationException When ex.InnerException IsNot Nothing
                    'For backwards compatibility, throw the inner exception of a TargetInvocationException.
                    Throw ex.InnerException

                End Try

                ReorderArgumentArray(targetProcedure, callArguments, arguments, copyBack, flags)
                Return result
            End Function

        End Class

        Friend NotInheritable Class Method

            Private _item As MemberInfo                     'The underlying method or property reflection object.
            Private _rawItem As MethodBase                  'The unsubstituted raw generic method.
            Private _parameters As ParameterInfo()          'The parameters used for this method by overload resolution.
            Private _rawParameters As ParameterInfo()       'The unsubstituted raw parameters of a generic method.
            Private _rawParametersFromType As ParameterInfo() 'The unsubstituted raw parameters of a generic method in the raw type.
            Private _rawDeclaringType As Type               'The uninstantiated type containing this method.

            Friend ReadOnly ParamArrayIndex As Integer       'The index of the ParamArray in the parameters array, -1 if method has no ParamArray.
            Friend ReadOnly ParamArrayExpanded As Boolean    'Indicates if this method's ParamArray should be considered in its expanded form.

            Friend NotCallable As Boolean                    'Indicates if this method has been rejected as uncallable.
            Friend RequiresNarrowingConversion As Boolean    'Indicates if an argument requires narrowing one of the method's parameters.
            Friend AllNarrowingIsFromObject As Boolean       'Indicates if the type of all arguments which require narrowing to this method's parameters are Object.
            Friend LessSpecific As Boolean                   'Indicates is this method loses the competition for most specific procedure.

            Friend ArgumentsValidated As Boolean             'Indicates if the arguments have been validated against this method.
            Friend NamedArgumentMapping As Integer()         'Table of indices into the argument array for mapping named arguments to parameters.
            Friend TypeArguments As Type()                   'Set of type arguments either supplied or inferred for this method.
            Friend ArgumentMatchingDone As Boolean           'Indicates whether the argument matching task (CanMatchArguments) has already been completed for this Method

            Private Sub New(
                    ByVal parameters As ParameterInfo(),
                    ByVal paramArrayIndex As Integer,
                    ByVal paramArrayExpanded As Boolean)

                Me._parameters = parameters
                Me._rawParameters = parameters
                Me.ParamArrayIndex = paramArrayIndex
                Me.ParamArrayExpanded = paramArrayExpanded

                Me.AllNarrowingIsFromObject = True  'Assume True until non-object narrowing is encountered.
            End Sub

            Friend Sub New(
                    ByVal method As MethodBase,
                    ByVal parameters As ParameterInfo(),
                    ByVal paramArrayIndex As Integer,
                    ByVal paramArrayExpanded As Boolean)

                MyClass.New(parameters, paramArrayIndex, paramArrayExpanded)
                Me._item = method
                Me._rawItem = method
            End Sub

            Friend Sub New(
                    ByVal [property] As PropertyInfo,
                    ByVal parameters As ParameterInfo(),
                    ByVal paramArrayIndex As Integer,
                    ByVal paramArrayExpanded As Boolean)

                MyClass.New(parameters, paramArrayIndex, paramArrayExpanded)
                Me._item = [property]
            End Sub

            Friend ReadOnly Property Parameters() As ParameterInfo()
                Get
                    Return _parameters
                End Get
            End Property

            Friend ReadOnly Property RawParameters() As ParameterInfo()
                Get
                    'After a generic method has been bound, we still need access
                    'to the raw, unbound parameters.
                    Return _rawParameters
                End Get
            End Property

            Friend ReadOnly Property RawParametersFromType() As ParameterInfo()
                <RequiresUnreferencedCode("Cannot statically analyze the type of _item")>
                Get
                    If _rawParametersFromType Is Nothing Then
                        If Not IsProperty Then
                            Dim item As MethodInfo = DirectCast(_item, MethodInfo)
                            If item.IsGenericMethod Then
                                item = item.GetGenericMethodDefinition()
                            End If
                            Dim declaringType As System.Type = item.DeclaringType
                            If declaringType.IsConstructedGenericType Then
                                declaringType = declaringType.GetGenericTypeDefinition
                            End If
                            Dim rawMethod As MethodBase = Nothing
                            For Each candidate As MethodInfo In declaringType.GetTypeInfo.GetDeclaredMethods(item.Name)
                                If candidate.HasSameMetadataDefinitionAs(item) Then
                                    rawMethod = candidate
                                    Exit For
                                End If
                            Next

                            Debug.Assert(rawMethod IsNot Nothing)
                            _rawParametersFromType = rawMethod.GetParameters()
                        Else
                            _rawParametersFromType = _rawParameters
                        End If
                    End If

                    Return _rawParametersFromType
                End Get
            End Property

            Friend ReadOnly Property DeclaringType() As Type
                Get
                    Return _item.DeclaringType
                End Get
            End Property

            Friend ReadOnly Property RawDeclaringType() As Type
                Get
                    If _rawDeclaringType Is Nothing Then
                        Dim declaringType As System.Type = _item.DeclaringType
                        If declaringType.IsConstructedGenericType Then
                            declaringType = declaringType.GetGenericTypeDefinition
                        End If
                        _rawDeclaringType = declaringType
                    End If

                    Return _rawDeclaringType
                End Get
            End Property

            Friend ReadOnly Property HasParamArray() As Boolean
                Get
                    Return ParamArrayIndex > -1
                End Get
            End Property

            Friend ReadOnly Property HasByRefParameter() As Boolean
                Get
                    For Each parameter As ParameterInfo In Parameters
                        If parameter.ParameterType.IsByRef Then
                            Return True
                        End If
                    Next
                    Return False
                End Get
            End Property

            Friend ReadOnly Property IsProperty() As Boolean
                Get
                    Return _item.MemberType = MemberTypes.Property
                End Get
            End Property

            Friend ReadOnly Property IsMethod() As Boolean
                Get
                    Return _item.MemberType = MemberTypes.Method OrElse
                           _item.MemberType = MemberTypes.Constructor
                End Get
            End Property

            Friend ReadOnly Property IsGeneric() As Boolean
                Get
                    Return Symbols.IsGeneric(_item)
                End Get
            End Property

            Friend ReadOnly Property TypeParameters() As Type()
                Get
                    Return Symbols.GetTypeParameters(_item)
                End Get
            End Property

            <RequiresUnreferencedCode("Cannot statically infer which method _rawItem is or if it has annotations")>
            Friend Function BindGenericArguments() As Boolean
                'This function instantiates a generic method with the type arguments supplied or inferred
                'for this method.
                '
                ' Constructing the generic binding using Reflection performs
                ' constraint checking. This is bad if the binding will be used
                ' to resolve overloaded calls since constraints should not participate
                ' in the selection process. Instead, constraints should be checked
                ' after overload resolution has selected a method. For now, there is
                ' nothing reasonble we can do since Reflection does not allow the
                ' instantiation of generic methods with arguments that violate the
                ' constraints. If a violation occurs, catch the exception and return
                ' false signifying that the binding failed.

                Debug.Assert(Me.ArgumentsValidated, "can't bind without validating arguments")
                Debug.Assert(Me.IsMethod, "binding to a non-method")
                Try
                    'We use the original raw generic method so we can rebind an already bound Method.
                    _item = DirectCast(_rawItem, MethodInfo).MakeGenericMethod(TypeArguments)
                    _parameters = Me.AsMethod.GetParameters
                    Return True
                Catch ex As ArgumentException
                    Return False
                End Try
            End Function

            Friend Function AsMethod() As MethodBase
                Debug.Assert(Me.IsMethod, "casting a non-method to a method")
                Return TryCast(_item, MethodBase)
            End Function

            Friend Function AsProperty() As PropertyInfo
                Debug.Assert(Me.IsProperty, "casting a non-property to a property")
                Return TryCast(_item, PropertyInfo)
            End Function

            Public Shared Operator =(ByVal left As Method, ByVal right As Method) As Boolean
                Return left._item Is right._item
            End Operator

            Public Shared Operator <>(ByVal left As Method, ByVal right As Method) As Boolean
                Return left._item IsNot right._item
            End Operator

            Public Shared Operator =(ByVal left As MemberInfo, ByVal right As Method) As Boolean
                Return left Is right._item
            End Operator

            Public Shared Operator <>(ByVal left As MemberInfo, ByVal right As Method) As Boolean
                Return left IsNot right._item
            End Operator

            Public Overrides Function ToString() As String
                Return MemberToString(_item)
            End Function

#If BINDING_LOG Then
            Private Function BoolStr(ByVal x As Boolean) As String
                If x Then Return "T" Else Return "F"
            End Function

            Friend Function DumpContents() As String
                Dim result As String = IIf(Me.IsMethod, "Meth", "Prop")
                result &= " " & m_Item.Name & " PAExpanded:" & BoolStr(ParamArrayExpanded) & " Container:" & m_Item.DeclaringType.Name & " ("
                For Each p As ParameterInfo In Parameters
                    result &= p.ParameterType.Name & ","
                Next
                result &= ")"
                result &= " PAIndex:" & CStr(ParamArrayIndex)
                result &= " NotCallable:" & BoolStr(NotCallable)
                result &= " ReqNar:" & BoolStr(RequiresNarrowingConversion)

                If RequiresNarrowingConversion Then
                    result &= " AllNarFromObj:" & BoolStr(AllNarrowingIsFromObject)
                End If

                Return result
            End Function
#End If

        End Class

        Friend NotInheritable Class TypedNothing
            'A class which represents a Nothing reference but with a particular type.
            'Normally, a Nothing reference converts to any type. However, during operator
            'resolution Nothing should match only one type.  This class acts as a place holder
            'for Nothing in the argument array and stores the Type which "Nothing" should have.

            Friend ReadOnly Type As Type

            Friend Sub New(ByVal type As Type)
                Me.Type = type
            End Sub
        End Class

    End Class

End Namespace
