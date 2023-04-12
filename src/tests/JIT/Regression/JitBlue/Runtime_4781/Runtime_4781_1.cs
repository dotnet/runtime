// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

// Test for proper ordering of a gc safepoint inducing arg and
// the newobj allocation

class Bar
{
    public Bar() 
    { 
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}

static class Observer
{
    public static bool failed;
}

class Foo : IDisposable
{
    
    public Foo(Bar b)
    {
        Console.WriteLine($"new Foo");
    }

    ~Foo() 
    {
        Console.WriteLine($"~Foo");
        Observer.failed = true;
    }

    public void Dispose() => GC.SuppressFinalize(this);
}

public class Runtime_4781_1
{
    static Bar s_bar = new Bar();

    [Fact]
    public static int TestEntryPoint()
    {
        var f = new Foo(s_bar);
        return Observer.failed ? -1 : 100;
    }
}
