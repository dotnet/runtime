// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public interface ITest
{
    public int VirtualMethod();
}

public class BaseClass : ITest
{
    public int NonVirtualMethod()
    {
        return 0xbaba;
    }

    public virtual int VirtualMethod()
    {
        return 0xbebe;
    }
}

public class DerivedClass : BaseClass
{
    public override int VirtualMethod()
    {
        return 0xdede;
    }

}

public struct MyStruct
{
    public int a;

    public MyStruct(int val)
    {
        a = val;
    }
}

public class MyObj
{
    public int ct;
    public MyStruct str;

    public MyObj(int val)
    {
        str = new MyStruct(val);
        ct = 10;
    }
}

public struct MyStruct2
{
    public int ct;
    public MyStruct str;

    public MyStruct2(int val)
    {
        str = new MyStruct(val);
        ct = 20;
    }
}

public class InterpreterTest
{
    static int Main(string[] args)
    {
        jitField1 = 42;
        jitField2 = 43;
        RunInterpreterTests();
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void RunInterpreterTests()
    {
        // Console.WriteLine("Run interp tests");
        if (SumN(50) != 1275)
            Environment.FailFast(null);
        if (Mul4(53, 24, 13, 131) != 2166216)
            Environment.FailFast(null);

        TestSwitch();

        if (!PowLoop(20, 10, 1661992960))
            Environment.FailFast(null);

        if (!TestJitFields())
            Environment.FailFast(null);
        // Disable below tests because they are potentially unstable since they do allocation
        // and we currently don't have GC support. They should pass locally though.
//        if (!TestFields())
//            Environment.FailFast(null);
//        if (!TestSpecialFields())
//            Environment.FailFast(null);
        if (!TestFloat())
            Environment.FailFast(null);

        if (!TestLocalloc())
            Environment.FailFast(null);

//        if (!TestVirtual())
//          Environment.FailFast(null);

        // For stackwalking validation
        System.GC.Collect();
    }

    public static int Mul4(int a, int b, int c, int d)
    {
        return a * b * c * d;
    }

    public static long SumN(int n)
    {
        if (n == 1)
            return 1;
        return (long)SumN(n - 1) + n;
    }

    public static int SwitchOp(int a, int b, int op)
    {
        switch (op)
        {
            case 0:
                return a + b;
            case 1:
                return a - b;
            case 2:
                return a * b;
            default:
               return 42;
        }
    }

    public static void TestSwitch()
    {
        int n0 = SwitchOp (20, 6, 0); // 26
        int n1 = SwitchOp (20, 6, 1); // 14
        int n2 = SwitchOp (20, 6, 2); // 120
        int n3 = SwitchOp (20, 6, 3); // 42

        if ((n0 + n1 + n2 + n3) != 202)
            Environment.FailFast(null);
    }

    public static bool PowLoop(int n, long nr, int expected)
    {
        long ret = 1;
        for (int i = 0; i < n; i++)
            ret *= nr;
        return (int)ret == expected;
    }

    public static int jitField1;
    [ThreadStatic]
    public static int jitField2;

    public static bool TestJitFields()
    {
        // These fields are initialized by the JIT
        // Test that interpreter accesses the correct address
        if (jitField1 != 42)
            return false;
        if (jitField2 != 43)
            return false;
        return true;
    }

    public static MyObj staticObj;
    public static MyStruct2 staticStr;

    public static void WriteInt(ref int a, int ct)
    {
        a = ct;
    }

    public static int ReadInt(ref int a)
    {
        return a;
    }

    public static bool TestFields()
    {
        MyObj obj = new MyObj(1);
        MyStruct2 str = new MyStruct2(2);

        int sum = obj.str.a + str.str.a + obj.ct + str.ct;
        if (sum != 33)
            return false;

        staticObj = obj;
        staticStr = str;

        sum = staticObj.str.a + staticStr.str.a + staticObj.ct + staticStr.ct;
        if (sum != 33)
            return false;

        WriteInt(ref str.str.a, 11);
        WriteInt(ref staticObj.str.a, 22);
        sum = ReadInt(ref str.str.a) + ReadInt(ref staticObj.str.a);
        if (sum != 33)
            return false;

        return true;
    }

    [ThreadStatic]
    public static MyObj threadStaticObj;
    [ThreadStatic]
    public static MyStruct2 threadStaticStr;

    public static bool TestSpecialFields()
    {
        threadStaticObj = new MyObj(1);
        threadStaticStr = new MyStruct2(2);

        int sum = threadStaticObj.str.a + threadStaticStr.str.a + threadStaticObj.ct + threadStaticStr.ct;
        if (sum != 33)
            return false;

        return true;
    }

    public static bool TestFloat()
    {
        float f1 = 14554.9f;
        float f2 = 12543.4f;

        float sum = f1 + f2;

        if ((sum - 27098.3) > 0.001 || (sum - 27098.3) < -0.001)
            return false;

        double d1 = 14554.9;
        double d2 = 12543.4;

        double diff = d1 - d2;

        if ((diff - 2011.5) > 0.001 || (diff - 2011.5) < -0.001)
            return false;

        return true;
    }

    public static bool TestLocalloc()
    {
        // Default fragment size is 4096 bytes

        // Small tests
        if (0 != LocallocIntTests(0)) return false;
        if (0 != LocallocIntTests(1)) return false;
        if (2 != LocallocIntTests(2)) return false;

        // Smoke tests
        if (32 != LocallocByteTests(32)) return false;
        if (32 != LocallocIntTests(32)) return false;
        if (32 != LocallocLongTests(32)) return false;

        // Single frame tests
        if (1024 != LocallocIntTests(1024)) return false;
        if (512 != LocallocLongTests(512)) return false;

        // New fragment tests
        if (1025 != LocallocIntTests(1025)) return false;
        if (513 != LocallocLongTests(513)) return false;

        // Multi-fragment tests
        if (10240 != LocallocIntTests(10240)) return false;
        if (5120 != LocallocLongTests(5120)) return false;

        // Consecutive allocations tests
        if ((256 + 512) != LocallocConsecutiveTests(256, 512)) return false;

        // Nested frames tests
        if (1024 != LocallocNestedTests(256, 256, 256, 256)) return false;
        if (2560 != LocallocNestedTests(1024, 256, 256, 1024)) return false;

        // Reuse fragment tests
        if (3072 != LocallocNestedTests(1024, 512, 512, 1024)) return false;

        return true;
    }

    public static unsafe int LocallocIntTests(int n)
    {
        int* a = stackalloc int[n];
        for (int i = 0; i < n; i++) a[i] = i;
        return n < 2 ? 0 : a[0] + a[1] + a[n - 1];
    }

    public static unsafe long LocallocLongTests(int n)
    {
        long* a = stackalloc long[n];
        for (int i = 0; i < n; i++) a[i] = i;
        return n < 2 ? 0 : a[0] + a[1] + a[n - 1];
    }

    public static unsafe int LocallocByteTests(int n)
    {
        byte* a = stackalloc byte[n];
        for (int i = 0; i < n; i++) a[i] = (byte)(i);
        return n < 2 ? 0 : a[0] + a[1] + a[n - 1];
    }

    public static unsafe int LocallocConsecutiveTests(int n, int m)
    {
        int* a = stackalloc int[n];
        int* b = stackalloc int[m];
        for (int i = 0; i < n; i++) a[i] = i;
        for (int i = 0; i < m; i++) b[i] = i;
        return a[0] + a[1] + a[n - 1] + b[0] + b[1] + b[m - 1];
    }

    public static unsafe int LocallocNestedTests(int n, int m, int p, int k)
    {
        int* a1 = stackalloc int[n];
        for (int i = 0; i < n; i++) a1[i] = i;
        int inner = LocallocConsecutiveTests(m, p);
        int* a2 = stackalloc int[k];
        for (int i = 0; i < k; i++) a2[i] = i;
        return a1[0] + a1[1] + a1[n - 1] + inner + a2[0] + a2[1] + a2[k - 1];
    }

    public static bool TestVirtual()
    {
        BaseClass bc = new DerivedClass();
        ITest itest = bc;

        if (bc.NonVirtualMethod() != 0xbaba)
            return false;
        if (bc.VirtualMethod() != 0xdede)
            return false;
        if (itest.VirtualMethod() != 0xdede)
            return false;
        bc = new BaseClass();
        itest = bc;
        if (bc.NonVirtualMethod() != 0xbaba)
            return false;
        if (bc.VirtualMethod() != 0xbebe)
            return false;
        if (itest.VirtualMethod() != 0xbebe)
            return false;
        return true;
    }
}
