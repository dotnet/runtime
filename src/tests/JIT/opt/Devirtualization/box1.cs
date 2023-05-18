// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

interface IPrint 
{
    void Print();
}

struct X<T> : IPrint
{
    public X(T t) { _t = t; }
    public void Print() { Console.WriteLine(_t); }
    T _t;
}

public class Y
{
    [Fact]
    public static int TestEntryPoint()
    {
        var s = new X<string>("hello, world!");
        // Jit should devirtualize, remove box,
        // change to call unboxed entry, then inline.
        ((IPrint)s).Print();
        return 100;
    }
}
