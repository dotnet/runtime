// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Reflection
{
    /// <summary>
    /// An array, pointer or reference type.
    /// </summary>
    internal sealed class ModifiedContainerType : ModifiedType
    {
        private readonly ModifiedType? _elementModifiedType;

        /// <summary>
        /// Create a root node.
        /// </summary>
        public ModifiedContainerType(Type containerType, int rootSignatureParameterIndex)
            : base(containerType, rootSignatureParameterIndex)
        {
            Debug.Assert(containerType.HasElementType);
            _elementModifiedType = Create(containerType.GetElementType()!, this, nestedSignatureIndex: -1, nestedSignatureParameterIndex: -1);
        }

        /// <summary>
        /// Create a child node.
        /// </summary>
        public ModifiedContainerType(
            Type containerType,
            ModifiedType root,
            int nestedSignatureIndex,
            int nestedSignatureParamterIndex)
            : base(containerType, root, nestedSignatureIndex, nestedSignatureParamterIndex)
        {
            Debug.Assert(containerType.HasElementType);
            _elementModifiedType = Create(containerType.GetElementType()!, root, nestedSignatureIndex, nestedSignatureParamterIndex);
        }

        public override Type? GetElementType() => _elementModifiedType;
    }
}
