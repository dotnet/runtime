// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   started with Java #2 program (Krause/Whipkey/Bennet/AhnTran/Enotus/Stalcup)
   adapted for C# by Jan de Vaan
   simplified and optimised to use TPL by Anthony Lloyd

   posted to Benchmarks Game as mandelbrot C# .NET Core #4
   (http://benchmarksgame.alioth.debian.org/u64q/program.php?test=mandelbrot&lang=csharpcore&id=4)
   modified to remove concurrency and operate with xunit-performance
*/

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Microsoft.Xunit.Performance;
using Microsoft.Xunit.Performance.Api;
using Xunit;

namespace BenchmarksGame
{
    public class Mandelbrot
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte getByte(double[] Crb, double Ciby, int x, int y)
        {
            int res = 0;
            for (int i = 0; i < 8; i += 2)
            {
                double Crbx = Crb[x + i], Crbx1 = Crb[x + i + 1];
                double Zr1 = Crbx, Zr2 = Crbx1;
                double Zi1 = Ciby, Zi2 = Ciby;

                int b = 0;
                int j = 49;
                do
                {
                    double nZr1 = Zr1 * Zr1 - Zi1 * Zi1 + Crbx;
                    Zi1 = Zr1 * Zi1 + Zr1 * Zi1 + Ciby;
                    Zr1 = nZr1;

                    double nZr2 = Zr2 * Zr2 - Zi2 * Zi2 + Crbx1;
                    Zi2 = Zr2 * Zi2 + Zr2 * Zi2 + Ciby;
                    Zr2 = nZr2;

                    if (Zr1 * Zr1 + Zi1 * Zi1 > 4)
                    {
                        b |= 2;
                        if (b == 3)
                            break;
                    }
                    if (Zr2 * Zr2 + Zi2 * Zi2 > 4)
                    {
                        b |= 1;
                        if (b == 3)
                            break;
                    }
                } while (--j > 0);
                res = (res << 2) + b;
            }
            return (byte)(res ^ -1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static byte[] Bench(int n, bool verbose = false)
        {
            double invN = 2.0 / n;
            var Crb = new double[n + 7];
            for (int i = 0; i < n; i++)
            {
                Crb[i] = i * invN - 1.5;
            }
            int lineLen = (n - 1) / 8 + 1;
            var data = new byte[n * lineLen];
            for (int i = 0; i < n; i++)
            {
                var Cibi = i * invN - 1.0;
                var offset = i * lineLen;
                for (int x = 0; x < lineLen; x++)
                    data[offset + x] = getByte(Crb, Cibi, x * 8, i);
            };

            if (verbose)
            {
                Console.Out.WriteLine("P4\n{0} {0}", n);
                Console.OpenStandardOutput().Write(data, 0, data.Length);
            }

            return data;
        }

        // Commented out data left in source to provide checksums for each case
        // Checksums calculated from the origonal source referenced at top of this source

        [Benchmark]
        //[InlineData(500,  "54-01-EE-C8-46-9B-AB-FA-54-9F-45-CE-98-89-66-A9")]
        //[InlineData(1000, "B2-13-51-CE-B0-29-2C-4E-75-5E-91-19-18-E4-0C-D9")]
        //[InlineData(2000, "5A-21-55-9B-7B-18-2F-34-9B-33-C5-F9-B5-2C-40-56")]
        //[InlineData(3000, "E5-82-85-0A-3C-89-69-B1-A8-21-63-52-75-B3-C8-33")]
        [InlineData(4000, "C7-E6-66-43-66-73-F8-A8-D3-B4-D7-97-2F-FC-A1-D3")]
        //[InlineData(5000, "6D-36-F1-F6-37-8F-34-EB-52-F9-2D-11-89-12-B2-2F")]
        //[InlineData(6000, "8B-05-78-EB-2E-0E-98-F2-C7-39-76-ED-0F-A9-D2-B8")]
        //[InlineData(7000, "01-F8-F2-2A-AB-70-C7-BA-E3-64-19-E7-D2-84-DF-57")]
        //[InlineData(8000, "C8-ED-D7-FB-65-66-3A-D9-C6-04-9E-96-E8-CA-4F-2C")]
        public static void Test(int n, string checksum)
        {
            foreach (var iteration in Benchmark.Iterations)
            {
                byte[] bitmap = null;
                using (iteration.StartMeasurement())
                {
                    bitmap = Bench(n);
                }
                using (var md5 = MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(bitmap);
                    Assert.Equal(checksum, BitConverter.ToString(hash));
                }
            }
        }

        public static bool VerifyBench(int n, string checksum)
        {
            byte[] bitmap = Bench(n);
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(bitmap);
                return checksum == BitConverter.ToString(hash);
            }
        }

        public static int Main(string[] args)
        {
            const int n = 500;
            const string checksum = "54-01-EE-C8-46-9B-AB-FA-54-9F-45-CE-98-89-66-A9";

            bool verified = VerifyBench(n, checksum);
            return (verified ? 100 : -1);
        }
    }
}
