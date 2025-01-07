// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    // V8's implementation of setTimer ignores delay parameter and always run immediately. So it could not be used to test this.
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsBrowserDomSupported))]
    public class TimerTests : IAsyncLifetime
    {
        public static IEnumerable<object[]> TestCases()
        {
            yield return new object[] { new int[0], 0, null, null };
            yield return new object[] { new[] { 10 }, 1, null, null };
            yield return new object[] { new[] { 10, 5 }, 2, null, null };
            yield return new object[] { new[] { 10, 20 }, 1, null, null };
            yield return new object[] { new[] { 800, 600, 400, 200, 100 }, 5, 13, 9 };
        }

        [MemberData(nameof(TestCases))]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWasmThreadingSupported))] // this test only makes sense with ST TimerQueue
        public async Task TestTimers(int[] timeouts, int? expectedSetCounter, int? expectedSetCounterAfterCleanUp, int? expectedHitCount)
        {
            int wasCalled = 0;
            Timer[] timers = new Timer[timeouts.Length];
            try
            {
                TimersJS.Log($"Waiting for runtime to settle");
                // the test is quite sensitive to timing and order of execution. Here we are giving time to timers of XHarness and previous tests to finish.
                await Task.Delay(2000);
                TimersJS.Install();
                TimersJS.Log($"Ready!");

                for (int i = 0; i < timeouts.Length; i++)
                {
                    int index = i;
                    TimersJS.Log($"Registering {index} delay {timeouts[i]}");
                    timers[i] = new Timer((_) =>
                    {
                        TimersJS.Log($"In timer{index}");
                        wasCalled++;
                    }, null, timeouts[i], 0);
                }

                var setCounter = TimersJS.GetRegisterCount();
                Assert.True(0 == wasCalled, $"wasCalled: {wasCalled}");
                Assert.True((expectedSetCounter ?? timeouts.Length) == setCounter, $"setCounter: actual {setCounter} expected {expectedSetCounter}");

            }
            finally
            {
                // the test is quite sensitive to timing and order of execution. 
                // Here we are giving time to our timers to finish.
                var afterLastTimer = timeouts.Length == 0 ? 500 : 500 + timeouts.Max();

                TimersJS.Log("wait for timers to run");
                // this delay is also implemented as timer, so it counts to asserts
                await Task.Delay(afterLastTimer);
                TimersJS.Log("cleanup");
                TimersJS.Cleanup();

                Assert.True(timeouts.Length == wasCalled, $"wasCalled: actual {wasCalled} expected {timeouts.Length}");

                if (expectedSetCounterAfterCleanUp != null)
                {
                    var setCounter = TimersJS.GetRegisterCount();
                    Assert.True(expectedSetCounterAfterCleanUp.Value == setCounter, $"setCounter: actual {setCounter} expected {expectedSetCounterAfterCleanUp.Value}");
                }

                if (expectedHitCount != null)
                {
                    var hitCounter = TimersJS.GetHitCount();
                    Assert.True(expectedHitCount == hitCounter, $"hitCounter: actual {hitCounter} expected {expectedHitCount}");
                }

                for (int i = 0; i < timeouts.Length; i++)
                {
                    timers[i].Dispose();
                }
            }
        }

        static JSObject _module;
        public async Task InitializeAsync()
        {
            if (_module == null)
            {
                _module = await JSHost.ImportAsync("Timers", "../timers.mjs");
            }
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }

    public static partial class TimersJS
    {
        [JSImport("log", "Timers")]
        public static partial void Log(string message);

        [JSImport("install", "Timers")]
        public static partial void Install();

        [JSImport("cleanup", "Timers")]
        public static partial void Cleanup();

        [JSImport("getRegisterCount", "Timers")]
        public static partial int GetRegisterCount();

        [JSImport("getHitCount", "Timers")]
        public static partial int GetHitCount();

    }
}
