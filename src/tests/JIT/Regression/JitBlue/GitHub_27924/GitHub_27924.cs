// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static int returnVal = 100;
    static byte[][] s = new byte[1000][];

    public const int DefaultSeed = 20010415;
    public static int Seed = Environment.GetEnvironmentVariable("CORECLR_SEED") switch
    {
        string seedStr when seedStr.Equals("random", StringComparison.OrdinalIgnoreCase) => new Random().Next(),
        string seedStr when int.TryParse(seedStr, out int envSeed) => envSeed,
        _ => DefaultSeed
    };

    static void Work()
    {
        for (uint i = 0; i < 1000000; i++)
        {
            var a = s[i++ % s.Length];

            ref byte p = ref a[0];
            ref byte q = ref a[1];

            if (Unsafe.ByteOffset(ref p, ref q) != new IntPtr(1))
            {
                Console.WriteLine("ERROR: i = " + i);
                returnVal = -1;
            }
            p = 1; q = 2;
        }
    }

    static int Main(string[] args)
    {
        for(int i = 0; i < s.Length; i++) s[i] = new byte[2];

        List<Task> tasks = new List<Task>();
        for(int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(Work));
        }

        Random r = new Random(Seed);
        for (uint i = 0; i < 10000; i++)
        {
            s[r.Next(s.Length)] = new byte[3 + r.Next(100)];
        }
        Task t = Task.WhenAll(tasks);
        t.Wait();
        return returnVal;
    }
}
