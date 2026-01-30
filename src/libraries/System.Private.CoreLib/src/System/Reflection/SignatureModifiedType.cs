// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    internal sealed class SignatureModifiedType : SignatureType
    {
        internal SignatureModifiedType(Type unmodifiedType, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers)
        {
            _unmodifiedType = unmodifiedType;
            _requiredCustomModifiers = requiredCustomModifiers;
            _optionalCustomModifiers = optionalCustomModifiers;
        }

        private readonly Type _unmodifiedType;
        private readonly Type[] _requiredCustomModifiers;
        private readonly Type[] _optionalCustomModifiers;

        public override Type UnderlyingSystemType => _unmodifiedType;
        public override Type[] GetRequiredCustomModifiers() => _requiredCustomModifiers;
        public override Type[] GetOptionalCustomModifiers() => _optionalCustomModifiers;

        public override bool IsTypeDefinition => _unmodifiedType.IsTypeDefinition;
        public override bool IsSZArray => _unmodifiedType.IsSZArray;
        public override bool IsVariableBoundArray => _unmodifiedType.IsVariableBoundArray;
        public override bool IsByRefLike => _unmodifiedType.IsByRefLike;
        public override bool IsGenericTypeDefinition => _unmodifiedType.IsGenericTypeDefinition;
        public override bool IsConstructedGenericType => _unmodifiedType.IsConstructedGenericType;
        public override bool IsGenericParameter => _unmodifiedType.IsGenericParameter;
        public override bool IsGenericTypeParameter => _unmodifiedType.IsGenericTypeParameter;
        public override bool IsGenericMethodParameter => _unmodifiedType.IsGenericMethodParameter;
        public override bool ContainsGenericParameters => _unmodifiedType.ContainsGenericParameters;
        public override Type[] GenericTypeArguments => _unmodifiedType.GenericTypeArguments;
        public override int GenericParameterPosition => _unmodifiedType.GenericParameterPosition;
        internal override SignatureType? ElementType => null;
        public override string Name => _unmodifiedType.Name;
        public override string? Namespace => _unmodifiedType.Namespace;
        public override bool IsEnum => _unmodifiedType.IsEnum;
        protected override bool HasElementTypeImpl() => _unmodifiedType.HasElementType;
        protected override bool IsArrayImpl() => _unmodifiedType.IsArray;
        protected override bool IsByRefImpl() => _unmodifiedType.IsByRef;
        protected override bool IsPointerImpl() => _unmodifiedType.IsPointer;
        public override int GetArrayRank() => _unmodifiedType.GetArrayRank();
        public override Type GetGenericTypeDefinition() => _unmodifiedType.GetGenericTypeDefinition();
        public override Type[] GetGenericArguments() => _unmodifiedType.GetGenericArguments();
        public override string ToString() => _unmodifiedType.ToString();
        protected override bool IsValueTypeImpl() => _unmodifiedType.IsValueType;
    }
}
