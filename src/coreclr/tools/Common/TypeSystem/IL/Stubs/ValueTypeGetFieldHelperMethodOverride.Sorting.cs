// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    partial class ValueTypeGetFieldHelperMethodOverride
    {
        protected override int ClassCode => 2036839816;

        protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            var otherMethod = (ValueTypeGetFieldHelperMethodOverride)other;

            return comparer.Compare(_owningType, otherMethod._owningType);
        }
    }
}
