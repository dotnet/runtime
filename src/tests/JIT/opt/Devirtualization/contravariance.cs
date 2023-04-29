// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

interface I<out T>
{
    T A();
}

class X<T> : I<T> where T: class
{
    T I<T>.A()
    {
        return (T)(object)"X";
    }
}

public class T
{
    static object F(I<object> i)
    {
        return i.A();
    }

    [Fact]
    public static int TestEntryPoint()
    {
        // Jit should inline F and then devirtualize the call to A.
        // (inlining A blocked by runtime lookup)
        object j = F(new X<string>());
        if (j is string)
        {
            return ((string)j)[0] + 12;
        }
        return -1;
    }
}
