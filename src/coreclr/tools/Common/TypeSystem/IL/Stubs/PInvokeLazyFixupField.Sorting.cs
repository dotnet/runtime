// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    partial class PInvokeLazyFixupField
    {
        protected override int ClassCode => -1784477702;

        protected override int CompareToImpl(FieldDesc other, TypeSystemComparer comparer)
        {
            return comparer.Compare(_targetMethod, ((PInvokeLazyFixupField)other)._targetMethod);
        }
    }
}
