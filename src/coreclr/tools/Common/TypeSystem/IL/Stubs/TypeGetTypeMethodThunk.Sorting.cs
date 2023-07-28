// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    // Functionality related to deterministic ordering of types
    internal partial class TypeGetTypeMethodThunk
    {
        protected override int ClassCode => -949164050;

        protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            var otherMethod = (TypeGetTypeMethodThunk)other;
            int result = StringComparer.Ordinal.Compare(DefaultAssemblyName, otherMethod.DefaultAssemblyName);
            if (result != 0)
                return result;

            return comparer.Compare(Signature, otherMethod.Signature);
        }
    }
}
