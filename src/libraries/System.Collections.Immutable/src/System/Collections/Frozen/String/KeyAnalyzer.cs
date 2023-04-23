// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
        public static void Analyze(ReadOnlySpan<string> uniqueStrings, bool ignoreCase, out AnalysisResults results)
        {
            // First, try to pick a substring comparer.
            // if we can't find a good substring comparer, fallback to a full string comparer.
            if (!UseSubstring(uniqueStrings, ignoreCase, out results))
            {
                UseFullString(uniqueStrings, ignoreCase, out results);
            }

            // Calculate the trivial rejection boundaries.
            int min = int.MaxValue, max = 0;
            foreach (string s in uniqueStrings)
            {
                if (s.Length < min)
                {
                    min = s.Length;
                }

                if (s.Length > max)
                {
                    max = s.Length;
                }
            }

            results.MinimumLength = min;
            results.MaximumLengthDiff = max - min;
        }

        private static bool UseSubstring(ReadOnlySpan<string> uniqueStrings, bool ignoreCase, out AnalysisResults results)
        {
            const double SufficientUniquenessFactor = 0.95; // 95% is good enough

            // What is the shortest string? This represents the maximum substring length we consider
            int maxSubstringLength = int.MaxValue;
            foreach (string s in uniqueStrings)
            {
                if (s.Length < maxSubstringLength)
                {
                    maxSubstringLength = s.Length;
                }
            }

            SubstringComparer leftComparer = ignoreCase ? new LeftJustifiedCaseInsensitiveSubstringComparer() : new LeftJustifiedSubstringComparer();
            SubstringComparer rightComparer = ignoreCase ? new RightJustifiedCaseInsensitiveSubstringComparer() : new RightJustifiedSubstringComparer();

            // try to find the minimal unique substring to use for comparisons
            var leftSet = new HashSet<string>(leftComparer);
            var rightSet = new HashSet<string>(rightComparer);
            for (int count = 1; count <= maxSubstringLength; count++)
            {
                for (int index = 0; index <= maxSubstringLength - count; index++)
                {
                    leftComparer.Index = index;
                    leftComparer.Count = count;

                    double factor = GetUniquenessFactor(leftSet, uniqueStrings);
                    if (factor >= SufficientUniquenessFactor)
                    {
                        bool allAscii = true;
                        foreach (string s in uniqueStrings)
                        {
                            if (!IsAllAscii(s.AsSpan(leftComparer.Index, leftComparer.Count)))
                            {
                                allAscii = false;
                                break;
                            }
                        }

                        results = new(allAscii, ignoreCase, 0, 0, leftComparer.Index, leftComparer.Count);
                        return true;
                    }

                    rightComparer.Index = -index - count;
                    rightComparer.Count = count;

                    factor = GetUniquenessFactor(rightSet, uniqueStrings);
                    if (factor >= SufficientUniquenessFactor)
                    {
                        bool allAscii = true;
                        foreach (string s in uniqueStrings)
                        {
                            if (!IsAllAscii(s.AsSpan(s.Length + rightComparer.Index, rightComparer.Count)))
                            {
                                allAscii = false;
                                break;
                            }
                        }

                        results = new(allAscii, ignoreCase, 0, 0, rightComparer.Index, rightComparer.Count);
                        return true;
                    }
                }
            }

            results = default;
            return false;
        }

        private static void UseFullString(ReadOnlySpan<string> uniqueStrings, bool ignoreCase, out AnalysisResults results)
        {
            bool allAscii = true;
            foreach (string s in uniqueStrings)
            {
                if (!IsAllAscii(s.AsSpan()))
                {
                    allAscii = false;
                    break;
                }
            }

            results = new(allAscii, ignoreCase, 0, 0, 0, 0);
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

                while (length > 3)
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
                    if (ch >= 0x7f)
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

        private static double GetUniquenessFactor(HashSet<string> set, ReadOnlySpan<string> uniqueStrings)
        {
            set.Clear();
            foreach (string s in uniqueStrings)
            {
                set.Add(s);
            }

            return set.Count / (double)uniqueStrings.Length;
        }

        internal struct AnalysisResults
        {
            public AnalysisResults(
                bool allAscii,
                bool ignoreCase,
                int minimumLength,
                int maximumLengthDiff,
                int hashIndex,
                int hashCount)
            {
                AllAscii = allAscii;
                IgnoreCase = ignoreCase;
                MinimumLength = minimumLength;
                MaximumLengthDiff = maximumLengthDiff;
                HashIndex = hashIndex;
                HashCount = hashCount;
            }

            public bool AllAscii { get; }
            public bool IgnoreCase { get; }
            public int MinimumLength { get; set; }
            public int MaximumLengthDiff { get; set; }
            public int HashIndex { get; }
            public int HashCount { get; }

            public bool SubstringHashing => HashCount != 0;
            public bool RightJustifiedSubstring => HashIndex < 0;
        }

        private abstract class SubstringComparer : IEqualityComparer<string>
        {
            public int Index;
            public int Count;
            public abstract bool Equals(string? x, string? y);
            public abstract int GetHashCode(string s);
        }

        private sealed class LeftJustifiedSubstringComparer : SubstringComparer
        {
            public override bool Equals(string? x, string? y) => x.AsSpan(Index, Count).SequenceEqual(y.AsSpan(Index, Count));
            public override int GetHashCode(string s) => Hashing.GetHashCodeOrdinal(s.AsSpan(Index, Count));
        }

        private sealed class LeftJustifiedCaseInsensitiveSubstringComparer : SubstringComparer
        {
            public override bool Equals(string? x, string? y) => x.AsSpan(Index, Count).Equals(y.AsSpan(Index, Count), StringComparison.OrdinalIgnoreCase);
            public override int GetHashCode(string s) => Hashing.GetHashCodeOrdinalIgnoreCase(s.AsSpan(Index, Count));
        }

        private sealed class RightJustifiedSubstringComparer : SubstringComparer
        {
            public override bool Equals(string? x, string? y) => x.AsSpan(x!.Length + Index, Count).SequenceEqual(y.AsSpan(y!.Length + Index, Count));
            public override int GetHashCode(string s) => Hashing.GetHashCodeOrdinal(s.AsSpan(s.Length + Index, Count));
        }

        private sealed class RightJustifiedCaseInsensitiveSubstringComparer : SubstringComparer
        {
            public override bool Equals(string? x, string? y) => x.AsSpan(x!.Length + Index, Count).Equals(y.AsSpan(y!.Length + Index, Count), StringComparison.OrdinalIgnoreCase);
            public override int GetHashCode(string s) => Hashing.GetHashCodeOrdinalIgnoreCase(s.AsSpan(s.Length + Index, Count));
        }
    }
}
