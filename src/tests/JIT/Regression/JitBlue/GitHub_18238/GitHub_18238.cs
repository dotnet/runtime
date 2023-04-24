// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Regression tests for bugs in fgMorphCast and optNarrowTree.

struct S0
{
    public byte F0;
    public sbyte F1;
    public sbyte F2;
    public S0 (byte f0, sbyte f1, sbyte f2)
    {
        F0 = f0;
        F1 = f1;
        F2 = f2;
    }
}

struct S1
{
    public bool F0;
    public short F1;
    public S1 (short f1) : this ()
    {
        F1 = f1;
        F0 = true;
    }
}

public static class GitHub_18238
{
    [Fact]
    public static int TestEntryPoint()
    {
        bool passed = true;

        ulong result1 = Test1.Run();
        ulong expectedResult1 = 64537;
        if (result1 != expectedResult1)
        {
            passed = false;
            Console.WriteLine(String.Format("Failed Test1: expected = {0}, actual = {1}", expectedResult1, result1));
        }

        S0 vr45 = new S0(0, 0, 0);
        int result2 = Test2.Run(vr45);
        int expectedResult2 = 65487;
        if (result2 != expectedResult2) {
            passed = false;
            Console.WriteLine(String.Format("Failed Test2: expected = {0}, actual = {1}", expectedResult2, result2));
        }

        int result3 = Test3.Run();
        int expectedResult3 = 65535;
        if (result3 != expectedResult3) {
            passed = false;
            Console.WriteLine(String.Format("Failed Test3: expected = {0}, actual = {1}", expectedResult3, result3));
        }

        uint result4 = Test4.Run ();
        uint expectedResult4 = 32779;
        if (result4 != expectedResult4) {
            passed = false;
            Console.WriteLine (String.Format ("Failed Test4: expected = {0}, actual = {1}", expectedResult4, result4));
        }

        if (passed)
        {
            Console.WriteLine("PASSED");
            return 100;
        }
        else
        {
            Console.WriteLine("FAILED");
            return -1;
        }
    }
}

static class Test1
{
    static short s_2 = -1000;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ulong Run()
    {
        ulong var1 = (ushort)(1U ^ s_2);
        return var1;
    }
}

static class Test2
{
    static bool [] [] s_9 = new bool [] [] { new bool [] { true } };

    [MethodImpl (MethodImplOptions.NoInlining)]
    public static int Run(S0 arg1)
    {
        arg1 = new S0 (0, -50, 0);
        char var0 = (char)(1U ^ arg1.F1);
        return s_9 [0] [0] ? (int)var0 : 0;
    }
}

static class Test3
{
    static int s_1 = 0xff;

    [MethodImpl (MethodImplOptions.NoInlining)]
    public static int Run()
    {
        int vr14 = (ushort)(sbyte)s_1;
        return (vr14);
    }
}

static class Test4
{
    static S1 s_1;

    [MethodImpl (MethodImplOptions.NoInlining)]
    public static uint Run()
    {
        char var0 = default(char);
        s_1 = new S1(-32767);
        var vr6 = s_1.F0;
        uint result = Run1(var0, 0, (ushort)(10L | s_1.F1), vr6, s_1.F1);
        return result;
    }


    [MethodImpl (MethodImplOptions.NoInlining)]
    static uint Run1(char arg0, long arg1, uint arg2, bool arg3, short arg4)
    {
        return arg2;
    }
}

