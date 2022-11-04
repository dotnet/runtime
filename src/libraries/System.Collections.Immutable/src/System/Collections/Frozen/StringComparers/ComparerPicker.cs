// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    internal static class ComparerPicker
    {
        /// <summary>
        /// Pick an optimal comparer for the set of strings and case-sensitivity mode.
        /// </summary>
        /// <remarks>
        /// The idea here is to find the shortest substring slice across all the input strings which yields a set of
        /// strings which are maximally unique. The optimal slice is then applied to incoming strings being hashed to
        /// perform the dictionary lookup. Keeping the slices as small as possible minimizes the number of characters
        /// involved in hashing, speeding up the whole process.
        ///
        /// What we do here is pretty simple. We loop over the input strings, looking for the shortest slice with a good
        /// enough uniqueness factor. We look at all the strings both left-justified and right-justified as this maximizes
        /// the opportunities to find unique slices, especially in the case of many strings with the same prefix or suffix.
        ///
        /// In whatever slice we end up with, if all the characters involved in the slice are ASCII and we're doing case-insensitive
        /// operations, then we can select an ASCII-specific case-insensitive comparer which yields faster overall performance.
        ///
        /// Warning: This code may reorganize (e.g. sort) the entries in the input array. It will not delete or add anything though.
        /// </remarks>
        public static StringComparerBase Pick(ReadOnlySpan<string> uniqueStrings, bool ignoreCase, out int minimumLength, out int maximumLengthDiff)
        {
            Debug.Assert(uniqueStrings.Length != 0);

            // First, try to pick a substring comparer.
            // if we couldn't find a good substring comparer, fallback to a full string comparer.
            StringComparerBase? c =
                PickSubstringComparer(uniqueStrings, ignoreCase) ??
                PickFullStringComparer(uniqueStrings, ignoreCase);

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

            minimumLength = min;
            maximumLengthDiff = max - min;
            return c;
        }

        private static StringComparerBase? PickSubstringComparer(ReadOnlySpan<string> uniqueStrings, bool ignoreCase)
        {
            const double SufficientUniquenessFactor = 0.95; // 95% is good enough

            // What is the shortest string? This represent the maximum substring length we consider
            int maxSubstringLength = int.MaxValue;
            foreach (string s in uniqueStrings)
            {
                if (s.Length < maxSubstringLength)
                {
                    maxSubstringLength = s.Length;
                }
            }

            SubstringComparerBase leftComparer = ignoreCase ? new LeftJustifiedCaseInsensitiveSubstringComparer() : new LeftJustifiedSubstringComparer();
            SubstringComparerBase rightComparer = ignoreCase ? new RightJustifiedCaseInsensitiveSubstringComparer() : new RightJustifiedSubstringComparer();

            // try to find the minimal unique substring to use for comparisons
            var leftSet = new HashSet<string>(new ComparerWrapper(leftComparer));
            var rightSet = new HashSet<string>(new ComparerWrapper(rightComparer));
            for (int count = 1; count <= maxSubstringLength; count++)
            {
                for (int index = 0; index <= maxSubstringLength - count; index++)
                {
                    leftComparer.Index = index;
                    leftComparer.Count = count;

                    double factor = GetUniquenessFactor(leftSet, uniqueStrings);
                    if (factor >= SufficientUniquenessFactor)
                    {
                        if (ignoreCase)
                        {
                            foreach (string ss in uniqueStrings)
                            {
                                if (!IsAllAscii(ss.AsSpan(leftComparer.Index, leftComparer.Count)))
                                {
                                    // keep the slower non-ascii comparer since we have some non-ascii text
                                    return leftComparer;
                                }
                            }

                            // optimize for all-ascii case
                            return new LeftJustifiedCaseInsensitiveAsciiSubstringComparer
                            {
                                Index = leftComparer.Index,
                                Count = leftComparer.Count,
                            };
                        }

                        // Optimize the single char case
                        if (leftComparer.Count == 1)
                        {
                            return new LeftJustifiedSingleCharComparer
                            {
                                Index = leftComparer.Index,
                                Count = 1,
                            };
                        }

                        return leftComparer;
                    }

                    rightComparer.Index = -index - count;
                    rightComparer.Count = count;

                    factor = GetUniquenessFactor(rightSet, uniqueStrings);
                    if (factor >= SufficientUniquenessFactor)
                    {
                        if (ignoreCase)
                        {
                            foreach (string ss in uniqueStrings)
                            {
                                if (!IsAllAscii(ss.AsSpan(ss.Length + rightComparer.Index, rightComparer.Count)))
                                {
                                    // keep the slower non-ascii comparer since we have some non-ascii text
                                    return rightComparer;
                                }
                            }

                            // optimize for all-ascii case
                            return new RightJustifiedCaseInsensitiveAsciiSubstringComparer
                            {
                                Index = rightComparer.Index,
                                Count = rightComparer.Count,
                            };
                        }

                        // Optimize the single char case
                        if (rightComparer.Count == 1)
                        {
                            return new RightJustifiedSingleCharComparer
                            {
                                Index = rightComparer.Index,
                                Count = 1,
                            };
                        }

                        return rightComparer;
                    }
                }
            }

            return null;
        }

        private static StringComparerBase PickFullStringComparer(ReadOnlySpan<string> uniqueStrings, bool ignoreCase)
        {
            if (!ignoreCase)
            {
                return new FullStringComparer();
            }

            foreach (string s in uniqueStrings)
            {
                if (!IsAllAscii(s.AsSpan()))
                {
                    return new FullCaseInsensitiveStringComparer();
                }
            }

            return new FullCaseInsensitiveAsciiStringComparer();
        }

        private sealed class ComparerWrapper : IEqualityComparer<string>
        {
            private readonly SubstringComparerBase _comp;

            public ComparerWrapper(SubstringComparerBase comp) => _comp = comp;

            public bool Equals(string? x, string? y) => _comp.EqualsPartial(x, y);
            public int GetHashCode([DisallowNull] string obj) => _comp.GetHashCode(obj);
        }

        // TODO https://github.com/dotnet/runtime/issues/28230:
        // Replace this once Ascii.IsValid exists.
        internal static unsafe bool IsAllAscii(ReadOnlySpan<char> s)
        {
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
    }
}
