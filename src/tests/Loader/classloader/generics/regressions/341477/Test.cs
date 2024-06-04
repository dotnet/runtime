// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is regression test for VSW 341477
// we were getting an assert failure due to using non-ASCII characters.

using System;
using Xunit;

public class Test_341477
{
    [Fact]
    public static void TestEntryPoint()
    {
        Hello<string> mystr = new Hello<string>("PASS");

        mystr.InstanceMethod<A>();
    }
}

public class A
{
    public A() {}
}

public class Hello<li\u0131\u0130>
{
    public li\u0131\u0130 a;
    public Hello (li\u0131\u0130 t)
    {
        a = t;
        Console.WriteLine (a.ToString ());
    }

    public \u043E\u0434\u0438\u043D InstanceMethod<\u043E\u0434\u0438\u043D> () where \u043E\u0434\u0438\u043D : new()
    {
        return new \u043E\u0434\u0438\u043D();

    }
}
