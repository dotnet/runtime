// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    // Functionality related to deterministic ordering of types
    public partial class PInvokeTargetNativeMethod
    {
        protected internal override int ClassCode => -1626939381;

        protected internal override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            var otherMethod = (PInvokeTargetNativeMethod)other;
            return comparer.Compare(_declMethod, otherMethod._declMethod);
        }
    }
}
