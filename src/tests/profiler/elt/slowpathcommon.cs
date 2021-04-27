// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace SlowPathELTTests
{
    [StructLayout(LayoutKind.Sequential)]
    public struct IntegerStruct
    {
        public int x;
        public int y;

        public IntegerStruct(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override String ToString()
        {
            return $"x={x} y={y}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FloatingPointStruct
    {
        public double d1;
        public double d2;

        public FloatingPointStruct(double d1, double d2)
        {
            this.d1 = d1;
            this.d2 = d2;
        }

        public override String ToString()
        {
            return $"d1={d1} d2={d2}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MixedStruct
    {
        public int x;
        public double d;

        public MixedStruct(int x, double d)
        {
            this.x = x;
            this.d = d;
        }

        public override String ToString()
        {
            return $"x={x} d={d}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LargeStruct
    {
        public int x0;
        public double d0;
        public int x1;
        public double d1;
        public int x2;
        public double d2;
        public int x3;
        public double d3;

        public LargeStruct(int x0, 
                           double d0,
                           int x1,
                           double d1,
                           int x2,
                           double d2,
                           int x3,
                           double d3)
        {
            this. x0 = x0;
            this.d0 = d0;
            this.x1 = x1;
            this.d1 = d1;
            this.x2 = x2;
            this.d2 = d2;
            this.x3 = x3;
            this.d3 = d3;
        }

        public override String ToString()
        {
            return $"x0={x0} d0={d0} x1={x1} d1={d1} x2={x2} d2={d2} x3={x3} d3={d3}";
        }
    }

    public class SlowPathELTHelpers
    {
        public static int RunTest()
        {
            Console.WriteLine($"SimpleArgsFunc returned {SimpleArgsFunc(-123, -4.3f, "Hello, test!")}");

            Console.WriteLine($"MixedStructFunc returned {MixedStructFunc(new MixedStruct(1, 1))}");

            Console.WriteLine($"LargeStructFunc returned {LargeStructFunc(new LargeStruct(0, 0, 1, 1, 2, 2, 3, 3))}");

            Console.WriteLine($"IntegerStructFunc returned {IntegerStructFunc(new IntegerStruct(14, 256))}");

            Console.WriteLine($"FloatingPointStructFunc returned {FloatingPointStructFunc(new FloatingPointStruct(13.0, 145.2))}");

            Console.WriteLine($"DoubleRetFunc returned {DoubleRetFunc()}");

            return 100;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static string SimpleArgsFunc(int x, float y, String str)
        {
            Console.WriteLine($"x={x} y={y} str={str}");
            return "Hello from SimpleArgsFunc!";
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static MixedStruct MixedStructFunc(MixedStruct ss)
        {
            Console.WriteLine($"ss={ss}");
            ss.x = 4;
            return ss;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static int LargeStructFunc(LargeStruct ls)
        {
            Console.WriteLine($"ls={ls}");
            return 3;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static IntegerStruct IntegerStructFunc(IntegerStruct its)
        {
            its.x = 21;
            return its;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static FloatingPointStruct FloatingPointStructFunc(FloatingPointStruct fps)
        {
            fps.d2 = 256.8;
            return fps;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static double DoubleRetFunc()
        {
            return 13.0;
        }
    }
}
