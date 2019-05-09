// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection
{
    internal abstract class SignatureGenericParameterType : SignatureType
    {
        protected SignatureGenericParameterType(int position)
        {
            Debug.Assert(position >= 0);
            _position = position;
        }
    
        public sealed override bool IsTypeDefinition => false;
        public sealed override bool IsGenericTypeDefinition => false;
        protected sealed override bool HasElementTypeImpl() => false;
        protected sealed override bool IsArrayImpl() => false;
        protected sealed override bool IsByRefImpl() => false;
        public sealed override bool IsByRefLike => false;
        protected sealed override bool IsPointerImpl() => false;
        public sealed override bool IsSZArray => false;
        public sealed override bool IsVariableBoundArray => false;
        public sealed override bool IsConstructedGenericType => false;
        public sealed override bool IsGenericParameter => true;
        public abstract override bool IsGenericMethodParameter { get; }
        public sealed override bool ContainsGenericParameters => true;
    
        internal sealed override SignatureType? ElementType => null;
        public sealed override int GetArrayRank() => throw new ArgumentException(SR.Argument_HasToBeArrayClass);
        public sealed override Type GetGenericTypeDefinition() => throw new InvalidOperationException(SR.InvalidOperation_NotGenericType);
        public sealed override Type[] GetGenericArguments() => Array.Empty<Type>();
        public sealed override Type[] GenericTypeArguments => Array.Empty<Type>();
        public sealed override int GenericParameterPosition => _position;
        public abstract override string Name { get; }
        public sealed override string? Namespace => null;
    
        public sealed override string ToString() => Name;
    
        private readonly int _position;
    }
}
