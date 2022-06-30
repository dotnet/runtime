using System.Collections.Concurrent;

namespace Microsoft.NET.Test.Runner
{
    public static class TestExtensions
    {
        private static readonly ConcurrentBag<Func<string, Task<string>>> s_runnerToExtensionCallbacks = new();

        internal static ConcurrentBag<Func<string, Task<string>>> RunnerToExtensionCallbacks { get; } = s_runnerToExtensionCallbacks;

        public static void RegisterRunnerToExtensionCallback(Func<string, Task<string>> callback)
            => s_runnerToExtensionCallbacks.Add(callback);
    }
}
