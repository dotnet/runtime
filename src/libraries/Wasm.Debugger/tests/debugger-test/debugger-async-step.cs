// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;

namespace DebuggerTests
{
    public class AsyncStepClass
    {
        static HttpClient client = new HttpClient();
        public static async Task TestAsyncStepOut()
        {
            await TestAsyncStepOut2("foobar");
        }

        public static async Task<int> TestAsyncStepOut2(string some)
        {
            var resp = await client.GetAsync("http://localhost:9400/debugger-driver.html");
            Console.WriteLine($"resp: {resp}"); /// BP at this line

            return 10;
        }

        public static void SimpleMethod()
        {
            var dt = new DateTime(4512, 1, 3, 5, 7, 9);
            OtherMethod0(dt.AddMinutes(10));
            Console.WriteLine ($"Back");
        }

        static void OtherMethod0(DateTime dt)
        {
            Console.WriteLine ($"dt: {dt}");
            OtherMethod1();
        }

        static void OtherMethod1()
        {
            Console.WriteLine ($"In OtherMethod1");
        }

        public static async Task StepOverTestAsync()
        {
            await MethodWithTwoAwaitsAsync();
            Console.WriteLine ($"StepOverTestAsync: done");
        }

        public static async Task MethodWithTwoAwaitsAsync()
        {
            Console.WriteLine ($"first await");
            await Task.Delay(50);
            Console.WriteLine ($"second await");
            await Task.Delay(50);
            Console.WriteLine ($"done");
        }
    }
}
