// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.Tracing;


namespace Sample
{

    [EventSource(Name = "WasmHello")]
    public class WasmHelloEventSource  : EventSource
    {
        public static readonly WasmHelloEventSource Instance = new ();

        private IncrementingEventCounter _calls;

        private WasmHelloEventSource ()
        {
        }

        [NonEvent]
        public void NewCallsCounter()
        {
            _calls?.Dispose();
            _calls = new ("fib-calls", this)
            {
                DisplayName = "Recursive Fib calls",
            };
        }

        [NonEvent]
        public void CountCall() {
            _calls?.Increment(1.0);
        }

        protected override void Dispose (bool disposing)
        {
            _calls?.Dispose();
            _calls = null;

            base.Dispose(disposing);
        }

        [Event(1, Message="Started Fib({0})", Level = EventLevel.Informational)]
        public void StartFib(int n)
        {
            if (!IsEnabled())
                return;

            WriteEvent(1, n);
        }

        [Event(2, Message="Stopped Fib({0}) = {1}", Level = EventLevel.Informational)]
        public void StopFib(int n, string result)
        {
            if (!IsEnabled())
                return;

            WriteEvent(2, n, result);
        }
    }

    public partial class Test
    {
        public static void Main(string[] args)
        {
            // not called.  See main.js for all the interesting bits
        }

        private static int iterations;
        private static CancellationTokenSource cts;

        public static CancellationToken GetCancellationToken()
        {
            cts ??= new CancellationTokenSource ();
            return cts.Token;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static long recursiveFib (int n)
        {
            if (n < 1)
                return 0;
            if (n == 1)
                return 1;
            WasmHelloEventSource.Instance.CountCall();
            return recursiveFib (n - 1) + recursiveFib (n - 2);
        }

#if false
            // dead code to prove that starting user threads isn't possible on the perftracing runtime
            public static void Meth() {
                    Thread.Sleep (500);
                    while (!GetCancellationToken().IsCancellationRequested) {
                            Console.WriteLine ("ping");
                            Thread.Sleep (500);
                    }
            }
#endif

        [JSExport]
        public static async Task<double> StartAsyncWork(int N)
        {
            CancellationToken ct = GetCancellationToken();
#if false
            new Thread(new ThreadStart(Meth)).Start();
#endif
            await Task.Delay(1);
            long b;
            WasmHelloEventSource.Instance.NewCallsCounter();
            iterations = 0;
            while (true)
            {
                WasmHelloEventSource.Instance.StartFib(N);
                await Task.Delay(1);
                b = recursiveFib (N);
                WasmHelloEventSource.Instance.StopFib(N, b.ToString());
                iterations++;
                            Console.WriteLine ("ping1");
                if (ct.IsCancellationRequested)
                    break;
            }
            Console.WriteLine ("stopping");
            long expected = fastFib(N);
            Console.WriteLine ("stopping2");
            if (expected == b)
                return (double)b;
            else {
                Console.Error.WriteLine ("expected {0}, but got {1}", expected, b);
                return 0.0;
            }
        }

        [JSExport]
        public static void StopWork()
        {
            cts.Cancel();
        }

        [JSExport]
        public static string GetIterationsDone()
        {
            return iterations.ToString();
        }

        private static long fastFib(int N) {
            if (N < 1)
                return 0;
            long a = 0;
            long b = 1;
            for (int i = 1; i < N; ++i) {
                long tmp = a+b;
                a = b;
                b = tmp;
            }
            return b;
        }

    }
}
