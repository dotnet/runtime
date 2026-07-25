// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Runtime_125805
{
    [Fact]
    public static void TestEntryPoint()
    {
        ExceptionReuse().GetAwaiter().GetResult();
    }

    private static async Task ExceptionReuse()
    {
        try
        {
            await Throws();
            Assert.Fail("Expected throw");
        }
        catch (NullReferenceException)
        {
        }

        try
        {
            await NoThrow();
        }
        catch (Exception ex)
        {
            Assert.Fail("Did not expect throw: " + ex);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task Throws()
    {
        await Task.Yield();
        throw new NullReferenceException();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task NoThrow()
    {
        await Task.Yield();
    }
}

