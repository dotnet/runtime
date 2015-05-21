// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections;

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

    public static int Main(String[] args)
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

