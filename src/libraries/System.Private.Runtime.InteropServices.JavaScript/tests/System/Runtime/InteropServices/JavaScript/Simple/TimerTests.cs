// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public static class TimerTests
    {
        static Function _wrapJsSetTimer = new Function(@"
            globalThis.setCounter = 0;
            globalThis.hitCounter = 0;
            console.log(`install`)
            if(!globalThis.originalSetTimeout){
                globalThis.originalSetTimeout = globalThis.setTimeout;
            }
            globalThis.setTimeout = (cb,time) => {
                var start=Date.now().valueOf();
                if(cb.name==='mono_wasm_set_timeout_exec_'){
                    globalThis.setCounter++;
                    console.log(`setCounter: ${globalThis.setCounter} now:${start} delay:${time} cb:${cb.name}`)
                }
                return globalThis.originalSetTimeout(()=>{
                    if(cb.name==='mono_wasm_set_timeout_exec_'){
                        var hit=Date.now().valueOf();
                        globalThis.hitCounter++;
                        var delta = hit-start;
                        console.log(`hitCounter: ${globalThis.hitCounter} now:${hit} delay:${time} delta:${delta} cb:${cb.name}`)
                    }
                    cb();
                }, time);
            };
            ");

        static Function _getSetCount = new Function(@"
            console.log(`setCounter: ${globalThis.setCounter}`)
            return globalThis.setCounter;
            ");

        static Function _getHitCount = new Function(@"
            console.log(`hitCounter: ${globalThis.hitCounter}`)
            return globalThis.hitCounter;
            ");

        static Function _resetCallCount = new Function(@"
            console.log(`reset: ${globalThis.setCounter}`)
            globalThis.setCounter=0;
            ");

        static Function _cleanupJsSetTimer = new Function(@"
            console.log(`cleanup setTimeout, setCounter: ${globalThis.setCounter} hitCounter: ${globalThis.hitCounter}`)
            //globalThis.setTimeout = globalThis.originalSetTimeout;
            ");


        static public async Task T0_NoTimer()
        {
            try
            {
                _wrapJsSetTimer.Call();

                var setCounter = (int)_getSetCount.Call();
                Assert.Equal(0, setCounter);
            }
            finally
            {
                await WaitForCleanup();
            }
        }

        static public async Task T1_OneTimer()
        {
            int wasCalled = 0;
            Timer? timer = null;
            try
            {
                _wrapJsSetTimer.Call();

                timer = new Timer((_) =>
                {
                    Console.WriteLine("In timer");
                    wasCalled++;
                }, null, 10, 0);

                var setCounter = (int)_getSetCount.Call();
                Assert.True(0 == wasCalled, $"wasCalled: {wasCalled}");
                Assert.True(1 == setCounter, $"setCounter: {setCounter}");
            }
            finally
            {
                await WaitForCleanup();
                Assert.True(1 == wasCalled, $"wasCalled: {wasCalled}");
                timer?.Dispose();
            }
        }

        static public async Task T2_SecondTimerEarlier()
        {
            int wasCalled = 0;
            Timer? timer1 = null;
            Timer? timer2 = null;
            try
            {
                _wrapJsSetTimer.Call();

                timer1 = new Timer((_) =>
                {
                    Console.WriteLine("In timer1");
                    wasCalled++;
                }, null, 10, 0);
                timer2 = new Timer((_) =>
                {
                    Console.WriteLine("In timer2");
                    wasCalled++;
                }, null, 5, 0);

                var setCounter = (int)_getSetCount.Call();
                Assert.True(2 == setCounter, $"setCounter: {setCounter}");
                Assert.True(0 == wasCalled, $"wasCalled: {wasCalled}");

            }
            finally
            {
                await WaitForCleanup();
                Assert.True(2 == wasCalled, $"wasCalled: {wasCalled}");
                timer1?.Dispose();
                timer2?.Dispose();
            }
        }

        static public async Task T3_SecondTimerLater()
        {
            int wasCalled = 0;
            Timer? timer1 = null;
            Timer? timer2 = null;
            try
            {
                _wrapJsSetTimer.Call();

                timer1 = new Timer((_) =>
                {
                    Console.WriteLine("In timer1");
                    wasCalled++;
                }, null, 10, 0);
                timer2 = new Timer((_) =>
                {
                    Console.WriteLine("In timer2");
                    wasCalled++;
                }, null, 20, 0);

                var setCounter = (int)_getSetCount.Call();
                Assert.True(0 == wasCalled, $"wasCalled: {wasCalled}");
                Assert.True(1 == setCounter, $"setCounter: {setCounter}");
            }
            finally
            {
                await WaitForCleanup();
                Assert.True(2 == wasCalled, $"wasCalled: {wasCalled}");
                timer1?.Dispose();
                timer2?.Dispose();
            }
        }

        static public async Task T5_FiveTimers()
        {
            int wasCalled = 0;
            Timer? timer1 = null;
            Timer? timer2 = null;
            Timer? timer3 = null;
            Timer? timer4 = null;
            Timer? timer5 = null;
            try
            {
                _wrapJsSetTimer.Call();

                timer1 = new Timer((_) =>
                {
                    Console.WriteLine("In timer1");
                    wasCalled++;
                }, null, 800, 0);
                timer2 = new Timer((_) =>
                {
                    Console.WriteLine("In timer2");
                    wasCalled++;
                }, null, 600, 0);
                timer3 = new Timer((_) =>
                {
                    Console.WriteLine("In timer3");
                    wasCalled++;
                }, null, 400, 0);
                timer4 = new Timer((_) =>
                {
                    Console.WriteLine("In timer4");
                    wasCalled++;
                }, null, 200, 0);
                timer5 = new Timer((_) =>
                {
                    Console.WriteLine("In timer5");
                    wasCalled++;
                }, null, 000, 0);

                var setCounter = (int)_getSetCount.Call();
                Assert.True(0 == wasCalled, $"wasCalled: {wasCalled}");
                Assert.True(5 == setCounter, $"setCounter: {setCounter}");
            }
            finally
            {
                await WaitForCleanup();
                var hitCounter = (int)_getHitCount.Call();
                var setCounter = (int)_getSetCount.Call();
                Assert.True(5 == wasCalled, $"wasCalled: {wasCalled}");
                Assert.True(8 == hitCounter, $"hitCounter: {hitCounter}");
                Assert.True(12 == setCounter, $"hitCounter: {hitCounter}");
                timer1?.Dispose();
                timer2?.Dispose();
                timer3?.Dispose();
                timer4?.Dispose();
                timer5?.Dispose();
            }
        }

        static private async Task WaitForCleanup()
        {
            Console.WriteLine("WaitForCleanup begin");
            await Task.Delay(1000);
            _cleanupJsSetTimer.Call();
            Console.WriteLine("WaitForCleanup end");
        }
    }
}
