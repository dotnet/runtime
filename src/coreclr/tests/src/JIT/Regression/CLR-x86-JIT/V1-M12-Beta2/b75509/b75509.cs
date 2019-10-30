// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;

public class Foo
{
    public static int Main(string[] args)
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
