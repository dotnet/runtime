// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;

using Internal.TypeSystem.Ecma;

namespace ILTrim.DependencyAnalysis
{
    public abstract class TokenBasedNodeWithDelayedSort : TokenBasedNode
    {
        public TokenBasedNodeWithDelayedSort(EcmaModule module, EntityHandle handle)
            : base(module, handle)
        {
        }

        public abstract void PrepareForDelayedSort(TokenMap.Builder tokenMap);
    }
}
