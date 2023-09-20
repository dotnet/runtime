// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;
using TestUtilities;
using Xunit.Abstractions;

namespace System.Net.Test.Common
{
    public static class HttpDebug
    {
        private sealed class State(ITestOutputHelper output)
        {
            private int _logCounter;

            public void WriteLine(string message)
            {
                // Avoid overwhelming the logs on noisy tests
                if (Interlocked.Increment(ref _logCounter) < 10_000)
                {
                    output.WriteLine(message);
                }
            }
        }

        private static readonly Lazy<TestEventListener> s_eventListener = new(
            () => new TestEventListener(WriteLine, TestEventListener.NetworkingEvents),
            LazyThreadSafetyMode.ExecutionAndPublication);

        private static readonly AsyncLocal<State?> s_scope = new();

        private static State? Scope => s_scope.Value;

        public static void DebugThisTest(ITestOutputHelper output)
        {
            s_scope.Value = new State(output);
            _ = s_eventListener.Value;
        }

        public static void WriteLine(string message) =>
            Scope?.WriteLine(message);

        public static void Log(string message, [CallerMemberName] string? memberName = null, [CallerLineNumber] int lineNumber = 0) =>
            Scope?.WriteLine($"[{memberName} #{lineNumber}] {message}");
    }
}
