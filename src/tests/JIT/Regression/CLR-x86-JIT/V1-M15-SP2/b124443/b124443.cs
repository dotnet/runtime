// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class ArrayBounds
{
    internal static void f1a()
    {
        int [] a = new int[4];
        for (int i=0; i < a.Length; --i)
        {
            a[i]=1;
        }
    }
    internal static void f2a()
    {
        int [] a = new int[4];
        for (int i=0; i < a.Length; --i)
        {
            int b = a[i];
        }
    }
    internal static void f3a()
    {
        int [] a = new int[4];
        for (int i=0; i < a.Length; --i)
        {
            Console.WriteLine(a[i]);
        }
    }
    internal static void f4a()
    {
        int [] a = new int[4];
        for (int i=0; i < a.Length; a[i]=i,--i)
        {
            // empty
        }
    }

    // ++i
    internal static void f1b()
    {
        int [] a = new int[4];
        for (int i=0; i <= a.Length; ++i)
        {
            a[i]=1;
        }
    }
    internal static void f2b()
    {
        int [] a = new int[4];
        for (int i=0; i <= a.Length; ++i)
        {
            int b = a[i];
        }
    }
    internal static void f3b()
    {
        int [] a = new int[4];
        for (int i=0; i <= a.Length; ++i)
        {
            Console.WriteLine(a[i]);
        }
    }
    internal static void f4b()
    {
        int [] a = new int[4];
        for (int i=0; i <= a.Length; a[i]=i,++i)
        {
            // empty
        }
    }

    // ++i, 0x7fff
    internal static void f1c()
    {
        bool [] a = new bool[0x7fff];
        for (short i=0x7ff0; i < a.Length+1; ++i)
        {
            a[i]=true;
        }
    }
    internal static void f2c()
    {
        bool [] a = new bool[0x7fff];
        for (short i=0x7ff0; i < a.Length+1; ++i)
        {
            bool b = a[i];
        }
    }
    internal static void f3c()
    {
        bool [] a = new bool[0x7fff];
        for (short i=0x7ffe; i < a.Length+1; ++i)
        {
            Console.WriteLine(a[i]);
        }
    }
    internal static void f4c()
    {
        bool [] a = new bool[0x7fff];
        for (short i=0x7ff0; i < a.Length+1; ++i)
        {
            a[i] = true;
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<IndexOutOfRangeException>(f1a);
        Assert.Throws<IndexOutOfRangeException>(f2a);
        Assert.Throws<IndexOutOfRangeException>(f3a);
        Assert.Throws<IndexOutOfRangeException>(f4a);
        Assert.Throws<IndexOutOfRangeException>(f1b);
        Assert.Throws<IndexOutOfRangeException>(f2b);
        Assert.Throws<IndexOutOfRangeException>(f3b);
        Assert.Throws<IndexOutOfRangeException>(f4b);
        Assert.Throws<IndexOutOfRangeException>(f1c);
        Assert.Throws<IndexOutOfRangeException>(f2c);
        Assert.Throws<IndexOutOfRangeException>(f3c);
        Assert.Throws<IndexOutOfRangeException>(f4c);
    }
}
