// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.TypeLoading;
using StructLayoutAttribute = System.Runtime.InteropServices.StructLayoutAttribute;

namespace System.Reflection
{
    /// <summary>
    /// Base type for modified types obtained through FieldInfo.GetModifiedFieldInfo(), PropertyInfo.GetModifiedPropertyInfo()
    /// and ParameterInfo.GetModifiedParameterInfo().
    /// </summary>
    internal abstract class RoModifiedType : RoType
    {
        private List<Type>? _requiredModifiersBuilder;
        private List<Type>? _optionalModifiersBuilder;
        private Type[]? _requiredModifiers;
        private Type[]? _optionalModifiers;
        private readonly RoType _unmodifiedType;

        protected RoModifiedType(RoType unmodifiedType)
        {
            _unmodifiedType = unmodifiedType;
        }

        public static RoModifiedType Create(RoType unmodifiedType)
        {
            RoModifiedType modifiedType;

            if (unmodifiedType is RoModifiedType mod)
            {
                // A nested type, such as a function pointer in 'delegate*<void>[]' array, may already be modified.
                modifiedType = mod;
            }
            else if (unmodifiedType.IsFunctionPointer)
            {
                modifiedType = new RoModifiedFunctionPointerType((RoFunctionPointerType)unmodifiedType);
            }
            else if (unmodifiedType.IsGenericType)
            {
                modifiedType = new RoModifiedGenericType((RoConstructedGenericType)unmodifiedType);
            }
            else if (unmodifiedType.HasElementType)
            {
                modifiedType = new RoModifiedHasElementType(unmodifiedType);
            }
            else
            {
                modifiedType = new RoModifiedStandaloneType(unmodifiedType);
            }

            return modifiedType;
        }

        public void AddRequiredModifier(Type type)
        {
            Debug.Assert(_requiredModifiers == null);
            _requiredModifiersBuilder ??= new List<Type>();
            _requiredModifiersBuilder.Add(type);
        }

        public void AddOptionalModifier(Type type)
        {
            Debug.Assert(_optionalModifiers == null);
            _optionalModifiersBuilder ??= new List<Type>();
            _optionalModifiersBuilder.Add(type);
        }

        // Below are the multitude of Type overloads. We throw NSE for members that would return an unmodified Type
        // directly or indirectly. We do this in case we want to support returning modified types instead.

        public override Type[] GetRequiredCustomModifiers()
        {
            if (_requiredModifiers == null)
            {
                if (_requiredModifiersBuilder == null)
                {
                    _requiredModifiers = EmptyTypes;
                }
                else
                {
                    _requiredModifiers = _requiredModifiersBuilder.ToArray();
                    _requiredModifiersBuilder = null;
                }
            }

            return Helpers.CloneArray(_requiredModifiers);
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            if (_optionalModifiers == null)
            {
                if (_optionalModifiersBuilder == null)
                {
                    _optionalModifiers = EmptyTypes;
                }
                else
                {
                    _optionalModifiers = _optionalModifiersBuilder.ToArray();
                    _optionalModifiersBuilder = null;
                }
            }

            return Helpers.CloneArray(_optionalModifiers);
        }

        public override Type UnderlyingSystemType => _unmodifiedType;

        // Modified types do not support Equals. That would need to include custom modifiers and any parameterized types recursively.
        // UnderlyingSystemType.Equals() should should be used if basic equality is necessary.
        public override bool Equals([NotNullWhen(true)] object? obj) => throw new NotSupportedException(SR.NotSupported_ModifiedType);
        public override bool Equals(Type? other) => throw new NotSupportedException(SR.NotSupported_ModifiedType);
        public override int GetHashCode() => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        public override string ToString() => _unmodifiedType.ToString();
        public sealed override bool IsEnum => _unmodifiedType.IsEnum;
        protected sealed override bool IsPrimitiveImpl() => _unmodifiedType.IsPrimitive;
        protected sealed override bool IsValueTypeImpl() => _unmodifiedType.IsValueType;
        public override bool IsTypeDefinition => _unmodifiedType.IsTypeDefinition;
        public override bool IsGenericTypeDefinition => _unmodifiedType.IsGenericTypeDefinition;
        protected override bool HasElementTypeImpl() => _unmodifiedType.Call_HasElementTypeImpl();
        protected override bool IsArrayImpl() => _unmodifiedType.Call_IsArrayImpl();
        public override bool IsSZArray => _unmodifiedType.IsSZArray();
        public override bool IsVariableBoundArray => _unmodifiedType.IsVariableBoundArray;
        protected override bool IsByRefImpl() => _unmodifiedType.Call_IsByRefImpl();
        protected override bool IsPointerImpl() => _unmodifiedType.Call_IsPointerImpl();
        public override bool IsFunctionPointer => _unmodifiedType.IsFunctionPointer;
        public override bool IsUnmanagedFunctionPointer => _unmodifiedType.IsUnmanagedFunctionPointer;
        public override bool IsConstructedGenericType => _unmodifiedType.IsConstructedGenericType;
        public override bool IsGenericParameter => _unmodifiedType.IsGenericParameter;
        public override bool IsGenericTypeParameter => _unmodifiedType.IsGenericTypeParameter;
        public override bool IsGenericMethodParameter => _unmodifiedType.IsGenericMethodParameter;
        public override bool ContainsGenericParameters => _unmodifiedType.ContainsGenericParameters;

        internal override RoModule GetRoModule() => _unmodifiedType.GetRoModule();

        public override int GetArrayRank() => _unmodifiedType.GetArrayRank();

        protected override string ComputeName() => _unmodifiedType.Call_ComputeName();
        protected override string? ComputeNamespace() => _unmodifiedType.Call_ComputeNamespace();
        protected override string? ComputeFullName() => _unmodifiedType.Call_ComputeFullName();

        protected override TypeAttributes ComputeAttributeFlags() => _unmodifiedType.Call_ComputeAttributeFlags();
        protected override TypeCode GetTypeCodeImpl() => _unmodifiedType.Call_GetTypeCodeImpl();

        public override MethodBase? DeclaringMethod => throw new NotSupportedException(SR.NotSupported_ModifiedType);
        protected override RoType? ComputeDeclaringType() => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        public override IEnumerable<TypeInfo> DeclaredNestedTypes
        {
#if NET
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicNestedTypes)]
#endif
            get { throw new NotSupportedException(SR.NotSupported_ModifiedType); }
        }

        public override IEnumerable<CustomAttributeData> CustomAttributes => _unmodifiedType.CustomAttributes;
        internal override bool IsCustomAttributeDefined(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name) => _unmodifiedType.IsCustomAttributeDefined(ns, name);
        internal override CustomAttributeData? TryFindCustomAttribute(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name) => _unmodifiedType.TryFindCustomAttribute(ns, name);

        public override int MetadataToken => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        internal override RoType? GetRoElementType() => null;

        public override Type GetGenericTypeDefinition() => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        // Generic parameters are supported.
        internal override RoType[] GetGenericTypeParametersNoCopy() => _unmodifiedType.GetGenericTypeParametersNoCopy();
        internal override RoType[] GetGenericTypeArgumentsNoCopy() => _unmodifiedType.GetGenericTypeArgumentsNoCopy();
        protected internal override RoType[] GetGenericArgumentsNoCopy() => _unmodifiedType.GetGenericArgumentsNoCopy();
        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override Type MakeGenericType(params Type[] typeArguments) => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        public override Type GetFunctionPointerReturnType() => throw new InvalidOperationException(SR.InvalidOperation_NotFunctionPointer);
        public override Type[] GetFunctionPointerParameterTypes() => throw new InvalidOperationException(SR.InvalidOperation_NotFunctionPointer);

        public override GenericParameterAttributes GenericParameterAttributes => _unmodifiedType.GenericParameterAttributes;
        public override int GenericParameterPosition => _unmodifiedType.GenericParameterPosition;
        public override Type[] GetGenericParameterConstraints() => throw new NotSupportedException(SR.NotSupported_ModifiedType);
        public override Guid GUID => _unmodifiedType.GUID;
        public override StructLayoutAttribute? StructLayoutAttribute => _unmodifiedType.StructLayoutAttribute;
        protected internal override RoType ComputeEnumUnderlyingType() => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        internal override RoType? ComputeBaseTypeWithoutDesktopQuirk() => throw new NotSupportedException(SR.NotSupported_ModifiedType);
        internal override IEnumerable<RoType> ComputeDirectlyImplementedInterfaces() => throw new NotSupportedException(SR.NotSupported_ModifiedType);

        // Low level support for the BindingFlag-driven enumerator apis.
        internal override IEnumerable<ConstructorInfo> GetConstructorsCore(NameFilter? filter) => throw new NotSupportedException(SR.NotSupported_ModifiedType);
        internal override IEnumerable<MethodInfo> GetMethodsCore(NameFilter? filter, Type reflectedType) => throw new NotSupportedException(SR.NotSupported_ModifiedType);
        internal override IEnumerable<EventInfo> GetEventsCore(NameFilter? filter, Type reflectedType) => throw new NotSupportedException(SR.NotSupported_ModifiedType);
        internal override IEnumerable<FieldInfo> GetFieldsCore(NameFilter? filter, Type reflectedType) => throw new NotSupportedException(SR.NotSupported_ModifiedType);
        internal override IEnumerable<PropertyInfo> GetPropertiesCore(NameFilter? filter, Type reflectedType) => throw new NotSupportedException(SR.NotSupported_ModifiedType);
        internal override IEnumerable<RoType> GetNestedTypesCore(NameFilter? filter) => throw new NotSupportedException(SR.NotSupported_ModifiedType);
    }
}
