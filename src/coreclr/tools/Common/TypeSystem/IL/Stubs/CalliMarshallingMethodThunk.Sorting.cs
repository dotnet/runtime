// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    // Functionality related to deterministic ordering of methods
    partial class CalliMarshallingMethodThunk
    {
        protected internal override int ClassCode => 1594107963;

        protected internal override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            var otherMethod = (CalliMarshallingMethodThunk)other;
            return comparer.Compare(_targetSignature, otherMethod._targetSignature);
        }
    }
}
