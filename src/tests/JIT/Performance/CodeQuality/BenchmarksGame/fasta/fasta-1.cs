// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from fasta C# .NET Core program
// http://benchmarksgame.alioth.debian.org/u64q/program.php?test=fasta&lang=csharpcore&id=1
// aka (as of 2017-09-01) rev 1.1 of https://alioth.debian.org/scm/viewvc.php/benchmarksgame/bench/fasta/fasta.csharp?root=benchmarksgame&view=log
// Best-scoring C# .NET Core version as of 2017-09-01

/* The Computer Language Benchmarks Game
  http://benchmarksgame.alioth.debian.org/

  contributed by Serge Smith
  further optimized (rewrote threading, random generation loop) by Jan de Vaan
  modified by Josh Goldfoot (fasta-repeat buffering)
*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BenchmarksGame
{
    public class Fasta_1
    {
        const int LineLength = 60;

        const int IM = 139968;
        const int IA = 3877;
        const int IC = 29573;
        static int seed = 42;

        [Fact]
        public static int TestEntryPoint()
        {
            return Test(null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Test(int? arg)
        {
            int n = arg ?? 1000;

            Bench(n, true);
            return 100;
        }

        static void Bench(int n, bool verbose)
        {
            MakeCumulative(IUB);
            MakeCumulative(HomoSapiens);

            using (var s = (verbose ? Console.OpenStandardOutput() : Stream.Null))
            {
                MakeRepeatFasta("ONE", "Homo sapiens alu", Encoding.ASCII.GetBytes(ALU), n * 2, s);
                MakeRandomFasta("TWO", "IUB ambiguity codes", IUB, n * 3, s);
                MakeRandomFasta("THREE", "Homo sapiens frequency", HomoSapiens, n * 5, s);
            }
        }

        public static IEnumerable<R> TransformQueue<T, R>(BlockingCollection<T> queue,
            Func<T, R> transform, int threadCount)
        {
            var tasks = new Task<R>[threadCount];

            for (int i = 0; i < threadCount; ++i)
            {
                T input;
                if (!queue.TryTake(out input, Timeout.Infinite))
                    break;

                tasks[i] = Task.Run(() => transform(input));
            }

            int pos = 0;
            while (true)
            {
                if (tasks[pos] == null)
                    break;

                yield return tasks[pos].Result;

                T input;
                tasks[pos] = queue.TryTake(out input, Timeout.Infinite)
                    ? Task.Run(() => transform(input))
                    : null;

                pos = (pos + 1) % threadCount;
            }
        }



        static void MakeRandomFasta(string id, string desc,
                                    Frequency[] a, int n, Stream s)
        {
            var queue = new BlockingCollection<int[]>(2);

            var bufferCount = Environment.ProcessorCount + 4;

            Task.Run(() =>
            {
                var len = LineLength * 40;
                var buffers = Enumerable.Range(0, bufferCount)
                .Select(i => new int[len]).ToArray();
                var index = 0;
                for (var i = 0; i < n; i += len)
                {
                    var buffer = n - i < len
                        ? new int[n - i]
                        : buffers[index++ % buffers.Length];

                    FillRandom(buffer);
                    queue.Add(buffer);
                }
                queue.CompleteAdding();
            });

            byte[] descStr = Encoding.ASCII.GetBytes(">" + id + " " + desc + "\n");
            s.Write(descStr, 0, descStr.Length);

            foreach (var r in TransformQueue(queue,
                rnd => SelectNucleotides(a, rnd), Environment.ProcessorCount))
            {
                s.Write(r, 0, r.Length);
            }

        }

        private static byte[] SelectNucleotides(Frequency[] a, int[] rnd)
        {
            var resLength = (rnd.Length / LineLength) * (LineLength + 1);
            if (rnd.Length % LineLength != 0)
            {
                resLength += rnd.Length % LineLength + 1;
            }

            var buf = new byte[resLength];
            var index = 0;
            for (var i = 0; i < rnd.Length; i += LineLength)
            {
                var len = Math.Min(LineLength, rnd.Length - i);
                for (var j = 0; j < len; ++j)
                    buf[index++] = SelectRandom(a, (int)rnd[i + j]);
                buf[index++] = (byte)'\n';
            }
            return buf;
        }

        static void MakeRepeatFasta(string id, string desc,
                                    byte[] alu, int n, Stream s)
        {
            byte[] descStr = Encoding.ASCII.GetBytes(">" + id + " " + desc + "\n");
            s.Write(descStr, 0, descStr.Length);

            /* JG: fasta_repeat repeats every len(alu) * line-length = 287 * 61 = 17507 characters.
               So, calculate this once, then just print that buffer over and over. */

            byte[] sequence;
            int sequenceLength;
            using (var unstandardOut = new MemoryStream(alu.Length * (LineLength + 1) + 1))
            {
                MakeRepeatFastaBuffer(alu, alu.Length * LineLength, unstandardOut);
                sequenceLength = (int)unstandardOut.Length;
                sequence = new byte[sequenceLength];
                unstandardOut.Seek(0, SeekOrigin.Begin);
                unstandardOut.Read(sequence, 0, sequenceLength);
            }
            int outputBytes = n + n / 60;
            while (outputBytes >= sequenceLength)
            {
                s.Write(sequence, 0, sequenceLength);
                outputBytes -= sequenceLength;
            }
            if (outputBytes > 0)
            {
                s.Write(sequence, 0, outputBytes);
                s.WriteByte((byte)'\n');
            }
        }

        static void MakeRepeatFastaBuffer(byte[] alu, int n, Stream s)
        {
            var index = 0;
            int m = 0;
            int k = 0;
            int kn = alu.Length;
            var buf = new byte[1024];

            while (n > 0)
            {
                m = n < LineLength ? n : LineLength;

                if (buf.Length - index < m)
                {
                    s.Write(buf, 0, index);
                    index = 0;
                }

                for (int i = 0; i < m; i++)
                {
                    if (k == kn)
                        k = 0;

                    buf[index++] = alu[k];
                    k++;
                }

                buf[index++] = (byte)'\n';
                n -= LineLength;
            }

            if (index != 0)
                s.Write(buf, 0, index);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte SelectRandom(Frequency[] a, int r)
        {
            for (int i = 0; i < a.Length - 1; i++)
                if (r < a[i].p)
                    return a[i].c;

            return a[a.Length - 1].c;
        }

        static void MakeCumulative(Frequency[] a)
        {
            double cp = 0;
            for (int i = 0; i < a.Length; i++)
            {
                cp += a[i].p;
                a[i].p = cp;
            }
        }

        static string ALU =
          "GGCCGGGCGCGGTGGCTCACGCCTGTAATCCCAGCACTTTGG" +
          "GAGGCCGAGGCGGGCGGATCACCTGAGGTCAGGAGTTCGAGA" +
          "CCAGCCTGGCCAACATGGTGAAACCCCGTCTCTACTAAAAAT" +
          "ACAAAAATTAGCCGGGCGTGGTGGCGCGCGCCTGTAATCCCA" +
          "GCTACTCGGGAGGCTGAGGCAGGAGAATCGCTTGAACCCGGG" +
          "AGGCGGAGGTTGCAGTGAGCCGAGATCGCGCCACTGCACTCC" +
          "AGCCTGGGCGACAGAGCGAGACTCCGTCTCAAAAA";

        struct Frequency
        {
            public readonly byte c;
            public double p;

            public Frequency(char c, double p)
            {
                this.c = (byte)c;
                this.p = (p * IM);
            }
        }

        static Frequency[] IUB = {
            new Frequency ('a', 0.27),
            new Frequency ('c', 0.12),
            new Frequency ('g', 0.12),
            new Frequency ('t', 0.27),

            new Frequency ('B', 0.02),
            new Frequency ('D', 0.02),
            new Frequency ('H', 0.02),
            new Frequency ('K', 0.02),
            new Frequency ('M', 0.02),
            new Frequency ('N', 0.02),
            new Frequency ('R', 0.02),
            new Frequency ('S', 0.02),
            new Frequency ('V', 0.02),
            new Frequency ('W', 0.02),
            new Frequency ('Y', 0.02)
        };

        static Frequency[] HomoSapiens = {
            new Frequency ('a', 0.3029549426680),
            new Frequency ('c', 0.1979883004921),
            new Frequency ('g', 0.1975473066391),
            new Frequency ('t', 0.3015094502008)
        };


        private static void FillRandom(int[] result)
        {
            var s = seed;
            for (var i = 0; i < result.Length; i++)
            {
                s = (s * IA + IC) % IM;
                result[i] = s;
            }
            seed = s;
        }
    }
}
