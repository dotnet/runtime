// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.InteropServices;

namespace DotnetFuzzing.Fuzzers;

internal sealed class SearchValuesStringFuzzer : IFuzzer
{
    public string[] TargetAssemblies => [];
    public string[] TargetCoreLibPrefixes { get; } = ["System.Buffers", "System.Globalization"];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        ReadOnlySpan<char> chars = MemoryMarshal.Cast<byte, char>(bytes);

        int newLine = chars.IndexOf('\n');
        if (newLine < 0)
        {
            return;
        }

        ReadOnlySpan<char> haystack = chars.Slice(newLine + 1);
        string[] needles = chars.Slice(0, newLine).ToString().Split(',');

        using var haystack0 = PooledBoundedMemory<char>.Rent(haystack, PoisonPagePlacement.Before);
        using var haystack1 = PooledBoundedMemory<char>.Rent(haystack, PoisonPagePlacement.After);

        Test(haystack0.Span, haystack1.Span, needles, StringComparison.Ordinal);
        Test(haystack0.Span, haystack1.Span, needles, StringComparison.OrdinalIgnoreCase);
    }

    private static void Test(ReadOnlySpan<char> haystack, ReadOnlySpan<char> haystackCopy, string[] needles, StringComparison comparisonType)
    {
        SearchValues<string> searchValues = SearchValues.Create(needles, comparisonType);

        int index = haystack.IndexOfAny(searchValues);
        Assert.Equal(index, haystackCopy.IndexOfAny(searchValues));
        Assert.Equal(index, IndexOfAnyReferenceImpl(haystack, needles, comparisonType));
    }

    private static int IndexOfAnyReferenceImpl(ReadOnlySpan<char> haystack, string[] needles, StringComparison comparisonType)
    {
        int minIndex = int.MaxValue;

        foreach (string needle in needles)
        {
            int i = haystack.IndexOf(needle, comparisonType);
            if ((uint)i < minIndex)
            {
                minIndex = i;
            }
        }

        return minIndex == int.MaxValue ? -1 : minIndex;
    }
}
