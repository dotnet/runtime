// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

public class ProgramException : Exception {}

public sealed class ProgramSubclass : Program
{
    public static readonly object s_Obj = new object();
}

public unsafe class Program
{
    private static int s_ReturnCode = 100;

    private Guid field;

    private static Program s_Instance = new ();

    private static Program GetClass() => throw new ProgramException();

    private static Guid GetGuid() => throw new ProgramException();

    private static IntPtr GetIntPtr() => throw new ProgramException();

    private static int* GetPtr() => throw new ProgramException();

    private static Span<byte> GetSpan() => throw new ProgramException();

    private static int GetInt(object obj) => throw new ProgramException();


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DoWork() => s_ReturnCode++;

    private static void TestCond0()
    {
        if (GetClass() == default)
            DoWork();
    }

    private static void TestCond1()
    {
        if (GetClass() is ProgramSubclass)
            DoWork();
    }

    private static void TestCond2()
    {
        if (GetInt(ProgramSubclass.s_Obj) != 42)
            DoWork();
    }

    private static void TestCond3()
    {
        if (GetClass() == s_Instance)
            DoWork();
    }

    private static void TestCond4()
    {
        if (GetClass().field == Guid.NewGuid())
            DoWork();
    }

    private static void TestCond5()
    {
        if (GetGuid() == default)
            DoWork();
    }

    private static void TestCond6()
    {
        if (GetIntPtr() == (IntPtr)42)
            DoWork();
    }

    private static void TestCond7()
    {
        if (*GetPtr() == 42)
            DoWork();
    }

    private static void TestCond8()
    {
        if (GetSpan()[4] == 42)
            DoWork();
    }

    private static bool TestRet1()
    {
        return GetClass() == default;
    }

    private static bool TestRet2()
    {
        return GetClass() == s_Instance;
    }

    private static bool TestRet3()
    {
        return GetClass() is ProgramSubclass;
    }

    private static bool TestRet4()
    {
        return GetInt(ProgramSubclass.s_Obj) == 42;
    }

    private static bool TestRet5()
    {
        return GetClass().field == Guid.NewGuid();
    }

    private static bool TestRet6()
    {
        return GetGuid() == default;
    }

    private static bool TestRet7()
    {
        return GetIntPtr() == (IntPtr)42;
    }

    private static bool TestRet8()
    {
        return *GetPtr() == 42;
    }

    private static bool TestRet9()
    {
        return GetSpan()[100] == 42;
    }

    private static Program TestTailCall1()
    {
        return GetClass();
    }

    private static Guid TestTailCall2()
    {
        return GetGuid();
    }

    private static IntPtr TestTailCall3()
    {
        return GetIntPtr();
    }

    private static int* TestTailCall4()
    {
        return GetPtr();
    }

    [Fact]
    public static int TestEntryPoint()
    {
        foreach (var method in typeof(Program)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Where(m => m.Name.StartsWith("Test")))
        {
            try
            {
                method.Invoke(null, null);
            }
            catch (TargetInvocationException tie)
            {
                if (tie.InnerException is ProgramException)
                {
                    continue;
                }
            }

            s_ReturnCode++;
        }
        return s_ReturnCode;
    }
}
