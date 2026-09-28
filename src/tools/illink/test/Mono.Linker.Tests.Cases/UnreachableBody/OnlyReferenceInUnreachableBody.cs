// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.UnreachableBody.Dependencies;

namespace Mono.Linker.Tests.Cases.UnreachableBody;

/// <summary>
/// This test was added to fix an issue with how SweepStep.SweepAssemblyReferences and CodeRewriterStep interact.
/// CodeRewriterStep must run before SweepStep to ensure that SweepStep.SweepAssemblyReferences doesn't see TypeReferences in method bodies that
/// are going to be removed by CodeRewriterStep.
/// </summary>
[SetupCompileBefore("other.dll", new[] { typeof(OtherAssembly2) })]
[RemovedAssemblyReference("test", "other")]
[RemovedAssembly("other.dll")]
[SetupLinkerArgument("--enable-opt", "unreachablebodies")]
public class OnlyReferenceInUnreachableBody
{
    public static void Main()
    {
        UsedToMarkMethod(null);
    }

    [Kept]
    static void UsedToMarkMethod(Foo f)
    {
        f.Method();
    }

    [Kept]
    class Foo
    {
        [Kept]
        [ExpectBodyModified]
        public void Method()
        {
            OtherAssembly2.Field = 1;
        }
    }
}
