// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Reflection
{
    internal abstract class SignatureHasElementType : SignatureType
    {
        protected SignatureHasElementType(SignatureType elementType)
        {
            Debug.Assert(elementType != null);  
            _elementType = elementType;
        }
    
        public sealed override bool IsTypeDefinition => false;
        public sealed override bool IsGenericTypeDefinition => false;
        protected sealed override bool HasElementTypeImpl() => true;
        protected abstract override bool IsArrayImpl();
        protected abstract override bool IsByRefImpl();
        public sealed override bool IsByRefLike => false;
        protected abstract override bool IsPointerImpl();
        public abstract override bool IsSZArray { get; }
        public abstract override bool IsVariableBoundArray { get; }
        public sealed override bool IsConstructedGenericType => false;
        public sealed override bool IsGenericParameter => false;
        public sealed override bool IsGenericTypeParameter => false;
        public sealed override bool IsGenericMethodParameter => false;
        public sealed override bool ContainsGenericParameters => _elementType.ContainsGenericParameters;
    
        internal sealed override SignatureType ElementType => _elementType;
        public abstract override int GetArrayRank();
        public sealed override Type GetGenericTypeDefinition() => throw new InvalidOperationException(SR.InvalidOperation_NotGenericType);
        public sealed override Type[] GetGenericArguments() => Array.Empty<Type>();
        public sealed override Type[] GenericTypeArguments => Array.Empty<Type>();
        public sealed override int GenericParameterPosition => throw new InvalidOperationException(SR.Arg_NotGenericParameter);
    
        public sealed override string Name => _elementType.Name + Suffix;
        public sealed override string Namespace => _elementType.Namespace;
    
        public sealed override string ToString() => _elementType.ToString() + Suffix;
    
        protected abstract string Suffix { get; } 
    
        private readonly SignatureType _elementType;
    }
}
