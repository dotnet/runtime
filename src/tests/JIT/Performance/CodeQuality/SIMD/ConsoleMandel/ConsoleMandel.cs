// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;

namespace SIMD
{
    public static class ConsoleMandel
    {
        private const int Pass = 100;
        private const int Fail = -1;
        private static bool s_silent = false;

        private static void DoNothing(int x, int y, int count) { }

        private static void DrawDot(int x, int y, int count)
        {
            if (x == 0)
                Console.WriteLine();
            Console.Write((count < 1000) ? ' ' : '*');
        }

        private static Algorithms.FractalRenderer.Render GetRenderer(Action<int, int, int> draw, int which)
        {
            return Algorithms.FractalRenderer.SelectRender(draw, Abort, IsVector(which), IsDouble(which), IsMulti(which), UsesADT(which), !UseIntTypes(which));
        }

        private static bool Abort() { return false; }

        private static bool UseIntTypes(int num) { return (num & 8) == 0; }

        private static bool IsVector(int num) { return num > 7; }

        private static bool IsDouble(int num) { return (num & 4) != 0; }

        private static bool IsMulti(int num) { return (num & 2) != 0; }

        private static bool UsesADT(int num) { return (num & 1) != 0; }

        private static void PrintDescription(int i)
        {
            Console.WriteLine("{0}: {1} {2}-Precision {3}Threaded using {4} and {5} int types", i,
                IsVector(i) ? "Vector" : "Scalar",
                IsDouble(i) ? "Double" : "Single",
                IsMulti(i) ? "Multi" : "Single",
                UsesADT(i) ? "ADT" : "Raw Values",
                UseIntTypes(i) ? "using" : "not using any");
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:\n    ConsoleMandel [0-23] -[bench #] where # is the number of iterations.");
            for (int i = 0; i < 24; i++)
            {
                PrintDescription(i);
            }
            Console.WriteLine("The numeric argument selects the implementation number;");
            Console.WriteLine("If not specified, all are run.");
            Console.WriteLine("In non-benchmark mode, dump a text view of the Mandelbrot set.");
            Console.WriteLine("In benchmark mode, a larger set is computed but nothing is dumped.");
        }

        [Fact]
        public static int TestEntryPoint()
        {
            return Test(Array.Empty<string>());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Test(string[] args)
        {
            try
            {
                int which = -1;
                bool verbose = false;
                bool bench = false;
                int iters = 1;
                int argNum = 0;
                while (argNum < args.Length)
                {
                    if (args[argNum].ToUpperInvariant() == "-BENCH")
                    {
                        bench = true;
                        if ((args.Length <= (argNum + 1)) || !Int32.TryParse(args[argNum + 1], out iters))
                        {
                            iters = 5;
                        }
                        argNum++;
                    }
                    else if (args[argNum].ToUpperInvariant() == "-V")
                    {
                        verbose = true;
                    }
                    else if (args[argNum].ToUpperInvariant() == "-S")
                    {
                        s_silent = true;
                    }
                    else if (!Int32.TryParse(args[argNum], out which))
                    {
                        PrintUsage();
                        return Fail;
                    }
                    argNum++;
                }
                if (bench)
                {
                    Bench(iters, which);
                    return Pass;
                }
                if (which == -1)
                {
                    PrintUsage();
                    return Pass;
                }
                if (verbose)
                {
                    PrintDescription(which);
                }
                if (IsVector(which))
                {
                    if (verbose)
                    {
                        Console.WriteLine("  Vector Count is {0}", IsDouble(which) ? System.Numerics.Vector<Double>.Count : System.Numerics.Vector<Single>.Count);
                        Console.WriteLine("  {0} Accelerated.", System.Numerics.Vector.IsHardwareAccelerated ? "IS" : "IS NOT");
                    }
                }
                var render = GetRenderer(DrawDot, which);
                render(-1.5f, .5f, -1f, 1f, 2.0f / 60.0f);
                return Pass;
            }
            catch (System.Exception)
            {
                return Fail;
            }
        }

        public static void Bench(int iters, int which)
        {
            float XC = -1.248f;
            float YC = -.0362f;
            float Range = .001f;
            float xmin = XC - Range;
            float xmax = XC + Range;
            float ymin = YC - Range;
            float ymax = YC + Range;
            float step = Range / 1000f; // This will render one million pixels
            float warm = Range / 100f; // To warm up, just render 10000 pixels :-)
            Algorithms.FractalRenderer.Render[] renderers = new Algorithms.FractalRenderer.Render[24];
            // Warm up each renderer
            if (!s_silent)
            {
                Console.WriteLine("Warming up...");
            }
            Stopwatch timer = new Stopwatch();
            int firstRenderer = (which == -1) ? 0 : which;
            int lastRenderer = (which == -1) ? (renderers.Length - 1) : which;
            for (int i = firstRenderer; i <= lastRenderer; i++)
            {
                renderers[i] = GetRenderer(DoNothing, i);
                timer.Restart();
                renderers[i](xmin, xmax, ymin, ymax, warm);
                timer.Stop();
                if (!s_silent)
                {
                    Console.WriteLine("{0}{1}{2}{3}{4} Complete [{5} ms]",
                        UseIntTypes(i) ? "IntBV  " : "Strict ",
                        IsVector(i) ? "Vector " : "Scalar ",
                        IsDouble(i) ? "Double " : "Single ",
                        UsesADT(i) ? "ADT " : "Raw ",
                        IsMulti(i) ? "Multi  " : "Single ",
                        timer.ElapsedMilliseconds);
                }
            }
            if (!s_silent)
            {
                Console.WriteLine(" Run Type                       :      Min      Max    Average    Std-Dev");
            }
            for (int i = firstRenderer; i <= lastRenderer; i++)
            {
                long totalTime = 0;
                long min = long.MaxValue;
                long max = long.MinValue;
                for (int count = 0; count < iters; count++)
                {
                    timer.Restart();
                    renderers[i](xmin, xmax, ymin, ymax, step);
                    timer.Stop();
                    long time = timer.ElapsedMilliseconds;
                    max = Math.Max(time, max);
                    min = Math.Min(time, min);
                    totalTime += time;
                }
                double avg = totalTime / (double)iters;
                double stdDev = Math.Sqrt(totalTime / (iters - 1.0)) / avg;
                if (s_silent)
                {
                    Console.WriteLine("Average: {0,0:0.0}", avg);
                }
                else
                {
                    Console.WriteLine("{0}{1}{2}{3}{4}: {5,8} {6,8} {7,10:0.0} {8,10:P}",
                        UseIntTypes(i) ? "IntBV  " : "Strict ",
                        IsVector(i) ? "Vector " : "Scalar ",
                        IsDouble(i) ? "Double " : "Single ",
                        UsesADT(i) ? "ADT " : "Raw ",
                        IsMulti(i) ? "Multi  " : "Single ",
                        min, max, avg, stdDev);
                }
            }
        }
    }
}
