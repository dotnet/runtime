// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    /// <summary>
    /// Base class for modified types and standalone modified type.
    /// Design supports code sharing between different runtimes and lazy loading of custom modifiers.
    /// </summary>
    // TODO (PR REVIEW COMMENT): no longer derive from TypeDelegator and throw NSE for members
    // that can directly or indirectly return an unmodified Type.
    internal partial class ModifiedType : TypeDelegator
    {
        private readonly TypeSignature _typeSignature;

        internal ModifiedType(Type unmodifiedType, TypeSignature typeSignature)
            : base(unmodifiedType)
        {
            _typeSignature = typeSignature;
        }

        /// <summary>
        /// Factory to create a node recursively based on the underlying, unmodified type.
        /// A type tree is formed due to arrays and pointers having an element type, function pointers
        /// having a return type and parameter types, and generic types having argument types.
        /// </summary>
        protected static Type Create(Type unmodifiedType, TypeSignature typeSignature)
        {
            Type modifiedType;
            if (unmodifiedType.IsFunctionPointer)
            {
                modifiedType = new ModifiedFunctionPointerType(unmodifiedType, typeSignature);
            }
            else if (unmodifiedType.HasElementType)
            {
                modifiedType = new ModifiedHasElementType(unmodifiedType, typeSignature);
            }
            else if (unmodifiedType.IsGenericType)
            {
                modifiedType = new ModifiedGenericType(unmodifiedType, typeSignature);
            }
            else
            {
                modifiedType = new ModifiedType(unmodifiedType, typeSignature);
            }
            return modifiedType;
        }

        public override Type[] GetRequiredCustomModifiers()
        {
            // No caching is performed; as is the case with FieldInfo.GetCustomModifiers and friends.
            return GetCustomModifiers(required: true);
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            // No caching is performed; as is the case with FieldInfo.GetCustomModifiers and friends.
            return GetCustomModifiers(required: false);
        }

        // TypeDelegator doesn't forward these the way we want:
        public override bool ContainsGenericParameters => typeImpl.ContainsGenericParameters; // not forwarded.
        public override bool Equals(Type? other) // Not forwarded.
        {
            if (other is ModifiedType otherModifiedType)
            {
                return ReferenceEquals(this, otherModifiedType);
            }

            return false;
        }
        public override int GetHashCode() => UnderlyingSystemType.GetHashCode(); // Not forwarded.
        public override Type GetGenericTypeDefinition() => typeImpl.GetGenericTypeDefinition(); // not forwarded.
        public override bool IsGenericType => typeImpl.IsGenericType; // Not forwarded.
        public override string ToString() => UnderlyingSystemType.ToString(); // Not forwarded.
        public override Type UnderlyingSystemType => typeImpl; // We don't want to forward to typeImpl.UnderlyingSystemType.
    }
}
