using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
    [SetupCompileArgument("/optimize+")]
    [SetupLinkerArgument("--enable-opt", "ipconstprop")]
    public class NestedFinallyInFinallyHandler
    {
        [Kept]
        static int CleanupCount;

        public static void Main()
        {
            Test();

            if (CleanupCount != 1)
                throw new InvalidOperationException();
        }

        [Kept]
        [ExpectBodyModified]
        static void Test()
        {
            try
            {
                if (AlwaysFalse)
                    Unreachable();
            }
            finally
            {
                try
                {
                    Reached();
                }
                finally
                {
                    Cleanup();
                }
            }
        }

        static bool AlwaysFalse => false;

        static void Unreachable() { }

        [Kept]
        static void Reached() { }

        [Kept]
        static void Cleanup() => CleanupCount++;
    }
}
