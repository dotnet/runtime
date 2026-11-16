// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class Runtime_137196
{
    private static int s_lastAttempt;

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(nameof(OperationCanceledException), RetryOnce());
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static string RetryOnce()
    {
        Exception? firstFailure = null;
        int attempt = 0;
        while (true)
        {
            try
            {
                Invoke(attempt++);
                return "unexpected success";
            }
            catch (InvalidOperationException ex)
            {
                firstFailure = ex;
            }
            catch (OperationCanceledException ex) when (firstFailure is not null)
            {
                return ex.GetType().Name;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Invoke(int attempt)
    {
        try
        {
            Throw(attempt);
        }
        finally
        {
            Volatile.Write(ref s_lastAttempt, attempt);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Throw(int attempt)
    {
        if (attempt == 0)
        {
            throw new InvalidOperationException("Retry once.");
        }

        throw new OperationCanceledException("Cancellation after retry.");
    }
}
