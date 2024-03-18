// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    public sealed partial class ImpliedRepeatedFieldDesc : FieldDesc
    {
        protected internal override int CompareToImpl(FieldDesc other, TypeSystemComparer comparer)
        {
            var impliedRepeatedFieldDesc = (ImpliedRepeatedFieldDesc)other;

            int result = comparer.Compare(_underlyingFieldDesc, impliedRepeatedFieldDesc._underlyingFieldDesc);

            if (result != 0)
            {
                return result;
            }

            return FieldIndex.CompareTo(impliedRepeatedFieldDesc.FieldIndex);
        }

        protected internal override int ClassCode => 31666958;
    }
}
