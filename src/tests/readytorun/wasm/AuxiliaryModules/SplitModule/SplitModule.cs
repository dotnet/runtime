// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

public class SplitModule
{
    [Fact]
    public static void RunBeforeActivation()
    {
        // This test is compiled with the 'names' section, so the method name should exist
        // in the Auxiliary module.
        byte[] methodName = Encoding.UTF8.GetBytes(nameof(EvaluateColdBody));
        Assert.True(File.ReadAllBytes("composite-r2r.1.wasm").AsSpan().IndexOf(methodName) >= 0,
            $"{nameof(EvaluateColdBody)} must have a body in the auxiliary module.");

        Assert.Equal(42, EvaluateColdBody(1));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int EvaluateColdBody(int value) => value * 37 + 5;
}
