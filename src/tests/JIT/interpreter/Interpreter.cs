// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
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

public class InterpreterTest
{
    static void TestCallingConvention0(int a, float b, int c, double d, int e, double f)
    {
        Console.WriteLine("TestCallingConvention0: a = {0}, b = {1}, c = {2}, d = {3}, e = {4}, f = {5}", a, b, c, d, e, f);
    }

    static void TestCallingConvention1(TestStruct s)
    {
        Console.WriteLine("TestCallingConvention1: a = {0}, b = {1}, c = {2}, d = {3}, e = {4}, f = {5}", s.a, s.b, s.c, s.d, s.e, s.f);
    }

    static TestStruct2 TestCallingConvention2()
    {
        TestStruct2 s;
        s.a = 1;
        s.b = 2;
        return s;
    }

    static Vector2 TestCallingConvention3()
    {
        Vector2 v = new Vector2(1, 2);
        return v;
    }

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

    static TestStruct4ii TestCallingConvention5()
    {
        TestStruct4ii s;
        s.a = 1;
        s.b = 2;
        s.c = 3;
        s.d = 4;
        return s;
    }

    static TestStruct4if TestCallingConvention6()
    {
        TestStruct4if s;
        s.a = 1;
        s.b = 2;
        s.c = 3.0f;
        s.d = 4.0f;
        return s;
    }

    static TestStruct4fi TestCallingConvention7()
    {
        TestStruct4fi s;
        s.a = 1.0f;
        s.b = 2.0f;
        s.c = 3;
        s.d = 4;
        return s;
    }

    static TestStruct4ff TestCallingConvention8()
    {
        TestStruct4ff s;
        s.a = 1.0f;
        s.b = 2.0f;
        s.c = 3.0f;
        s.d = 4.0f;
        return s;
    }

    static void TestCallingConvention9(TestStruct4fi s)
    {
        Console.WriteLine("TestCallingConvention9: a = {0}, b = {1}, c = {2}, d = {3}", s.a, s.b, s.c, s.d);
    }

    static void TestCallingConvention10(TestStruct3d s)
    {
        Console.WriteLine("TestCallingConvention10: a = {0}, b = {1}, c = {2}", s.a, s.b, s.c);
    }

    static TestStruct3d TestCallingConvention11()
    {
        TestStruct3d s;
        s.a = 1.0f;
        s.b = 2.0f;
        s.c = 3.0f;
        return s;
    }

    static void TestCallingConvention12(byte a, byte b, byte c, byte d, byte e, byte f, byte g, byte h, byte i, char j, int k, int l, long m)
    {
        Console.WriteLine("TestCallingConvention12: a = {0}, b = {1}, c = {2}, d = {3}, e = {4}, f = {5}, g = {6}, h = {7}, i = {8}, j = {9}, k = {10}, l = {11}, m = {12}", a, b, c, d, e, f, g, h, i, j, k, l, m);
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
        if (SumN(50) != 1275)
            Environment.FailFast(null);
        if (Mul4(53, 24, 13, 131) != 2166216)
            Environment.FailFast(null);

        TestSwitch();

        if (!PowLoop(20, 10, 1661992960))
            Environment.FailFast(null);

        if (!TestJitFields())
            Environment.FailFast(null);
        if (!TestFields())
            Environment.FailFast(null);
        if (!TestStructRefFields())
            Environment.FailFast(null);
        if (!TestSpecialFields())
            Environment.FailFast(null);
        if (!TestFloat())
            Environment.FailFast(null);

        if (!TestLocalloc())
            Environment.FailFast(null);

        if (!TestVirtual())
            Environment.FailFast(null);

        if (!TestBoxing())
            Environment.FailFast(null);

        if (!TestArray())
            Environment.FailFast(null);

        if (!TestXxObj())
            Environment.FailFast(null);

        if (!TestSizeof())
            Environment.FailFast(null);

        if (!TestLdtoken())
            Environment.FailFast(null);
        /*
        if (!TestMdArray())
            Environment.FailFast(null);
        */
        TestExceptionHandling();

        System.GC.Collect();
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
        } catch (Exception) {
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
            bool dummy=  (int)ret == 100;

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
        } catch (Exception) {
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

    public static bool TestBoxing()
    {
        int l = 7, r = 4;
        object s = BoxedSubtraction(l, r);
        // `(s is int result)` generates isinst so we have to do this in steps
        int result = (int)s;
        return result == 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static object BoxedSubtraction (object lhs, object rhs) {
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
        // FIXME: This generates roughly:
        // newobj int[,].ctor
        // ldtoken int[,]
        // call System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray
        // The newobj currently fails because int[,].ctor isn't a real method, the interp needs to use getCallInfo to determine how to invoke it
        int[,] a = {{1, 2}, {3, 4}};
        return a[0, 1] == 2;
    }
}
