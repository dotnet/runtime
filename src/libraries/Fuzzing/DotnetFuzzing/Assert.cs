// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace DotnetFuzzing;

internal static class Assert
{
    // Feel free to add any other helpers you need.

    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            Throw(expected, actual);
        }

        static void Throw(T expected, T actual) =>
            throw new AssertException($"Expected={expected} Actual={actual}");
    }

    public static void SequenceEqual<T>(ReadOnlySpan<T> expected, ReadOnlySpan<T> actual)
    {
        if (!expected.SequenceEqual(actual))
        {
            Throw(expected, actual);
        }

        static void Throw(ReadOnlySpan<T> expected, ReadOnlySpan<T> actual)
        {
            Equal(expected.Length, actual.Length);

            int diffIndex = expected.CommonPrefixLength(actual);

            throw new AssertException($"Expected={expected[diffIndex]} Actual={actual[diffIndex]} at index {diffIndex}");
        }
    }

    public static void SequenceEqual(ReadOnlySpan<char> expected, StringBuilder actual)
    {
        Equal(expected.Length, actual.Length);

        foreach (ReadOnlyMemory<char> chunk in actual.GetChunks())
        {
            SequenceEqual(expected.Slice(0, chunk.Length), chunk.Span);

            expected = expected.Slice(chunk.Length);
        }

        Equal(0, expected.Length);
    }

    private sealed class AssertException : Exception
    {
        public AssertException(string message) : base(message) { }
    }
}
