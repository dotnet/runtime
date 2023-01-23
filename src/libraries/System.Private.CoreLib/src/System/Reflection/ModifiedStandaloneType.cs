// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    internal sealed class ModifiedStandaloneType : ModifiedType
    {
        /// <summary>
        /// Create a root node.
        /// </summary>
        public ModifiedStandaloneType(
            Type delegatingType,
            object rootFieldParameterOrProperty,
            int rootSignatureParameterIndex)
            : base(delegatingType, rootFieldParameterOrProperty, rootSignatureParameterIndex) { }

        /// <summary>
        /// Create a child node.
        /// </summary>
        public ModifiedStandaloneType(
            Type delegatingType,
            ModifiedType? root,
            int nestedSignatureIndex,
            int nestedSignatureParameterIndex)
            : base(delegatingType, root, nestedSignatureIndex, nestedSignatureParameterIndex) { }
    }
}
