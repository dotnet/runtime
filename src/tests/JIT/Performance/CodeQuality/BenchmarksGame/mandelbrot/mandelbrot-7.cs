// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Adapted from mandelbrot C# .NET Core #7 program
// http://benchmarksgame.alioth.debian.org/u64q/program.php?test=mandelbrot&lang=csharpcore&id=7
// aka (as of 2017-10-02) rev 1.2 of https://alioth.debian.org/scm/viewvc.php/benchmarksgame/bench/mandelbrot/mandelbrot.csharp-7.csharp?root=benchmarksgame&view=log
// Best-scoring C# .NET Core version as of 2017-10-02

/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   started with Java #2 program (Krause/Whipkey/Bennet/AhnTran/Enotus/Stalcup)
   adapted for C# by Jan de Vaan
   simplified and optimised to use TPL by Anthony Lloyd
   simplified to compute Cib alongside Crb by Tanner Gooding
   optimized to use Vector<double> by Tanner Gooding
*/

using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Xunit.Performance;
using Xunit;

[assembly: OptimizeForBenchmarks]

namespace BenchmarksGame
{
    public class MandelBrot_7
    {
        // Vector<double>.Count is treated as a constant by the JIT, don't bother
        // storing it in a temporary variable anywhere below.

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe byte GetByte(double* pCrb, double Ciby, int x, int y)
        {
            // This currently does not do anything special for 'Count > 2'

            var res = 0;

            for (var i = 0; i < 8; i += 2)
            {
                var Crbx = Unsafe.Read<Vector<double>>(pCrb + x + i);
                var Zr = Crbx;
                var vCiby = new Vector<double>(Ciby);
                var Zi = vCiby;

                var b = 0;
                var j = 49;

                do
                {
                    var nZr = Zr * Zr - Zi * Zi + Crbx;
                    Zi = Zr * Zi + Zr * Zi + vCiby;
                    Zr = nZr;

                    var t = Zr * Zr + Zi * Zi;

                    if (t[0] > 4)
                    {
                        b |= 2;

                        if (b == 3)
                        {
                            break;
                        }
                    }

                    if (t[1] > 4)
                    {
                        b |= 1;

                        if (b == 3)
                        {
                            break;
                        }
                    }

                } while (--j > 0);

                res = (res << 2) + b;
            }

            return (byte)(res ^ -1);
        }

        public static int Main(string[] args)
        {
            var size = (args.Length > 0) ? int.Parse(args[0]) : 80;
            var lineLength = size >> 3;

            var data = DoBench(size, lineLength);
            var dataLength = size * lineLength;

            Console.Out.Write("P4\n{0} {0}\n", size);
            Console.OpenStandardOutput().Write(data, 0, dataLength);

            return MatchesChecksum(data, dataLength, "3B-EF-65-05-1D-39-7F-9B-96-8D-EF-98-BF-06-CE-74") ? 100 : -1;
        }

        // Commented out data left in source to provide checksums for each case

        [Benchmark(InnerIterationCount = 7)]
        //[InlineData(1000, 125, "B2-13-51-CE-B0-29-2C-4E-75-5E-91-19-18-E4-0C-D9")]
        //[InlineData(2000, 250, "5A-21-55-9B-7B-18-2F-34-9B-33-C5-F9-B5-2C-40-56")]
        //[InlineData(3000, 375, "E5-82-85-0A-3C-89-69-B1-A8-21-63-52-75-B3-C8-33")]
        [InlineData(4000, 500, "C7-E6-66-43-66-73-F8-A8-D3-B4-D7-97-2F-FC-A1-D3")]
        //[InlineData(5000, 625, "6D-36-F1-F6-37-8F-34-EB-52-F9-2D-11-89-12-B2-2F")]
        //[InlineData(6000, 750, "8B-05-78-EB-2E-0E-98-F2-C7-39-76-ED-0F-A9-D2-B8")]
        //[InlineData(7000, 875, "01-F8-F2-2A-AB-70-C7-BA-E3-64-19-E7-D2-84-DF-57")]
        //[InlineData(8000, 1000, "C8-ED-D7-FB-65-66-3A-D9-C6-04-9E-96-E8-CA-4F-2C")]
        public static void Bench(int size, int lineLength, string checksum)
        {
            byte[] bytes = null;

            Benchmark.Iterate(() => {
                bytes = DoBench(size, lineLength);
            });

            Assert.True(MatchesChecksum(bytes, size * lineLength, checksum));
        }

        static bool MatchesChecksum(byte[] bytes, int length, string checksum)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(bytes, 0, length);
                return (checksum == BitConverter.ToString(hash));
            }
        }

        static unsafe byte[] DoBench(int size, int lineLength)
        {
            var adjustedSize = size + (Vector<double>.Count * 8);
            adjustedSize &= ~(Vector<double>.Count * 8);

            var Crb = new double[adjustedSize];
            var Cib = new double[adjustedSize];

            fixed (double* pCrb = &Crb[0])
            fixed (double* pCib = &Cib[0])
            {
                var invN = new Vector<double>(2.0 / size);

                var onePtFive = new Vector<double>(1.5);
                var step = new Vector<double>(Vector<double>.Count);

                Vector<double> value;

                if (Vector<double>.Count == 2)
                {
                    // Software implementation should hit this path too

                    value = new Vector<double>(new double[] {
                        0, 1
                    });
                }
                else if (Vector<double>.Count == 4)
                {
                    value = new Vector<double>(new double[] {
                        0, 1, 2, 3
                    });
                }
                else
                {
                    // No hardware supports about 'Count == 8' today

                    value = new Vector<double>(new double[] {
                        0, 1, 2, 3, 4, 5, 6, 7
                    });
                }

                for (var i = 0; i < size; i += Vector<double>.Count)
                {
                    var t = value * invN;

                    Unsafe.Write(pCrb + i, t - onePtFive);
                    Unsafe.Write(pCib + i, t - Vector<double>.One);

                    value += step;
                }
            }

            var data = new byte[adjustedSize * lineLength];

            fixed (double* pCrb = &Crb[0])
            {
                // C# doesn't let us pass a pinned variable to a lambda directly
                var _Crb = pCrb;

                Parallel.For(0, size, y => {
                    var offset = y * lineLength;

                    for (var x = 0; x < lineLength; x++)
                    {
                        data[offset + x] = GetByte(_Crb, Cib[y], x * 8, y);
                    }
                });
            }

            return data;
        }
    }
}
