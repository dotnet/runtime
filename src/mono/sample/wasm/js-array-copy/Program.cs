// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static async Task<int> TestMeaning()
        {
            var factory = new Function("size", "const array = new Uint8Array(new ArrayBuffer(size)); for (var i = 0; i < size; i++) array[i] = Math.round(255 * Math.random()); return array;");
            var rnd = new Random();

            int iterations = 10;

            List<Task> tasks=new List<Task>();

            for (int i = 0; i < iterations; i++)
            {
                tasks.Add(Task.Run(async () => {
                    int bufferSize = 1024 * 1024 * Math.Max(2, rnd.Next(50));
                    var proxy = (Uint8Array)factory.Call(null, bufferSize);
                    Console.WriteLine($"{bufferSize}, proxy {proxy.Length}");

                    await Task.Delay(1000 * rnd.Next(10));

                    Console.WriteLine($"{bufferSize}, proxy {proxy.Length}");

                    var data = proxy.ToArray();

                    Console.WriteLine($"{bufferSize}, data {data.Length}, {data[0]}, {data[1]}, {data[bufferSize - 1]}, {data[bufferSize - 2]}");

                    if (bufferSize != data.Length || bufferSize != proxy.Length)
                        throw new Exception("Dimmensions not matched");

                    proxy.Dispose();
                }));
            }

            await Task.WhenAll(tasks.ToArray());

            return 42;
        }
    }
}
