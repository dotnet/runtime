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
    public struct Fp32x2Struct
    {
        public float x;
        public float y;

        public Fp32x2Struct(float x, float y)
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
    public struct Fp32x3Struct
    {
        public float x;
        public float y;
        public float z;

        public Fp32x3Struct(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public override String ToString()
        {
            return $"x={x} y={y} z={z}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Fp32x4Struct
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public Fp32x4Struct(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public override String ToString()
        {
            return $"x={x} y={y} z={z} w={w}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Fp64x2Struct
    {
        public double x;
        public double y;

        public Fp64x2Struct(double x, double y)
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
    public struct Fp64x3Struct
    {
        public double x;
        public double y;
        public double z;

        public Fp64x3Struct(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public override String ToString()
        {
            return $"x={x} y={y} z={z}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Fp64x4Struct
    {
        public double x;
        public double y;
        public double z;
        public double w;

        public Fp64x4Struct(double x, double y, double z, double w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public override String ToString()
        {
            return $"x={x} y={y} z={z} w={w}";
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

            Console.WriteLine($"Fp32x2StructFunc returned {Fp32x2StructFunc(new Fp32x2Struct(13.0f, 145.2f))}");

            Console.WriteLine($"Fp32x3StructFunc returned {Fp32x3StructFunc(new Fp32x3Struct(13.0f, 145.2f, 321.98f))}");

            Console.WriteLine($"Fp32x4StructFunc returned {Fp32x4StructFunc(new Fp32x4Struct(13.0f, 145.2f, 321.98f, 27.03f))}");

            Console.WriteLine($"Fp64x2StructFunc returned {Fp64x2StructFunc(new Fp64x2Struct(13.0, 145.2))}");

            Console.WriteLine($"Fp64x3StructFunc returned {Fp64x3StructFunc(new Fp64x3Struct(13.0, 145.2, 321.98))}");

            Console.WriteLine($"Fp64x4StructFunc returned {Fp64x4StructFunc(new Fp64x4Struct(13.0, 145.2, 321.98, 27.03))}");

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
        public static Fp32x2Struct Fp32x2StructFunc(Fp32x2Struct fps)
        {
            fps.y = 256.8f;
            return fps;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Fp32x3Struct Fp32x3StructFunc(Fp32x3Struct fps)
        {
            fps.z = 256.8f;
            return fps;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Fp32x4Struct Fp32x4StructFunc(Fp32x4Struct fps)
        {
            fps.w = 256.8f;
            return fps;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Fp64x2Struct Fp64x2StructFunc(Fp64x2Struct fps)
        {
            fps.y = 256.8;
            return fps;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Fp64x3Struct Fp64x3StructFunc(Fp64x3Struct fps)
        {
            fps.z = 256.8;
            return fps;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Fp64x4Struct Fp64x4StructFunc(Fp64x4Struct fps)
        {
            fps.w = 256.8;
            return fps;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static double DoubleRetFunc()
        {
            return 13.0;
        }
    }
}
