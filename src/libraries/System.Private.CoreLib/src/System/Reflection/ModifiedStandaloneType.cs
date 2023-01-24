// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    internal sealed class ModifiedStandaloneType : ModifiedType
    {
        public ModifiedStandaloneType(
            Type delegatingType,
            object? signtureProvider,
            int rootSignatureParameterIndex,
            int nestedSignatureIndex,
            int nestedSignatureParameterIndex,
            bool isRoot)
            : base(
                  delegatingType,
                  signtureProvider,
                  rootSignatureParameterIndex,
                  nestedSignatureIndex,
                  nestedSignatureParameterIndex,
                  isRoot)
        {
        }
    }
}
