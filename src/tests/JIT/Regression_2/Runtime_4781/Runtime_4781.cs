// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

// Test for proper ordering of exception-causing ctor args and
// the newobj allocation

class Foo : IDisposable
{
    public bool IsConstructed { get; } = true;
    public Foo(int ignored) { }
    
    ~Foo()
    {
        if (!IsConstructed)
        {
            Console.WriteLine("Finalizing a non-constructed object?!");
            Runtime_4781.Fail();
        }
    }
    
    public void Dispose() => GC.SuppressFinalize(this);
}

public class Runtime_4781
{
    private static int Throw() => throw new NotSupportedException();
    private static bool failed = false;
    internal static void Fail() { failed = true; }
    
    private static IDisposable Test()
    {
        try
        {
            int x = Throw();
            return new Foo(x);
        }
        catch
        {
        }
        return new Foo(2);
    }
    
    [Fact]
    public static int TestEntryPoint()
    {
        Test().Dispose();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        return failed ? -1 : 100;
    }
}
