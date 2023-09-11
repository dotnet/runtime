// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from fasta C# .NET Core #2 program
// http://benchmarksgame.alioth.debian.org/u64q/program.php?test=fasta&lang=csharpcore&id=2
// aka (as of 2017-09-01) rev 1.2 of https://alioth.debian.org/scm/viewvc.php/benchmarksgame/bench/fasta/fasta.csharp-2.csharp?root=benchmarksgame&view=log
// Best-scoring single-threaded C# .NET Core version as of 2017-09-01

/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   contributed by Isaac Gouy
   optimizations by Alp Toker <alp@atoker.com>
*/

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

namespace BenchmarksGame
{
    public class Fasta_2
    {
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
            MakeCumulative(HomoSapiens);
            MakeCumulative(IUB);

            using (Stream s = (verbose ? Console.OpenStandardOutput() : Stream.Null))
            {
                MakeRepeatFasta("ONE", "Homo sapiens alu", Encoding.ASCII.GetBytes(ALU), n * 2, s);
                MakeRandomFasta("TWO", "IUB ambiguity codes", IUB, n * 3, s);
                MakeRandomFasta("THREE", "Homo sapiens frequency", HomoSapiens, n * 5, s);
            }
        }

        // The usual pseudo-random number generator

        const int IM = 139968;
        const int IA = 3877;
        const int IC = 29573;
        static int seed = 42;

        static double random(double max)
        {
            return max * ((seed = (seed * IA + IC) % IM) * (1.0 / IM));
        }

        // Weighted selection from alphabet

        static string ALU =
            "GGCCGGGCGCGGTGGCTCACGCCTGTAATCCCAGCACTTTGG" +
            "GAGGCCGAGGCGGGCGGATCACCTGAGGTCAGGAGTTCGAGA" +
            "CCAGCCTGGCCAACATGGTGAAACCCCGTCTCTACTAAAAAT" +
            "ACAAAAATTAGCCGGGCGTGGTGGCGCGCGCCTGTAATCCCA" +
            "GCTACTCGGGAGGCTGAGGCAGGAGAATCGCTTGAACCCGGG" +
            "AGGCGGAGGTTGCAGTGAGCCGAGATCGCGCCACTGCACTCC" +
            "AGCCTGGGCGACAGAGCGAGACTCCGTCTCAAAAA";

        class Frequency
        {
            public byte c;
            public double p;

            public Frequency(char c, double p)
            {
                this.c = (byte)c;
                this.p = p;
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

        static void MakeCumulative(Frequency[] a)
        {
            double cp = 0.0;
            for (int i = 0; i < a.Length; i++)
            {
                cp += a[i].p;
                a[i].p = cp;
            }
        }

        // naive
        static byte SelectRandom(Frequency[] a)
        {
            double r = random(1.0);

            for (int i = 0; i < a.Length; i++)
                if (r < a[i].p)
                    return a[i].c;

            return a[a.Length - 1].c;
        }

        const int LineLength = 60;
        static int index = 0;
        static byte[] buf = new byte[1024];

        static void MakeRandomFasta(string id, string desc, Frequency[] a, int n, Stream s)
        {
            index = 0;
            int m = 0;

            byte[] descStr = Encoding.ASCII.GetBytes(">" + id + " " + desc + "\n");
            s.Write(descStr, 0, descStr.Length);

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
                    buf[index++] = SelectRandom(a);
                }

                buf[index++] = (byte)'\n';
                n -= LineLength;
            }

            if (index != 0)
                s.Write(buf, 0, index);
        }

        static void MakeRepeatFasta(string id, string desc, byte[] alu, int n, Stream s)
        {
            index = 0;
            int m = 0;
            int k = 0;
            int kn = alu.Length;

            byte[] descStr = Encoding.ASCII.GetBytes(">" + id + " " + desc + "\n");
            s.Write(descStr, 0, descStr.Length);

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
    }
}
