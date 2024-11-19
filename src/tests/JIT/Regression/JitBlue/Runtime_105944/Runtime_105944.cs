// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public unsafe class Runtime_105944
{
    [Fact]
    public static void TestEntryPoint()
    {
        if (!Sve.IsSupported)
        {
            return;
        }
        
        using BoundedMemory<byte> memory = BoundedMemory.Allocate<byte>(Vector<byte>.Count);
        fixed (byte* pMemory = &memory.Span.GetPinnableReference())
        {
            Assert.True(ReorderSetGetFfr(pMemory));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ReorderSetGetFfr(byte* pMemory)
    {
        Sve.SetFfr(Sve.CreateTrueMaskByte());
        Sve.LoadVectorFirstFaulting(Sve.CreateTrueMaskByte(), pMemory + 1);

        if (Sve.GetFfrByte() == Sve.CreateTrueMaskByte())
        {
            Sve.SetFfr(Sve.CreateTrueMaskByte());
            return false;
        }
        else
        {
            Sve.SetFfr(Sve.CreateTrueMaskByte());
            return true;
        }
    }
}
