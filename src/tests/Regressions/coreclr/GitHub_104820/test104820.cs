// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Test104820
{
    // Test that SSP is updated properly when resuming after catch when the shadow stack
    // contains the instruction pointer of the resume frame multiple times and the SSP needs
    // to be restored to the location of one that's not the first one found on the shadow
    // stack.
    // This test fails with stack overflow of the shadow stack if the SSP is updated incorrectly.
    static void ShadowStackPointerUpdateTest(int depth)
    {
        if (depth == 0)
        {
            throw new Exception();
        }

        for (int i = 0; i < 1000; i++)
        {
            try
            {
                try
                {
                    ShadowStackPointerUpdateTest(depth - 1);
                }
                catch (Exception)
                {
                    throw new Exception();
                }
            }
            catch (Exception) when (depth == 100)
            {
            }
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
	ShadowStackPointerUpdateTest(100);
    }
}
