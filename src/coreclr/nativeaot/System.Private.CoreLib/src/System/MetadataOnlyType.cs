// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;
using System.Runtime.CompilerServices;
using Internal.Reflection.Augments;
using Internal.Reflection.Core.Execution;
using Internal.Runtime;
using Internal.Runtime.Augments;

namespace System
{
    internal sealed unsafe class MetadataOnlyType : TypeInfo, ICloneable
    {
        private RuntimeTypeInfo _runtimeTypeInfo;

        internal MetadataOnlyType(RuntimeTypeInfo runtimeTypeInfo)
        {
            _runtimeTypeInfo = runtimeTypeInfo;
        }

        internal RuntimeTypeInfo GetRuntimeTypeInfo() => _runtimeTypeInfo;

        public override string? GetEnumName(object value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!Enum.TryGetUnboxedValueOfEnumOrInteger(value, out _))
                throw new ArgumentException(SR.Arg_MustBeEnumBaseTypeOrEnum, nameof(value));

            // Enums must have RuntimeType
            throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");
        }

        public override string[] GetEnumNames()
        {
            // Enums must have RuntimeType
            throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");
        }

        public override Type GetEnumUnderlyingType()
        {
            // Enums must have RuntimeType
            throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");
        }

        public override bool IsEnumDefined(object value)
        {
            ArgumentNullException.ThrowIfNull(value);

            // Enums must have RuntimeType
            throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");
        }

        [RequiresDynamicCode("It might not be possible to create an array of the enum type at runtime. Use the GetValues<TEnum> overload instead.")]
        public override Array GetEnumValues()
        {
            // Enums must have RuntimeType
            throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");
        }

        public override Array GetEnumValuesAsUnderlyingType()
        {
            // Enums must have RuntimeType
            throw new ArgumentException(SR.Arg_MustBeEnum, "enumType");
        }

        public override int GetHashCode()
            => RuntimeHelpers.GetHashCode(this);

        public override RuntimeTypeHandle TypeHandle
            => GetRuntimeTypeInfo().TypeHandle;

        internal new unsafe bool IsInterface
            => GetRuntimeTypeInfo().IsInterface;

        protected override bool IsValueTypeImpl()
            => GetRuntimeTypeInfo().IsValueType;

        public override unsafe bool IsEnum
            => GetRuntimeTypeInfo().IsEnum;

        internal unsafe bool IsActualEnum
            => GetRuntimeTypeInfo().IsEnum;

        protected override unsafe bool IsArrayImpl()
            => GetRuntimeTypeInfo().IsArray;

        protected override unsafe bool IsByRefImpl()
            => GetRuntimeTypeInfo().IsByRef;

        protected override unsafe bool IsPointerImpl()
            => GetRuntimeTypeInfo().IsPointer;

        protected override unsafe bool HasElementTypeImpl()
            => GetRuntimeTypeInfo().HasElementType;

        public override Type? GetElementType()
            => GetRuntimeTypeInfo().GetElementType();

        public override int GetArrayRank()
            => GetRuntimeTypeInfo().GetArrayRank();

        public override Type? BaseType
            => GetRuntimeTypeInfo().BaseType;

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type[] GetInterfaces()
            => GetRuntimeTypeInfo().GetInterfaces();

        public override bool IsTypeDefinition
            => GetRuntimeTypeInfo().IsTypeDefinition;

        public override bool IsGenericType
            => GetRuntimeTypeInfo().IsConstructedGenericType || GetRuntimeTypeInfo().IsGenericTypeDefinition;

        public override bool IsGenericTypeDefinition
            => GetRuntimeTypeInfo().IsGenericTypeDefinition;

        public override bool IsConstructedGenericType
            => GetRuntimeTypeInfo().IsConstructedGenericType;

        public override Type GetGenericTypeDefinition()
            => GetRuntimeTypeInfo().GetGenericTypeDefinition();

        public override Type[] GenericTypeArguments
            => GetRuntimeTypeInfo().GenericTypeArguments;

        public override Type[] GenericTypeParameters
            => GetRuntimeTypeInfo().GenericTypeParameters;

        public override Type[] GetGenericArguments()
        {
            if (IsConstructedGenericType)
                return GenericTypeArguments;
            if (IsGenericTypeDefinition)
                return GenericTypeParameters;
            return EmptyTypes;
        }

        public override bool IsGenericParameter
            => GetRuntimeTypeInfo().IsGenericParameter;
        public override bool IsGenericTypeParameter
            => GetRuntimeTypeInfo().IsGenericTypeParameter;
        public override bool IsGenericMethodParameter
            => GetRuntimeTypeInfo().IsGenericMethodParameter;

        public override int GenericParameterPosition
            => GetRuntimeTypeInfo().GenericParameterPosition;
        public override GenericParameterAttributes GenericParameterAttributes
            => GetRuntimeTypeInfo().GenericParameterAttributes;
        public override Type[] GetGenericParameterConstraints()
            => GetRuntimeTypeInfo().GetGenericParameterConstraints();

        protected override bool IsPrimitiveImpl() => GetRuntimeTypeInfo().IsPrimitive && !GetRuntimeTypeInfo().IsEnum;

        public override bool IsSZArray
            => GetRuntimeTypeInfo().IsSZArray;

        public override bool IsVariableBoundArray
            => GetRuntimeTypeInfo().IsVariableBoundArray;

        public override bool IsByRefLike
            => GetRuntimeTypeInfo().IsByRefLike;

        public override bool IsFunctionPointer
            => GetRuntimeTypeInfo().IsFunctionPointer;

        public override bool IsUnmanagedFunctionPointer
            => GetRuntimeTypeInfo().IsUnmanagedFunctionPointer;

        public override Type[] GetFunctionPointerParameterTypes()
            => GetRuntimeTypeInfo().GetFunctionPointerParameterTypes();

        public override Type GetFunctionPointerReturnType()
            => GetRuntimeTypeInfo().GetFunctionPointerReturnType();

        //
        // Implementation shared with MetadataType
        //

        public override string ToString()
            => GetRuntimeTypeInfo().ToString();

        public override bool Equals(object? obj) => ReferenceEquals(obj, this);

        object ICloneable.Clone() => this;

        public override bool IsSecurityCritical => true;
        public override bool IsSecuritySafeCritical => false;
        public override bool IsSecurityTransparent => false;

        public override Type UnderlyingSystemType => this;

        public override Type? DeclaringType => GetRuntimeTypeInfo().DeclaringType;
        public override Type? ReflectedType => DeclaringType;

        protected override bool IsCOMObjectImpl() => false;

        protected override TypeCode GetTypeCodeImpl() => ReflectionAugments.GetRuntimeTypeCode(this);

        protected override TypeAttributes GetAttributeFlagsImpl() => GetRuntimeTypeInfo().Attributes;

        public override Type[] GetFunctionPointerCallingConventions()
        {
            if (!IsFunctionPointer)
                throw new InvalidOperationException(SR.InvalidOperation_NotFunctionPointer);

            // Requires a modified type to return the modifiers.
            return EmptyTypes;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers)
            => GetRuntimeTypeInfo().GetConstructorImpl(bindingAttr, binder, callConvention, types, modifiers);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
            => GetRuntimeTypeInfo().GetConstructors(bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo? GetEvent(string name, BindingFlags bindingAttr)
            => GetRuntimeTypeInfo().GetEvent(name, bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
            => GetRuntimeTypeInfo().GetEvents(bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo? GetField(string name, BindingFlags bindingAttr)
            => GetRuntimeTypeInfo().GetField(name, bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
            => GetRuntimeTypeInfo().GetFields(bindingAttr);

        [DynamicallyAccessedMembers(GetAllMembers)]
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
            => GetRuntimeTypeInfo().GetMembers(bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
            => GetRuntimeTypeInfo().GetMethodImpl(name, RuntimeTypeInfo.GenericParameterCountAny, bindingAttr, binder, callConvention, types, modifiers);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        protected override MethodInfo? GetMethodImpl(string name, int genericParameterCount, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
            => GetRuntimeTypeInfo().GetMethodImpl(name, genericParameterCount, bindingAttr, binder, callConvention, types, modifiers);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
            => GetRuntimeTypeInfo().GetMethods(bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type? GetNestedType(string name, BindingFlags bindingAttr)
            => GetRuntimeTypeInfo().GetNestedType(name, bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
            => GetRuntimeTypeInfo().GetNestedTypes(bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        protected override PropertyInfo? GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers)
            => GetRuntimeTypeInfo().GetPropertyImpl(name, bindingAttr, binder, returnType, types, modifiers);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
            => GetRuntimeTypeInfo().GetProperties(bindingAttr);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public override object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters)
            => GetRuntimeTypeInfo().InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        public override Type? GetInterface(string name, bool ignoreCase)
            => GetRuntimeTypeInfo().GetInterface(name, ignoreCase);

        public override bool IsDefined(Type attributeType, bool inherit)
            => GetRuntimeTypeInfo().IsDefined(attributeType, inherit);

        public override object[] GetCustomAttributes(bool inherit)
            => GetRuntimeTypeInfo().GetCustomAttributes(inherit);

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            => GetRuntimeTypeInfo().GetCustomAttributes(attributeType, inherit);

        public override IEnumerable<CustomAttributeData> CustomAttributes
            => GetRuntimeTypeInfo().CustomAttributes;

        public override IList<CustomAttributeData> GetCustomAttributesData()
            => GetRuntimeTypeInfo().GetCustomAttributesData();

        public override string Name => GetRuntimeTypeInfo().Name;

        public override string? Namespace => GetRuntimeTypeInfo().Namespace;

        public override string? AssemblyQualifiedName => GetRuntimeTypeInfo().AssemblyQualifiedName;

        public override string? FullName => GetRuntimeTypeInfo().FullName;

        public override Assembly Assembly => GetRuntimeTypeInfo().Assembly;

        public override Module Module => GetRuntimeTypeInfo().Module;

        public override Guid GUID => GetRuntimeTypeInfo().GUID;

        public override bool HasSameMetadataDefinitionAs(MemberInfo other) => GetRuntimeTypeInfo().HasSameMetadataDefinitionAs(other);

        public override Type MakePointerType()
            => GetRuntimeTypeInfo().MakePointerType();

        public override Type MakeByRefType()
            => GetRuntimeTypeInfo().MakeByRefType();

        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        public override Type MakeArrayType()
            => GetRuntimeTypeInfo().MakeArrayType();

        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        public override Type MakeArrayType(int rank)
            => GetRuntimeTypeInfo().MakeArrayType(rank);

        [RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override Type MakeGenericType(params Type[] instantiation)
            => GetRuntimeTypeInfo().MakeGenericType(instantiation);
    }
}
