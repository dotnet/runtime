// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem.Interop
{
    // Functionality related to deterministic ordering of methods
    partial class PInvokeDelegateWrapperConstructor
    {
        protected internal override int ClassCode => 1000342011;

        protected internal override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            var owningType = (PInvokeDelegateWrapper)OwningType;
            var otherOwningType = (PInvokeDelegateWrapper)other.OwningType;
            return comparer.Compare(owningType.DelegateType, otherOwningType.DelegateType);
        }
    }
}
