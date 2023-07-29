// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    internal static class KeyAnalyzer
    {
        /// <summary>
        /// Look for well-known patterns we can optimize for in a set of dictionary or set keys.
        /// </summary>
        /// <remarks>
        /// The idea here is to find the shortest substring slice across all the input strings which yields a set of
        /// strings which are maximally unique. The optimal slice is then applied to incoming strings being hashed to
        /// perform dictionary/set lookups. Keeping the slices as small as possible minimizes the number of characters
        /// involved in hashing, speeding up the whole process.
        ///
        /// What we do here is pretty simple. We loop over the input strings, looking for the shortest slice with a good
        /// enough uniqueness factor. We look at all the strings both left-justified and right-justified as this maximizes
        /// the opportunities to find unique slices, especially in the case of many strings with the same prefix or suffix.
        ///
        /// In whatever slice we end up with, if all the characters involved in the slice are ASCII and we're doing case-insensitive
        /// operations, then we can select an ASCII-specific case-insensitive comparer which yields faster overall performance.
        /// </remarks>
        public static AnalysisResults Analyze(
            string[] uniqueStrings, bool ignoreCase, int minLength, int maxLength)
        {
            Debug.Assert(uniqueStrings.Length > 0);

            if (minLength > 0)
            {
                const int MaxSubstringLengthLimit = 8; // arbitrary small-ish limit...it's not worth the increase in algorithmic complexity to analyze longer substrings

                // Sufficient uniqueness factor of 95% is good enough.
                // Instead of ensuring that 95% of data is good, we stop when we know that at least 5% is bad.
                int acceptableNonUniqueCount = uniqueStrings.Length / 20;

                // Try to pick a substring comparer.
                SubstringComparer comparer = ignoreCase ? new JustifiedCaseInsensitiveSubstringComparer() : new JustifiedSubstringComparer();
                HashSet<string> set = new HashSet<string>(
#if NET6_0_OR_GREATER
                    uniqueStrings.Length,
#endif
                    comparer);

                // For each substring length...preferring the shortest length that provides
                // enough uniqueness
                int maxSubstringLength = Math.Min(minLength, MaxSubstringLengthLimit);
                for (int count = 1; count <= maxSubstringLength; count++)
                {
                    comparer.Count = count;

                    // For each index, get a uniqueness factor for the left-justified substrings.
                    // If any is above our threshold, we're done.
                    for (int index = 0; index <= minLength - count; index++)
                    {
                        comparer.Index = index;

                        if (HasSufficientUniquenessFactor(set, uniqueStrings, acceptableNonUniqueCount))
                        {
                            return CreateAnalysisResults(set, ignoreCase, minLength, maxLength, index, count);
                        }
                    }

                    // There were no left-justified substrings of this length available.
                    // If all of the strings are of the same length, then just checking left-justification is sufficient.
                    // But if any strings are of different lengths, then we'll get different alignments for left- vs
                    // right-justified substrings, and so we also check right-justification.
                    if (minLength != maxLength)
                    {
                        // when Index is negative, we're offsetting from the right, ensure we're at least
                        // far enough from the right that we have count characters available
                        comparer.Index = -count;

                        // For each index, get a uniqueness factor for the right-justified substrings.
                        // If any is above our threshold, we're done.
                        for (int offset = 0; offset <= minLength - count; offset++, comparer.Index--)
                        {
                            if (HasSufficientUniquenessFactor(set, uniqueStrings, acceptableNonUniqueCount))
                            {
                                return CreateAnalysisResults(set, ignoreCase, minLength, maxLength, comparer.Index, count);
                            }
                        }
                    }
                }
            }

            // Could not find a substring index/length that was good enough, use the entire string.
            return CreateAnalysisResults(uniqueStrings, ignoreCase, minLength, maxLength, 0, 0);
        }

        private static AnalysisResults CreateAnalysisResults(
            IEnumerable<string> uniqueStrings, bool ignoreCase, int minLength, int maxLength, int index, int count)
        {
            // Start off by assuming all strings are ASCII
            bool allAsciiIfIgnoreCase = true;

            // If we're case-sensitive, it doesn't matter if the strings are ASCII or not.
            // But if we're case-insensitive, we can switch to a faster comparer if all the
            // substrings are ASCII, so we check each.
            if (ignoreCase)
            {
                // Further, if the ASCII substrings don't contain any letters, then we can
                // actually perform the comparison as case-sensitive even if case-insensitive
                // was requested, as there's nothing that would compare equally to the substring
                // other than the substring itself.
                bool canSwitchIgnoreCaseToCaseSensitive = true;

                foreach (string s in uniqueStrings)
                {
                    // Get the span for the substring.
                    ReadOnlySpan<char> substring = count == 0 ? s.AsSpan() : Slicer(s, index, count);

                    // If the substring isn't ASCII, bail out to return the results.
                    if (!IsAllAscii(substring))
                    {
                        allAsciiIfIgnoreCase = false;
                        canSwitchIgnoreCaseToCaseSensitive = false;
                        break;
                    }

                    // All substrings so far are still ASCII only.  If this one contains any ASCII
                    // letters, mark that we can't switch to case-sensitive.
                    if (canSwitchIgnoreCaseToCaseSensitive && ContainsAnyLetters(substring))
                    {
                        canSwitchIgnoreCaseToCaseSensitive = false;
                    }
                }

                // If we can switch to case-sensitive, do so.
                if (canSwitchIgnoreCaseToCaseSensitive)
                {
                    ignoreCase = false;
                }
            }

            // Return the analysis results.
            return new AnalysisResults(ignoreCase, allAsciiIfIgnoreCase, index, count, minLength, maxLength);
        }

        internal static unsafe bool IsAllAscii(ReadOnlySpan<char> s)
        {
#if NET8_0_OR_GREATER
            return System.Text.Ascii.IsValid(s);
#else
            fixed (char* src = s)
            {
                uint* ptrUInt32 = (uint*)src;
                int length = s.Length;

                while (length >= 4)
                {
                    if (!AllCharsInUInt32AreAscii(ptrUInt32[0] | ptrUInt32[1]))
                    {
                        return false;
                    }

                    ptrUInt32 += 2;
                    length -= 4;
                }

                char* ptrChar = (char*)ptrUInt32;
                while (length-- > 0)
                {
                    char ch = *ptrChar++;
                    if (ch >= 0x80)
                    {
                        return false;
                    }
                }
            }

            return true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool AllCharsInUInt32AreAscii(uint value) => (value & ~0x007F_007Fu) == 0;
#endif
        }

#if NET8_0_OR_GREATER
        private static readonly SearchValues<char> s_asciiLetters = SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
#endif
        private static bool ContainsAnyLetters(ReadOnlySpan<char> s)
        {
            Debug.Assert(IsAllAscii(s));

#if NET8_0_OR_GREATER
            return s.ContainsAny(s_asciiLetters);
#else
            foreach (char c in s)
            {
                Debug.Assert(c <= 0x7f);
                if ((uint)((c | 0x20) - 'a') <= (uint)('z' - 'a'))
                {
                    return true;
                }
            }
            return false;
#endif
        }

        private static bool HasSufficientUniquenessFactor(HashSet<string> set, IEnumerable<string> uniqueStrings, int acceptableNonUniqueCount)
        {
            set.Clear();

            foreach (string s in uniqueStrings)
            {
                if (!set.Add(s) && --acceptableNonUniqueCount < 0)
                {
                    return false;
                }
            }

            return true;
        }

        internal readonly struct AnalysisResults
        {
            public AnalysisResults(bool ignoreCase, bool allAsciiIfIgnoreCase, int hashIndex, int hashCount, int minLength, int maxLength)
            {
                IgnoreCase = ignoreCase;
                AllAsciiIfIgnoreCase = allAsciiIfIgnoreCase;
                HashIndex = hashIndex;
                HashCount = hashCount;
                MinimumLength = minLength;
                MaximumLengthDiff = maxLength - minLength;
            }

            public bool IgnoreCase { get; }
            public bool AllAsciiIfIgnoreCase { get; }
            public int HashIndex { get; }
            public int HashCount { get; }
            public int MinimumLength { get; }
            public int MaximumLengthDiff { get; }

            public bool SubstringHashing => HashCount != 0;
            public bool RightJustifiedSubstring => HashIndex < 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> Slicer(this string s, int index, int count) => s.AsSpan((index >= 0 ? index : s.Length + index), count);

        private abstract class SubstringComparer : IEqualityComparer<string>
        {
            public int Index;   // offset from left side (if positive) or right side (if negative) of the string
            public int Count;   // number of characters in the span

            public abstract bool Equals(string? x, string? y);
            public abstract int GetHashCode(string s);
        }

        private sealed class JustifiedSubstringComparer : SubstringComparer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override bool Equals(string? x, string? y) => x!.Slicer(Index, Count).SequenceEqual(y!.Slicer(Index, Count));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode(string s) => Hashing.GetHashCodeOrdinal(s.Slicer(Index, Count));
        }

        private sealed class JustifiedCaseInsensitiveSubstringComparer : SubstringComparer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override bool Equals(string? x, string? y) => x!.Slicer(Index, Count).Equals(y!.Slicer(Index, Count), StringComparison.OrdinalIgnoreCase);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode(string s) => Hashing.GetHashCodeOrdinalIgnoreCase(s.Slicer(Index, Count));
        }
    }
}
