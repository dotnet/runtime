// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class StaticReadonlyStructWithGC
{
    [Fact]
    public static int TestEntryPoint()
    {
        // Pre-initialize host type
        RuntimeHelpers.RunClassConstructor(typeof(StaticReadonlyStructWithGC).TypeHandle);

        if (!Test1()) throw new Exception("Test1 failed");
        if (!Test2()) throw new Exception("Test2 failed");
        if (!Test3()) throw new Exception("Test3 failed");
        if (!Test4()) throw new Exception("Test4 failed");
        if (!Test5()) throw new Exception("Test5 failed");
        if (!Test6()) throw new Exception("Test6 failed");
        if (!Test7()) throw new Exception("Test7 failed");
        if (!Test8()) throw new Exception("Test8 failed");
        if (!Test9()) throw new Exception("Test9 failed");
        return 100;
    }

    static readonly MyStruct MyStructFld = new()
    {
        A = "A",
        B = 111111.ToString(), // non-literal
        C = new MyStruct2 { A = "AA" },
        D = typeof(int),
        E = () => 42,
        F = new MyStruct3 { A = typeof(double), B = typeof(string) },
        G = new int[0],
        H = null
    };

    [MethodImpl(MethodImplOptions.NoInlining)] static bool Test1() => MyStructFld.A == "A";
    [MethodImpl(MethodImplOptions.NoInlining)] static bool Test2() => MyStructFld.B == "111111";
    [MethodImpl(MethodImplOptions.NoInlining)] static bool Test3() => MyStructFld.C.A == "AA";
    [MethodImpl(MethodImplOptions.NoInlining)] static bool Test4() => MyStructFld.D == typeof(int);
    [MethodImpl(MethodImplOptions.NoInlining)] static bool Test5() => MyStructFld.E() == 42;
    [MethodImpl(MethodImplOptions.NoInlining)] static bool Test6() => MyStructFld.F.A == typeof(double);
    [MethodImpl(MethodImplOptions.NoInlining)] static bool Test7() => MyStructFld.F.B == typeof(string);
    [MethodImpl(MethodImplOptions.NoInlining)] static bool Test8() => MyStructFld.G.Length == 0;
    [MethodImpl(MethodImplOptions.NoInlining)] static bool Test9() => MyStructFld.H == null;

    struct MyStruct
    {
        public string A;
        public string B;
        public MyStruct2 C;
        public Type D;
        public Func<int> E;
        public MyStruct3 F;
        public int[] G;
        public object H;
    }

    struct MyStruct2
    {
        public string A;
    }

    struct MyStruct3
    {
        public Type A;
        public Type B;
    }
}
