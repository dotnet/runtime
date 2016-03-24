// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   contributed by Robert F. Tobler
   optimized based on java & C# by Enotus, Isaac Gouy, and Alp Toker

   modified for use with xunit-performance
*/

using Microsoft.Xunit.Performance;
using System;
using System.IO;
using System.Text;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

public static class FastaRedux
{
#if DEBUG
    private const int Iterations = 1;
#else
    const int Iterations = 5;
#endif

    public static int Main(string[] args)
    {
        AccumulateAndScale(s_homoSapiens);
        AccumulateAndScale(s_IUB);
        int n = args.Length > 0 ? Int32.Parse(args[0]) : 2500;
        using (Stream s = Console.OpenStandardOutput())
        {
            s.WriteRepeatFasta("ONE", "Homo sapiens alu", Encoding.ASCII.GetBytes(s_ALU), n * 2);
            s.WriteRandomFasta("TWO", "IUB ambiguity codes", s_IUB, n * 3);
            s.WriteRandomFasta("THREE", "Homo sapiens frequency", s_homoSapiens, n * 5);
        }
        return 100;
    }

    [Benchmark]
    public static void Bench()
    {
        int n = 2500000;
        AccumulateAndScale(s_homoSapiens);
        AccumulateAndScale(s_IUB);
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                using (Stream s = Stream.Null)
                {
                    for (int i = 0; i < Iterations; i++)
                    {
                        s.WriteRepeatFasta("ONE", "Homo sapiens alu", Encoding.ASCII.GetBytes(s_ALU), n * 2);
                        s.WriteRandomFasta("TWO", "IUB ambiguity codes", s_IUB, n * 3);
                        s.WriteRandomFasta("THREE", "Homo sapiens frequency", s_homoSapiens, n * 5);
                    }
                }
            }
        }
    }

    private const int LINE_LEN = 60;
    private const int BUF_LEN = 64 * 1024;
    private const byte LF = (byte)'\n';

    private const int LOOKUP_LEN = 1024;
    private const double LOOKUP_SCALE = LOOKUP_LEN - 1;

    private static readonly string s_ALU =
        "GGCCGGGCGCGGTGGCTCACGCCTGTAATCCCAGCACTTTGG" +
        "GAGGCCGAGGCGGGCGGATCACCTGAGGTCAGGAGTTCGAGA" +
        "CCAGCCTGGCCAACATGGTGAAACCCCGTCTCTACTAAAAAT" +
        "ACAAAAATTAGCCGGGCGTGGTGGCGCGCGCCTGTAATCCCA" +
        "GCTACTCGGGAGGCTGAGGCAGGAGAATCGCTTGAACCCGGG" +
        "AGGCGGAGGTTGCAGTGAGCCGAGATCGCGCCACTGCACTCC" +
        "AGCCTGGGCGACAGAGCGAGACTCCGTCTCAAAAA";

    private struct Freq
    {
        public double P;
        public byte C;

        public Freq(char c, double p) { C = (byte)c; P = p; }
    }

    private static Freq[] s_IUB = {
      new Freq('a', 0.27), new Freq('c', 0.12), new Freq('g', 0.12),
      new Freq('t', 0.27), new Freq('B', 0.02), new Freq('D', 0.02),
      new Freq('H', 0.02), new Freq('K', 0.02), new Freq('M', 0.02),
      new Freq('N', 0.02), new Freq('R', 0.02), new Freq('S', 0.02),
      new Freq('V', 0.02), new Freq('W', 0.02), new Freq('Y', 0.02),
    };

    private static Freq[] s_homoSapiens = {
      new Freq ('a', 0.3029549426680), new Freq ('c', 0.1979883004921),
      new Freq ('g', 0.1975473066391), new Freq ('t', 0.3015094502008),
    };

    private static void AccumulateAndScale(Freq[] a)
    {
        double cp = 0.0;
        for (int i = 0; i < a.Length; i++)
            a[i].P = (cp += a[i].P) * LOOKUP_SCALE;
        a[a.Length - 1].P = LOOKUP_SCALE;
    }

    private static byte[] s_buf = new byte[BUF_LEN];

    private static int WriteDesc(this byte[] buf, string id, string desc)
    {
        var ds = Encoding.ASCII.GetBytes(">" + id + " " + desc + "\n");
        for (int i = 0; i < ds.Length; i++) buf[i] = ds[i];
        return BUF_LEN - ds.Length;
    }

    private static int Min(int a, int b) { return a < b ? a : b; }

    private static void WriteRepeatFasta(
          this Stream s, string id, string desc, byte[] alu, int nr)
    {
        int alen = alu.Length;
        int ar = alen, br = s_buf.WriteDesc(id, desc), lr = LINE_LEN;
        while (nr > 0)
        {
            int r = Min(Min(nr, lr), Min(ar, br));
            for (int ai = alen - ar, bi = BUF_LEN - br, be = bi + r;
                bi < be; bi++, ai++)
                s_buf[bi] = alu[ai];
            nr -= r; lr -= r; br -= r; ar -= r;
            if (ar == 0) ar = alen;
            if (br == 0) { s.Write(s_buf, 0, BUF_LEN); br = BUF_LEN; }
            if (lr == 0) { s_buf[BUF_LEN - (br--)] = LF; lr = LINE_LEN; }
            if (br == 0) { s.Write(s_buf, 0, BUF_LEN); br = BUF_LEN; }
        }
        if (lr < LINE_LEN) s_buf[BUF_LEN - (br--)] = LF;
        if (br < BUF_LEN) s.Write(s_buf, 0, BUF_LEN - br);
    }

    private static Freq[] s_lookup = new Freq[LOOKUP_LEN];

    private static void CreateLookup(Freq[] fr)
    {
        for (int i = 0, j = 0; i < LOOKUP_LEN; i++)
        {
            while (fr[j].P < i) j++;
            s_lookup[i] = fr[j];
        }
    }

    private const int IM = 139968;
    private const int IA = 3877;
    private const int IC = 29573;
    private const double SCALE = LOOKUP_SCALE / IM;

    private static int s_last = 42;

    private static void WriteRandomFasta(
          this Stream s, string id, string desc, Freq[] fr, int nr)
    {
        CreateLookup(fr);
        int br = s_buf.WriteDesc(id, desc), lr = LINE_LEN;
        while (nr > 0)
        {
            int r = Min(Min(nr, lr), br);
            for (int bi = BUF_LEN - br, be = bi + r; bi < be; bi++)
            {
                double p = SCALE * (s_last = (s_last * IA + IC) % IM);
                int ai = (int)p; if (s_lookup[ai].P < p) ai++;
                s_buf[bi] = s_lookup[ai].C;
            }
            nr -= r; lr -= r; br -= r;
            if (br == 0) { s.Write(s_buf, 0, BUF_LEN); br = BUF_LEN; }
            if (lr == 0) { s_buf[BUF_LEN - (br--)] = LF; lr = LINE_LEN; }
            if (br == 0) { s.Write(s_buf, 0, BUF_LEN); br = BUF_LEN; }
        }
        if (lr < LINE_LEN) s_buf[BUF_LEN - (br--)] = LF;
        if (br < BUF_LEN) s.Write(s_buf, 0, BUF_LEN - br);
    }
}

