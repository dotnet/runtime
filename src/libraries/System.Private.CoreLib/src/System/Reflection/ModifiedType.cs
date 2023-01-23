// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Reflection
{
    /// <summary>
    /// Base class for all modified types.
    /// Design supports code sharing between different runtimes and lazy loading of custom modifiers.
    /// </summary>
    internal abstract partial class ModifiedType : TypeDelegator
    {
        private readonly ModifiedType? _root;
        private object? _rootFieldParameterOrProperty;

        // These 3 fields, in order, determine the lookup hierarchy for custom modifiers.
        // The native tree traveral must match the managed semantics in order to indexes to match up.
        protected readonly int _rootSignatureParameterIndex;
        private readonly int _nestedSignatureIndex;
        private readonly int _nestedSignatureParameterIndex;

        /// <summary>
        /// Create a root node.
        /// </summary>
        protected ModifiedType(
            Type unmodifiedType,
            object rootFieldParameterOrProperty,
            int rootSignatureParameterIndex) : base(unmodifiedType)
        {
            _root = this;
            _rootFieldParameterOrProperty = rootFieldParameterOrProperty;
            _rootSignatureParameterIndex = rootSignatureParameterIndex;
            _nestedSignatureParameterIndex = -1;
        }

        /// <summary>
        /// Create a child node.
        /// </summary>
        protected ModifiedType(
            Type unmodifiedType,
            ModifiedType? root,
            int nestedSignatureIndex,
            int nestedSignatureParameterIndex) : base(unmodifiedType)
        {
            _rootSignatureParameterIndex = -1;
            _root = root;
            _nestedSignatureIndex = nestedSignatureIndex;
            _nestedSignatureParameterIndex = nestedSignatureParameterIndex;
        }

        /// <summary>
        /// Factory to create a child node recursively.
        /// A type tree is formed due to arrays and pointers having an element type, function pointers
        /// having a return type and parameter types and generic types having argument types.
        /// </summary>
        public static ModifiedType Create(
            Type unmodifiedType,
            ModifiedType root,
            int nestedSignatureIndex,
            int nestedSignatureParameterIndex)
        {
            ModifiedType modifiedType;

            if (unmodifiedType.IsFunctionPointer)
            {
                modifiedType = new ModifiedFunctionPointerType(unmodifiedType, root, nestedSignatureIndex, nestedSignatureParameterIndex);
            }
            else if (unmodifiedType.HasElementType)
            {
                modifiedType = new ModifiedContainerType(unmodifiedType, root, nestedSignatureIndex, nestedSignatureParameterIndex);
            }
            else if (unmodifiedType.IsGenericType)
            {
                modifiedType = new ModifiedGenericType(unmodifiedType, root, nestedSignatureIndex, nestedSignatureParameterIndex);
            }
            else
            {
                modifiedType = new ModifiedStandaloneType(unmodifiedType, root, nestedSignatureIndex, nestedSignatureParameterIndex);
            }

            return modifiedType;
        }

        /// <summary>
        /// The root signature's parameter index (0 for properties, 1 for fields, 0..n for methods).
        /// A value of -1 means the value is not used because the node is not a root.
        /// </summary>
        protected int RootSignatureParameterIndex => Root._rootSignatureParameterIndex;

        /// <summary>
        /// The nested signature's index into the recursive type tree.
        /// A signature exists for function pointers and generic types.
        /// </summary>
        // For delegate*<void>[]: 0 for the function pointer since nested by the array
        // For delegate*<delegate*<void>>: -1 for the outer (since not nested); 0 for the inner
        // For delegate*<delegate*<void>>[]: 0 for the outer; 1 for the inner
        protected int NestedSignatureIndex => _nestedSignatureIndex;

        /// <summary>
        /// From a given signature from <see cref="NestedSignatureIndex"/>, which parameter index does
        /// the node belong to. 0 for return; 1..n for parameters.
        /// A value of -1 means the value is not used because the node is not a signature parameter.
        /// /// </summary>
        protected int NestedSignatureParameterIndex => _nestedSignatureParameterIndex;

        /// <summary>
        /// The root node which contains the signature reference and _rootSignatureParameterIndex.
        /// </summary>
        public ModifiedType Root
        {
            get
            {
                Debug.Assert( _root != null );
                return _root;
            }
        }

        public override Type[] GetRequiredCustomModifiers()
        {
            // No caching is performed; as is the case with FieldInfo.GetCustomModifiers etc.
            return GetCustomModifiers(required: true);
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            // No caching is performed; as is the case with FieldInfo.GetCustomModifiers etc.
            return GetCustomModifiers(required: false);
        }

        private Type[] GetCustomModifiers(bool required)
        {
            Type[] modifiers = EmptyTypes;

            if (_nestedSignatureParameterIndex >= 0)
            {
                modifiers = GetCustomModifiersFromSignature(required);
            }
            else if (ReferenceEquals(this, Root))
            {
                object? obj = _rootFieldParameterOrProperty;
                Debug.Assert(obj is not null);

                if (obj is FieldInfo fieldInfo)
                {
                    modifiers = required ? fieldInfo.GetRequiredCustomModifiers() : fieldInfo.GetOptionalCustomModifiers();
                }
                else if (obj is ParameterInfo parameterInfo)
                {
                    modifiers = required ? parameterInfo.GetRequiredCustomModifiers() : parameterInfo.GetOptionalCustomModifiers();
                }
                else if (obj is PropertyInfo propertyInfo)
                {
                    modifiers = required ? propertyInfo.GetRequiredCustomModifiers() : propertyInfo.GetOptionalCustomModifiers();
                }
                else
                {
                    Debug.Assert(false);
                }
            }

            return modifiers;
        }

        // TypeDelegator doesn't forward these the way we want:
        public override Type UnderlyingSystemType => typeImpl; // We don't want to forward to typeImpl.UnderlyingSystemType.
        public override bool IsGenericType => typeImpl.IsGenericType;
        public override string ToString() => UnderlyingSystemType.ToString(); // Not forwarded.
        public override int GetHashCode() => UnderlyingSystemType.GetHashCode(); // Not forwarded.
        public override bool Equals(Type? other) // Not forwarded.
        {
            if (other is ModifiedType otherModifiedType)
            {
                return ReferenceEquals(this, otherModifiedType);
            }

            return false;
        }

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
