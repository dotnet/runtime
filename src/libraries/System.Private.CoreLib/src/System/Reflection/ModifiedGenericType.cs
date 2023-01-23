// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Reflection
{
    internal sealed partial class ModifiedGenericType : ModifiedType
    {
        private readonly ModifiedType[] _argumentTypes;

        /// <summary>
        /// Create a root node.
        /// </summary>
        public ModifiedGenericType(
            Type genericType,
            Type[] requiredModifiers,
            Type[] optionalModifiers,
            int rootSignatureParameterIndex)
            : base(genericType, requiredModifiers, optionalModifiers, rootSignatureParameterIndex)
        {
            Debug.Assert(genericType.IsGenericType);
            // To support modifiers on generic types, pass nestedSignatureIndex:0.
            _argumentTypes = CreateArguments(genericType.GetGenericArguments(), this, nestedSignatureIndex: -1);
        }

        /// <summary>
        /// Create a child node.
        /// </summary>
        public ModifiedGenericType(
            Type genericType,
            ModifiedType root,
            int nestedSignatureIndex,
            int nestedSignatureParameterIndex)
            : base(genericType, root, nestedSignatureIndex, nestedSignatureParameterIndex)
        {
            Debug.Assert(genericType.IsGenericType);
            // To support modifiers on generic types, pass nestedSignatureIndex+1 here.
            _argumentTypes = CreateArguments(genericType.GetGenericArguments(), root, nestedSignatureIndex);
        }

        public override Type[] GetGenericArguments() => CloneArray<Type>(_argumentTypes);

        private static ModifiedType[] CreateArguments(Type[] argumentTypes, ModifiedType root, int nestedSignatureIndex)
        {
            int count = argumentTypes.Length;
            ModifiedType[] modifiedTypes = new ModifiedType[count];
            for (int i = 0; i < count; i++)
            {
                // To support modifiers on generic types, pass an index value for nestedSignatureParameterIndex.
                modifiedTypes[i] = Create(argumentTypes[i], root, nestedSignatureIndex, nestedSignatureParameterIndex: -1);
            }

            return modifiedTypes;
        }
    }
}
