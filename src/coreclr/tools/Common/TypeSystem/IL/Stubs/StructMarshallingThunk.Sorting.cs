// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    // Functionality related to deterministic ordering of types
    public partial class StructMarshallingThunk
    {
        protected override int ClassCode => 340834018;

        protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            var otherMethod = (StructMarshallingThunk)other;

            int result = ThunkType - otherMethod.ThunkType;
            if (result != 0)
                return result;

            return comparer.Compare(ManagedType, otherMethod.ManagedType);
        }
    }
}
