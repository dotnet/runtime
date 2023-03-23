// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public sealed class TestException : Exception
{
    private int counter;
    public TestException(int counter, string format, params object[] args)
        : base(String.Format(format, args))
    {
        this.counter = counter;
    }

    public int Counter
    {
        get { return this.counter; }
    }

    public override string Message
    {
        get { return String.Format("{0}: {1}", this.Counter, base.Message); }
    }
}

public static class Test_nullabletypes
{
    public static int counter = 0;

    internal static void IsFalse(bool value)
    {
        counter++;
        if (value)
            throw new TestException(counter, "Expected false, got true");
    }

    internal static void Eval(bool expr)
    {
        counter++;
        if (!expr)
            throw new TestException(counter, "Expected true, got false");
    }

    internal static void Eval(object obj1, object obj2)
    {
        counter++;
        if (!((obj1 != null) && (obj2 != null) && (obj1.GetType().Equals(obj2.GetType())) && obj1.Equals(obj2)))
            throw new TestException(counter, "Failure while Comparing {1} to {2}", obj1, obj2);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            NullableTests.Run();
            Console.WriteLine("Test SUCCESS");
            return 100;
        }
        catch (TestException ex)
        {
            Console.WriteLine(ex);
            Console.WriteLine("Test FAILED");
            return ex.Counter + 101;
        }
    }
}
