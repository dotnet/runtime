// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Base
{
    public virtual int Foo(int x) 
    { 
        return x + 1;
    }
}

// Override via a shared generic.
//
// Jit must be careful to set the right context
// for the shared methods when devirtualizing.

public class Derived<T> : Base
{
    public override sealed int Foo(int x)
    {
        if (typeof(T) == typeof(string))
        {
            return x + 42;
        }
        else if (typeof(T) == typeof(int))
        {
            return x + 31;
        }
        else 
        {
            return x + 22;
        }
    }
}

// All calls to Foo should devirtualize, however we can't
// get the b.Foo case yet because we don't recognize b
// as having an exact type.

public class Test_sharedoverride
{
    [Fact]
    public static int TestEntryPoint()
    {
        var ds = new Derived<string>();
        var dx = new Derived<object>();
        var di = new Derived<int>();
        var b  = new Base();

        int resultD = ds.Foo(1) + dx.Foo(1) + di.Foo(1) + b.Foo(1);

        return resultD;
    }
}
