// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    /// <summary>
    /// Base class for all modified types.
    /// Design supports code sharing between different runtimes and lazy loading of custom modifiers.
    /// </summary>
    // TODO (PR REVIEW COMMENT): no longer derive from TypeDelegator and throw NSE for members
    // that can directly or indirectly return an unmodified Type.
    internal abstract partial class ModifiedType : TypeDelegator
    {
        private readonly object? _signatureProvider;

        // These 3 fields, in order, determine the lookup hierarchy for custom modifiers.
        // The native tree traversal must match the managed semantics in order for indexes to match up.
        private readonly int _rootSignatureParameterIndex;
        private readonly int _nestedSignatureIndex;
        private readonly int _nestedSignatureParameterIndex;

        private bool _isRoot;

        protected ModifiedType(
            Type unmodifiedType,
            object? signatureProvider,
            int rootSignatureParameterIndex,
            int nestedSignatureIndex,
            int nestedSignatureParameterIndex,
            bool isRoot) : base(unmodifiedType)
        {
            _signatureProvider = signatureProvider;
            _rootSignatureParameterIndex = rootSignatureParameterIndex;
            _nestedSignatureIndex = nestedSignatureIndex;
            _nestedSignatureParameterIndex = nestedSignatureParameterIndex;
            _isRoot = isRoot;
        }

        /// <summary>
        /// Factory to create a node recursively based on the underlying, unmodified type.
        /// A type tree is formed due to arrays and pointers having an element type, function pointers
        /// having a return type and parameter types, and generic types having argument types.
        /// </summary>
        protected static ModifiedType Create(
            Type unmodifiedType,
            object? signatureProvider,
            int rootSignatureParameterIndex,
            ref int nestedSignatureIndex,
            int nestedSignatureParameterIndex,
            bool isRoot = false)
        {
            ModifiedType modifiedType;

            if (unmodifiedType.IsFunctionPointer)
            {
                modifiedType = new ModifiedFunctionPointerType(
                    unmodifiedType,
                    signatureProvider,
                    rootSignatureParameterIndex,
                    ref nestedSignatureIndex,
                    nestedSignatureParameterIndex,
                    isRoot);
            }
            else if (unmodifiedType.HasElementType)
            {
                modifiedType = new ModifiedHasElementType(
                    unmodifiedType,
                    signatureProvider,
                    rootSignatureParameterIndex,
                    ref nestedSignatureIndex,
                    nestedSignatureParameterIndex,
                    isRoot);
            }
            else if (unmodifiedType.IsGenericType)
            {
                modifiedType = new ModifiedGenericType(
                    unmodifiedType,
                    signatureProvider,
                    rootSignatureParameterIndex,
                    ref nestedSignatureIndex,
                    nestedSignatureParameterIndex,
                    isRoot);
            }
            else
            {
                modifiedType = new ModifiedStandaloneType(
                    unmodifiedType,
                    signatureProvider,
                    rootSignatureParameterIndex,
                    nestedSignatureIndex, // Passing byref is not necessary; no more recursion.
                    nestedSignatureParameterIndex,
                    isRoot);
            }

            return modifiedType;
        }


        /// <summary>
        /// The runtime-specific information to look up a signature, such as 'Signature' class or a type handle.
        /// </summary>
        protected object? SignatureProvider => _signatureProvider;

        /// <summary>
        /// Part 1 of 3 to determine the lookup hierarchy for custom modifiers.
        /// Specifies the root signature's parameter index (0 for properties, 1 for fields, 0..n for methods).
        /// </summary>
        protected int RootSignatureParameterIndex => _rootSignatureParameterIndex;

        /// <summary>
        /// Part 2 of 3 to determine the lookup hierarchy for custom modifiers.
        /// Specifies the nested signature's index into the recursive type tree which was the running count
        /// for each signature node as it was created.
        /// A value of -1 means the node is not nested under any signature.
        /// </summary>
        protected int NestedSignatureIndex => _nestedSignatureIndex;

        /// <summary>
        /// Part 3 of 3 to determine the lookup hierarchy for custom modifiers.
        /// Specifies the parameter index from a given signature node. 0 for return; 1..n for parameters.
        /// A value of -1 means the node is not a signature parameter.
        /// </summary>
        protected int NestedSignatureParameterIndex => _nestedSignatureParameterIndex;

        protected virtual bool IsRoot => _isRoot;

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

        public static T[] CloneArray<T>(T[] original, int start = 0)
        {
            if (original.Length == 0)
            {
                return original;
            }

            T[] copy = new T[original.Length - start];
            Array.Copy(sourceArray: original, sourceIndex: start, destinationArray: copy, destinationIndex: 0, length: original.Length - start);
            return copy;
        }
    }
}
