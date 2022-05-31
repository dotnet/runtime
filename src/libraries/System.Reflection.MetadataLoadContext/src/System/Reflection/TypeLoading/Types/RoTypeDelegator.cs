// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using StructLayoutAttribute = System.Runtime.InteropServices.StructLayoutAttribute;

namespace System.Reflection.TypeLoading
{
    // Similar to TypeDelegator, wraps an instance of a Type (in this case, RoType) and delegates calls to it.
    // TypeDelegator cannot be used since it cannot derive from RoType.
    // This is only used by function pointers which are based on method signatures which the MetadataReader
    // instantiates with the "modified type" pattern of chained types to represent custom modifiers.
    internal abstract class RoTypeDelegator : RoType
    {
        private RoType _typeImpl;

        protected RoTypeDelegator(RoType actualType)
        {
            _typeImpl = actualType;
        }

        public RoType TypeImpl => _typeImpl;

        // RoType also overrides Equals to ensure both l- and r-value are checked for actualType.
        public override bool Equals([NotNullWhen(true)] object? obj) => _typeImpl.Equals(obj);

        public override int GetHashCode() => _typeImpl.GetHashCode();
        public override string ToString() => _typeImpl.ToString();

        public override bool IsTypeDefinition => _typeImpl.IsTypeDefinition;
        public override bool IsGenericTypeDefinition => _typeImpl.IsGenericTypeDefinition;
        protected override bool HasElementTypeImpl() => _typeImpl.Call_HasElementTypeImpl();
        protected override bool IsArrayImpl() => _typeImpl.Call_IsArrayImpl();
        public override bool IsSZArray => _typeImpl.IsSZArray();
        public override bool IsVariableBoundArray => _typeImpl.IsVariableBoundArray;
        protected override bool IsByRefImpl() => _typeImpl.Call_IsByRefImpl();
        protected override bool IsPointerImpl() => _typeImpl.Call_IsPointerImpl();
        public override bool IsFunctionPointer => _typeImpl.IsFunctionPointer;
        public override bool IsUnmanagedFunctionPointer => _typeImpl.IsUnmanagedFunctionPointer;
        public override bool IsConstructedGenericType => _typeImpl.IsConstructedGenericType;
        public override bool IsGenericParameter => _typeImpl.IsGenericParameter;
        public override bool IsGenericTypeParameter => _typeImpl.IsGenericTypeParameter;
        public override bool IsGenericMethodParameter => _typeImpl.IsGenericMethodParameter;
        public override bool ContainsGenericParameters => _typeImpl.ContainsGenericParameters;

        internal override RoModule GetRoModule() => _typeImpl.GetRoModule();

        public override int GetArrayRank() => _typeImpl.GetArrayRank();

        protected override string ComputeName() => _typeImpl.Call_ComputeName();
        protected override string? ComputeNamespace() => _typeImpl.Call_ComputeNamespace();
        protected override string? ComputeFullName() => _typeImpl.Call_ComputeFullName();

        protected override TypeAttributes ComputeAttributeFlags() => _typeImpl.Call_ComputeAttributeFlags();
        protected override TypeCode GetTypeCodeImpl() => _typeImpl.Call_GetTypeCodeImpl();

        public override MethodBase? DeclaringMethod => _typeImpl.DeclaringMethod;
        protected override RoType? ComputeDeclaringType() => _typeImpl.Call_ComputeDeclaringType();

        public override IEnumerable<CustomAttributeData> CustomAttributes => _typeImpl.CustomAttributes;
        internal override bool IsCustomAttributeDefined(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name) => _typeImpl.IsCustomAttributeDefined(ns, name);
        internal override CustomAttributeData? TryFindCustomAttribute(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name) => _typeImpl.TryFindCustomAttribute(ns, name);

        public override int MetadataToken => _typeImpl.MetadataToken;

        internal override RoType? GetRoElementType() => _typeImpl.GetRoElementType();

        public override Type GetGenericTypeDefinition() => _typeImpl.GetGenericTypeDefinition();
        internal override RoType[] GetGenericTypeParametersNoCopy() => _typeImpl.GetGenericTypeParametersNoCopy();
        internal override RoType[] GetGenericTypeArgumentsNoCopy() => _typeImpl.GetGenericTypeArgumentsNoCopy();
        protected internal override RoType[] GetGenericArgumentsNoCopy() => _typeImpl.GetGenericArgumentsNoCopy();
        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override Type MakeGenericType(params Type[] typeArguments) => _typeImpl.MakeGenericType(typeArguments);

        public override GenericParameterAttributes GenericParameterAttributes => _typeImpl.GenericParameterAttributes;
        public override int GenericParameterPosition => _typeImpl.GenericParameterPosition;
        public override Type[] GetGenericParameterConstraints() => _typeImpl.GetGenericParameterConstraints();
        public override Type GetFunctionPointerReturnType() => _typeImpl.GetFunctionPointerReturnType();
        public override Type[] GetFunctionPointerParameterTypes() => _typeImpl.GetFunctionPointerParameterTypes();
        public override Guid GUID => _typeImpl.GUID;
        public override StructLayoutAttribute? StructLayoutAttribute => _typeImpl.StructLayoutAttribute;
        protected internal override RoType ComputeEnumUnderlyingType() => _typeImpl.ComputeEnumUnderlyingType();

        internal override RoType? ComputeBaseTypeWithoutDesktopQuirk() => _typeImpl.ComputeBaseTypeWithoutDesktopQuirk();
        internal override IEnumerable<RoType> ComputeDirectlyImplementedInterfaces() => _typeImpl.ComputeDirectlyImplementedInterfaces();

        // Low level support for the BindingFlag-driven enumerator apis.
        internal override IEnumerable<ConstructorInfo> GetConstructorsCore(NameFilter? filter) => _typeImpl.GetConstructorsCore(filter);
        internal override IEnumerable<MethodInfo> GetMethodsCore(NameFilter? filter, Type reflectedType) => _typeImpl.GetMethodsCore(filter, reflectedType);
        internal override IEnumerable<EventInfo> GetEventsCore(NameFilter? filter, Type reflectedType) => _typeImpl.GetEventsCore(filter, reflectedType);
        internal override IEnumerable<FieldInfo> GetFieldsCore(NameFilter? filter, Type reflectedType) => _typeImpl.GetFieldsCore(filter, reflectedType);
        internal override IEnumerable<PropertyInfo> GetPropertiesCore(NameFilter? filter, Type reflectedType) => _typeImpl.GetPropertiesCore(filter, reflectedType);
        internal override IEnumerable<RoType> GetNestedTypesCore(NameFilter? filter) => _typeImpl.GetNestedTypesCore(filter);
    }
}
