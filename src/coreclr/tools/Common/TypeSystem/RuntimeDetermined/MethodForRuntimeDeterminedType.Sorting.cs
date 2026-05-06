// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Functionality related to deterministic ordering of types
    public partial class MethodForRuntimeDeterminedType
    {
        protected internal override int ClassCode => 719937490;

        protected internal override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            var otherMethod = (MethodForRuntimeDeterminedType)other;

            int result = comparer.CompareWithinClass(_rdType, otherMethod._rdType);
            if (result != 0)
                return result;

            return comparer.Compare(_typicalMethodDef, otherMethod._typicalMethodDef);
        }
    }
}
