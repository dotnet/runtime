// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        return (Test1.Run() & Test2.Run() & Test3.Run()) ? 100 : -1;
    }
}

class Test1
{
    class C0
    {
        public sbyte F0;
        public ushort F7;
        public uint F8;
    }

    static C0 s_1;

    public static bool Run()
    {
        s_1 = new C0();
        try
        {
            M0();
            return true;
        }
        catch (NullReferenceException)
        {
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void M0()
    {
#pragma warning disable 1717, 1718
        bool vr0 = (s_1.F7 < s_1.F0) ^ (s_1.F8 != s_1.F8);
        if (vr0)
        {
            s_1.F7 = s_1.F7;
        }
    }
}

class Test2
{
    class C0
    {
        public long F0;
        public C0(long f0)
        {
            F0 = f0;
        }
    }

    static char s_1;
    static ulong s_2 = 1;
    static int s_6;
    static C0 s_7;
    static C0 s_9 = new C0(-1L);

    public static bool Run()
    {
        s_6 = 0;
        M1();
        return s_7.F0 == 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void M1()
    {
        long vr1 = ((0 & (M2() + s_9.F0)) | s_1) / (long)s_2;
        s_7 = s_9;
    }

    static int M2()
    {
        s_9 = new C0(0);
        return s_6;
    }
}

class Test3
{
    public static bool Run()
    {
        try
        {
            M5(new ulong[0]);
            return false;
        }
        catch (IndexOutOfRangeException)
        {
            return true;
        }
        catch (DivideByZeroException)
        {
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte M5(ulong[] arg0)
    {
        int var0 = (ushort)((0 % arg0[0]) | (byte)(-32768 * (int)(0 & arg0[0])));
        return (byte)var0;
    }
}
