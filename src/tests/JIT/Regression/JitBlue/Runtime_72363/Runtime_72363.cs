// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

public class Runtime_72363
{
    public static int Main()
    {
        var bi = new ListImpl();
        for (int i = 0; i < 100; i++)
        {
            Check(bi);

            if (i > 30 && i < 40)
            {
                Thread.Sleep(10);
            }
        }

        if (Check(new StringImpl()))
        {
            Console.WriteLine("FAIL: A string is not an IList<int>!");
            return -1;
        }

        Console.WriteLine("PASS");
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Check(IFace<object> i)
    {
        // We would use type information about the GDV guess here to optimize.
        if (i.Foo() is IList<int>)
            return true;

        return false;
    }
}

public interface IFace<out T>
{
    T Foo();
}

public class ListImpl : IFace<List<int>>
{
    public List<int> Foo()
    {
        return new List<int> { 10 };
    }
}

public class StringImpl : IFace<string>
{
    public string Foo()
    {
        return "Hello, world!";
    }
}
