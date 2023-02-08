// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class Foo
{
    [Fact]
    public static int TestEntryPoint()
    {
        Foo o = new Foo();
        Object a = 2.718281828458999;
        System.Console.WriteLine(o.Convert(o.Compare(a, 2.718281828458999) ? 1 : 0));
        return 100;
    }

    public Boolean Compare(Object a, Object b)
    {
        return (double)a == (double)b;
    }

    public String Convert(Object o)
    {
        return o.ToString();
    }
}
