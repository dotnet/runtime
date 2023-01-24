// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    // Functionality related to deterministic ordering of types
    internal partial class AssemblyGetExecutingAssemblyMethodThunk
    {
        protected override int ClassCode => 1459986716;

        protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            var otherMethod = (AssemblyGetExecutingAssemblyMethodThunk)other;
            return StringComparer.Ordinal.Compare(ExecutingAssembly.GetName().Name, otherMethod.ExecutingAssembly.GetName().Name);
        }
    }
}
