// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

public static class ThreadStaticAlignmentFallback
{
    private const int Pass = 100;
    private const int Fail = -1;
    private const int PaddingCount = 12;

    public static int Main()
    {
        int paddingCount = new Random().Next(PaddingCount);
        Console.WriteLine($"Using {paddingCount} padding fields.");
        return InitializePadding<object>(paddingCount) && LongStorage.Check() ? Pass : Fail;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool InitializePadding<T>(int count)
    {
        if (count == 0)
        {
            return true;
        }

        if (!Padding<T>.Check())
        {
            return false;
        }

        return InitializePadding<Padding<T>>(count - 1);
    }

    private sealed class Padding<T>
    {
        [ThreadStatic]
        private static int s_value;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Check()
        {
            if (s_value != 0)
            {
                return false;
            }

            s_value = 42;
            return s_value == 42;
        }
    }

    private static class LongStorage
    {
        [ThreadStatic]
        private static long s_value;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Check()
        {
            if (Interlocked.Increment(ref s_value) != 1)
            {
                return false;
            }

            s_value = 0x1122334455667788;
            return s_value == 0x1122334455667788;
        }
    }
}
