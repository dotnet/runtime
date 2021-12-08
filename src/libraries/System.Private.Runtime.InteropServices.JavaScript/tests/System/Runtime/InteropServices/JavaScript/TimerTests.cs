// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    [Trait("Category", "Pavel")]
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
            yield return new object[] { new[] { 10 }, 1 , null, null };
            yield return new object[] { new[] { 10, 5 }, 2 , null, null };
            yield return new object[] { new[] { 10, 20 }, 1 , null, null };
            yield return new object[] { new[] { 800, 600, 400, 200, 050 }, 5, 13, 9 };
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public async Task TestTimers(int[] timeouts, int? expectedSetCounter, int? expectedSetCounterAfterCleanUp, int? expectedHitCount)
        {
            int wasCalled = 0;
            Timer[] timers = new Timer[timeouts.Length];
            try
            {
                _log.Call(null, $"Waiting for runtime to settle");
                await Task.Delay(2000);
                _installWrapper.Call();
                _log.Call(null, $"Ready!");

                for (int i = 0; i < timeouts.Length; i++)
                {
                    int index = i;
                    _log.Call(null, $"Registering {index} delay {timeouts[i]}");
                    timers[i] = new Timer((_) =>
                    {
                        _log.Call(null, $"In timer{index}");
                        wasCalled++;
                    }, null, timeouts[i], 0);
                }

                var setCounter = (int)_getRegisterCount.Call();
                Assert.True(0 == wasCalled, $"wasCalled: {wasCalled}");
                Assert.True((expectedSetCounter ?? timeouts.Length) == setCounter, $"setCounter: actual {setCounter} expected {expectedSetCounter}");

            }
            finally
            {
                await WaitForCleanup();
                Assert.True(timeouts.Length == wasCalled, $"wasCalled: actual {wasCalled} expected {timeouts.Length}");

                if (expectedSetCounterAfterCleanUp != null)
                {
                    var setCounter = (int)_getRegisterCount.Call();
                    Assert.True(expectedSetCounterAfterCleanUp.Value == setCounter, $"setCounter: actual {setCounter} expected {expectedSetCounterAfterCleanUp.Value}");
                }

                if (expectedHitCount != null)
                {
                    var hitCounter = (int)_getHitCount.Call();
                    Assert.True(expectedHitCount == hitCounter, $"hitCounter: actual {hitCounter} expected {expectedHitCount}");
                }

                for (int i = 0; i < timeouts.Length; i++)
                {
                    timers[i].Dispose();
                }
            }
        }

        private async Task WaitForCleanup()
        {
            _log.Call(null, "wait for cleanup begin");
            await Task.Delay(1200);
            _cleanupWrapper.Call();
            _log.Call(null, "wait for cleanup end");
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
                await (Task)helper.Call();

                _timersHelper = (JSObject)Runtime.GetGlobalObject("timersHelper");
                _installWrapper = (Function)_timersHelper.GetObjectProperty("install");
                _getRegisterCount = (Function)_timersHelper.GetObjectProperty("getRegisterCount");
                _getHitCount = (Function)_timersHelper.GetObjectProperty("getHitCount");
                _cleanupWrapper = (Function)_timersHelper.GetObjectProperty("cleanup");
                var console = (JSObject)Runtime.GetGlobalObject("console");
                _log = (Function)console.GetObjectProperty("log");
            }
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }
}
