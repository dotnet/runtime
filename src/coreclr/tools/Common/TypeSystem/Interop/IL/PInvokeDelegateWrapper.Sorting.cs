// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem.Interop
{
    // Functionality related to determinstic ordering of types
    partial class PInvokeDelegateWrapper
    {
        protected override int ClassCode => -262930217;

        protected override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer)
        {
            return comparer.Compare(DelegateType, ((PInvokeDelegateWrapper)other).DelegateType);
        }
    }
}
