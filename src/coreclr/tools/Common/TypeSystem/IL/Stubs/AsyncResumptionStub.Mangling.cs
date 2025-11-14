// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.IL.Stubs;
using Internal.TypeSystem;

namespace ILCompiler
{
    public partial class AsyncResumptionStub : ILStubMethod, IPrefixMangledMethod
    {
        MethodDesc IPrefixMangledMethod.BaseMethod => _owningMethod;

        string IPrefixMangledMethod.Prefix => "Resume";
    }
}
