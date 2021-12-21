// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    // Functionality related to deterministic ordering of types
    partial class ForwardDelegateCreationThunk
    {
        protected internal override int ClassCode => 1026039617;

        protected internal override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            var otherMethod = (ForwardDelegateCreationThunk)other;

            return comparer.Compare(DelegateType, otherMethod.DelegateType);
        }
    }
}
