// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class ExposedLocalsNumbering
{
    private const int UnsafeIndex = 1;

    private static volatile bool s_finished;
    private static int* s_pIndex = (int*)NativeMemory.Alloc(4);

    [Fact]
    public static int TestEntryPoint()
    {
        const int RetryCount = 100;

        try
        {
            new Thread(_ =>
            {
                while (!s_finished)
                {
                    *s_pIndex = UnsafeIndex;
                }
            }).Start();
        }
        catch (PlatformNotSupportedException)
        {
            return 100;
        }

        int[] array = new int[UnsafeIndex + 1];
        array[UnsafeIndex] = 1;

        for (int i = 0; i < RetryCount; i++)
        {
            try
            {
                if (RunBoundsChecks(array.AsSpan(0, UnsafeIndex)) != 0)
                {
                    s_finished = true;
                    return 101;
                }
            }
            catch (IndexOutOfRangeException) { }
        }

        s_finished = true;
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int RunBoundsChecks(Span<int> span)
    {
        int result = 0;
        int index = 0;
        CaptureIndex(&index);

        result += span[index];
        result += span[index];
        result += span[index];
        result += span[index];

        result += span[index];
        result += span[index];
        result += span[index];
        result += span[index];

        result += span[index];
        result += span[index];
        result += span[index];
        result += span[index];

        int* pSafeIndex = (int*)NativeMemory.AllocZeroed(4);
        s_pIndex = pSafeIndex;

        // Wait until the mutator thread sees the switch, so that it
        // doesn't write to the stack of a method that has returned.
        while (Volatile.Read(ref *pSafeIndex) != UnsafeIndex) { }

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CaptureIndex(int* pIndex)
    {
        s_pIndex = pIndex;
    }
}
