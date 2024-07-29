// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace IndexingSideEffects;

public class IndexingSideEffects
{
    [Fact]
    public static int TestEntryPoint()
    {
        if (!Problem())
        {
            return 101;
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Problem()
    {
        bool result = false;

        try
        {
            TryIndexing(Array.Empty<int>());
        }
        catch (IndexOutOfRangeException)
        {
            result = true;
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TryIndexing(int[] a)
    {
        // Make sure that early flowgraph simplification does not remove the side effect of indexing
        // when deleting the relop.
        if (a[int.MaxValue] == 0)
        {
            NopInlinedCall();
        }
    }

    private static void NopInlinedCall() { }
}
