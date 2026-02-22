// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    internal sealed class SignatureModifiedType : SignatureType
    {
        internal SignatureModifiedType(Type baseType, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers)
        {
            if (baseType is SignatureModifiedType modifiedType)
            {
                baseType = modifiedType.UnderlyingSystemType;
                requiredCustomModifiers = [.. requiredCustomModifiers, .. modifiedType.GetRequiredCustomModifiers()];
                optionalCustomModifiers = [.. optionalCustomModifiers, .. modifiedType.GetOptionalCustomModifiers()];
            }

            _unmodifiedType = baseType;
            _requiredCustomModifiers = requiredCustomModifiers;
            _optionalCustomModifiers = optionalCustomModifiers;
        }

        private readonly Type _unmodifiedType;
        private readonly Type[] _requiredCustomModifiers;
        private readonly Type[] _optionalCustomModifiers;

        public override Type UnderlyingSystemType => _unmodifiedType;
        public override Type[] GetRequiredCustomModifiers() => (Type[])_requiredCustomModifiers.Clone();
        public override Type[] GetOptionalCustomModifiers() => (Type[])_optionalCustomModifiers.Clone();

        public override bool IsTypeDefinition => _unmodifiedType.IsTypeDefinition;
        public override bool IsSZArray => _unmodifiedType.IsSZArray;
        public override bool IsVariableBoundArray => _unmodifiedType.IsVariableBoundArray;
        public override bool IsByRefLike => _unmodifiedType.IsByRefLike;
        public override bool IsFunctionPointer => _unmodifiedType.IsFunctionPointer;
        public override bool IsGenericTypeDefinition => _unmodifiedType.IsGenericTypeDefinition;
        public override bool IsConstructedGenericType => _unmodifiedType.IsConstructedGenericType;
        public override bool IsGenericParameter => _unmodifiedType.IsGenericParameter;
        public override bool IsGenericTypeParameter => _unmodifiedType.IsGenericTypeParameter;
        public override bool IsGenericMethodParameter => _unmodifiedType.IsGenericMethodParameter;
        public override bool IsUnmanagedFunctionPointer => _unmodifiedType.IsUnmanagedFunctionPointer;
        public override bool ContainsGenericParameters => _unmodifiedType.ContainsGenericParameters;
        public override Type[] GenericTypeArguments => _unmodifiedType.GenericTypeArguments;
        public override int GenericParameterPosition => _unmodifiedType.GenericParameterPosition;
        internal override SignatureType? ElementType => HasElementType ? new SignatureModifiedType(_unmodifiedType.GetElementType()!, [], []) : null;
        public override string Name => _unmodifiedType.Name;
        public override string? Namespace => _unmodifiedType.Namespace;
        public override bool IsEnum => _unmodifiedType.IsEnum;
        protected override bool HasElementTypeImpl() => _unmodifiedType.HasElementType;
        protected override bool IsArrayImpl() => _unmodifiedType.IsArray;
        protected override bool IsByRefImpl() => _unmodifiedType.IsByRef;
        protected override bool IsPointerImpl() => _unmodifiedType.IsPointer;
        public override int GetArrayRank() => _unmodifiedType.GetArrayRank();
        public override Type[] GetFunctionPointerCallingConventions() => _unmodifiedType.GetFunctionPointerCallingConventions();
        public override Type[] GetFunctionPointerParameterTypes() => _unmodifiedType.GetFunctionPointerParameterTypes();
        public override Type GetFunctionPointerReturnType() => _unmodifiedType.GetFunctionPointerReturnType();
        public override Type GetGenericTypeDefinition() => _unmodifiedType.GetGenericTypeDefinition();
        public override Type[] GetGenericArguments() => _unmodifiedType.GetGenericArguments();
        public override string ToString() => _unmodifiedType.ToString();
        protected override bool IsValueTypeImpl() => _unmodifiedType.IsValueType;
    }
}
