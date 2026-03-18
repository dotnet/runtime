// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Tests that consecutive range checks in return blocks are combined
// via range check cloning.

public class ReturnBlockRangeCheckCloning
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ArrayAccessReturn(int[] abcd)
    {
        return abcd[0] + abcd[1] + abcd[2] + abcd[3];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ArrayAccessReturnWithOffset(int[] arr, int i)
    {
        return arr[i] + arr[i + 1] + arr[i + 2];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int SpanAccessReturn(ReadOnlySpan<int> span)
    {
        return span[0] + span[1] + span[2] + span[3];
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int[] arr = new int[] { 10, 20, 30, 40 };

        if (ArrayAccessReturn(arr) != 100)
            return 0;

        if (ArrayAccessReturnWithOffset(arr, 0) != 60)
            return 0;

        if (ArrayAccessReturnWithOffset(arr, 1) != 90)
            return 0;

        if (SpanAccessReturn(arr) != 100)
            return 0;

        // Test that out-of-range still throws
        bool threwArrayAccess = false;
        try
        {
            ArrayAccessReturn(new int[] { 1, 2, 3 });
        }
        catch (IndexOutOfRangeException)
        {
            threwArrayAccess = true;
        }
        if (!threwArrayAccess)
            return 0;

        bool threwOffset = false;
        try
        {
            ArrayAccessReturnWithOffset(arr, 2);
        }
        catch (IndexOutOfRangeException)
        {
            threwOffset = true;
        }
        if (!threwOffset)
            return 0;

        return 100;
    }
}
