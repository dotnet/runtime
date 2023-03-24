// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Functionality related to deterministic ordering of types and members
    public partial class FieldForInstantiatedType
    {
        protected internal override int ClassCode => 1140200283;

        protected internal override int CompareToImpl(FieldDesc other, TypeSystemComparer comparer)
        {
            var otherField = (FieldForInstantiatedType)other;

            int result = comparer.CompareWithinClass(_instantiatedType, otherField._instantiatedType);
            if (result != 0)
                return result;

            return comparer.Compare(_fieldDef, otherField._fieldDef);
        }
    }
}
