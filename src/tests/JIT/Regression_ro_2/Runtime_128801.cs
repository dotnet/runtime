// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for an x86 GC hole in Tier0/minopts code: a stack-to-stack
// copy of a ReadOnlySpan<T> went through the unroll path that loaded the
// byref slot into a scratch register without reporting it in the GC info,
// so a moving GC between the load and the store could leave the local with
// a stale byref pointing into a free object. Needs GCStress to reliably
// reproduce; CI pipelines that exercise GCStress will catch a regression.

namespace Runtime_128801;

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_128801
{
    [Fact]
    public static int TestEntryPoint()
    {
        byte[][] buffers = new byte[256][];
        for (int i = 0; i < buffers.Length; i++)
        {
            buffers[i] = new byte[2];
        }

        for (int i = 0; i < 50_000; i++)
        {
            if (!SpanCopyAndUse(buffers[i % buffers.Length]))
            {
                return -1;
            }
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool SpanCopyAndUse(Span<byte> s)
    {
        Span<byte> local = s;
        if (local.Length < 2)
        {
            return false;
        }
        ref byte p = ref local[0];
        ref byte q = ref local[1];
        if (Unsafe.ByteOffset(ref p, ref q) != new IntPtr(1))
        {
            return false;
        }
        p = 1;
        q = 2;
        return true;
    }
}
