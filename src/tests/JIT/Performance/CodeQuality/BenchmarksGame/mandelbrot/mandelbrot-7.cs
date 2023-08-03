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
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;

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

        [Fact]
        public static int TestEntryPoint()
        {
            return Test(null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Test(int? arg)
        {
            int size = arg ?? 80;
            var lineLength = size >> 3;

            var data = DoBench(size, lineLength);
            var dataLength = size * lineLength;

            Console.Out.Write("P4\n{0} {0}\n", size);
            Console.OpenStandardOutput().Write(data, 0, dataLength);

            return MatchesChecksum(data, dataLength, "3B-EF-65-05-1D-39-7F-9B-96-8D-EF-98-BF-06-CE-74") ? 100 : -1;
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
