// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DotnetFuzzing;

internal static class Assert
{
    // Feel free to add any other helpers as needed.

    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            Throw(expected, actual);
        }

        static void Throw(T expected, T actual) =>
            throw new Exception($"Expected={expected} Actual={actual}");
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

            throw new Exception($"Expected={expected[diffIndex]} Actual={actual[diffIndex]} at index {diffIndex}");
        }
    }
}
