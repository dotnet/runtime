// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    static Exception ex = new Exception();
    static byte[] data = null;
    static int count = 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Alloc()
    {
        data = new byte[65536];
        if (count % 16 == 0)
        {
            // Force compacting GC
            GC.Collect(2, GCCollectionMode.Forced, true, true);
        }
        count++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test()
    {
        try
        {
            throw ex;
        }
        catch (Exception)
        {
            throw;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object Dummy(object a)
    {
        return a;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Finally(object a)
    {
        // This puts object a into the first nonvolatile register
        object b = Dummy(a);
        Alloc();
        Dummy(b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test2(object a)
    {
        try
        {
            try
            {
                Test();
            }
            finally
            {
                Finally(a);
            }
        }
        catch (Exception)
        {
        }
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object Foo(int x)
    {
        return x.ToString();
    }

    static int sum = 0;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Verify(object a)
    {
        sum += ((string)a).Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test3()
    {
        // This puts object a into the first nonvolatile register
        object a = Foo(5);
        Test2(a);
        Verify(a);
    }

    [Fact]
    public static void TestEntryPoint()
    {
        for (int i = 0; i < 100; i++)
        {
            Test3();
        }
    }
}
