// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Runtime_132017
{
    private static readonly List<string> s_log = new List<string>();
    private static int s_calls;

    // The nested catch starts this task without awaiting it; keep it around so
    // that the test can wait for it deterministically.
    private static Task s_pending;

    [Fact]
    public static void TestEntryPoint()
    {
        s_log.Clear();
        s_calls = 0;
        s_pending = null;

        var test = new Runtime_132017();
        test.M(false).GetAwaiter().GetResult();
        test.M(true).GetAwaiter().GetResult();
        s_pending.GetAwaiter().GetResult();

        Assert.Equal(new[] { "filter", "filter", "outer", "filter", "outer" }, s_log);
        Assert.Equal(2, s_calls);
    }

    private static bool Filter(bool result)
    {
        s_log.Add("filter");
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private async Task M(bool first)
    {
        try
        {
            throw new Exception("outer");
        }
        catch (Exception ex) when (Filter(first))
        {
            await Print(ex);
        }
        catch (Exception ex) when (Filter(true))
        {
            try
            {
                await Task.Yield();
                throw new Exception();
            }
            catch
            {
                s_pending = Print(ex);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task Print(Exception ex)
    {
        string message = ex.Message;
        s_log.Add(message);
        await Task.Yield();
        s_calls++;
    }
}
