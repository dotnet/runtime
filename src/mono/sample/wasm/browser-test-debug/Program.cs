// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript.Tests;

namespace Sample
{
    public partial class Test
    {
        public static class Assert
        {
            public static void Equal<T>(T expected, T actual)
            {
                if (!object.Equals(expected, actual))
                    throw new Exception($"AssertHelper.Equal failed. Expected: {expected}. Actual: {actual}.");
            }
        }
        public static async Task<int> Main(string[] args)
        {
            var rand = new Random();
            Console.WriteLine("Today's lucky number is " + rand.Next(100) + " and " + Guid.NewGuid());

            return 0;
        }
        
        public static async Task JsExportTaskOfInt(int value)
        {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

            var res = JavaScriptTestHelper.invoke1_TaskOfInt(tcs.Task, nameof(JavaScriptTestHelper.AwaitTaskOfObject));
            tcs.SetResult(value);
            await Task.Yield();
            var rr = await res;
            await Task.Yield();
            
            Assert.Equal(value, rr);
            //GC.Collect();
        }

        [JSExport]
        public static async Task DoTestMethod()
        {
            await JavaScriptTestHelper.InitializeAsync();
            await JsExportTaskOfInt(-2147483648);
        }
    }
}
