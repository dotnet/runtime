// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    // Functionality related to deterministic ordering of methods
    partial class DelegateMarshallingMethodThunk
    {
        protected internal override int ClassCode => 1018037605;

        protected internal override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            var otherMethod = (DelegateMarshallingMethodThunk)other;
            int result = (int)Kind - (int)otherMethod.Kind;
            if (result != 0)
                return result;

            return comparer.Compare(_delegateType, otherMethod._delegateType);
        }
    }
}
