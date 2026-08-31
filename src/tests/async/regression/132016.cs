// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Runtime_132016
{
    private static readonly List<string> s_log = new List<string>();

    [Fact]
    public static void TestEntryPoint()
    {
        s_log.Clear();
        new Runtime_132016().M().GetAwaiter().GetResult();

        Assert.Equal(new[] { "outer", "outer", "inner", "inner", "outer", "inner" }, s_log);
    }

    private static bool Filter(Exception ex, bool result)
    {
        s_log.Add(ex.Message);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private async Task M()
    {
        try
        {
            throw new Exception("outer");
        }
        catch (Exception ex) when (Filter(ex, false))
        {
            await Print(ex);
        }
        catch (Exception ex) when (Filter(ex, true))
        {
            try
            {
                throw new Exception("inner");
            }
            catch (Exception ex2) when (Filter(ex2, false))
            {
                await Print(ex);
                await Print(ex2);
            }
            catch (Exception ex2) when (Filter(ex2, true))
            {
                try
                {
                    throw new Exception();
                }
                catch
                {
                    await Print(ex);
                    await Print(ex2);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task Print(Exception ex)
    {
        await Task.Yield();
        s_log.Add(ex.Message);
    }
}
