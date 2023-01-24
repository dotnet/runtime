// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    // Functionality related to deterministic ordering of types
    public partial class DynamicInvokeMethodThunk
    {
        protected override int ClassCode => -1980933220;

        protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            var otherMethod = (DynamicInvokeMethodThunk)other;
            return comparer.Compare(_targetSignature, otherMethod._targetSignature);
        }
    }
}
