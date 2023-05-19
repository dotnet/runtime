// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class InnerLoop
{
    [Fact]
    public static int TestEntryPoint()
    {
        int[] a = new int[1000];
        a[555] = 1;

        int result = 0;

        for (int i = 0; i < 1000; i++)
        {
            for (int j = i; j < 1000; j++)
            {
                result += a[j];
            }
        }

        return result - 456;
    }
}
