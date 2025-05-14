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
//        if (!TestVirtual())
//          Environment.FailFast(null);

        if (!TestArray())
            Environment.FailFast(null);

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

    public static bool TestArray()
    {
        // sbyte
        if (!ArraySByte(0, 0)) return false;
        if (!ArraySByte(32, 1)) return false;
        if (!ArraySByte(32, 32)) return false;
        if (!ArraySByte(32, sbyte.MinValue)) return false;
        if (!ArraySByte(32, sbyte.MaxValue)) return false;

        // byte
        if (!ArrayByte(0, 0)) return false;
        if (!ArrayByte(32, 1)) return false;
        if (!ArrayByte(32, 32)) return false;
        if (!ArrayByte(32, byte.MinValue)) return false;
        if (!ArrayByte(32, byte.MaxValue)) return false;

        // short
        if (!ArrayInt16(0, 0)) return false;
        if (!ArrayInt16(32, 1)) return false;
        if (!ArrayInt16(32, 32)) return false;
        if (!ArrayInt16(32, short.MinValue)) return false;
        if (!ArrayInt16(32, short.MaxValue)) return false;

        // ushort
        if (!ArrayUInt16(0, 0)) return false;
        if (!ArrayUInt16(32, 1)) return false;
        if (!ArrayUInt16(32, 32)) return false;
        if (!ArrayUInt16(32, ushort.MinValue)) return false;
        if (!ArrayUInt16(32, ushort.MaxValue)) return false;

        // int
        if (!ArrayInt32(0, 0)) return false;
        if (!ArrayInt32(32, 1)) return false;
        if (!ArrayInt32(32, 32)) return false;
        if (!ArrayInt32(32, int.MinValue)) return false;
        if (!ArrayInt32(32, int.MaxValue)) return false;

        // uint
        if (!ArrayUInt32(0, 0)) return false;
        if (!ArrayUInt32(32, 1)) return false;
        if (!ArrayUInt32(32, 32)) return false;
        if (!ArrayUInt32(32, uint.MinValue)) return false;
        if (!ArrayUInt32(32, uint.MaxValue)) return false;

        // // long
        if (!ArrayInt64(0, 0)) return false;
        if (!ArrayInt64(1, 1)) return false;
        if (!ArrayInt64(32, 32)) return false;
        if (!ArrayInt64(32, Int64.MinValue)) return false;
        if (!ArrayInt64(32, Int64.MaxValue)) return false;

        // float
        if (!ArrayFloat(0, 0)) return false;
        if (!ArrayFloat(1, 1)) return false;
        if (!ArrayFloat(32, 32)) return false;
        if (!ArrayFloat(32, float.MinValue)) return false;
        if (!ArrayFloat(32, float.MaxValue)) return false;

        // // double
        if (!ArrayDouble(0, 0)) return false;
        if (!ArrayDouble(1, 1)) return false;
        if (!ArrayDouble(32, 32)) return false;
        // FIXME: ldc.r8 is NaN
        // if (!ArrayDouble(32, double.MinValue)) return false;
        // if (!ArrayDouble(32, double.MaxValue)) return false;

        return true;
    }

    public static bool ArraySByte(int length, sbyte value)
    {
        sbyte[] values = new sbyte[length];
        if (values.Length != length)
            return false;

        if (length == 0)
            return true;

        values[0] = value;
        values[length - 1] = value;

        if (values[0] != value)
            return false;
        if (values[length - 1] != value)
            return false;

        return true;
    }

    public static bool ArrayByte(int length, byte value)
    {
        byte[] values = new byte[length];
        if (values.Length != length)
            return false;

        if (length == 0)
            return true;

        values[0] = value;
        values[length - 1] = value;

        if (values[0] != value)
            return false;
        if (values[length - 1] != value)
            return false;

        return true;
    }

    public static bool ArrayInt16(int length, short value)
    {
        short[] values = new short[length];
        if (values.Length != length)
            return false;

        if (length == 0)
            return true;

        values[0] = value;
        values[length - 1] = value;

        if (values[0] != value)
            return false;
        if (values[length - 1] != value)
            return false;

        return true;
    }

    public static bool ArrayUInt16(int length, ushort value)
    {
        ushort[] values = new ushort[length];
        if (values.Length != length)
            return false;

        if (length == 0)
            return true;

        values[0] = value;
        values[length - 1] = value;

        if (values[0] != value)
            return false;
        if (values[length - 1] != value)
            return false;

        return true;
    }

    public static bool ArrayInt32(int length, int value)
    {
        int[] values = new int[length];
        if (values.Length != length)
            return false;

        if (length == 0)
            return true;

        values[0] = value;
        values[length - 1] = value;

        if (values[0] != value)
            return false;
        if (values[length - 1] != value)
            return false;

        return true;
    }

    public static bool ArrayUInt32(int length, uint value)
    {
        uint[] values = new uint[length];
        if (values.Length != length)
            return false;

        if (length == 0)
            return true;

        values[0] = value;
        values[length - 1] = value;

        if (values[0] != value)
            return false;
        if (values[length - 1] != value)
            return false;

        return true;
    }

    public static bool ArrayInt64(int length, long value)
    {
        long[] values = new long[length];
        if (values.Length != length)
            return false;

        if (length == 0)
            return true;

        values[0] = value;
        values[length - 1] = value;

        if (values[0] != value)
            return false;
        if (values[length - 1] != value)
            return false;

        return true;
    }

    public static bool ArrayFloat(int length, float value)
    {
        float[] values = new float[length];
        if (values.Length != length)
            return false;

        if (length == 0)
            return true;

        values[0] = value;
        values[length - 1] = value;

        if (values[0] != value)
            return false;
        if (values[length - 1] != value)
            return false;

        return true;
    }

    public static bool ArrayDouble(int length, double value)
    {
        double[] values = new double[length];
        if (values.Length != length)
            return false;

        if (length == 0)
            return true;

        values[0] = value;
        values[length - 1] = value;

        if (values[0] != value)
            return false;
        if (values[length - 1] != value)
            return false;

        return true;
    }
}
