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
            Console.WriteLine ("Hello, World!");
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
        public static async Task<int> StartAsyncWork()
        {
            CancellationToken ct = GetCancellationToken();
            int a;
            int b;
            const int N = 30;
            const int expected = 832040;
            while (true)
            {
                a = 0; b = 1;
                for (int i = 1; i < N; i++)
                {
                    int tmp = a + b;
                    a = b;
                    b = tmp;
                    await Task.Delay(1).ConfigureAwait(false);
                }
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
