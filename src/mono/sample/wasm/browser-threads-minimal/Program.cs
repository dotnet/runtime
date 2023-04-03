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

        public static int CountingCollatzTest()
        {
            const int limit = 5000;
            const int maxInput = 500_000;
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

            if (bigly == 241677 && hugely == 0 && maxSteps == 448)
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
