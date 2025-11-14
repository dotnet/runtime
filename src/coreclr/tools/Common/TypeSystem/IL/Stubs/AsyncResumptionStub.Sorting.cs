// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.IL.Stubs;
using Internal.TypeSystem;

namespace ILCompiler
{
    public partial class AsyncResumptionStub : ILStubMethod
    {
        protected override int ClassCode => 0x773ab1;

        protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            return comparer.Compare(_owningMethod, ((AsyncResumptionStub)other)._owningMethod);
        }
    }
}
