// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Functionality related to deterministic ordering of types
    public partial class RuntimeDeterminedType
    {
        protected internal override int ClassCode => 351938209;

        protected internal sealed override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer)
        {
            var otherType = (RuntimeDeterminedType)other;
            int result = comparer.Compare(_rawCanonType, otherType._rawCanonType);
            if (result != 0)
                return result;

            return comparer.Compare(_runtimeDeterminedDetailsType, otherType._runtimeDeterminedDetailsType);
        }
    }
}
