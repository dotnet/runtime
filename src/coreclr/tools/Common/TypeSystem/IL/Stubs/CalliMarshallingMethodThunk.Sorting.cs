// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    // Functionality related to deterministic ordering of methods
    public partial class CalliMarshallingMethodThunk
    {
        protected override int ClassCode => 1594107963;

        protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            var otherMethod = (CalliMarshallingMethodThunk)other;
            int result = RuntimeMarshallingEnabled.CompareTo(otherMethod.RuntimeMarshallingEnabled);
            if (result != 0)
            {
                return result;
            }
            return comparer.Compare(_targetSignature, otherMethod._targetSignature);
        }
    }
}
