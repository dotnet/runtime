// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections;
using Xunit;

public delegate void MyDelegate();

public class GenType<T>
{
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public void foo()
    {
        bar();
    }

    public MyDelegate bar()
    {
        return new MyDelegate(this.baz);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public virtual void baz()
    {
    }
}

public class cs1
{
    internal static int s_Zero = 0;
    internal static int s_i = 0;

    public cs1()
    {
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void foo()
    {
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            GenType<string> o = new GenType<string>();
            o.foo();
            Console.WriteLine("Test SUCCESS");
            return 100;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Console.WriteLine("Test FAILED");
            return 666;
        }
    }
}

