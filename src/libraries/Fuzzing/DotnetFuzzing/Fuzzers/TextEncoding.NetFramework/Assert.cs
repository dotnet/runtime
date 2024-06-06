// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace DotnetFuzzing
{
    internal static class Assert
    {
        public static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new Exception($"Expected={expected} Actual={actual}");
            }
        }

        public static void SequenceEqual(ReadOnlySpan<char> expected, ReadOnlySpan<char> actual)
        {
            if (!expected.SequenceEqual(actual))
            {
                Equal(expected.Length, actual.Length);
                int diffIndex = expected.CommonPrefixLength(actual);
                throw new Exception($"Expected={expected[diffIndex]} Actual={actual[diffIndex]} at index {diffIndex}");
            }
        }

        public static int CommonPrefixLength(this ReadOnlySpan<char> span, ReadOnlySpan<char> other)
        {
            int length = Math.Min(span.Length, other.Length);

            for (int i = 0; i < length; i++)
            {
                if (span[i] != other[i])
                {
                    return i;
                }
            }

            return length;
        }
    }
}
