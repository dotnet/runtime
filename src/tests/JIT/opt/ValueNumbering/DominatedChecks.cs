// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class DominatedChecks
{
    private static int s_marker, s_sink;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Observe() { }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void FinallyCheck(int[] values)
    {
        try
        {
            s_sink = values.Length;
        }
        finally
        {
            if (values.Length == values.Length)
                s_marker = 1;
            // Nested EH prevents finally cloning without a configuration switch.
            try { Observe(); }
            catch (InvalidOperationException) { s_marker = 2; }
        }
    }

    [Fact]
    public static void FailedTryDoesNotEstablishFinallyFact()
    {
        s_marker = 0;
        Assert.Throws<NullReferenceException>(() => FinallyCheck(null));
        Assert.Equal(0, s_marker);
    }

}
