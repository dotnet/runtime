// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[module: KeptAttributeAttribute(typeof(System.Security.UnverifiableCodeAttribute))]

namespace Mono.Linker.Tests.Cases.Basic
{
    [SetupCompileArgument("/unsafe")]
    [SkipILVerify] // ILVerify doesn't handle calli
    [KeptMember(".cctor()")]
    public unsafe class Calli
    {
        [Kept]
        private static readonly delegate*<object, void> _pfn = null;

        public static void Main()
        {
            CallCalli(null);
        }

        [Kept]
        static void CallCalli(object o)
        {
            _pfn(o);
        }
    }
}
