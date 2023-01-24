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

        public ModifiedContainerType(
            Type containerType,
            object? signatureProvider,
            int rootSignatureParameterIndex,
            int nestedSignatureIndex,
            int nestedSignatureParameterIndex,
            bool isRoot)
            : base(
                  containerType,
                  signatureProvider,
                  rootSignatureParameterIndex,
                  nestedSignatureIndex,
                  nestedSignatureParameterIndex,
                  isRoot)
        {
            Debug.Assert(containerType.HasElementType);
            _elementModifiedType = Create(
                containerType.GetElementType()!,
                signatureProvider,
                rootSignatureParameterIndex,
                nestedSignatureIndex,
                nestedSignatureParameterIndex : -1);
        }

        public override Type? GetElementType() => _elementModifiedType;
    }
}
