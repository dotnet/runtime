// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    internal sealed class SignatureModifiedType : SignatureType
    {
        internal SignatureModifiedType(Type baseType, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers)
        {
            _baseType = baseType;
            _requiredCustomModifiers = requiredCustomModifiers;
            _optionalCustomModifiers = optionalCustomModifiers;
        }

        private readonly Type _baseType;
        private readonly Type[] _requiredCustomModifiers;
        private readonly Type[] _optionalCustomModifiers;

        public override Type[] GetRequiredCustomModifiers() => _requiredCustomModifiers;
        public override Type[] GetOptionalCustomModifiers() => _optionalCustomModifiers;

        public override bool IsTypeDefinition => _baseType.IsTypeDefinition;
        public override bool IsSZArray => _baseType.IsSZArray;
        public override bool IsVariableBoundArray => _baseType.IsVariableBoundArray;
        public override bool IsByRefLike => _baseType.IsByRefLike;
        public override bool IsGenericTypeDefinition => _baseType.IsGenericTypeDefinition;
        public override bool IsConstructedGenericType => _baseType.IsConstructedGenericType;
        public override bool IsGenericParameter => _baseType.IsGenericParameter;
        public override bool IsGenericTypeParameter => _baseType.IsGenericTypeParameter;
        public override bool IsGenericMethodParameter => _baseType.IsGenericMethodParameter;
        public override bool ContainsGenericParameters => _baseType.ContainsGenericParameters;
        public override Type[] GenericTypeArguments => _baseType.GenericTypeArguments;
        public override int GenericParameterPosition => _baseType.GenericParameterPosition;
        internal override SignatureType? ElementType => null;
        public override string Name => _baseType.Name;
        public override string? Namespace => _baseType.Namespace;
        public override bool IsEnum => _baseType.IsEnum;
        protected override bool HasElementTypeImpl() => _baseType.HasElementType;
        protected override bool IsArrayImpl() => _baseType.IsArray;
        protected override bool IsByRefImpl() => _baseType.IsByRef;
        protected override bool IsPointerImpl() => _baseType.IsPointer;
        public override int GetArrayRank() => _baseType.GetArrayRank();
        public override Type GetGenericTypeDefinition() => _baseType.GetGenericTypeDefinition();
        public override Type[] GetGenericArguments() => _baseType.GetGenericArguments();
        public override string ToString() => _baseType.ToString();
        protected override bool IsValueTypeImpl() => _baseType.IsValueType;
    }
}
