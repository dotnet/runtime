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
        static JSObject _timersHelper;
        static Function _installWrapper;
        static Function _getRegisterCount;
        static Function _getHitCount;
        static Function _cleanupWrapper;
        static Function _log;

        public static IEnumerable<object[]> TestCases()
        {
            yield return new object[] { new int[0], 0, null, null };
            yield return new object[] { new[] { 10 }, 1, null, null };
            yield return new object[] { new[] { 10, 5 }, 2, null, null };
            yield return new object[] { new[] { 10, 20 }, 1, null, null };
            yield return new object[] { new[] { 800, 600, 400, 200, 100 }, 5, 13, 9 };
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public async Task TestTimers(int[] timeouts, int? expectedSetCounter, int? expectedSetCounterAfterCleanUp, int? expectedHitCount)
        {
            int wasCalled = 0;
            Timer[] timers = new Timer[timeouts.Length];
            try
            {
                _log.Call(_timersHelper, $"Waiting for runtime to settle");
                // the test is quite sensitive to timing and order of execution. Here we are giving time to timers of XHarness and previous tests to finish.
                await Task.Delay(2000);
                _installWrapper.Call(_timersHelper);
                _log.Call(_timersHelper, $"Ready!");

                for (int i = 0; i < timeouts.Length; i++)
                {
                    int index = i;
                    _log.Call(_timersHelper, $"Registering {index} delay {timeouts[i]}");
                    timers[i] = new Timer((_) =>
                    {
                        _log.Call(_timersHelper, $"In timer{index}");
                        wasCalled++;
                    }, null, timeouts[i], 0);
                }

                var setCounter = (int)_getRegisterCount.Call(_timersHelper);
                Assert.True(0 == wasCalled, $"wasCalled: {wasCalled}");
                Assert.True((expectedSetCounter ?? timeouts.Length) == setCounter, $"setCounter: actual {setCounter} expected {expectedSetCounter}");

            }
            finally
            {
                // the test is quite sensitive to timing and order of execution. 
                // Here we are giving time to our timers to finish.
                var afterLastTimer = timeouts.Length == 0 ? 500 : 500 + timeouts.Max();

                _log.Call(_timersHelper, "wait for timers to run");
                // this delay is also implemented as timer, so it counts to asserts
                await Task.Delay(afterLastTimer);
                _log.Call(_timersHelper, "cleanup");
                _cleanupWrapper.Call(_timersHelper);

                Assert.True(timeouts.Length == wasCalled, $"wasCalled: actual {wasCalled} expected {timeouts.Length}");

                if (expectedSetCounterAfterCleanUp != null)
                {
                    var setCounter = (int)_getRegisterCount.Call(_timersHelper);
                    Assert.True(expectedSetCounterAfterCleanUp.Value == setCounter, $"setCounter: actual {setCounter} expected {expectedSetCounterAfterCleanUp.Value}");
                }

                if (expectedHitCount != null)
                {
                    var hitCounter = (int)_getHitCount.Call(_timersHelper);
                    Assert.True(expectedHitCount == hitCounter, $"hitCounter: actual {hitCounter} expected {expectedHitCount}");
                }

                for (int i = 0; i < timeouts.Length; i++)
                {
                    timers[i].Dispose();
                }
            }
        }

        public async Task InitializeAsync()
        {
            if (_timersHelper == null)
            {
                Function helper = new Function(@"
                    const loadTimersJs = async () => {
                        await import('./timers.js');
                    };
                    return loadTimersJs();
                ");
                await (Task)helper.Call(_timersHelper);

                _timersHelper = (JSObject)Runtime.GetGlobalObject("timersHelper");
                _installWrapper = (Function)_timersHelper.GetObjectProperty("install");
                _getRegisterCount = (Function)_timersHelper.GetObjectProperty("getRegisterCount");
                _getHitCount = (Function)_timersHelper.GetObjectProperty("getHitCount");
                _cleanupWrapper = (Function)_timersHelper.GetObjectProperty("cleanup");
                _log = (Function)_timersHelper.GetObjectProperty("log");
            }
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }
}
