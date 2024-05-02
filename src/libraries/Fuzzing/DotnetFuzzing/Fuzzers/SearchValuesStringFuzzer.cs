// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.InteropServices;

namespace DotnetFuzzing.Fuzzers;

internal sealed class SearchValuesStringFuzzer : IFuzzer
{
    public string[] TargetAssemblies => [];
    public string[] TargetCoreLibPrefixes => ["System.Buffers", "System.Globalization"];

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

        Test(haystack, needles, StringComparison.Ordinal);
        Test(haystack, needles, StringComparison.OrdinalIgnoreCase);
    }

    private static void Test(ReadOnlySpan<char> haystack, string[] needles, StringComparison comparisonType)
    {
        SearchValues<string> searchValues = SearchValues.Create(needles, comparisonType);

        Assert.Equal(IndexOfAnyReferenceImpl(haystack, needles, comparisonType), haystack.IndexOfAny(searchValues));
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
