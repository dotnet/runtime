// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from fannkuch-redux C# .NET Core #5 program
// http://benchmarksgame.alioth.debian.org/u64q/program.php?test=fannkuchredux&lang=csharpcore&id=5
// aka (as of 2017-09-01) rev 1.6 of https://alioth.debian.org/scm/viewvc.php/benchmarksgame/bench/fannkuchredux/fannkuchredux.csharp-5.csharp?root=benchmarksgame&view=log
// Best-scoring C# .NET Core version as of 2017-09-01

/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   contributed by Isaac Gouy, transliterated from Oleg Mazurov's Java program
   concurrency fix and minor improvements by Peperud
   parallel and small optimisations by Anthony Lloyd
*/

using System;
using System.Threading;
using System.Runtime.CompilerServices;
using Xunit;

namespace BenchmarksGame
{
    public class FannkuchRedux_5
    {
        static int[] fact, chkSums, maxFlips;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void firstPermutation(int[] p, int[] pp, int[] count, int idx)
        {
            for (int i = 0; i < p.Length; ++i) p[i] = i;
            for (int i = count.Length - 1; i > 0; --i)
            {
                int d = idx / fact[i];
                count[i] = d;
                if (d > 0)
                {
                    idx = idx % fact[i];
                    for (int j = i; j >= 0; --j) pp[j] = p[j];
                    for (int j = 0; j <= i; ++j) p[j] = pp[(j + d) % (i + 1)];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int nextPermutation(int[] p, int[] count)
        {
            int first = p[1];
            p[1] = p[0];
            p[0] = first;
            int i = 1;
            while (++count[i] > i)
            {
                count[i++] = 0;
                int next = p[1];
                p[0] = next;
                for (int j = 1; j < i;) p[j] = p[++j];
                p[i] = first;
                first = next;
            }
            return first;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int countFlips(int first, int[] p, int[] pp)
        {
            if (first == 0) return 0;
            if (p[first] == 0) return 1;
            for (int i = 0; i < pp.Length; i++) pp[i] = p[i];
            int flips = 2;
            while (true)
            {
                for (int lo = 1, hi = first - 1; lo < hi; lo++, hi--)
                {
                    int t = pp[lo];
                    pp[lo] = pp[hi];
                    pp[hi] = t;
                }
                int tp = pp[first];
                if (pp[tp] == 0) return flips;
                pp[first] = first;
                first = tp;
                flips++;
            }
        }

        static void run(int n, int taskId, int taskSize)
        {
            int[] p = new int[n], pp = new int[n], count = new int[n];
            firstPermutation(p, pp, count, taskId * taskSize);
            int chksum = countFlips(p[0], p, pp);
            int maxflips = chksum;
            while (--taskSize > 0)
            {
                var flips = countFlips(nextPermutation(p, count), p, pp);
                chksum += (1 - (taskSize % 2) * 2) * flips;
                if (flips > maxflips) maxflips = flips;
            }
            chkSums[taskId] = chksum;
            maxFlips[taskId] = maxflips;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            return Test(null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Test(int? arg)
        {
            int n = arg ?? 7;
            int sum = Bench(n, true);

            int expected = 16;

            // Return 100 on success, anything else on failure.
            return sum - expected + 100;
        }

        static int Bench(int n, bool verbose)
        {
            fact = new int[n + 1];
            fact[0] = 1;
            var factn = 1;
            for (int i = 1; i < fact.Length; i++) { fact[i] = factn *= i; }

            // For n == 7 and nTasks > 8, the algorithm returns chkSum != 228
            // Hence, we restrict the processor count to 8 to get consistency on
            // all the hardwares.
            // See https://github.com/dotnet/runtime/issues/67157
            int nTasks = Math.Min(Environment.ProcessorCount, 8);
            chkSums = new int[nTasks];
            maxFlips = new int[nTasks];
            int taskSize = factn / nTasks;
            var threads = new Thread[nTasks];
            for (int i = 1; i < nTasks; i++)
            {
                int j = i;
                (threads[j] = new Thread(() => run(n, j, taskSize))).Start();
            }
            run(n, 0, taskSize);
            int chksum = chkSums[0], maxflips = maxFlips[0];
            for (int i = 1; i < threads.Length; i++)
            {
                threads[i].Join();
                chksum += chkSums[i];
                if (maxFlips[i] > maxflips) maxflips = maxFlips[i];
            }
            if (verbose) Console.Out.WriteLineAsync(chksum + "\nPfannkuchen(" + n + ") = " + maxflips);

            return maxflips;
        }
    }
}
