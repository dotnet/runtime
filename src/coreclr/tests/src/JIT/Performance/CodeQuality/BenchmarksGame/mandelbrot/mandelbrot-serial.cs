// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Adapted from mandelbrot C# .NET Core #2 program
// http://benchmarksgame.alioth.debian.org/u64q/program.php?test=mandelbrot&lang=csharpcore&id=2
// Best-scoring single-threaded C# .NET Core version as of 2017-09-01

/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/
 *
 * Adapted by Antti Lankila from the earlier Isaac Gouy's implementation
 */

using System;
using System.IO;

namespace BenchmarksGame
{
    class Mandelbrot
    {

        public static void Main(String[] args)
        {

            int width = 100;
            if (args.Length > 0)
                width = Int32.Parse(args[0]);

            int height = width;
            int maxiter = 50;
            double limit = 4.0;

            Console.WriteLine("P4");
            Console.WriteLine("{0} {1}", width, height);
            Stream s = Console.OpenStandardOutput(1024);

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
        }
    }
}
