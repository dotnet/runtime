// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

public interface ITest
{
    public int VirtualMethod();
    public Type GenericVirtualMethod<T>(out bool isBase);
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

    public virtual Type GenericVirtualMethod<T>(out bool isBase)
    {
        isBase = true;
        return typeof(T);
    }
}

public class DerivedClass : BaseClass
{
    public override int VirtualMethod()
    {
        return 0xdede;
    }

    public override Type GenericVirtualMethod<T>(out bool isBase)
    {
        isBase = false;
        return typeof(T);
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

public struct StructWithRefs
{
    public MyObj o1, o2;

    public StructWithRefs(int val1, int val2)
    {
        o1 = new MyObj(val1);
        o2 = new MyObj(val2);
    }
}

public struct TestStruct
{
    public int a;
    public int b;
    public int c;
    public int d;
    public int e;
    public int f;
}

public struct TestStruct2
{
    public int a;
    public int b;
}

public struct TestStruct4ii
{
    public int a;
    public int b;
    public int c;
    public int d;
}

public struct TestStruct4if
{
    public int a;
    public int b;
    public float c;
    public float d;
}

public struct TestStruct4fi
{
    public float a;
    public float b;
    public int c;
    public int d;
}

public struct TestStruct4ff
{
    public float a;
    public float b;
    public float c;
    public float d;
}


public struct TestStruct3d
{
    public double a;
    public double b;
    public double c;
}

class DummyClass
{
    public int field;
    public DummyClass(int f)
    {
        field = f;
    }
}

struct DummyStruct
{
    public int field;
    public DummyStruct(int f)
    {
        field = f;
    }
}

struct DummyStructRef
{
    public DummyClass field;
    public DummyStructRef(DummyClass f)
    {
        field = f;
    }
}

public class InterpreterTest
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCallingConvention0(int a, float b, int c, double d, int e, double f)
    {
        Console.WriteLine("TestCallingConvention0: a = {0}, b = {1}, c = {2}, d = {3}, e = {4}, f = {5}", a, b, c, d, e, f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCallingConvention0Rev(int a, float b, int c, double d, int e, double f)
    {
        Console.Write("TestCallingConvention0Rev: a = ");
        Console.Write(a);
        Console.Write(", b = ");
        Console.Write(b);
        Console.Write(", c = ");
        Console.Write(c);
        Console.Write(", d = ");
        Console.Write(d);
        Console.Write(", e = ");
        Console.Write(e);
        Console.Write(", f = ");
        Console.Write(f);
        Console.WriteLine();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCallingConvention0JitToInterpreter(bool init)
    {
        if (!init)
        {
            TestCallingConvention0Rev(1, 2.0f, 3, 4.0, 5, 6.0);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCallingConvention1(TestStruct s)
    {
        Console.WriteLine("TestCallingConvention1: a = {0}, b = {1}, c = {2}, d = {3}, e = {4}, f = {5}", s.a, s.b, s.c, s.d, s.e, s.f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCallingConvention1Rev(TestStruct s)
    {
        Console.Write("TestCallingConvention1Rev: a = ");
        Console.Write(s.a);
        Console.Write(", b = ");
        Console.Write(s.b);
        Console.Write(", c = ");
        Console.Write(s.c);
        Console.Write(", d = ");
        Console.Write(s.d);
        Console.Write(", e = ");
        Console.Write(s.e);
        Console.Write(", f = ");
        Console.Write(s.f);
        Console.WriteLine();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCallingConvention1JitToInterpreter(bool init)
    {
        if (!init)
        {
            TestStruct s;
            s.a = 1;
            s.b = 2;
            s.c = 3;
            s.d = 4;
            s.e = 5;
            s.f = 6;
            TestCallingConvention1Rev(s);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static TestStruct2 TestCallingConvention2()
    {
        TestStruct2 s;
        s.a = 1;
        s.b = 2;
        return s;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static TestStruct2 TestCallingConvention2Rev()
    {
        TestStruct2 s;
        s.a = 1;
        s.b = 2;
        return s;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCallingConvention2JitToInterpreter(bool init)
    {
        if (!init)
        {
            TestStruct2 s = TestCallingConvention2Rev();
            Console.WriteLine("TestCallingConvention2Rev: s = {0}, {1}", s.a, s.b);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector2 TestCallingConvention3()
    {
        Vector2 v = new Vector2(1, 2);
        return v;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector2 TestCallingConvention3Rev()
    {
        Vector2 v = new Vector2(1, 2);
        return v;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCallingConvention3JitToInterpreter(bool init)
    {
        if (!init)
        {
            Vector2 v = TestCallingConvention3Rev();
            Console.WriteLine("TestCallingConvention3Rev: v = {0}, {1}", v[0], v[1]);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static TestStruct TestCallingConvention4()
    {
        TestStruct s;
        s.a = 1;
        s.b = 2;
        s.c = 3;
        s.d = 4;
        s.e = 5;
        s.f = 6;
        return s;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static TestStruct TestCallingConvention4Rev()
    {
        TestStruct s;
        s.a = 1;
        s.b = 2;
        s.c = 3;
        s.d = 4;
        s.e = 5;
        s.f = 6;
        return s;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCallingConvention4JitToInterpreter(bool init)
    {
        if (!init)
        {
            TestStruct s = TestCallingConvention4Rev();
            Console.WriteLine("TestCallingConvention4Rev: s = {0}, {1}, {2}, {3}, {4}, {5}", s.a, s.b, s.c, s.d, s.e, s.f);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static TestStruct4ii TestCallingConvention5()
    {
        TestStruct4ii s;
        s.a = 1;
        s.b = 2;
        s.c = 3;
        s.d = 4;
        return s;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static TestStruct4ii TestCallingConvention5Rev()
    {
        TestStruct4ii s;
        s.a = 1;
        s.b = 2;
        s.c = 3;
        s.d = 4;
        return s;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCallingConvention5JitToInterpreter(bool init)
    {
        if (!init)
        {
            TestStruct4ii s = TestCallingConvention5Rev();
            Console.WriteLine("TestCallingConvention5Rev: s = {0}, {1}, {2}, {3}", s.a, s.b, s.c, s.d);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static TestStruct4if TestCallingConvention6()
    {
        TestStruct4if s;
        s.a = 1;
        s.b = 2;
        s.c = 3.0f;
        s.d = 4.0f;
        return s;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static TestStruct4if TestCallingConvention6Rev()
    {
        TestStruct4if s;
        s.a = 1;
        s.b = 2;
        s.c = 3.0f;
        s.d = 4.0f;
        return s;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCallingConvention6JitToInterpreter(bool init)
    {
        if (!init)
        {
            TestStruct4if s = TestCallingConvention6Rev();
            Console.WriteLine("TestCallingConvention6Rev: s = {0}, {1}, {2}, {3}", s.a, s.b, s.c, s.d);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static TestStruct4fi TestCallingConvention7()
    {
        TestStruct4fi s;
        s.a = 1.0f;
        s.b = 2.0f;
        s.c = 3;
        s.d = 4;
        return s;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static TestStruct4fi TestCallingConvention7Rev()
    {
        TestStruct4fi s;
        s.a = 1.0f;
        s.b = 2.0f;
        s.c = 3;
        s.d = 4;
        return s;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCallingConvention7JitToInterpreter(bool init)
    {
        if (!init)
        {
            TestStruct4fi s = TestCallingConvention7Rev();
            Console.WriteLine("TestCallingConvention7Rev: s = {0}, {1}, {2}, {3}", s.a, s.b, s.c, s.d);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static TestStruct4ff TestCallingConvention8()
    {
        TestStruct4ff s;
        s.a = 1.0f;
        s.b = 2.0f;
        s.c = 3.0f;
        s.d = 4.0f;
        return s;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static TestStruct4ff TestCallingConvention8Rev()
    {
        TestStruct4ff s;
        s.a = 1.0f;
        s.b = 2.0f;
        s.c = 3.0f;
        s.d = 4.0f;
        return s;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCallingConvention8JitToInterpreter(bool init)
    {
        if (!init)
        {
            TestStruct4ff s = TestCallingConvention8Rev();
            Console.WriteLine("TestCallingConvention8Rev: s = {0}, {1}, {2}, {3}", s.a, s.b, s.c, s.d);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCallingConvention9(TestStruct4fi s)
    {
        Console.WriteLine("TestCallingConvention9: a = {0}, b = {1}, c = {2}, d = {3}", s.a, s.b, s.c, s.d);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCallingConvention9Rev(TestStruct4fi s)
    {
        Console.Write("TestCallingConvention9Rev: a = ");
        Console.Write(s.a);
        Console.Write(", b = ");
        Console.Write(s.b);
        Console.Write(", c = ");
        Console.Write(s.c);
        Console.Write(", d = ");
        Console.Write(s.d);
        Console.WriteLine();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCallingConvention9JitToInterpreter(bool init)
    {
        if (!init)
        {
            TestStruct4fi s = new TestStruct4fi();
            s.a = 1.0f;
            s.b = 2.0f;
            s.c = 3;
            s.d = 4;
            TestCallingConvention9Rev(s);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCallingConvention10(TestStruct3d s)
    {
        Console.WriteLine("TestCallingConvention10: a = {0}, b = {1}, c = {2}", s.a, s.b, s.c);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCallingConvention10Rev(TestStruct3d s)
    {
        Console.Write("TestCallingConvention10Rev: a = ");
        Console.Write(s.a);
        Console.Write(", b = ");
        Console.Write(s.b);
        Console.Write(", c = ");
        Console.Write(s.c);
        Console.WriteLine();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCallingConvention10JitToInterpreter(bool init)
    {
        if (!init)
        {
            TestStruct3d s = new TestStruct3d();
            s.a = 1.0f;
            s.b = 2.0f;
            s.c = 3.0f;
            TestCallingConvention10Rev(s);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static TestStruct3d TestCallingConvention11()
    {
        TestStruct3d s;
        s.a = 1.0f;
        s.b = 2.0f;
        s.c = 3.0f;
        return s;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static TestStruct3d TestCallingConvention11Rev()
    {
        TestStruct3d s;
        s.a = 1.0f;
        s.b = 2.0f;
        s.c = 3.0f;
        return s;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCallingConvention11JitToInterpreter(bool init)
    {
        if (!init)
        {
            TestStruct3d s = TestCallingConvention11Rev();
            Console.WriteLine("TestCallingConvention11Rev: s = {0}, {1}, {2}", s.a, s.b, s.c);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCallingConvention12(byte a, byte b, byte c, byte d, byte e, byte f, byte g, byte h, byte i, char j, int k, int l, long m)
    {
        Console.WriteLine("TestCallingConvention12: a = {0}, b = {1}, c = {2}, d = {3}, e = {4}, f = {5}, g = {6}, h = {7}, i = {8}, j = {9}, k = {10}, l = {11}, m = {12}", a, b, c, d, e, f, g, h, i, j, k, l, m);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCallingConvention12Rev(byte a, byte b, byte c, byte d, byte e, byte f, byte g, byte h, byte i, char j, int k, int l, long m)
    {
        Console.Write("TestCallingConvention12Rev: a = ");
        Console.Write(a);
        Console.Write(", b = ");
        Console.Write(b);
        Console.Write(", c = ");
        Console.Write(c);
        Console.Write(", d = ");
        Console.Write(d);
        Console.Write(", e = ");
        Console.Write(e);
        Console.Write(", f = ");
        Console.Write(f);
        Console.Write(", g = ");
        Console.Write(g);
        Console.Write(", h = ");
        Console.Write(h);
        Console.Write(", i = ");
        Console.Write(i);
        Console.Write(", j = ");
        Console.Write(j);
        Console.Write(", k = ");
        Console.Write(k);
        Console.Write(", l = ");
        Console.Write(l);
        Console.Write(", m = ");
        Console.Write(m);
        Console.WriteLine();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestCallingConvention12JitToInterpreter(bool init)
    {
        if (!init)
        {
            TestCallingConvention12Rev(1, 2, 3, 4, 5, 6, 7, 8, 9, 'a', 10, 11, 12);
        }
    }

    // This method is invoked before we start interpretting anything, so the methods invoked in it will be jitted.
    // This is necessary for the calling convention tests that test calls from the interpreter to the JITted code
    // to actually test things.
    static void EnsureCallingConventionTestTargetMethodsAreJitted()
    {
        TestCallingConvention0(1, 2.0f, 3, 4.0, 5, 6.0);

        TestStruct s = new TestStruct();
        s.a = 1;
        s.b = 2;
        s.c = 3;
        s.d = 4;
        s.e = 5;
        s.f = 6;
        TestCallingConvention1(s);

        TestStruct2 s2 = TestCallingConvention2();

        Vector2 v = TestCallingConvention3();

        TestStruct s4 = TestCallingConvention4();

        TestStruct4ii s5 = TestCallingConvention5();

        TestStruct4if s6 = TestCallingConvention6();

        TestStruct4fi s7 = TestCallingConvention7();

        TestStruct4ff s8 = TestCallingConvention8();

        TestStruct4fi s9 = new TestStruct4fi();
        s9.a = 1.0f;
        s9.b = 2.0f;
        s9.c = 3;
        s9.d = 4;
        TestCallingConvention9(s9);

        TestStruct3d s10 = new TestStruct3d();
        s10.a = 1.0f;
        s10.b = 2.0f;
        s10.c = 3.0f;
        TestCallingConvention10(s10);

        TestStruct3d s11 = TestCallingConvention11();
        Console.WriteLine("TestCallingConvention11: s = ");
        Console.WriteLine(s11.a);
        Console.WriteLine(s11.b);
        Console.WriteLine(s11.c);

        TestCallingConvention12(1, 2, 3, 4, 5, 6, 7, 8, 9, 'a', 10, 11, 12);

        TestCallingConvention0JitToInterpreter(true);
        TestCallingConvention1JitToInterpreter(true);
        TestCallingConvention2JitToInterpreter(true);
        TestCallingConvention3JitToInterpreter(true);
        TestCallingConvention4JitToInterpreter(true);
        TestCallingConvention5JitToInterpreter(true);
        TestCallingConvention6JitToInterpreter(true);
        TestCallingConvention7JitToInterpreter(true);
        TestCallingConvention8JitToInterpreter(true);
        TestCallingConvention9JitToInterpreter(true);
        TestCallingConvention10JitToInterpreter(true);
        TestCallingConvention11JitToInterpreter(true);
        TestCallingConvention12JitToInterpreter(true);
    }

    static int Main(string[] args)
    {
        jitField1 = 42;
        jitField2 = 43;

        EnsureCallingConventionTestTargetMethodsAreJitted();

        RunInterpreterTests();
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void RunInterpreterTests()
    {
        TestCallingConvention0JitToInterpreter(false);
        TestCallingConvention1JitToInterpreter(false);
        TestCallingConvention2JitToInterpreter(false);
        TestCallingConvention3JitToInterpreter(false);
        TestCallingConvention4JitToInterpreter(false);
        TestCallingConvention5JitToInterpreter(false);
        TestCallingConvention6JitToInterpreter(false);
        TestCallingConvention7JitToInterpreter(false);
        TestCallingConvention8JitToInterpreter(false);
        TestCallingConvention9JitToInterpreter(false);
        TestCallingConvention10JitToInterpreter(false);
        TestCallingConvention11JitToInterpreter(false);
        TestCallingConvention12JitToInterpreter(false);

        TestCallingConvention0(1, 2.0f, 3, 4.0, 5, 6.0);

        TestStruct s = new TestStruct();
        s.a = 1;
        s.b = 2;
        s.c = 3;
        s.d = 4;
        s.e = 5;
        s.f = 6;
        TestCallingConvention1(s);

        TestStruct2 s2 = TestCallingConvention2();
        Console.WriteLine("TestCallingConvention: s = ");
        Console.WriteLine(s2.a);
        Console.WriteLine(s2.b);

#if VECTOR_ALIGNMENT_WORKS
        // Interpreter-TODO: enable this again after fixing the alignment for the Vector2 struct and similar ones
        Vector2 v = TestCallingConvention3();
        Console.WriteLine("TestCallingConvention: v = ");
        Console.WriteLine(v[0]);
        Console.WriteLine(v[1]);
#endif
        TestStruct s4 = TestCallingConvention4();
        Console.WriteLine("TestCallingConvention: s = ");
        Console.WriteLine(s4.a);
        Console.WriteLine(s4.b);
        Console.WriteLine(s4.c);
        Console.WriteLine(s4.d);
        Console.WriteLine(s4.e);
        Console.WriteLine(s4.f);

        TestStruct4ii s5 = TestCallingConvention5();
        Console.WriteLine("TestCallingConvention: s = ");
        Console.WriteLine(s5.a);
        Console.WriteLine(s5.b);
        Console.WriteLine(s5.c);
        Console.WriteLine(s5.d);

        TestStruct4if s6 = TestCallingConvention6();
        Console.WriteLine("TestCallingConvention: s = ");
        Console.WriteLine(s6.a);
        Console.WriteLine(s6.b);
        Console.WriteLine(s6.c);
        Console.WriteLine(s6.d);

        TestStruct4fi s7 = TestCallingConvention7();
        Console.WriteLine("TestCallingConvention: s = ");
        Console.WriteLine(s7.a);
        Console.WriteLine(s7.b);
        Console.WriteLine(s7.c);
        Console.WriteLine(s7.d);

        TestStruct4ff s8 = TestCallingConvention8();
        Console.WriteLine("TestCallingConvention: s = ");
        Console.WriteLine(s8.a);
        Console.WriteLine(s8.b);
        Console.WriteLine(s8.c);
        Console.WriteLine(s8.d);

        TestStruct4fi s9 = new TestStruct4fi();
        s9.a = 1.0f;
        s9.b = 2.0f;
        s9.c = 3;
        s9.d = 4;
        TestCallingConvention9(s9);

        TestStruct3d s10 = new TestStruct3d();
        s10.a = 1.0f;
        s10.b = 2.0f;
        s10.c = 3.0f;
        TestCallingConvention10(s10);

        TestStruct3d s11 = TestCallingConvention11();
        Console.WriteLine("TestCallingConvention: s = ");
        Console.WriteLine(s11.a);
        Console.WriteLine(s11.b);
        Console.WriteLine(s11.c);

        TestCallingConvention12(1, 2, 3, 4, 5, 6, 7, 8, 9, 'a', 10, 11, 12);

        // Console.WriteLine("Run interp tests");
        Console.WriteLine("Sum");
        if (SumN(50) != 1275)
            Environment.FailFast(null);
        Console.WriteLine("Mul4");
        if (Mul4(53, 24, 13, 131) != 2166216)
            Environment.FailFast(null);

        Console.WriteLine("TestSwitch");
        TestSwitch();

        Console.WriteLine("PowLoop");
        if (!PowLoop(20, 10, 1661992960))
            Environment.FailFast(null);

        Console.WriteLine("TestJitFields");
        if (!TestJitFields())
            Environment.FailFast(null);
        Console.WriteLine("TestFields");
        if (!TestFields())
            Environment.FailFast(null);
        Console.WriteLine("TestStructRefFields");
        if (!TestStructRefFields())
            Environment.FailFast(null);
        Console.WriteLine("TestSpecialFields");
        if (!TestSpecialFields())
            Environment.FailFast(null);
        Console.WriteLine("TestFloat");
        if (!TestFloat())
            Environment.FailFast(null);

        // Unchecked to ensure that the divide-by-zero here doesn't throw since we're using it to generate a NaN
        unchecked
        {
            if (!TestConvOvf(1, 2, 3, 4, 1.0 / 0.0, -32, 1234567890))
                Environment.FailFast(null);

            if (!TestConvBoundaries(
                32767.999999999996, 32768.00000000001,
                2147483647.9999998, 2147483648.0000005
            ))
                Environment.FailFast(null);

            if (!TestConvBoundaries(
                -32768.99999999999, -32769.00000000001,
                -2147483648.9999995, -2147483649.0000005
            ))
                Environment.FailFast(null);
        }

        Console.WriteLine("TestLocalloc");
        if (!TestLocalloc())
            Environment.FailFast(null);

        Console.WriteLine("TestVirtual");
        if (!TestVirtual())
            Environment.FailFast(null);

        Console.WriteLine("TestBoxing");
        if (!TestBoxing())
            Environment.FailFast(null);

        Console.WriteLine("TestArray");
        if (!TestArray())
            Environment.FailFast(null);

        Console.WriteLine("TestXxObj");
        if (!TestXxObj())
            Environment.FailFast(null);

        Console.WriteLine("TestSizeof");
        if (!TestSizeof())
            Environment.FailFast(null);

        Console.WriteLine("TestLdtoken");
        if (!TestLdtoken())
            Environment.FailFast(null);

        Console.WriteLine("TestMdArray");
        if (!TestMdArray())
            Environment.FailFast(null);

        Console.WriteLine("TestExceptionHandling");
        TestExceptionHandling();

        Console.WriteLine("TestStringCtor");
        if (!TestStringCtor())
            Environment.FailFast(null);

        Console.WriteLine("TestSharedGenerics");
        if (!TestSharedGenerics())
            Environment.FailFast(null);

        Console.WriteLine("TestDelegate");
        if (!TestDelegate())
            Environment.FailFast(null);

        Console.WriteLine("TestCalli");
        if (!TestCalli())
            Environment.FailFast(null);

        Console.WriteLine("TestStaticVirtualGeneric_CodePointerCase");
        if (!TestStaticVirtualGeneric_CodePointerCase())
            Environment.FailFast(null);

        System.GC.Collect();

        Console.WriteLine("All tests passed successfully!");
    }

    public static void TestExceptionHandling()
    {
        TestTryFinally();
        TestCatchCurrent();
        TestCatchFinally();
        TestFilterCatchCurrent();
        TestFilterFailedCatchCurrent();
        TestFilterCatchFinallyCurrent();
        TestFilterFailedCatchFinallyCurrent();
        TestCatchNested();
        TestCatchFinallyNested();
        TestFilterCatchNested();
        TestFilterFailedCatchNested();
        TestFilterCatchFinallyNested();
        TestFilterFailedCatchFinallyNested();
        TestFinallyBeforeCatch();
        TestModifyAlias();

        TestThrowWithinCatch();
        TestThrowWithinFinally();
        TestFinallyWithInnerTryBeforeCatch();
        TestFuncletAccessToLocals();
        TestFinallyRefLocal();
    }

    public static void TestFuncletAccessToLocals()
    {
        int a = 7;
        int b = 3;
        MyStruct2 str = new MyStruct2(2);

        try
        {
            Console.WriteLine(1);
            try
            {
                Console.WriteLine(2);
                throw null;
            }
            catch (Exception e) when (b == 3)
            {
                Console.WriteLine(b);
                Console.WriteLine(e.Message);
                try
                {
                    Console.WriteLine(4);
                }
                catch (Exception e1)
                {
                    Console.WriteLine(5);
                    Console.WriteLine(e1.Message);
                }
                finally
                {
                    Console.WriteLine(6);
                }
            }
            finally
            {
                Console.WriteLine(a);
            }
        }
        catch (Exception e2)
        {
            Console.WriteLine(8);
            Console.WriteLine(e2.Message);
        }
    }

    public static bool TestFilter(ref TestStruct2 s)
    {
        return s.a == 1;
    }

    public static void TestFinallyRefLocal()
    {
        TestStruct2 s;
        s.a = 1;
        s.b = 2;
        try
        {
            throw null;
        }
        catch (Exception e) when (TestFilter(ref s))
        {
        }
    }

    public static void TestTryFinally()
    {
        int x = 0;
        try
        {
            x *= 10;
            x += 1;
        }
        finally
        {
            x *= 10;
            x += 2;
        }

        if (x != 12)
        {
            throw null;
        }
    }

    public static void TestNestedTryFinally()
    {
        int x = 0;
        try
        {
            x *= 10;
            x += 1;
            try
            {
                x *= 10;
                x += 2;
            }
            finally
            {
                x *= 10;
                x += 3;
            }
        }
        finally
        {
            x *= 10;
            x += 4;
        }
        if (x != 1234)
        {
            throw null;
        }
    }

    public static void TestFinallyBeforeCatch()
    {
        int x = 0;
        try
        {
            x *= 10;
            x += 1;
            try
            {
                x *= 10;
                x += 2;
                throw null;
            }
            finally
            {
                x *= 10;
                x += 3;
            }
        }
        catch (Exception)
        {
            x *= 10;
            x += 4;
        }
        if (x != 1234)
        {
            throw null;
        }
    }

    public static unsafe void TestModifyAlias()
    {
        int x = 1;
        int* y = &x;
        try
        {
            throw null;
        }
        catch (Exception)
        {
            // At this point, we are modifying the slot in the original frame
            *y = 2;
            // But then we check the value in the current frame, this will fail
            if (x != 2)
            {
                throw null;
            }
        }
    }

    public static void TestThrowWithinCatch()
    {
        try
        {
            try
            {
                throw null;
            }
            catch (Exception)
            {
                throw null;
            }
        }
        catch (Exception)
        {
        }
    }

    public static void TestThrowWithinFinally()
    {
        try
        {
            try
            {
                throw null;
            }
            catch (Exception)
            {
            }
            finally
            {
                throw null;
            }
        }
        catch (Exception)
        {
        }
    }

    public static void Throw()
    {
        throw null; // Simulating the throw operation
    }

    public static void TestCatchCurrent()
    {
        int x = 0;
        try
        {
            x *= 10;
            x += 1;
            throw null;
        }
        catch (Exception)
        {
            x *= 10;
            x += 2;
        }
        if (x != 12)
        {
            throw null;
        }
    }

    public static void TestCatchFinally()
    {
        int x = 0;
        try
        {
            x *= 10;
            x += 1;
            throw null;
        }
        catch (Exception)
        {
            x *= 10;
            x += 2;
        }
        finally
        {
            // Copied from PowLoop
            // This small block of code require retry in GenerateCode
            // and this test that the retry logic is correct even when the retry happen within a funclet

            int n = 5;
            int nr = 10;
            long ret = 1;
            for (int i = 0; i < n; i++)
                ret *= nr;
            bool dummy = (int)ret == 100;

            x *= 10;
            x += 3;
        }
        if (x != 123)
        {
            throw null;
        }
    }

    public static void TestFilterCatchCurrent()
    {
        int x = 0;
        try
        {
            x *= 10;
            x += 1;
            throw null;
        }
        catch (Exception) when (x == 1)
        {
            x *= 10;
            x += 2;
        }
        if (x != 12)
        {
            throw null;
        }
    }

    public static void TestFinallyWithInnerTryBeforeCatch()
    {
        int x = 0;
        try
        {
            x *= 10;
            x += 1;
            try
            {
                x *= 10;
                x += 2;
                throw null;
            }
            finally
            {
                try
                {
                    x *= 10;
                    x += 3;
                }
                finally
                {
                    x *= 10;
                    x += 4;
                }
                x *= 10;
                x += 5;
            }
        }
        catch (Exception)
        {
            x *= 10;
            x += 6;
        }
        if (x != 123456)
        {
            throw null;
        }
    }

    public static void TestFilterFailedCatchCurrent()
    {
        int x = 0;
        try
        {
            x *= 10;
            x += 1;
            throw null;
        }
        catch (Exception) when (x != 1)
        {
            x *= 10;
            x += 2;
        }
        catch (Exception) when (x == 1)
        {
            x *= 10;
            x += 3;
        }
        if (x != 13)
        {
            throw null;
        }
    }

    public static void TestFilterCatchFinallyCurrent()
    {
        int x = 0;
        try
        {
            x *= 10;
            x += 1;
            throw null;
        }
        catch (Exception) when (x == 1)
        {
            x *= 10;
            x += 2;
        }
        finally
        {
            x *= 10;
            x += 3;
        }
        if (x != 123)
        {
            throw null;
        }
    }


    public static void TestFilterFailedCatchFinallyCurrent()
    {
        int x = 0;
        try
        {
            x *= 10;
            x += 1;
            throw null;
        }
        catch (Exception) when (x != 1)
        {
            x *= 10;
            x += 2;
        }
        catch (Exception) when (x == 1)
        {
            x *= 10;
            x += 3;
        }
        finally
        {
            x *= 10;
            x += 4;
        }
        if (x != 134)
        {
            throw null;
        }
    }
    public static void TestCatchNested()
    {
        int x = 0;
        try
        {
            x *= 10;
            x += 1;
            Throw();
        }
        catch (Exception)
        {
            x *= 10;
            x += 2;
        }
        if (x != 12)
        {
            throw null;
        }
    }

    public static void TestCatchFinallyNested()
    {
        int x = 0;
        try
        {
            x *= 10;
            x += 1;
            Throw();
        }
        catch (Exception)
        {
            x *= 10;
            x += 2;
        }
        finally
        {
            x *= 10;
            x += 3;
        }
        if (x != 123)
        {
            throw null;
        }
    }

    public static void TestFilterCatchNested()
    {
        int x = 0;
        try
        {
            x *= 10;
            x += 1;
            Throw();
        }
        catch (Exception) when (x == 1)
        {
            x *= 10;
            x += 2;
        }
        if (x != 12)
        {
            throw null;
        }
    }

    public static void TestFilterFailedCatchNested()
    {
        int x = 0;
        try
        {
            x *= 10;
            x += 1;
            Throw();
        }
        catch (Exception) when (x != 1)
        {
            x *= 10;
            x += 2;
        }
        catch (Exception) when (x == 1)
        {
            x *= 10;
            x += 3;
        }
        if (x != 13)
        {
            throw null;
        }
    }

    public static void TestFilterCatchFinallyNested()
    {
        int x = 0;
        try
        {
            x *= 10;
            x += 1;
            Throw();
        }
        catch (Exception) when (x == 1)
        {
            x *= 10;
            x += 2;
        }
        finally
        {
            x *= 10;
            x += 3;
        }
        if (x != 123)
        {
            throw null;
        }
    }

    public static void TestFilterFailedCatchFinallyNested()
    {
        int x = 0;
        try
        {
            x *= 10;
            x += 1;
            Throw();
        }
        catch (Exception) when (x != 1)
        {
            x *= 10;
            x += 2;
        }
        catch (Exception) when (x == 1)
        {
            x *= 10;
            x += 3;
        }
        finally
        {
            x *= 10;
            x += 4;
        }
        if (x != 134)
        {
            throw null;
        }
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
        int n0 = SwitchOp(20, 6, 0); // 26
        int n1 = SwitchOp(20, 6, 1); // 14
        int n2 = SwitchOp(20, 6, 2); // 120
        int n3 = SwitchOp(20, 6, 3); // 42

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

    public static bool TestConvOvf(float r4, double r8, int i4, long i8, double nan, int negativeInt, long hugeInt)
    {
        checked
        {
            byte a = (byte)r4,
                b = (byte)r8,
                c = (byte)i4,
                d = (byte)i8;

            if (a != r4)
                return false;
            if (b != r8)
                return false;
            if (c != i4)
                return false;
            if (d != i8)
                return false;

            try
            {
                a = (byte)nan;
                return false;
            }
            catch (OverflowException)
            {
            }

            try
            {
                b = (byte)hugeInt;
                return false;
            }
            catch (OverflowException)
            {
            }

            try
            {
                c = (byte)negativeInt;
                return false;
            }
            catch (OverflowException)
            {
            }
        }

        return true;
    }

    public static bool TestConvBoundaries(double inRangeShort, double outOfRangeShort, double inRangeInt, double outOfRangeInt)
    {
        // In unchecked mode, the interpreter saturates on float->int conversions if the value is out of range
        unchecked
        {
            short a = (short)inRangeShort,
                b = (short)outOfRangeShort;
            int c = (int)inRangeInt,
                d = (int)outOfRangeInt;

            if (a != b)
                return false;
            if (c != d)
                return false;
        }

        checked
        {
            short tempA = (short)inRangeShort;
            try
            {
                tempA = (short)outOfRangeShort;
                return false;
            }
            catch (OverflowException)
            {
            }

            int tempB = (int)inRangeInt;
            try
            {
                tempB = (int)outOfRangeInt;
                return false;
            }
            catch (OverflowException)
            {
            }
        }

        return true;
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

        ref int str_a = ref str.str.a;

        System.GC.Collect();

        staticObj = obj;
        staticStr = str;

        System.GC.Collect();

        sum = staticObj.str.a + staticStr.str.a + staticObj.ct + staticStr.ct;
        if (sum != 33)
            return false;

        WriteInt(ref str_a, 11);
        WriteInt(ref staticObj.str.a, 22);
        sum = ReadInt(ref str_a) + ReadInt(ref staticObj.str.a);
        if (sum != 33)
            return false;

        if (str_a != str.str.a)
            return false;

        return true;
    }

    public static bool TestStructRefFields()
    {
        StructWithRefs s = new StructWithRefs(3, 42);
        if (s.o1.str.a != 3)
            return false;
        if (s.o2.str.a != 42)
            return false;

        System.GC.Collect();

        if (s.o1.str.a != 3)
            return false;
        if (s.o2.str.a != 42)
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

        System.GC.Collect();

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

        Console.WriteLine("bc.NonVirtualMethod");
        if (bc.NonVirtualMethod() != 0xbaba)
            return false;
        Console.WriteLine("bc.VirtualMethod");
        if (bc.VirtualMethod() != 0xdede)
            return false;
        Console.WriteLine("bc.GenericVirtualMethod");
        bool isBase = false;
        Type retType;
        Console.WriteLine("bc.GenericVirtualMethod<int>");
        retType = bc.GenericVirtualMethod<int>(out isBase);
        if (retType != typeof(int) || isBase)
            return false;
        Console.WriteLine("bc.GenericVirtualMethod<string>");
        retType = bc.GenericVirtualMethod<string>(out isBase);
        if (retType != typeof(string) || isBase)
            return false;
        Console.WriteLine("itest.VirtualMethod");
        if (itest.VirtualMethod() != 0xdede)
            return false;
        Console.WriteLine("itest.GenericVirtualMethod<int>");
        retType = itest.GenericVirtualMethod<int>(out isBase);
        if (retType != typeof(int) || isBase)
            return false;
        Console.WriteLine("itest.GenericVirtualMethod<string>");
        retType = itest.GenericVirtualMethod<string>(out isBase);
        if (retType != typeof(string) || isBase)
            return false;

        bc = new BaseClass();
        itest = bc;
        Console.WriteLine("bc.NonVirtualMethod");
        if (bc.NonVirtualMethod() != 0xbaba)
            return false;
        Console.WriteLine("bc.VirtualMethod");
        if (bc.VirtualMethod() != 0xbebe)
            return false;
        Console.WriteLine("bc.GenericVirtualMethod<int>");
        retType = bc.GenericVirtualMethod<int>(out isBase);
        if (retType != typeof(int) || !isBase)
            return false;
        Console.WriteLine("bc.GenericVirtualMethod<string>");
        retType = bc.GenericVirtualMethod<string>(out isBase);
        if (retType != typeof(string) || !isBase)
            return false;
        Console.WriteLine("itest.VirtualMethod");
        if (itest.VirtualMethod() != 0xbebe)
            return false;
        Console.WriteLine("itest.GenericVirtualMethod<int>");
        retType = itest.GenericVirtualMethod<int>(out isBase);
        if (retType != typeof(int) || !isBase)
            return false;
        Console.WriteLine("itest.GenericVirtualMethod<string>");
        retType = itest.GenericVirtualMethod<string>(out isBase);
        if (retType != typeof(string) || !isBase)
            return false;
        return true;
    }

    public static bool TestStringCtor()
    {
        string s = new string('a', 4);
        if (s.Length != 4)
            return false;
        if (s[0] != 'a')
            return false;
        if (s != "aaaa")
            return false;
        return true;
    }

    private static Type LoadType<T>()
    {
        return typeof(T);
    }

    class GenericClass<T>
    {
        public Type GetTypeOfTInstance()
        {
            return typeof(T);
        }
        public static Type GetTypeOfTStatic()
        {
            return typeof(T);
        }
    }

    public static bool TestSharedGenerics()
    {
        if (!TestSharedGenerics_CallsTo())
            return false;

        Console.WriteLine("Test calls to shared generics from generic code (unshared generics)");
        if (!TestGenerics_CallsFrom<int>())
            return false;
        Console.WriteLine("Test calls to shared generics from generic code (shared generics)");
        if (!TestGenerics_CallsFrom<string>())
            return false;

        return true;
    }

    public static bool TestSharedGenerics_CallsTo()
    {
        Console.WriteLine("Test calls to shared generics from non-generic code");
        if (LoadType<string>() != typeof(string))
            return false;
        if (LoadType<object>() != typeof(object))
            return false;

        if (new GenericClass<string>().GetTypeOfTInstance() != typeof(string))
            return false;
        if (new GenericClass<object>().GetTypeOfTInstance() != typeof(object))
            return false;

        if (GenericClass<object>.GetTypeOfTStatic() != typeof(object))
            return false;

        if (GenericClass<string>.GetTypeOfTStatic() != typeof(string))
            return false;

        return true;
    }

    public static bool TestGenerics_CallsFrom<T>()
    {
        if (LoadType<T>() != typeof(T))
            return false;

        if (new GenericClass<T>().GetTypeOfTInstance() != typeof(T))
            return false;

        if (GenericClass<T>.GetTypeOfTStatic() != typeof(T))
            return false;

        return true;
    }

    public static bool TestBoxing()
    {
        int l = 7, r = 4;
        object s = BoxedSubtraction(l, r);
        // `(s is int result)` generates isinst so we have to do this in steps
        int result = (int)s;
        return result == 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static object BoxedSubtraction(object lhs, object rhs)
    {
        return (int)lhs - (int)rhs;
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

        // double
        if (!ArrayDouble(0, 0)) return false;
        if (!ArrayDouble(1, 1)) return false;
        if (!ArrayDouble(32, 32)) return false;

        // ref and value types
        if (!TestObjectArray()) return false;
        if (!TestStructArray()) return false;
        if (!TestStructRefArray()) return false;
        if (!ArrayJagged(1)) return false;
        if (!ArrayMD1()) return false;
        if (!ArrayObj(1)) return false;
        if (!ArrayStruct(1)) return false;

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

    public unsafe static bool TestObjectArray()
    {
        DummyClass[] array = new DummyClass[10];
        array[0] = new DummyClass(42);
        return array[0].field == 42;
    }

    public unsafe static bool TestStructArray()
    {
        DummyStruct[] array = new DummyStruct[10];
        array[0] = new DummyStruct(42);
        return array[0].field == 42;
    }

    public unsafe static bool TestStructRefArray()
    {
        DummyStructRef[] array = new DummyStructRef[10];
        DummyClass d = new DummyClass(42);
        array[0] = new DummyStructRef(d);
        return array[0].field.field == 42;
    }

    public static bool ArrayJagged(int i)
    {
        int[][] a = new int[2][];
        a[0] = new int[2] { 0, 1 };
        a[1] = new int[2] { 2, 3 };
        return a[1][i] == 3;
    }

    public static bool ArrayMD1()
    {
        int[,] a = { { 1, 2 }, { 3, 4 } };
        return true;
    }

    public static bool ArrayObj(int i)
    {
        DummyClass[] a = {new DummyClass(0), new DummyClass(1), new DummyClass(2), new DummyClass(3), new DummyClass(4),
                    new DummyClass(5), new DummyClass(6), new DummyClass(7), new DummyClass(8), new DummyClass(9)};
        return a[i].field == i;
    }

    public static bool ArrayStruct(int i)
    {
        DummyStruct[] a = {new DummyStruct(0), new DummyStruct(1), new DummyStruct(2), new DummyStruct(3), new DummyStruct(4),
                    new DummyStruct(5), new DummyStruct(6), new DummyStruct(7), new DummyStruct(8), new DummyStruct(9)};
        return a[i].field == i;
    }

    public static unsafe bool TestXxObj()
    {
        // FIXME: There is no way to generate cpobj opcodes with roslyn at present.
        // The only source of cpobj I've found other than hand-written IL tests is ilmarshalers.h, so once pinvoke marshaling is
        //  supported, we can use that to verify that cpobj works. Until then, this method only tests ldobj/stobj.
        TestStruct4fi a = new TestStruct4fi
        {
            a = 1,
            b = 2,
            c = 3,
            d = 4,
        }, b = default;
        ref TestStruct4fi c = ref a,
            d = ref b;

        if (b.a == a.a)
            return false;

        c = d;

        if (b.a != a.a)
            return false;

        return true;
    }

    public static unsafe bool TestSizeof()
    {
        if (sizeof(int) != 4)
            return false;
        if (sizeof(double) != 8)
            return false;
        if (sizeof(MyStruct) != 4)
            return false;
        return true;
    }

    public static int LdtokenField = 7;

    public static bool TestLdtoken()
    {
        Type t = typeof(int);
        int i = 42;
        if (!ReferenceEquals(t, i.GetType()))
            return false;
        // These generate field and method ldtoken opcodes, but the test fails because we are missing castclass and possibly also generics
        /*
        System.Linq.Expressions.Expression<Func<int>> f = () => LdtokenField;
        System.Linq.Expressions.Expression<Action> a = () => TestLdtoken();
        */
        return true;
    }

    public static bool TestMdArray()
    {
        int[,] a = { { 1, 2 }, { 3, 4 } };
        if (a[0, 1] != 2)
            return false;

        object[,] b = new object[1, 1];
        ref object bElt = ref b[0, 0];
        bElt = null;

        object[,] c = new string[1, 1];

        try
        {
            ref object cElt = ref c[0, 0];
            return false;
        }
        catch (ArrayTypeMismatchException)
        {
        }

        ref readonly object cElt2 = ref c[0, 0];

        return true;
    }

    private static int _fieldA;
    private static int _fieldB;
    private static int _fieldResult;
    private static void MultiplyAandB()
    {
        _fieldResult = _fieldA * _fieldB;
    }

    private static Type _typeFromFill;

    private static void Fill<T>()
    {
        _typeFromFill = typeof(T);
    }

    private static Func<int> GetDelegateFromBaseClass(BaseClass bc)
    {
        return bc.VirtualMethod;
    }

    public static bool TestDelegate()
    {
        _fieldA = 3;
        _fieldB = 1;
        _fieldResult = 0;

        // This tests delegate creation, ldftn, and invocation via the "Invoke" method
        Action func = new Action(MultiplyAandB);

        _fieldB = 4;
        Console.WriteLine("CallingFunc first time");
        func();
        Console.WriteLine("Return CallingFunc first time");
        if (_fieldResult != 12)
        {
            Console.WriteLine("Delegate test failed: expected 12, got " + _fieldResult);
            return false;
        }

        _fieldB = 3;
        Console.WriteLine("CallingFunc second time");
        func();
        Console.WriteLine("Return CallingFunc second time");
        if (_fieldResult != 9)
        {
            Console.WriteLine("Delegate test failed: expected 9, got " + _fieldResult);
            return false;
        }

        if (GetDelegateFromBaseClass(new BaseClass())() != 0xbebe)
        {
            Console.WriteLine("Delegate test failed: expected 0xbebe, got " + GetDelegateFromBaseClass(new BaseClass())());
            return false;
        }

        if (GetDelegateFromBaseClass(new DerivedClass())() != 0xdede)
        {
            Console.WriteLine("Delegate test failed: expected 0xdede, got " + GetDelegateFromBaseClass(new DerivedClass())());
            return false;
        }
        return true;
    }

    public unsafe static bool TestCalli()
    {
        delegate*<void> func = &MultiplyAandB;

        _fieldA = 3;
        _fieldB = 1;
        _fieldResult = 0;

        // This tests ldftn, and calli

        _fieldB = 4;
        Console.WriteLine("CallingFunc first time");
        func();
        Console.WriteLine("Return CallingFunc first time");
        if (_fieldResult != 12)
        {
            Console.WriteLine("Calli test failed: expected 12, got " + _fieldResult);
            return false;
        }

        _fieldB = 3;
        Console.WriteLine("CallingFunc second time");
        func();
        Console.WriteLine("Return CallingFunc second time");
        if (_fieldResult != 9)
        {
            Console.WriteLine("Calli test failed: expected 9, got " + _fieldResult);
            return false;
        }

        GetCalliGeneric<int>()();
        if (_typeFromFill != typeof(int))
        {
            Console.WriteLine("Calli generic test failed: expected int, got " + _typeFromFill);
            return false;
        }


        GetCalliGeneric<object>()();
        if (_typeFromFill != typeof(object))
        {
            Console.WriteLine("Calli generic test failed: expected object, got " + _typeFromFill);
            return false;
        }

        GetCalliGeneric<string>()();
        if (_typeFromFill != typeof(string))
        {
            Console.WriteLine("Calli generic test failed: expected string, got " + _typeFromFill);
            return false;
        }
        return true;
    }

    private static unsafe delegate*<void> GetCalliGeneric<T>()
    {
        return &Fill<T>;
    }

    interface IStaticVirtualGeneric<T>
    {
        abstract static int StaticVirtualGeneric();
    }

    struct MyGenericStruct<T> : IStaticVirtualGeneric<string>, IStaticVirtualGeneric<object>
    {
        static int IStaticVirtualGeneric<string>.StaticVirtualGeneric()
        {
            return 1;
        }
        static int IStaticVirtualGeneric<object>.StaticVirtualGeneric()
        {
            return 2;
        }
    }

    private static int StaticVirtualGeneric<T, U>() where T : IStaticVirtualGeneric<U>
    {
        return T.StaticVirtualGeneric();
    }

    public static bool TestStaticVirtualGeneric_CodePointerCase()
    {
        if (StaticVirtualGeneric<MyGenericStruct<BaseClass>, string>() != 1)
            return false;
        if (StaticVirtualGeneric<MyGenericStruct<BaseClass>, object>() != 2)
            return false;

        return true;
    }
}
