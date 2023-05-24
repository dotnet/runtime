// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Sample
{
    public partial class Test
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            return 0;
        }

        [JSExport]
        public static async Task TestCanStartThread()
        {
            Console.WriteLine($"smoke: TestCanStartThread 1 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
            var tcs = new TaskCompletionSource<int>();
            var t = new Thread(() =>
            {
                Console.WriteLine($"smoke: TestCanStartThread 2 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
                var childTid = Thread.CurrentThread.ManagedThreadId;
                tcs.SetResult(childTid);
                Console.WriteLine($"smoke: TestCanStartThread 3 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
            });
            t.Start();
            Console.WriteLine($"smoke: TestCanStartThread 4 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
            var childTid = await tcs.Task;
            Console.WriteLine($"smoke: TestCanStartThread 5 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
            t.Join();
            Console.WriteLine($"smoke: TestCanStartThread 6 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
            if (childTid == Thread.CurrentThread.ManagedThreadId)
                throw new Exception("Child thread ran on same thread as parent");
        }

        [JSImport("globalThis.setTimeout")]
        static partial void GlobalThisSetTimeout([JSMarshalAs<JSType.Function>] Action cb, int timeoutMs);

        [JSImport("globalThis.fetch")]
        private static partial Task<JSObject> GlobalThisFetch(string url);

        [JSExport]
        public static async Task TestCallSetTimeoutOnWorker()
        {
            var t = Task.Run(TimeOutThenComplete);
            await t;
            Console.WriteLine ($"XYZ: Main Thread caught task tid:{Thread.CurrentThread.ManagedThreadId}");
        }

        const string fetchhelper = "./fetchelper.js";

        [JSImport("responseText", fetchhelper)]
        private static partial Task<string> FetchHelperResponseText(JSObject response, int delayMs);

        [JSExport]
        public static async Task<string> FetchBackground(string url)
        {
            Console.WriteLine($"smoke: FetchBackground 1 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
            var t = Task.Run(async () =>
            {
                Console.WriteLine($"smoke: FetchBackground 2 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
                var x=JSHost.ImportAsync(fetchhelper, "./fetchhelper.js");
                Console.WriteLine($"smoke: FetchBackground 3A ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
                using var import = await x;
                Console.WriteLine($"smoke: FetchBackground 3B ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
                var r = await GlobalThisFetch(url);
                Console.WriteLine($"smoke: FetchBackground 4 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
                var ok = (bool)r.GetPropertyAsBoolean("ok");

                Console.WriteLine($"smoke: FetchBackground 5 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
                if (ok)
                {
                    #if DEBUG
                    var text = await FetchHelperResponseText(r, 5000);
                    #else
                    var text = await FetchHelperResponseText(r, 25000);
                    #endif
                    Console.WriteLine($"smoke: FetchBackground 6 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
                    return text;
                }
                Console.WriteLine($"smoke: FetchBackground 7 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
                return "not-ok";
            });
            var r = await t;
            Console.WriteLine($"XYZ: FetchBackground thread:{Thread.CurrentThread.ManagedThreadId} background thread returned");
            return r;
        }

        private static async Task TimeOutThenComplete()
        {
            var tcs = new TaskCompletionSource();
            Console.WriteLine ($"XYZ: Task running tid:{Thread.CurrentThread.ManagedThreadId}");
            GlobalThisSetTimeout(() => {
                tcs.SetResult();
                Console.WriteLine ($"XYZ: Timeout fired tid:{Thread.CurrentThread.ManagedThreadId}");
            }, 250);
            Console.WriteLine ($"XYZ: Task sleeping tid:{Thread.CurrentThread.ManagedThreadId}");
            await tcs.Task;
            Console.WriteLine ($"XYZ: Task resumed tid:{Thread.CurrentThread.ManagedThreadId}");
        }

        [JSExport]
        public static async Task<int> RunBackgroundThreadCompute()
        {
            var tcs = new TaskCompletionSource<int>();
            var t = new Thread(() => {
                var n = CountingCollatzTest();
                tcs.SetResult(n);
            });
            t.Start();
            return await tcs.Task;
        }

        [JSExport]
        public static async Task<int> RunBackgroundLongRunningTaskCompute()
        {
            var factory = new TaskFactory();
            var t = factory.StartNew<int> (() => {
                var n = CountingCollatzTest();
                return n;
            }, TaskCreationOptions.LongRunning);
            return await t;
        }

        [JSExport]
        public static async Task<int> RunBackgroundTaskRunCompute()
        {
            var t1 = Task.Run (() => {
                var n = CountingCollatzTest();
                return n;
            });
            var t2 = Task.Run (() => {
                var n = CountingCollatzTest();
                return n;
            });
            var rs = await Task.WhenAll (new [] { t1, t2 });
            if (rs[0] != rs[1])
                throw new Exception ($"Results from two tasks {rs[0]}, {rs[1]}, differ");
            return rs[0];
        }

        public static int CountingCollatzTest()
        {
            const int limit = 5000;
            const int maxInput = 200_000;
            int bigly = 0;
            int hugely = 0;
            int maxSteps = 0;
            for (int n = 1; n < maxInput; n++) {
                int steps = CountingCollatz ((long)n, limit);
                if (steps > maxSteps)
                    maxSteps = steps;
                if (steps > 120)
                    bigly++;
                if (steps >= limit)
                    hugely++;
            }

            Console.WriteLine ($"Bigly: {bigly}, Hugely: {hugely}, maxSteps: {maxSteps}");

            if (bigly == 86187 && hugely == 0 && maxSteps == 382)
                return 524;
            else
                return 0;
        }


        private static int CountingCollatz (long n, int limit)
        {
            int steps = 0;
            while (n > 1) {
                n = Collatz1 (n);
                steps++;
                if (steps >= limit)
                    break;
            }
            return steps;
        }

        private static long Collatz1 (long n)
        {
            if (n <= 0)
                throw new Exception("Unexpected non-positive input");
            if (n % 2 == 0)
                return n / 2;
            else
                return 3 * n + 1;
        }
    }
}
