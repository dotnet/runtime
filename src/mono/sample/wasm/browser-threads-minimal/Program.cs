// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.WebSockets;

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
        public static async Task LockTest()
        {
            var lck=new Object();
            Console.WriteLine("LockTest A ManagedThreadId: "+Thread.CurrentThread.ManagedThreadId);
            Monitor.Enter(lck);
            Console.WriteLine("LockTest B ManagedThreadId: "+Thread.CurrentThread.ManagedThreadId);
            Monitor.Exit(lck);
            Console.WriteLine("LockTest C ManagedThreadId: "+Thread.CurrentThread.ManagedThreadId);

            await Task.Run(() =>
            {
                Console.WriteLine("LockTest D ManagedThreadId: "+Thread.CurrentThread.ManagedThreadId);
                Monitor.Enter(lck);
                Console.WriteLine("LockTest E ManagedThreadId: "+Thread.CurrentThread.ManagedThreadId);
                Monitor.Exit(lck);
                Console.WriteLine("LockTest F ManagedThreadId: "+Thread.CurrentThread.ManagedThreadId);
            });

            /* deadlock
            Monitor.Enter(lck);
            await Task.Run(() =>
            {
                Console.WriteLine("LockTest G ManagedThreadId: "+Thread.CurrentThread.ManagedThreadId);
                Monitor.Enter(lck);
                Console.WriteLine("LockTest H ManagedThreadId: "+Thread.CurrentThread.ManagedThreadId);
                Monitor.Exit(lck);
                Console.WriteLine("LockTest I ManagedThreadId: "+Thread.CurrentThread.ManagedThreadId);
            });
            Monitor.Exit(lck);
            Console.WriteLine("LockTest J ManagedThreadId: "+Thread.CurrentThread.ManagedThreadId);
            */

            /* deadlock
            await Task.Run(() =>
            {
                Console.WriteLine("LockTest K ManagedThreadId: "+Thread.CurrentThread.ManagedThreadId);
                Monitor.Enter(lck);
            });
            Console.WriteLine("LockTest L ManagedThreadId: "+Thread.CurrentThread.ManagedThreadId);
            Monitor.Enter(lck);
            Console.WriteLine("LockTest M ManagedThreadId: "+Thread.CurrentThread.ManagedThreadId);
            */
        }


        [JSExport]
        public static async Task DisposeTest()
        {
            Console.WriteLine("DisposeTest A ManagedThreadId: "+Thread.CurrentThread.ManagedThreadId);
            var test1 = JSHost.GlobalThis.GetPropertyAsJSObject("test1");
            var test2 = JSHost.GlobalThis.GetPropertyAsJSObject("test2");
            Console.WriteLine("DisposeTest 0 ManagedThreadId: "+Thread.CurrentThread.ManagedThreadId);
            await Task.Delay(10).ConfigureAwait(false);
            Console.WriteLine("DisposeTest 1 ManagedThreadId: "+Thread.CurrentThread.ManagedThreadId);
            test1.Dispose();

            Console.WriteLine("DisposeTest 2 ManagedThreadId: "+Thread.CurrentThread.ManagedThreadId);
            Console.WriteLine("DisposeTest 3 ManagedThreadId: "+Thread.CurrentThread.ManagedThreadId);

            await Task.Run(() =>
            {
                Console.WriteLine("DisposeTest 4 ManagedThreadId: "+Thread.CurrentThread.ManagedThreadId);
                test2.Dispose();
                Console.WriteLine("DisposeTest 5 ManagedThreadId: "+Thread.CurrentThread.ManagedThreadId);
            });
        }


        [JSImport("globalThis.setTimeout")]
        static partial void GlobalThisSetTimeout([JSMarshalAs<JSType.Function>] Action cb, int timeoutMs);

        [JSImport("globalThis.fetch")]
        private static partial Task<JSObject> GlobalThisFetch(string url);

        [JSImport("globalThis.console.log")]
        private static partial void GlobalThisConsoleLog(string text);

        const string fetchhelper = "../fetchhelper.js";

        [JSImport("responseText", fetchhelper)]
        private static partial Task<string> FetchHelperResponseText(JSObject response, int delayMs);

        [JSImport("delay", fetchhelper)]
        private static partial Task Delay(int timeoutMs);

        [JSExport]
        internal static Task TestHelloWebWorker()
        {
            Console.WriteLine($"smoke: TestHelloWebWorker 1 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
            Task t = WebWorker.RunAsync(() =>
            {
                Console.WriteLine($"smoke: TestHelloWebWorker 2 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
                GlobalThisConsoleLog($"smoke: TestHelloWebWorker 3 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
                Console.WriteLine($"smoke: TestHelloWebWorker 4 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
            });
            Console.WriteLine($"smoke: TestHelloWebWorker 5 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
            return t.ContinueWith(Gogo);
        }

        private static void Gogo(Task t)
        {
            Console.WriteLine($"smoke: TestHelloWebWorker 6 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
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

        static bool _timerDone = false;

        [JSExport]
        internal static void StartTimerFromWorker()
        {
            Console.WriteLine("smoke: StartTimerFromWorker 1 utc {0}", DateTime.UtcNow.ToUniversalTime());
            WebWorker.RunAsync(async () =>
            {
                while (!_timerDone)
                {
                    await Task.Delay(1 * 1000);
                    Console.WriteLine("smoke: StartTimerFromWorker 2 utc {0}", DateTime.UtcNow.ToUniversalTime());
                }
                Console.WriteLine("smoke: StartTimerFromWorker done utc {0}", DateTime.UtcNow.ToUniversalTime());
            });
        }

        [JSExport]
        internal static void StartAllocatorFromWorker()
        {
            Console.WriteLine("smoke: StartAllocatorFromWorker 1 utc {0}", DateTime.UtcNow.ToUniversalTime());
            WebWorker.RunAsync(async () =>
            {
                while (!_timerDone)
                {
                    await Task.Delay(1 * 100);
                    var x = new List<int[]>();
                    for (int i = 0; i < 1000; i++)
                    {
                        var v = new int[1000];
                        v[i] = i;
                        x.Add(v);
                    }
                    Console.WriteLine("smoke: StartAllocatorFromWorker 2 utc {0} {1} {2}", DateTime.UtcNow.ToUniversalTime(), x[1][1], GC.GetTotalAllocatedBytes());
                }
                Console.WriteLine("smoke: StartAllocatorFromWorker done utc {0}", DateTime.UtcNow.ToUniversalTime());
            });
        }

        [JSExport]
        internal static void StopTimerFromWorker()
        {
            _timerDone = true;
        }

        [JSExport]
        public static async Task TestCallSetTimeoutOnWorker()
        {
            await WebWorker.RunAsync(() => TimeOutThenComplete());
            Console.WriteLine($"XYZ: Main Thread caught task tid:{Thread.CurrentThread.ManagedThreadId}");
        }

        private static async Task<string> HttpClientGet(string name, string url)
        {
            Console.WriteLine($"smoke: {name} 1 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
            using var client = new HttpClient();
            using var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var text = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"smoke: {name} 2 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
            return text;
        }

        [JSExport]
        public static Task<string> HttpClientMain(string url)
        {
            return HttpClientGet("HttpClientMain", url);
        }

        [JSExport]
        public static Task<string> HttpClientWorker(string url)
        {
            return WebWorker.RunAsync(() =>
            {
                return HttpClientGet("HttpClientWorker", url);
            });
        }

        [JSExport]
        public static Task<string> HttpClientPool(string url)
        {
            return Task.Run(() =>
            {
                return HttpClientGet("HttpClientPool", url);
            });
        }

        [JSExport]
        public static Task<string> HttpClientThread(string url)
        {
            var tcs = new TaskCompletionSource<string>();
            var t = new Thread(() => {
                var t = HttpClientGet("HttpClientThread", url);
                // this is blocking!
                tcs.SetResult(t.Result);
            });
            t.Start();
            return tcs.Task;
        }

        private static async Task<string> WsClientHello(string name, string url)
        {
            Console.WriteLine($"smoke: {name} 1 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
            using var client = new ClientWebSocket ();
            await client.ConnectAsync(new Uri(url), CancellationToken.None);
            var message=new byte[]{0x68,0x65,0x6C,0x6C,0x6F};// hello
            var body = new ReadOnlyMemory<byte>(message);
            await client.SendAsync(body, WebSocketMessageType.Text, true, CancellationToken.None);
            Console.WriteLine($"smoke: {name} 2 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
            return "ok";
        }

        [JSExport]
        public static Task<string> WsClientMain(string url)
        {
            return WsClientHello("WsClientHello", url);
        }

        [JSExport]
        public static async Task<string> FetchBackground(string url)
        {
            Console.WriteLine($"smoke: FetchBackground 1 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
            var t = WebWorker.RunAsync(async () =>
            {
                var ctx = SynchronizationContext.Current;

                Console.WriteLine($"smoke: FetchBackground 2 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
                var x = JSHost.ImportAsync(fetchhelper, "../fetchhelper.js");
                Console.WriteLine($"smoke: FetchBackground 3A ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
                // using var import = await x.ConfigureAwait(false);
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
            Console.WriteLine($"smoke: FetchBackground thread:{Thread.CurrentThread.ManagedThreadId} background thread returned");
            return r;
        }

        [ThreadStatic]
        public static int meaning = 42;

        [JSExport]
        public static async Task TestTLS()
        {
            Console.WriteLine($"smoke {meaning}: TestTLS 1 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
            meaning = 40;
            await WebWorker.RunAsync(async () =>
            {
                Console.WriteLine($"smoke {meaning}: TestTLS 2 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
                meaning = 41;
                await JSHost.ImportAsync(fetchhelper, "../fetchhelper.js");
                Console.WriteLine($"smoke {meaning}: TestTLS 3 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
                meaning = 43;
                Console.WriteLine($"smoke {meaning}: TestTLS 4 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
                await Delay(100);
                meaning = 44;
                Console.WriteLine($"smoke {meaning}: TestTLS 5 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
            });
            Console.WriteLine($"smoke {meaning}: TestTLS 9 ManagedThreadId:{Thread.CurrentThread.ManagedThreadId}, SynchronizationContext: {SynchronizationContext.Current?.GetType().FullName ?? "null"}");
        }

        private static async Task TimeOutThenComplete()
        {
            var tcs = new TaskCompletionSource();
            Console.WriteLine($"smoke: Task running tid:{Thread.CurrentThread.ManagedThreadId}");
            GlobalThisSetTimeout(() =>
            {
                tcs.SetResult();
                Console.WriteLine($"smoke: Timeout fired tid:{Thread.CurrentThread.ManagedThreadId}");
            }, 250);
            Console.WriteLine($"smoke: Task sleeping tid:{Thread.CurrentThread.ManagedThreadId}");
            await tcs.Task;
            Console.WriteLine($"smoke: Task resumed tid:{Thread.CurrentThread.ManagedThreadId}");
        }

        [JSExport]
        public static async Task<int> RunBackgroundThreadCompute()
        {
            var tcs = new TaskCompletionSource<int>();
            var t = new Thread(() =>
            {
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
            var t = factory.StartNew<int>(() =>
            {
                var n = CountingCollatzTest();
                return n;
            }, TaskCreationOptions.LongRunning);
            return await t;
        }

        [JSExport]
        public static async Task<int> RunBackgroundTaskRunCompute()
        {
            var t1 = Task.Run(() =>
            {
                var n = CountingCollatzTest();
                return n;
            });
            var t2 = Task.Run(() =>
            {
                var n = CountingCollatzTest();
                return n;
            });
            var rs = await Task.WhenAll(new[] { t1, t2 });
            if (rs[0] != rs[1])
                throw new Exception($"Results from two tasks {rs[0]}, {rs[1]}, differ");
            return rs[0];
        }

        [JSExport]
        internal static void GCCollect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }


        public static int CountingCollatzTest()
        {
            const int limit = 5000;
            const int maxInput = 200_000;
            int bigly = 0;
            int hugely = 0;
            int maxSteps = 0;
            for (int n = 1; n < maxInput; n++)
            {
                int steps = CountingCollatz((long)n, limit);
                if (steps > maxSteps)
                    maxSteps = steps;
                if (steps > 120)
                    bigly++;
                if (steps >= limit)
                    hugely++;
            }

            Console.WriteLine($"Bigly: {bigly}, Hugely: {hugely}, maxSteps: {maxSteps}");

            if (bigly == 86187 && hugely == 0 && maxSteps == 382)
                return 524;
            else
                return 0;
        }


        private static int CountingCollatz(long n, int limit)
        {
            int steps = 0;
            while (n > 1)
            {
                n = Collatz1(n);
                steps++;
                if (steps >= limit)
                    break;
            }
            return steps;
        }

        private static long Collatz1(long n)
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
