using System.Threading.Tasks;

namespace Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies
{
    public static class NestedFinallyInAsyncMethod_Lib
    {
        public static int CleanupCount { get; private set; }

        public static async Task Test()
        {
            try
            {
                await Task.Yield();
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

        static void Reached() { }

        static void Cleanup() => CleanupCount++;
    }
}
