using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
    [SetupCompileArgument("/optimize+")]
    [SetupLinkerArgument("--enable-opt", "ipconstprop")]
    public class NestedFinallyInFinallyHandler
    {
        public static void Main()
        {
            Test();
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
                    try
                    {
                        Cleanup();
                    }
                    finally
                    {
                        NestedCleanup();
                    }
                }
            }
        }

        static bool AlwaysFalse => false;

        static void Unreachable() { }

        [Kept]
        static void Reached() { }

        [Kept]
        static void Cleanup() { }

        [Kept]
        static void NestedCleanup() { }
    }
}
