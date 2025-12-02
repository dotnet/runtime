// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DotnetFuzzing.Fuzzers;

internal sealed class SearchValuesByteCharFuzzer : IFuzzer
{
    public string[] TargetAssemblies => [];
    public string[] TargetCoreLibPrefixes { get; } = ["System.Buffers", "System.SpanHelpers", "System.PackedSpanHelpers"];

    public unsafe void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        int newLine = bytes.IndexOf((byte)'\n');
        if (newLine < 0)
        {
            return;
        }

        ReadOnlySpan<byte> haystack = bytes.Slice(newLine + 1);
        ReadOnlySpan<byte> values = bytes.Slice(0, newLine);

        using var byteHaystack0 = PooledBoundedMemory<byte>.Rent(haystack, PoisonPagePlacement.Before);
        using var byteHaystack1 = PooledBoundedMemory<byte>.Rent(haystack, PoisonPagePlacement.After);

        Test(byteHaystack0.Span, byteHaystack1.Span, values, SearchValues.Create(values));

        if ((nuint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(haystack)) % 2 != 0 && !haystack.IsEmpty)
        {
            // Ensure that the haystack is 2-byte aligned now that we're about to cast it to chars.
            haystack = haystack.Slice(1);
        }

        using var charHaystack0 = PooledBoundedMemory<char>.Rent(MemoryMarshal.Cast<byte, char>(haystack), PoisonPagePlacement.Before);
        using var charHaystack1 = PooledBoundedMemory<char>.Rent(MemoryMarshal.Cast<byte, char>(haystack), PoisonPagePlacement.After);

        ReadOnlySpan<char> charValues = MemoryMarshal.Cast<byte, char>(values);
        Test(charHaystack0.Span, charHaystack1.Span, charValues, SearchValues.Create(charValues));
    }

    private static void Test<T>(ReadOnlySpan<T> haystack, ReadOnlySpan<T> haystackCopy, ReadOnlySpan<T> values, SearchValues<T> searchValues)
        where T : struct, INumber<T>, IMinMaxValue<T>
    {
        int indexOfAny = haystack.IndexOfAny(searchValues);
        int indexOfAnyExcept = haystack.IndexOfAnyExcept(searchValues);
        int lastIndexOfAny = haystack.LastIndexOfAny(searchValues);
        int lastIndexOfAnyExcept = haystack.LastIndexOfAnyExcept(searchValues);

        Assert.Equal(indexOfAny, haystackCopy.IndexOfAny(searchValues));
        Assert.Equal(indexOfAnyExcept, haystackCopy.IndexOfAnyExcept(searchValues));
        Assert.Equal(lastIndexOfAny, haystackCopy.LastIndexOfAny(searchValues));
        Assert.Equal(lastIndexOfAnyExcept, haystackCopy.LastIndexOfAnyExcept(searchValues));

        Assert.Equal(IndexOfAnyScalar(haystack, searchValues), indexOfAny);
        Assert.Equal(IndexOfAnyExceptScalar(haystack, searchValues), indexOfAnyExcept);
        Assert.Equal(LastIndexOfAnyScalar(haystack, searchValues), lastIndexOfAny);
        Assert.Equal(LastIndexOfAnyExceptScalar(haystack, searchValues), lastIndexOfAnyExcept);

        Assert.Equal(indexOfAny >= 0, lastIndexOfAny >= 0);
        Assert.Equal(indexOfAnyExcept >= 0, lastIndexOfAnyExcept >= 0);

        Assert.Equal(indexOfAny >= 0, haystack.ContainsAny(searchValues));
        Assert.Equal(indexOfAnyExcept >= 0, haystack.ContainsAnyExcept(searchValues));

        if (!values.IsEmpty)
        {
            Assert.Equal(0, values.IndexOfAny(searchValues));
            Assert.Equal(-1, values.IndexOfAnyExcept(searchValues));
        }
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
