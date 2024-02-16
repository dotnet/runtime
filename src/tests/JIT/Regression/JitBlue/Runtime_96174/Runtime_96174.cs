// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_96174
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MethodWithoutLocalsInit()
    {
        SmallStackalloc();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MethodWithLoop()
    {
        for (int i = 0; i < 10000; i++)
        {
            SmallStackalloc();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SmallStackalloc()
    {
        Span<byte> buf = stackalloc byte[4];
        if (!buf.SequenceEqual("\0\0\0\0"u8))
            throw new InvalidOperationException("Stackalloc was not zero-initialized");
        Consume(buf);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(Span<byte> _) {}

    [Fact]
    public static void Test()
    {
        MethodWithoutLocalsInit();
        MethodWithLoop();
    }
}
