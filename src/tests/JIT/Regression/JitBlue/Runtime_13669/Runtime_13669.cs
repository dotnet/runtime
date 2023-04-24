// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        int result = 0;

        ReadOnlySpan<char> span = string.Empty.AsSpan();
        for (int i = 0; i < 1_000; i++)
        {
            result ^= TrimSourceCopied(span).Length;
        }

        return (result == 0) ? 100 : -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ReadOnlySpan<char> TrimSourceCopied(ReadOnlySpan<char> span)
    {
        int start = 0;
        for (; start < span.Length; start++)
        {
            if (!char.IsWhiteSpace(span[start]))
            {
                break;
            }
        }

        int end = span.Length - 1;
        for (; end > start; end--)
        {
            if (!char.IsWhiteSpace(span[end]))
            {
                break;
            }
        }

        return span.Slice(start, end - start + 1);
    }
}
