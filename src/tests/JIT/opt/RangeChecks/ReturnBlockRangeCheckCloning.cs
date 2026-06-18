// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

    // The element loads dereference span._reference, which can throw NRE.
    // Coalescing the four bounds checks by strengthening the first one to
    // index 3 would change the observable exception from NRE (thrown by the
    // first element load when the bounds check at index 0 passes) into IOOB
    // (thrown by the strengthened check at index 3 when _length is smaller).
    // This method exists so the test below can verify the precedence is
    // preserved: a stack-resident Span with null _reference and _length = 2
    // must throw NullReferenceException, not IndexOutOfRangeException.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int NullRefSpanAccessReturn(ReadOnlySpan<int> span)
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

        // A ReadOnlySpan<int> with null _reference and _length = 2.
        // Accessing span[0] passes the bounds check, then the element load
        // dereferences null and must throw NullReferenceException. The bounds
        // check for span[2] would throw IndexOutOfRangeException, but we must
        // never see that here -- the NRE on span[0]'s element load comes first.
        // If a future change to bounds-check coalescing unsoundly strengthens
        // the first check to index 3 and removes the followers, IOOB would
        // surface in place of NRE and this test would fail.
        ReadOnlySpan<int> nullRefSpan = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.NullRef<int>(), 2);
        bool threwNullRef = false;
        try
        {
            NullRefSpanAccessReturn(nullRefSpan);
        }
        catch (NullReferenceException)
        {
            threwNullRef = true;
        }
        if (!threwNullRef)
            return 0;

        return 100;
    }
}
