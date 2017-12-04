// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System
{
    /// <summary>Helpers for string-like operations on spans of chars.</summary>
    internal static class StringSpanHelpers
    {
        // TODO https://github.com/dotnet/corefx/issues/21395: Provide public, efficient implementations

        public static bool Equals(this ReadOnlySpan<char> left, ReadOnlySpan<char> right, StringComparison comparisonType) =>
            comparisonType == StringComparison.Ordinal ? Equals(left, right) :
            comparisonType == StringComparison.OrdinalIgnoreCase ? EqualsOrdinalIgnoreCase(left, right) :
            throw new ArgumentOutOfRangeException(nameof(comparisonType));

        public static bool Equals(this ReadOnlySpan<char> left, string right) =>
            Equals(left, right.AsReadOnlySpan());

        public static bool Equals(this ReadOnlySpan<char> left, ReadOnlySpan<char> right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }
            
            return true;
        }

        private static bool EqualsOrdinalIgnoreCase(this ReadOnlySpan<char> left, ReadOnlySpan<char> right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                char x = left[i], y = right[i];
                if (x != y &&
                    TextInfo.ToUpperAsciiInvariant(x) != TextInfo.ToUpperAsciiInvariant(y))
                {
                    return false;
                }
            }

            return true;
        }

        public static ReadOnlySpan<char> Trim(this ReadOnlySpan<char> source)
        {
            int startIndex = 0, endIndex = source.Length - 1;

            while (startIndex <= endIndex && char.IsWhiteSpace(source[startIndex]))
            {
                startIndex++;
            }

            while (endIndex >= startIndex && char.IsWhiteSpace(source[endIndex]))
            {
                endIndex--;
            }

            return source.Slice(startIndex, endIndex - startIndex + 1);
        }

        public static int IndexOf(this ReadOnlySpan<char> source, char value) =>
            IndexOf(source, value, 0);

        public static int IndexOf(this ReadOnlySpan<char> source, char value, int startIndex)
        {
            for (int i = startIndex; i < source.Length; i++)
            {
                if (source[i] == value)
                {
                    return i;
                }
            }

            return -1;
        }

        public static bool Contains(this ReadOnlySpan<char> source, char value)
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == value)
                {
                    return true;
                }
            }

            return false;
        }

        public static ReadOnlySpan<char> Remove(this ReadOnlySpan<char> source, int startIndex, int count)
        {
            if (startIndex < 0) throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_StartIndex);
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NegativeCount);
            if (count > source.Length - startIndex) throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_IndexCount);

            if (count == 0)
            {
                return source;
            }

            int newLength = source.Length - count;
            if (newLength == 0)
            {
                return ReadOnlySpan<char>.Empty;
            }

            Span<char> result = new char[newLength];
            source.Slice(0, startIndex).CopyTo(result);
            source.Slice(startIndex + count).CopyTo(result.Slice(startIndex));
            return result;
        }
    }
}
