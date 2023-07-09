// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Xunit;

namespace BenchmarksGame
{
    public class Mandelbrot_2
    {
        [Fact]
        public static int TestEntryPoint()
        {
            return Test(null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Test(int? arg)
        {
            int width = arg ?? 80;

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
