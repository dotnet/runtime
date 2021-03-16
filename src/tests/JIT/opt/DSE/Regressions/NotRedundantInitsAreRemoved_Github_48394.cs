// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public struct Struct1
{
    public long a;
    public long b;
}

public class NotRedundantInitsAreRemoved_Github_48394
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ValidateAndAssignValue<T>(ref T obj) where T : new()
    {
        if (obj != null)
            throw new Exception("obj was expected to be null");

        obj = new T();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ValidateAndAssignValue(ref int val) 
    {
        if (val != 0)
            throw new Exception("val was expected to be zero");

        val = 42;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ValidateAndAssignValue(ref Struct1 val)
    {
        if (val.a != 0 || val.b != 0)
            throw new Exception("val was expected to be zero");

        val.a = 42;
        val.b = 43;
    }

    public static void TestRefs()
    {
        object obj = null;
        int i = 0;
        do
        {
            obj = null;
            ValidateAndAssignValue(ref obj);
        }
        while (i++ < 2);
    }

    public static void TestInt()
    {
        int val = 0;
        int i = 0;
        do
        {
            val = 0;
            ValidateAndAssignValue(ref val);
        }
        while (i++ < 2);
    }

    public static void TestStruct()
    {
        Struct1 val = default;
        int i = 0;
        do
        {
            val.a = 0;
            val.b = 0; 
            ValidateAndAssignValue(ref val);
        }
        while (i++ < 2);
    }

    public static unsafe void TestTakeAddress()
    {
        int a = 0;
        int* pa = &a;
        *pa = 42;
        a = 0;
        if (a != 0)
            throw new Exception("a was expected to be zero");
    }

    public static unsafe void TestTakeAddress(bool cond)
    {
        int a = 0;
        if (cond)
        {
            int* pa = &a;
            *pa = 42;
        }
        a = 0;
        if (a != 0)
            throw new Exception("a was expected to be zero");
    }

    public static void ReproFrom_GitHub_48394()
    {
        bool x = false;
        int c = 0;
        try
        {
            while (true)
            {
                c = 0;
                if (!x)
                {
                    c = c + 100;
                    x = true;
                }
                else
                {
                    return;
                }
            }
        }
        finally
        {
            if (c != 0)
                throw new Exception("c was expected to be 100");
        }
    }

    public static int Zero => 0;

    public static void TestInt2()
    {
        int val = Zero;
        int i = 0;

    label:
        val = Zero;
        ValidateAndAssignValue(ref val);
        i++;
        if (i < 10)
            goto label;
    }

    public static int Main()
    {
        ReproFrom_GitHub_48394();
        TestTakeAddress();
        TestTakeAddress(true);
        TestTakeAddress(false);
        TestInt();
        TestInt2();
        TestRefs();
        TestStruct();
        return 100;
    }
}
