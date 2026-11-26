// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for https://github.com/dotnet/runtime/issues/132268
//
// Requires an arm64 AOT compilation (crossgen2/NativeAOT) to hit the byref constant path.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Runtime_132268;

public unsafe class Runtime_132268
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref byte GetNonNullPinnableReference(Span<byte> buffer)
    {
        return ref buffer.Length != 0 ? ref MemoryMarshal.GetReference(buffer) : ref Unsafe.AsRef<byte>((void*)1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Consume(byte* p, int length) => p is null ? -1 : length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Pin(Span<byte> destination)
    {
        fixed (byte* p = &GetNonNullPinnableReference(destination))
        {
            int status = Consume(p, destination.Length);

            if (status != 0)
            {
                throw new InvalidOperationException();
            }

            return status;
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(0, Pin(default));
    }
}
