// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Adapted from mandelbrot C# .NET Core #4 program
// http://benchmarksgame.alioth.debian.org/u64q/program.php?test=mandelbrot&lang=csharpcore&id=4
// Best-scoring C# .NET Core version as of 2017-09-01

/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
    
   started with Java #2 program (Krause/Whipkey/Bennet/AhnTran/Enotus/Stalcup)
   adapted for C# by Jan de Vaan
   simplified and optimised to use TPL by Anthony Lloyd
*/

using System;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.CompilerServices;

namespace BenchmarksGame
{
    public class MandelBrot
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

                    if (Zr1 * Zr1 + Zi1 * Zi1 > 4) { b |= 2; if (b == 3) break; }
                    if (Zr2 * Zr2 + Zi2 * Zi2 > 4) { b |= 1; if (b == 3) break; }
                } while (--j > 0);
                res = (res << 2) + b;
            }
            return (byte)(res ^ -1);
        }

        public static void Main(String[] args)
        {
            var n = args.Length > 0 ? Int32.Parse(args[0]) : 200;
            double invN = 2.0 / n;
            var Crb = new double[n + 7];
            for (int i = 0; i < n; i++) { Crb[i] = i * invN - 1.5; }
            int lineLen = (n - 1) / 8 + 1;
            var data = new byte[n * lineLen];
            Parallel.For(0, n, y =>
            {
                var Ciby = y * invN - 1.0;
                var offset = y * lineLen;
                for (int x = 0; x < lineLen; x++)
                    data[offset + x] = getByte(Crb, Ciby, x * 8, y);
            });
            Console.Out.WriteLine("P4\n{0} {0}", n);
            Console.OpenStandardOutput().Write(data, 0, data.Length);
        }
    }
}
