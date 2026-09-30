// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// An exception thrown from a finally that is called on the normal path must
// propagate to the caller's catch. Under WebAssembly R2R, a method whose only
// call is the call to its finally did not set up an unwindable frame, so the
// stack walk lost the caller and the exception was reported as unhandled.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134975
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NonReturningFinally()
    {
        try
        {
            goto Done;
        }
        finally
        {
            throw new Exception();
        }
    Done:
        return 0;
    }

    // NoOptimization keeps the finally from being cloned onto the normal path.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static int ReturningFinally(bool doThrow)
    {
        int x = 0;
        try
        {
            x = 1;
        }
        finally
        {
            if (doThrow)
            {
                throw new Exception();
            }
        }

        return x;
    }

    [Fact]
    public static void TestNonReturningFinally()
    {
        bool caught = false;
        try
        {
            NonReturningFinally();
        }
        catch (Exception)
        {
            caught = true;
        }

        Assert.True(caught);
    }

    [Fact]
    public static void TestReturningFinally()
    {
        Assert.Equal(1, ReturningFinally(false));

        bool caught = false;
        try
        {
            ReturningFinally(true);
        }
        catch (Exception)
        {
            caught = true;
        }

        Assert.True(caught);
    }
}
