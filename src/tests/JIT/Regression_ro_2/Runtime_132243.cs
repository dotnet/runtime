// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_132243;

public class Runtime_132243
{
    private const int MaxLocalBufferLength = 261;

    // The guarding branch is what bounds the allocation size, so the localloc must not be
    // made unconditional - that is what the interop string marshalling stubs rely on.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static nint AllocateGuarded(int size)
    {
        unsafe
        {
            byte* buffer = null;
            if (size <= MaxLocalBufferLength)
            {
                byte* stackBuffer = stackalloc byte[size];
                buffer = stackBuffer;
            }

            return (nint)buffer;
        }
    }

    [Fact]
    public static void ConditionalLocallocIsNotSpeculated()
    {
        Assert.NotEqual(0, AllocateGuarded(16));
        Assert.Equal(0, AllocateGuarded(int.MaxValue / 2));
    }
}
