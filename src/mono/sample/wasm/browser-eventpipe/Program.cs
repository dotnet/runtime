// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Sample
{
    public class Test
    {
        public static void Main(string[] args)
        {
            Console.WriteLine ("Hello, World!");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static async Task<int> StartAsyncWork()
        {
            int a = 0;
            int b = 1;
            const int N = 30;
            const int expected = 832040;
            for (int i = 1; i < N; i++)
            {
                int tmp = a + b;
                a = b;
                b = tmp;
                await Task.Yield();
            }
            return b == expected ? 42 : 0;
        }
    }
}
