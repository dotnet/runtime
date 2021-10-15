using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System;

namespace Mono.Linker.Tests.Cases.Basic
{
    [Kept]
    class Finalizer
    {
        static void Main()
        {
            MentionUnallocatedType(null);
            new AllocatedTypeWithFinalizer();
        }

        [Kept]
        static void MentionUnallocatedType(UnallocatedTypeWithFinalizer u) { }

        [Kept]
        class UnallocatedTypeWithFinalizer
        {
            // Not kept
            ~UnallocatedTypeWithFinalizer() { }
        }

        [Kept]
        [KeptMember(".ctor()")]
        class AllocatedTypeWithFinalizer
        {
            [Kept]
            ~AllocatedTypeWithFinalizer() { }
        }
    }
}
