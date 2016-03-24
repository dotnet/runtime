// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   contributed by Isaac Gouy
   optimizations by Alp Toker <alp@atoker.com>

   modified for use with xunit-performance
*/

using Microsoft.Xunit.Performance;
using System;
using System.IO;
using System.Text;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

public class Fasta
{
#if DEBUG
    private const int Iterations = 1;
#else
    const int Iterations = 800;
#endif

    public static int Main(string[] args)
    {
        MakeCumulative(s_homoSapiens);
        MakeCumulative(s_IUB);

        int n = args.Length > 0 ? Int32.Parse(args[0]) : 1000;

        using (Stream s = Console.OpenStandardOutput())
        {
            MakeRepeatFasta("ONE", "Homo sapiens alu", Encoding.ASCII.GetBytes(s_ALU), n * 2, s);
            MakeRandomFasta("TWO", "IUB ambiguity codes", s_IUB, n * 3, s);
            MakeRandomFasta("THREE", "Homo sapiens frequency", s_homoSapiens, n * 5, s);
        }
        return 100;
    }

    [Benchmark]
    public static void Bench()
    {
        int n = 5000;
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                for (int i = 0; i < Iterations; i++)
                {
                    using (Stream s = Stream.Null)
                    {
                        MakeRepeatFasta("ONE", "Homo sapiens alu", Encoding.ASCII.GetBytes(s_ALU), n * 2, s);
                        MakeRandomFasta("TWO", "IUB ambiguity codes", s_IUB, n * 3, s);
                        MakeRandomFasta("THREE", "Homo sapiens frequency", s_homoSapiens, n * 5, s);
                    }
                }
            }
        }
    }

    // The usual pseudo-random number generator

    private const int IM = 139968;
    private const int IA = 3877;
    private const int IC = 29573;
    private static int s_seed = 42;

    private static double random(double max)
    {
        return max * ((s_seed = (s_seed * IA + IC) % IM) * (1.0 / IM));
    }

    // Weighted selection from alphabet

    private static string s_ALU =
        "GGCCGGGCGCGGTGGCTCACGCCTGTAATCCCAGCACTTTGG" +
        "GAGGCCGAGGCGGGCGGATCACCTGAGGTCAGGAGTTCGAGA" +
        "CCAGCCTGGCCAACATGGTGAAACCCCGTCTCTACTAAAAAT" +
        "ACAAAAATTAGCCGGGCGTGGTGGCGCGCGCCTGTAATCCCA" +
        "GCTACTCGGGAGGCTGAGGCAGGAGAATCGCTTGAACCCGGG" +
        "AGGCGGAGGTTGCAGTGAGCCGAGATCGCGCCACTGCACTCC" +
        "AGCCTGGGCGACAGAGCGAGACTCCGTCTCAAAAA";

    private class Frequency
    {
        public byte c;
        public double p;

        public Frequency(char c, double p)
        {
            this.c = (byte)c;
            this.p = p;
        }
    }

    private static Frequency[] s_IUB = {
        new Frequency ('a', 0.27)
            ,new Frequency ('c', 0.12)
            ,new Frequency ('g', 0.12)
            ,new Frequency ('t', 0.27)

            ,new Frequency ('B', 0.02)
            ,new Frequency ('D', 0.02)
            ,new Frequency ('H', 0.02)
            ,new Frequency ('K', 0.02)
            ,new Frequency ('M', 0.02)
            ,new Frequency ('N', 0.02)
            ,new Frequency ('R', 0.02)
            ,new Frequency ('S', 0.02)
            ,new Frequency ('V', 0.02)
            ,new Frequency ('W', 0.02)
            ,new Frequency ('Y', 0.02)
    };

    private static Frequency[] s_homoSapiens = {
        new Frequency ('a', 0.3029549426680)
            ,new Frequency ('c', 0.1979883004921)
            ,new Frequency ('g', 0.1975473066391)
            ,new Frequency ('t', 0.3015094502008)
    };

    private static void MakeCumulative(Frequency[] a)
    {
        double cp = 0.0;
        for (int i = 0; i < a.Length; i++)
        {
            cp += a[i].p;
            a[i].p = cp;
        }
    }

    // naive
    private static byte SelectRandom(Frequency[] a)
    {
        double r = random(1.0);

        for (int i = 0; i < a.Length; i++)
            if (r < a[i].p)
                return a[i].c;

        return a[a.Length - 1].c;
    }

    private const int LineLength = 60;
    private static int s_index = 0;
    private static byte[] s_buf = new byte[1024];

    private static void MakeRandomFasta(string id, string desc, Frequency[] a, int n, Stream s)
    {
        s_index = 0;
        int m = 0;

        byte[] descStr = Encoding.ASCII.GetBytes(">" + id + " " + desc + "\n");
        s.Write(descStr, 0, descStr.Length);

        while (n > 0)
        {
            m = n < LineLength ? n : LineLength;

            if (s_buf.Length - s_index < m)
            {
                s.Write(s_buf, 0, s_index);
                s_index = 0;
            }

            for (int i = 0; i < m; i++)
            {
                s_buf[s_index++] = SelectRandom(a);
            }

            s_buf[s_index++] = (byte)'\n';
            n -= LineLength;
        }

        if (s_index != 0)
            s.Write(s_buf, 0, s_index);
    }

    private static void MakeRepeatFasta(string id, string desc, byte[] alu, int n, Stream s)
    {
        s_index = 0;
        int m = 0;
        int k = 0;
        int kn = alu.Length;

        byte[] descStr = Encoding.ASCII.GetBytes(">" + id + " " + desc + "\n");
        s.Write(descStr, 0, descStr.Length);

        while (n > 0)
        {
            m = n < LineLength ? n : LineLength;

            if (s_buf.Length - s_index < m)
            {
                s.Write(s_buf, 0, s_index);
                s_index = 0;
            }

            for (int i = 0; i < m; i++)
            {
                if (k == kn)
                    k = 0;

                s_buf[s_index++] = alu[k];
                k++;
            }

            s_buf[s_index++] = (byte)'\n';
            n -= LineLength;
        }

        if (s_index != 0)
            s.Write(s_buf, 0, s_index);
    }
}
