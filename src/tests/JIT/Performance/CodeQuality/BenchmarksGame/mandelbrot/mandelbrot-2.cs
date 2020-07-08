// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Adapted from mandelbrot C# .NET Core #2 program
// http://benchmarksgame.alioth.debian.org/u64q/program.php?test=mandelbrot&lang=csharpcore&id=2
// aka (as of 2017-09-01) rev 1.2 of https://alioth.debian.org/scm/viewvc.php/benchmarksgame/bench/mandelbrot/mandelbrot.csharp-2.csharp?root=benchmarksgame&view=log
// Best-scoring single-threaded C# .NET Core version as of 2017-09-01

/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 *
 * Adapted by Antti Lankila from the earlier Isaac Gouy's implementation
 */

using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Xunit.Performance;
using Xunit;

[assembly: OptimizeForBenchmarks]

namespace BenchmarksGame
{
    public class Mandelbrot_2
    {
        public static int Main(String[] args)
        {
            int width = 80;
            if (args.Length > 0)
                width = Int32.Parse(args[0]);

            int lineLen = (width - 1) / 8 + 1;
            var bytes = new byte[width * lineLen];
            var memStream = new MemoryStream(bytes);

            DoBench(width, memStream, true);

            if (!MatchesChecksum(bytes, "3B-EF-65-05-1D-39-7F-9B-96-8D-EF-98-BF-06-CE-74"))
            {
                return -1;
            }
            return 100;
        }

        // Commented out data left in source to provide checksums for each case

        [Benchmark]
        //[InlineData(1000, "B2-13-51-CE-B0-29-2C-4E-75-5E-91-19-18-E4-0C-D9")]
        //[InlineData(2000, "5A-21-55-9B-7B-18-2F-34-9B-33-C5-F9-B5-2C-40-56")]
        //[InlineData(3000, "E5-82-85-0A-3C-89-69-B1-A8-21-63-52-75-B3-C8-33")]
        [InlineData(4000, "C7-E6-66-43-66-73-F8-A8-D3-B4-D7-97-2F-FC-A1-D3")]
        //[InlineData(5000, "6D-36-F1-F6-37-8F-34-EB-52-F9-2D-11-89-12-B2-2F")]
        //[InlineData(6000, "8B-05-78-EB-2E-0E-98-F2-C7-39-76-ED-0F-A9-D2-B8")]
        //[InlineData(7000, "01-F8-F2-2A-AB-70-C7-BA-E3-64-19-E7-D2-84-DF-57")]
        //[InlineData(8000, "C8-ED-D7-FB-65-66-3A-D9-C6-04-9E-96-E8-CA-4F-2C")]
        public static void Bench(int width, string checksum)
        {
            int lineLen = (width - 1) / 8 + 1;
            byte[] bytes = null;

            Benchmark.Iterate(() =>
            {
                bytes = new byte[width * lineLen];
                var memStream = new MemoryStream(bytes);

                DoBench(width, memStream, false);
            });

            Assert.True(MatchesChecksum(bytes, checksum));
        }

        static bool MatchesChecksum(byte[] bytes, string checksum)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(bytes);
                return (checksum == BitConverter.ToString(hash));
            }
        }

        static void DoBench(int width, MemoryStream s, bool verbose)
        {
            int height = width;
            int maxiter = 50;
            double limit = 4.0;

            if (verbose)
            {
                Console.WriteLine("P4");
                Console.WriteLine("{0} {1}", width, height);
            }

            for (int y = 0; y < height; y++)
            {
                int bits = 0;
                int xcounter = 0;
                double Ci = 2.0 * y / height - 1.0;

                for (int x = 0; x < width; x++)
                {
                    double Zr = 0.0;
                    double Zi = 0.0;
                    double Cr = 2.0 * x / width - 1.5;
                    int i = maxiter;

                    bits = bits << 1;
                    do
                    {
                        double Tr = Zr * Zr - Zi * Zi + Cr;
                        Zi = 2.0 * Zr * Zi + Ci;
                        Zr = Tr;
                        if (Zr * Zr + Zi * Zi > limit)
                        {
                            bits |= 1;
                            break;
                        }
                    } while (--i > 0);

                    if (++xcounter == 8)
                    {
                        s.WriteByte((byte)(bits ^ 0xff));
                        bits = 0;
                        xcounter = 0;
                    }
                }
                if (xcounter != 0)
                    s.WriteByte((byte)((bits << (8 - xcounter)) ^ 0xff));
            }

            if (verbose)
            {
                s.WriteTo(Console.OpenStandardOutput());
            }
        }
    }
}
