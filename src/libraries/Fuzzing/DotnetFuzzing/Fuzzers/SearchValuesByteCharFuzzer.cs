// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;

namespace DotnetFuzzing.Fuzzers;

internal sealed class SearchValuesByteCharFuzzer : IFuzzer
{
    public string[] TargetAssemblies => [];
    public string[] TargetCoreLibPrefixes => ["System.Buffers", "System.SpanHelpers", "System.PackedSpanHelpers"];
    public string BlameAlias => "mizupan";

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        int newLine = bytes.IndexOf((byte)'\n');
        if (newLine < 0)
        {
            return;
        }

        ReadOnlySpan<byte> haystack = bytes.Slice(newLine + 1);
        ReadOnlySpan<byte> values = bytes.Slice(0, newLine);

        Test(haystack, values, SearchValues.Create(values));

        Test(MemoryMarshal.Cast<byte, char>(haystack), MemoryMarshal.Cast<byte, char>(values), SearchValues.Create(MemoryMarshal.Cast<byte, char>(values)));
    }

    private static void Test<T>(ReadOnlySpan<T> haystack, ReadOnlySpan<T> values, SearchValues<T> searchValues)
        where T : struct, INumber<T>, IMinMaxValue<T>
    {
        int indexOfAny = haystack.IndexOfAny(searchValues);
        int indexOfAnyExcept = haystack.IndexOfAnyExcept(searchValues);
        int lastIndexOfAny = haystack.LastIndexOfAny(searchValues);
        int lastIndexOfAnyExcept = haystack.LastIndexOfAnyExcept(searchValues);

        Assert.Equal(IndexOfAnyScalar(haystack, searchValues), indexOfAny);
        Assert.Equal(IndexOfAnyExceptScalar(haystack, searchValues), indexOfAnyExcept);
        Assert.Equal(LastIndexOfAnyScalar(haystack, searchValues), lastIndexOfAny);
        Assert.Equal(LastIndexOfAnyExceptScalar(haystack, searchValues), lastIndexOfAnyExcept);

        Assert.Equal(indexOfAny >= 0, lastIndexOfAny >= 0);
        Assert.Equal(indexOfAnyExcept >= 0, lastIndexOfAnyExcept >= 0);

        Assert.Equal(indexOfAny >= 0, haystack.ContainsAny(searchValues));
        Assert.Equal(indexOfAnyExcept >= 0, haystack.ContainsAnyExcept(searchValues));

        Assert.Equal(0, values.IndexOfAny(searchValues));
        Assert.Equal(-1, values.IndexOfAnyExcept(searchValues));
    }

    private static int IndexOfAnyScalar<T>(ReadOnlySpan<T> haystack, SearchValues<T> values)
        where T : IEquatable<T>
    {
        for (int i = 0; i < haystack.Length; i++)
        {
            if (values.Contains(haystack[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static int IndexOfAnyExceptScalar<T>(ReadOnlySpan<T> haystack, SearchValues<T> values)
        where T : IEquatable<T>
    {
        for (int i = 0; i < haystack.Length; i++)
        {
            if (!values.Contains(haystack[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static int LastIndexOfAnyScalar<T>(ReadOnlySpan<T> haystack, SearchValues<T> values)
        where T : IEquatable<T>
    {
        for (int i = haystack.Length - 1; i >= 0; i--)
        {
            if (values.Contains(haystack[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static int LastIndexOfAnyExceptScalar<T>(ReadOnlySpan<T> haystack, SearchValues<T> values)
        where T : IEquatable<T>
    {
        for (int i = haystack.Length - 1; i >= 0; i--)
        {
            if (!values.Contains(haystack[i]))
            {
                return i;
            }
        }

        return -1;
    }
}
