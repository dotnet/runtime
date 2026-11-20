// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class DiscardedLocalAddress
{
    private struct SpanStub
    {
        public int Length;
        public int Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Slice(int start, int length)
        {
            if ((uint)start > (uint)Length || (uint)length > (uint)(Length - start))
                throw new ArgumentOutOfRangeException();
            return length;
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(13, SliceLength(new SpanStub { Length = 10, Value = 7 }));
        Assert.Equal(7, SliceLength(new SpanStub { Length = 4, Value = 7 }));
        Assert.Equal(0, SliceLength(new SpanStub { Length = 3, Value = 7 }));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SliceLength(SpanStub span)
    {
        // Folding the first check must not expose the local receiver, so the
        // second check can use the facts about span.Length.
        // CHECK-NOT: CORINFO_HELP_THROW
        ref SpanStub alias = ref span;
        if (alias.Length < 4)
            return 0;
        return alias.Slice(0, alias.Length - 4) + span.Value;
    }
}
