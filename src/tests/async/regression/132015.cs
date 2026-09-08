// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Runtime_132015
{
    [Fact]
    public static void AssignInFilterIsVisibleAfterSuspension()
    {
        Assert.Equal("success", AssignInFilter().GetAwaiter().GetResult());
    }

    [Fact]
    public static void AssignInNestedFinallyIsVisibleAfterSuspension()
    {
        Assert.Equal("success", AssignInNestedFinally().GetAwaiter().GetResult());
        Assert.Equal("success", AssignInNestedFinallyWithFilter().GetAwaiter().GetResult());
    }

    // The filter assigns the exception variable, which must survive the
    // suspension inside the handler.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<string> AssignInFilter()
    {
        try
        {
            throw new Exception("thrown");
        }
        catch (Exception caught) when ((caught = new Exception("success")) != null)
        {
            await Task.Yield();
            return caught.Message;
        }
    }

    // The nested finally runs during the second pass, before the handler is
    // entered, so its assignment must survive the suspension in the handler.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<string> AssignInNestedFinally()
    {
        string s = null;
        try
        {
            try
            {
                throw new Exception("thrown");
            }
            finally
            {
                s = "success";
            }
        }
        catch (Exception)
        {
            await Task.Yield();
            return s ?? "<null>";
        }
    }

    // Same as above, but a filter runs before the nested finally, so the
    // handler must not assume the value that the filter observed.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<string> AssignInNestedFinallyWithFilter()
    {
        string s = null;
        try
        {
            try
            {
                throw new Exception("thrown");
            }
            finally
            {
                s = "success";
            }
        }
        catch (Exception) when (s is null)
        {
            await Task.Yield();
            return s ?? "<null>";
        }
    }
}
