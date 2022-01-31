// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    // Functionality related to deterministic ordering of types
    partial class EnumGetHashCodeThunk
    {
        protected override int ClassCode => 261739662;

        protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            var otherMethod = (EnumGetHashCodeThunk)other;
            return comparer.Compare(_owningType, otherMethod._owningType);
        }
    }

    partial class EnumEqualsThunk
    {
        protected override int ClassCode => -1774524780;

        protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            var otherMethod = (EnumEqualsThunk)other;
            return comparer.Compare(_owningType, otherMethod._owningType);
        }
    }
}
