// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.Reflection
{
    internal sealed class SignatureFunctionPointerType : SignatureType
    {
        internal SignatureFunctionPointerType(Type returnType, Type[] parameterTypes, bool isUnmanaged, Type[] callingConventions)
        {
            _returnType = returnType;
            _parameterTypes = parameterTypes;
            _isUnmanaged = isUnmanaged;
            _callingConventions = callingConventions;
        }

        private readonly Type _returnType;
        private readonly Type[] _parameterTypes;
        private readonly bool _isUnmanaged;
        private readonly Type[] _callingConventions;

        public override bool IsFunctionPointer => true;
        public override bool IsUnmanagedFunctionPointer => _isUnmanaged;

        public override Type[] GetFunctionPointerCallingConventions() => (Type[])_callingConventions.Clone();
        public override Type[] GetFunctionPointerParameterTypes() => (Type[])_parameterTypes.Clone();
        public override Type GetFunctionPointerReturnType() => _returnType;

        public override bool IsEnum => false;
        public override bool IsTypeDefinition => false;
        public override bool IsSZArray => false;
        public override bool IsVariableBoundArray => false;
        public override bool IsByRefLike => false;
        public override bool IsGenericTypeDefinition => false;
        public override bool IsConstructedGenericType => false;
        public override bool IsGenericParameter => false;
        public override bool IsGenericTypeParameter => false;
        public override bool IsGenericMethodParameter => false;
        public override bool ContainsGenericParameters
        {
            get
            {
                if (_returnType.ContainsGenericParameters)
                {
                    return true;
                }

                for (int i = 0; i < _parameterTypes.Length; i++)
                {
                    if (_parameterTypes[i].ContainsGenericParameters)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
        public override Type[] GenericTypeArguments => [];
        public override int GenericParameterPosition => 0;
        internal override SignatureType? ElementType => null;
        public override string Name => string.Empty;
        public override string? Namespace => null;
        protected override bool HasElementTypeImpl() => false;
        protected override bool IsArrayImpl() => false;
        protected override bool IsByRefImpl() => false;
        protected override bool IsPointerImpl() => false;
        public sealed override int GetArrayRank() => throw new ArgumentException(SR.Argument_HasToBeArrayClass);
        public sealed override Type GetGenericTypeDefinition() => throw new InvalidOperationException(SR.InvalidOperation_NotGenericType);
        public override Type[] GetGenericArguments() => [];
        public override Type[] GetOptionalCustomModifiers() => [];
        public override Type[] GetRequiredCustomModifiers() => [];
        protected override bool IsValueTypeImpl() => false;

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append(_returnType.ToString());
            sb.Append('(');

            for (int i = 0; i < _parameterTypes.Length; i++)
            {
                sb.Append(_parameterTypes[i].ToString());
                if (i < _parameterTypes.Length - 1)
                    sb.Append(", ");
            }

            sb.Append(')');
            return sb.ToString();
        }
    }
}
