// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Reflection;
using Xunit;

public class My
{

    static void bar()
    {
        Other.field = 123;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void foo(bool f)
    {
        if (f) 
            bar();
    }

    public static void Worker()
    {
        try
        {
            throw new Exception("Hello world");
        }
        finally
        {
            foo(false);
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            typeof(My).GetMethod("Worker").Invoke(null, null);

            Console.WriteLine("FAILED: No exception thrown.");
            return 102;
        }
        catch (TargetInvocationException e)
        {
            Exception inner = e.GetBaseException();
            Console.WriteLine(inner);

            if (inner.Message == "Hello world")
            {
                Console.WriteLine("PASSED: Caught expected exception");
                return 100;
            }
            else
            {
                Console.WriteLine("FAILED: Wrong exception thrown. Expected: Exception with message 'Hello world'. Actual: " + inner.Message);
                return 101;
            }
        }
    }

}
