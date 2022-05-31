// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using StructLayoutAttribute = System.Runtime.InteropServices.StructLayoutAttribute;

namespace System.Reflection.TypeLoading
{
    // Similar to TypeDelegator, wraps an instance of a Type (in this case, RoType) and delegates calls to it.
    // This is only used by function pointers which are based on method signatures which the MetadataReader
    // instantiates with the "modified type" pattern of chained types to represent custom modifiers.
    internal abstract class RoTypeDelegator : RoType
    {
        private RoType _actualType;

        protected RoTypeDelegator(RoType actualType)
        {
            _actualType = actualType;
        }

        public RoType ActualType => _actualType;

        // RoType also overrides Equals to ensure both l- and r-value are checked for actualType.
        public sealed override bool Equals([NotNullWhen(true)] object? obj) => _actualType.Equals(obj);

        public sealed override int GetHashCode() => _actualType.GetHashCode();
        public override string ToString() => _actualType.ToString();

        public override bool IsTypeDefinition => _actualType.IsTypeDefinition;
        public override bool IsGenericTypeDefinition => _actualType.IsGenericTypeDefinition;
        protected override bool HasElementTypeImpl() => _actualType.Call_HasElementTypeImpl();
        protected override bool IsArrayImpl() => _actualType.Call_IsArrayImpl();
        public override bool IsSZArray => _actualType.IsSZArray();
        public override bool IsVariableBoundArray => _actualType.IsVariableBoundArray;
        protected override bool IsByRefImpl() => _actualType.Call_IsByRefImpl();
        protected override bool IsPointerImpl() => _actualType.Call_IsPointerImpl();
        public override bool IsFunctionPointer => _actualType.IsFunctionPointer;
        public override bool IsUnmanagedFunctionPointer => _actualType.IsUnmanagedFunctionPointer;
        public override bool IsConstructedGenericType => _actualType.IsConstructedGenericType;
        public override bool IsGenericParameter => _actualType.IsGenericParameter;
        public override bool IsGenericTypeParameter => _actualType.IsGenericTypeParameter;
        public override bool IsGenericMethodParameter => _actualType.IsGenericMethodParameter;
        public override bool ContainsGenericParameters => _actualType.ContainsGenericParameters;

        internal override RoModule GetRoModule() => _actualType.GetRoModule();

        public override int GetArrayRank() => _actualType.GetArrayRank();

        protected override string ComputeName() => _actualType.Call_ComputeName();
        protected override string? ComputeNamespace() => _actualType.Call_ComputeNamespace();
        protected override string? ComputeFullName() => _actualType.Call_ComputeFullName();

        protected override TypeAttributes ComputeAttributeFlags() => _actualType.Call_ComputeAttributeFlags();
        protected override TypeCode GetTypeCodeImpl() => _actualType.Call_GetTypeCodeImpl();

        public override MethodBase? DeclaringMethod => _actualType.DeclaringMethod;
        protected override RoType? ComputeDeclaringType() => _actualType.Call_ComputeDeclaringType();

        public override IEnumerable<CustomAttributeData> CustomAttributes => _actualType.CustomAttributes;
        internal override bool IsCustomAttributeDefined(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name) => _actualType.IsCustomAttributeDefined(ns, name);
        internal override CustomAttributeData? TryFindCustomAttribute(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name) => _actualType.TryFindCustomAttribute(ns, name);

        public override int MetadataToken => _actualType.MetadataToken;

        internal override RoType? GetRoElementType() => _actualType.GetRoElementType();

        public override Type GetGenericTypeDefinition() => _actualType.GetGenericTypeDefinition();
        internal override RoType[] GetGenericTypeParametersNoCopy() => _actualType.GetGenericTypeParametersNoCopy();
        internal override RoType[] GetGenericTypeArgumentsNoCopy() => _actualType.GetGenericTypeArgumentsNoCopy();
        protected internal override RoType[] GetGenericArgumentsNoCopy() => _actualType.GetGenericArgumentsNoCopy();
        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override Type MakeGenericType(params Type[] typeArguments) => _actualType.MakeGenericType(typeArguments);

        public override GenericParameterAttributes GenericParameterAttributes => _actualType.GenericParameterAttributes;
        public override int GenericParameterPosition => _actualType.GenericParameterPosition;
        public override Type[] GetGenericParameterConstraints() => _actualType.GetGenericParameterConstraints();
        public override Type[] GetFunctionPointerCallingConventions() => _actualType.GetFunctionPointerCallingConventions();
#if FUNCTIONPOINTER_SUPPORT
        public override FunctionPointerParameterInfo GetFunctionPointerReturnParameter() => _actualType.GetFunctionPointerReturnParameter();
        public override FunctionPointerParameterInfo[] GetFunctionPointerParameterInfos() => _actualType.GetFunctionPointerParameterInfos();
#endif
        public override Guid GUID => _actualType.GUID;
        public override StructLayoutAttribute? StructLayoutAttribute => _actualType.StructLayoutAttribute;
        protected internal override RoType ComputeEnumUnderlyingType() => _actualType.ComputeEnumUnderlyingType();

        internal override RoType? ComputeBaseTypeWithoutDesktopQuirk() => _actualType.ComputeBaseTypeWithoutDesktopQuirk();
        internal override IEnumerable<RoType> ComputeDirectlyImplementedInterfaces() => _actualType.ComputeDirectlyImplementedInterfaces();

        // Low level support for the BindingFlag-driven enumerator apis.
        internal override IEnumerable<ConstructorInfo> GetConstructorsCore(NameFilter? filter) => _actualType.GetConstructorsCore(filter);
        internal override IEnumerable<MethodInfo> GetMethodsCore(NameFilter? filter, Type reflectedType) => _actualType.GetMethodsCore(filter, reflectedType);
        internal override IEnumerable<EventInfo> GetEventsCore(NameFilter? filter, Type reflectedType) => _actualType.GetEventsCore(filter, reflectedType);
        internal override IEnumerable<FieldInfo> GetFieldsCore(NameFilter? filter, Type reflectedType) => _actualType.GetFieldsCore(filter, reflectedType);
        internal override IEnumerable<PropertyInfo> GetPropertiesCore(NameFilter? filter, Type reflectedType) => _actualType.GetPropertiesCore(filter, reflectedType);
        internal override IEnumerable<RoType> GetNestedTypesCore(NameFilter? filter) => _actualType.GetNestedTypesCore(filter);
    }
}
