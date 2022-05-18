// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;


namespace Sample
{
    public class Test
    {
        public static void Main(string[] args)
        {
            // not called.  See main.js for all the interesting bits
        }

        private static int iterations;
        private static CancellationTokenSource cts;

        public static CancellationToken GetCancellationToken()
        {
            if (cts == null) {
                cts = new CancellationTokenSource ();
            }
            return cts.Token;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static long recursiveFib (int n)
        {
            if (n < 1)
                return 0;
            if (n == 1)
                return 1;
            return recursiveFib (n - 1) + recursiveFib (n - 2);
        }

        public static async Task<int> StartAsyncWork()
        {
            CancellationToken ct = GetCancellationToken();
            long b;
            const int N = 35;
            const long expected = 9227465;
            while (true)
            {
                await Task.Delay(1).ConfigureAwait(false);
                b = recursiveFib (N);
                if (ct.IsCancellationRequested)
                    break;
                iterations++;
            }
            return b == expected ? 42 : 0;
        }

        public static void StopWork()
        {
            cts.Cancel();
        }

        public static string GetIterationsDone()
        {
            return iterations.ToString();
        }
    }
}
