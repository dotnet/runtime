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

    // The following structs are to test correctness of how ProfileArgIterator works with
    // multi-reg return values when running on a System V ABI OS (e.g. Linux and macOS x64).
    // A struct up to 16 bytes (inclusive) can be returned in (rax, rdx), (rax, xmm0), (xmm0, rax), (xmm0, xmm1).
    // We already have a comprehensive set of Fp{n}x{m}Struct above that covers the last variant,
    // so there is no need to duplicate them here.

    [StructLayout(LayoutKind.Sequential)]
    public struct IntegerSseStruct
    {
        public int x;
        public int y;
        public double z;

        public IntegerSseStruct(int x, int y, double z)
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
    public struct SseIntegerStruct
    {
        public float x;
        public float y;
        public int z;

        public SseIntegerStruct(float x, float y, int z)
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
    public struct MixedSseStruct
    {
        public float x;
        public int y;
        public float z;
        public float w;

        public MixedSseStruct(float x, int y, float z, float w)
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
    public struct SseMixedStruct
    {
        public float x;
        public float y;
        public int z;
        public float w;

        public SseMixedStruct(float x, float y, int z, float w)
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
    public struct MixedMixedStruct
    {
        public float x;
        public int y;
        public int z;
        public float w;

        public MixedMixedStruct(float x, int y, int z, float w)
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

    public class SlowPathELTHelpers
    {
        public static int RunTest()
        {
            Console.WriteLine($"SimpleArgsFunc returned {SimpleArgsFunc(-123, -4.3f, "Hello, test!")}");

            Console.WriteLine($"MixedStructFunc returned {MixedStructFunc(new MixedStruct(1, 1))}");

            Console.WriteLine($"LargeStructFunc returned {LargeStructFunc(new LargeStruct(0, 0, 1, 1, 2, 2, 3, 3))}");

            Console.WriteLine($"IntegerStructFunc returned {IntegerStructFunc(new IntegerStruct(14, 256))}");

            var fp32x2 = new Fp32x2Struct(1.2f, 3.5f);
            var fp32x3 = new Fp32x3Struct(6.7f, 10.11f, 13.14f);
            var fp32x4 = new Fp32x4Struct(15.17f, 19.21f, 22.23f, 26.29f);

            Console.WriteLine($"Fp32x2StructFunc returned {Fp32x2StructFunc(fp32x2)}");

            Console.WriteLine($"Fp32x2StructFp32x3StructFunc returned {Fp32x2StructFp32x3StructFunc(fp32x2, fp32x3)}");

            Console.WriteLine($"Fp32x3StructFunc returned {Fp32x3StructFunc(fp32x3)}");

            Console.WriteLine($"Fp32x3StructFp32x2StructFunc returned {Fp32x3StructFp32x2StructFunc(fp32x3, fp32x2)}");

            Console.WriteLine($"Fp32x3StructSingleFp32x3StructSingleFunc returned {Fp32x3StructSingleFp32x3StructSingleFunc(fp32x3, 1.2f, fp32x3, 3.5f)}");

            Console.WriteLine($"Fp32x4StructFunc returned {Fp32x4StructFunc(fp32x4)}");

            Console.WriteLine($"Fp32x4StructFp32x4StructFunc returned {Fp32x4StructFp32x4StructFunc(fp32x4, fp32x4)}");

            var fp64x2 = new Fp64x2Struct(1.2, 3.5);
            var fp64x3 = new Fp64x3Struct(6.7, 10.11, 13.14);
            var fp64x4 = new Fp64x4Struct(15.17, 19.21, 22.23, 26.29);

            Console.WriteLine($"Fp64x2StructFunc returned {Fp64x2StructFunc(fp64x2)}");

            Console.WriteLine($"Fp64x2StructFp64x3StructFunc returned {Fp64x2StructFp64x3StructFunc(fp64x2, fp64x3)}");

            Console.WriteLine($"Fp64x3StructFunc returned {Fp64x3StructFunc(fp64x3)}");

            Console.WriteLine($"Fp64x3StructFp64x2StructFunc returned {Fp64x3StructFp64x2StructFunc(fp64x3, fp64x2)}");

            Console.WriteLine($"Fp64x3StructDoubleFp64x3StructDoubleFunc returned {Fp64x3StructDoubleFp64x3StructDoubleFunc(fp64x3, 1.2, fp64x3, 3.5)}");

            Console.WriteLine($"Fp64x4StructFunc returned {Fp64x4StructFunc(fp64x4)}");

            Console.WriteLine($"Fp64x4StructFp64x4StructFunc returned {Fp64x4StructFp64x4StructFunc(fp64x4, fp64x4)}");

            Console.WriteLine($"DoubleRetFunc returned {DoubleRetFunc()}");

            Console.WriteLine($"FloatRetFunc returned {FloatRetFunc()}");

            Console.WriteLine($"IntegerSseStructFunc returned {IntegerSseStructFunc()}");

            Console.WriteLine($"SseIntegerStructFunc returned {SseIntegerStructFunc()}");

            Console.WriteLine($"MixedSseStructFunc returned {MixedSseStructFunc()}");

            Console.WriteLine($"SseMixedStructFunc returned {SseMixedStructFunc()}");

            Console.WriteLine($"MixedMixedStructFunc returned {MixedMixedStructFunc()}");

            var s1 = new MixedStruct(1, 1);
            var s2 = new MixedStruct(2, 2);
            var s3 = new MixedStruct(3, 3);
            var s4 = new MixedStruct(4, 4);
            var s5 = new MixedStruct(5, 5);
            var s6 = new MixedStruct(6, 6);
            var s7 = new MixedStruct(7, 7);
            var s8 = new MixedStruct(8, 8);
            var s9 = new MixedStruct(9, 9);
            Console.WriteLine($"IntManyMixedStructFunc returned {IntManyMixedStructFunc(11, s1, s2, s3, s4, s5, s6, s7, s8, s9)}");

            Console.WriteLine($"DoubleManyMixedStructFunc returned {DoubleManyMixedStructFunc(11.0, s1, s2, s3, s4, s5, s6, s7, s8, s9)}");

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
            fps.y = 2;
            return fps;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Fp32x3Struct Fp32x2StructFp32x3StructFunc(Fp32x2Struct fps1, Fp32x3Struct fps2)
        {
            return fps2;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Fp32x3Struct Fp32x3StructFunc(Fp32x3Struct fps)
        {
            fps.z = 3;
            return fps;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Fp32x2Struct Fp32x3StructFp32x2StructFunc(Fp32x3Struct fps1, Fp32x2Struct fps2)
        {
            return fps2;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Fp32x3Struct Fp32x3StructSingleFp32x3StructSingleFunc(Fp32x3Struct fps1, float flt1, Fp32x3Struct fps2, float flt2)
        {
            return fps2;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Fp32x4Struct Fp32x4StructFunc(Fp32x4Struct fps)
        {
            fps.w = 4;
            return fps;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Fp32x4Struct Fp32x4StructFp32x4StructFunc(Fp32x4Struct fps1, Fp32x4Struct fps2)
        {
            return fps2;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Fp32x2Struct Fp32x4StructFp32x4StructFunc(Fp32x3Struct fps1, Fp32x3Struct fps2, Fp32x2Struct fps3)
        {
            return fps3;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Fp64x2Struct Fp64x2StructFunc(Fp64x2Struct fps)
        {
            fps.y = 2;
            return fps;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Fp64x3Struct Fp64x2StructFp64x3StructFunc(Fp64x2Struct fps1, Fp64x3Struct fps2)
        {
            return fps2;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Fp64x3Struct Fp64x3StructFunc(Fp64x3Struct fps)
        {
            fps.z = 3;
            return fps;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Fp64x2Struct Fp64x3StructFp64x2StructFunc(Fp64x3Struct fps1, Fp64x2Struct fps2)
        {
            return fps2;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Fp64x3Struct Fp64x3StructDoubleFp64x3StructDoubleFunc(Fp64x3Struct fps1, double dbl1, Fp64x3Struct fps2, double dbl2)
        {
            return fps2;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Fp64x4Struct Fp64x4StructFunc(Fp64x4Struct fps)
        {
            fps.w = 4;
            return fps;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Fp64x4Struct Fp64x4StructFp64x4StructFunc(Fp64x4Struct fps1, Fp64x4Struct fps2)
        {
            return fps2;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Fp64x2Struct Fp64x4StructFp64x4StructFunc(Fp64x3Struct fps1, Fp64x3Struct fps2, Fp64x2Struct fps3)
        {
            return fps3;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static double DoubleRetFunc()
        {
            return 13.0;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static float FloatRetFunc()
        {
            return 13.0f;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static IntegerSseStruct IntegerSseStructFunc()
        {
            return new IntegerSseStruct(1, 2, 3.5);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static SseIntegerStruct SseIntegerStructFunc()
        {
            return new SseIntegerStruct(1.2f, 3.5f, 6);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static MixedSseStruct MixedSseStructFunc()
        {
            return new MixedSseStruct(1.2f, 3, 5.6f, 7.10f);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static SseMixedStruct SseMixedStructFunc()
        {
            return new SseMixedStruct(1.2f, 3.5f, 6, 7.10f);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static MixedMixedStruct MixedMixedStructFunc()
        {
            return new MixedMixedStruct(1.2f, 3, 5, 6.7f);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static MixedStruct IntManyMixedStructFunc(int i, MixedStruct s1, MixedStruct s2, MixedStruct s3, MixedStruct s4, MixedStruct s5, MixedStruct s6, MixedStruct s7, MixedStruct s8, MixedStruct s9)
        {
            Console.WriteLine($"i={i} s1=({s1}) s2=({s2}) s3=({s3}) s4=({s4}) s5=({s5}) s6=({s6}) s7=({s7}) s8=({s8}) s9=({s9})");
            return s1;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static MixedStruct DoubleManyMixedStructFunc(double d, MixedStruct s1, MixedStruct s2, MixedStruct s3, MixedStruct s4, MixedStruct s5, MixedStruct s6, MixedStruct s7, MixedStruct s8, MixedStruct s9)
        {
            Console.WriteLine($"d={d} s1=({s1}) s2=({s2}) s3=({s3}) s4=({s4}) s5=({s5}) s6=({s6}) s7=({s7}) s8=({s8}) s9=({s9})");
            return s1;
        }
    }
}
