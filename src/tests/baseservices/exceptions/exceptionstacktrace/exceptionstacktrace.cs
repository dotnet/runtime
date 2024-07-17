// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public static class Program
{
    static MethodInfo throwHelper = typeof(Program).GetMethod("ThrowHelper");

    static Exception theException = new Exception("My exception");
    static int recursion;

    private static void VerifyStackTrace(Exception e, string[] templateStackTrace)
    {
        string[] stackTrace = e.StackTrace.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(templateStackTrace.Length, stackTrace.Length);

        for (int i = 0; i < stackTrace.Length; i++)
        {
            Assert.Contains(templateStackTrace[i], stackTrace[i]);
        }
    }

    [Fact]
    public static void DynamicMethodsWithRethrow()
    {
        try
        {
            ThrowHelper();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());

            VerifyStackTrace(e, new string[]
            {
                "Program.ThrowHelper()",
                "MyMethod4()",
                "Program.AppendDynamicFrame()",
                "Program.ThrowHelper()",
                "MyMethod3()",
                "Program.AppendDynamicFrame()",
                "Program.ThrowHelper()",
                "MyMethod2()",
                "Program.AppendDynamicFrame()",
                "Program.ThrowHelper()",
                "MyMethod1()",
                "Program.AppendDynamicFrame()",
                "Program.ThrowHelper()",
                "MyMethod0()",
                "Program.AppendDynamicFrame()",
                "Program.ThrowHelper()",
                "Program.DynamicMethodsWithRethrow()"
            });
        }
    }

    [Fact]
    public static void ThrowExistingExceptionAgain()
    {
        try
        {
            F1();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            VerifyStackTrace(e, new string[]
            {
                "Program.E1()",
                "Program.F1()",
                "Program.ThrowExistingExceptionAgain()"
            });
        }
    }

    [Fact]
    public static void RethrowViaThrow()
    {
        try
        {
            F();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            VerifyStackTrace(e, new string[]
            {
                "Program.A()",
                "Program.B()",
                "Program.C()",
                "Program.D()",
                "Program.E()",
                "Program.F()",
                "Program.RethrowViaThrow()"
            });
        }
    }

    [Fact]
    public static void RethrowViaThrowException()
    {
        try
        {
            F2();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            VerifyStackTrace(e, new string[]
            {
                "Program.D2()",
                "Program.E2()",
                "Program.F2()",
                "Program.RethrowViaThrowException()"
            });
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void A()
    {
        throw new Exception("A");
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    static void B()
    {
        A();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void C()
    {
        B();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void D()
    {
        try
        {
            C();
        }
        catch (Exception e)
        {
            throw;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void E()
    {
        D();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void F()
    {
        E();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void D2()
    {
        try
        {
            C();
        }
        catch (Exception e)
        {
            throw e;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void E2()
    {
        D2();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void F2()
    {
        E2();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void D1(out Exception e)
    {
        e = null;
        try
        {
            C();
        }
        catch (Exception e2)
        {
            e = e2;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void E1()
    {
        D1(out Exception e);
        throw e;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void F1()
    {
        E1();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowHelper()
    {
        if (recursion++ < 5)
            AppendDynamicFrame();

        throw theException;
    }

    static int counter;

    static void AppendDynamicFrame()
    {
        var dm = new DynamicMethod($"MyMethod{counter++}", null, null);
        var il = dm.GetILGenerator();
        il.Emit(OpCodes.Call, throwHelper);
        il.Emit(OpCodes.Call, throwHelper);
        il.Emit(OpCodes.Ret);

        var d = dm.CreateDelegate<Action>();
        try
        {
            d();
        }
        catch (Exception e)
        {
            throw;
        }
    }
}
