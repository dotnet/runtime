// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Note: In this repro, we accidently mark a non-EH variable as needing a spill which
// corrupts the value stored on stack. The problem repros only on linux/arm64.
using Xunit;
class C0
{
    public short F0;
    public byte F1;
    public ushort F2;
    public ulong F3;
}

struct S0
{
    public C0 F0;
    public uint F1;
    public S0(C0 f0) : this()
    {
        F0 = f0;
    }
}

class C1
{
    public ulong F0;
    public bool F1;
    public sbyte F2;
    public sbyte F3;
    public C0 F4;
    public sbyte F5;
    public uint F6;
    public C1(C0 f4, bool f7)
    {
        F4 = f4;
    }
}

class C2
{
    public ulong F0;
}

class C3
{
    public int F0;
    public C3(int f0)
    {
    }
}

public class Program
{
    internal static bool[][][] s_9 = new bool[][][] { new bool[][] { new bool[] { true } } };
    internal static C1[,] s_12 = new C1[,] { { new C1(new C0(), true) } };
    internal static S0 s_17 = new S0(new C0());
    internal static C1[,][] s_21 = new C1[,][] { { new C1[] { new C1(new C0(), false) } } };
    internal static short s_32;
    internal static C1 s_34 = new C1(new C0(), false);
    internal static C2 s_35 = new C2();
    internal static C1 s_114 = new C1(new C0(), false);
    internal static ushort[][] s_133 = new ushort[][] { new ushort[] { 0 }, new ushort[] { 0 }, new ushort[] { 0 }, new ushort[] { 1, 1, 1 }, new ushort[] { 0 }, new ushort[] { 0 }, new ushort[] { 0 }, new ushort[] { 0 }, new ushort[] { 0 } };
    internal static long[] s_138 = new long[] { 0 };

    [Fact]
    public static int TestEntryPoint()
    {
        s_32 = s_32;
        M64(new C3(0));
        return 100;
    }

    internal static S0 M64(C3 argThis)
    {
        if (argThis.F0 <= 0)
        {
            short var1 = s_32;
            try
            {
                var vr8 = new bool[][] { new bool[] { true }, new bool[] { false }, new bool[] { false }, new bool[] { true }, new bool[] { false } };
            }
            finally
            {
                ushort var13 = s_12[0, 0].F4.F2;
            }

            System.GC.KeepAlive(var1);
        }

        long[][] var16 = new long[][] { new long[] { -1, 0, 0, -1, -1, 0, 0, 0, 0 }, new long[] { 1, 0, -1, 0, 0, 0, 0, 0, 1, 0 }, new long[] { 1, 0, 0, 1, -1, 0 }, new long[] { 1, 1, -1, 0 } };
        C2 var17 = s_35;
        S0 var18 = s_17;
        var vr6 = new bool[][] { new bool[] { true, true, true, false, false, false, false, false, true }, new bool[] { false, false, true, false, true, false }, new bool[] { true }, new bool[] { true }, new bool[] { false }, new bool[] { false, false, true }, new bool[] { true }, new bool[] { false, true, false } };
        if (0 >= var17.F0)
        {
            var18.F0.F1 = 1;
        }

        var18.F0 = new C0();
        C1[] var22 = new C1[] { new C1(new C0(), false) };
        C3[][][] var24;
        System.GC.KeepAlive(argThis.F0);
        System.GC.KeepAlive(var18.F0.F2);
        System.GC.KeepAlive(var18.F0.F3);
        System.GC.KeepAlive(10);
        System.GC.KeepAlive(var22[0].F0);
        System.GC.KeepAlive(var22[0].F4.F3);
        System.GC.KeepAlive(var22[0].F5);
        return var18;
    }
}