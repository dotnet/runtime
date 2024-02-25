// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Text;
using static System.Buffers.StringSearchValuesHelper;

namespace System.Buffers
{
    internal static class StringSearchValues
    {
        private const int TeddyBucketCount = 8;

        private static readonly SearchValues<char> s_asciiLetters =
            SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");

        private static readonly SearchValues<char> s_allAsciiExceptLowercase =
            SearchValues.Create("\0\u0001\u0002\u0003\u0004\u0005\u0006\a\b\t\n\v\f\r\u000E\u000F\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001A\u001B\u001C\u001D\u001E\u001F !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`{|}~\u007F");

        public static SearchValues<string> Create(ReadOnlySpan<string> values, bool ignoreCase)
        {
            if (values.Length == 0)
            {
                return new EmptySearchValues<string>();
            }

            if (values.Length == 1)
            {
                // Avoid additional overheads for single-value inputs.
                string value = values[0];
                ArgumentNullException.ThrowIfNull(value, nameof(values));
                string normalizedValue = NormalizeIfNeeded(value, ignoreCase);

                AnalyzeValues(new ReadOnlySpan<string>(ref normalizedValue), ref ignoreCase, out bool ascii, out bool asciiLettersOnly, out _, out _);
                return CreateForSingleValue(normalizedValue, uniqueValues: null, ignoreCase, ascii, asciiLettersOnly);
            }

            var uniqueValues = new HashSet<string>(values.Length, ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

            foreach (string value in values)
            {
                ArgumentNullException.ThrowIfNull(value, nameof(values));

                uniqueValues.Add(value);
            }

            if (uniqueValues.Contains(string.Empty))
            {
                return new SingleStringSearchValuesFallback<SearchValues.FalseConst>(string.Empty, uniqueValues);
            }

            Span<string> normalizedValues = new string[uniqueValues.Count];
            int i = 0;
            foreach (string value in uniqueValues)
            {
                normalizedValues[i++] = NormalizeIfNeeded(value, ignoreCase);
            }
            Debug.Assert(i == normalizedValues.Length);

            // Aho-Corasick's ctor expects values to be sorted by length.
            normalizedValues.Sort(static (a, b) => a.Length.CompareTo(b.Length));

            // We may not end up choosing Aho-Corasick as the implementation, but it has a nice property of
            // finding all the unreachable values during the construction stage, so we build the trie early.
            HashSet<string>? unreachableValues = null;
            var ahoCorasickBuilder = new AhoCorasickBuilder(normalizedValues, ignoreCase, ref unreachableValues);

            if (unreachableValues is not null)
            {
                // Some values are exact prefixes of other values.
                // Exclude those values now to reduce the number of buckets and make verification steps cheaper during searching.
                normalizedValues = RemoveUnreachableValues(normalizedValues, unreachableValues);
            }

            SearchValues<string> searchValues = CreateFromNormalizedValues(normalizedValues, uniqueValues, ignoreCase, ref ahoCorasickBuilder);
            ahoCorasickBuilder.Dispose();
            return searchValues;

            static string NormalizeIfNeeded(string value, bool ignoreCase)
            {
                if (ignoreCase && value.AsSpan().ContainsAnyExcept(s_allAsciiExceptLowercase))
                {
                    string upperCase = string.FastAllocateString(value.Length);
                    int charsWritten = Ordinal.ToUpperOrdinal(value, new Span<char>(ref upperCase.GetRawStringData(), upperCase.Length));
                    Debug.Assert(charsWritten == upperCase.Length);
                    value = upperCase;
                }

                return value;
            }

            static Span<string> RemoveUnreachableValues(Span<string> values, HashSet<string> unreachableValues)
            {
                int newCount = 0;
                foreach (string value in values)
                {
                    if (!unreachableValues.Contains(value))
                    {
                        values[newCount++] = value;
                    }
                }

                Debug.Assert(newCount <= values.Length - unreachableValues.Count);
                Debug.Assert(newCount > 0);

                return values.Slice(0, newCount);
            }
        }

        private static SearchValues<string> CreateFromNormalizedValues(
            ReadOnlySpan<string> values,
            HashSet<string> uniqueValues,
            bool ignoreCase,
            ref AhoCorasickBuilder ahoCorasickBuilder)
        {
            AnalyzeValues(values, ref ignoreCase, out bool allAscii, out bool asciiLettersOnly, out bool nonAsciiAffectedByCaseConversion, out int minLength);

            if (values.Length == 1)
            {
                // We may reach this if we've removed unreachable values and ended up with only 1 remaining.
                return CreateForSingleValue(values[0], uniqueValues, ignoreCase, allAscii, asciiLettersOnly);
            }

            if ((Ssse3.IsSupported || AdvSimd.Arm64.IsSupported) &&
                TryGetTeddyAcceleratedValues(values, uniqueValues, ignoreCase, allAscii, asciiLettersOnly, nonAsciiAffectedByCaseConversion, minLength) is { } searchValues)
            {
                return searchValues;
            }

            // Fall back to Aho-Corasick for all other multi-value sets.
            AhoCorasick ahoCorasick = ahoCorasickBuilder.Build();

            if (!ignoreCase)
            {
                return PickAhoCorasickImplementation<CaseSensitive>(ahoCorasick, uniqueValues);
            }

            if (nonAsciiAffectedByCaseConversion)
            {
                if (ContainsIncompleteSurrogatePairs(values))
                {
                    // Aho-Corasick can't deal with the matching semantics of standalone surrogate code units.
                    // We will use a slow but correct O(n * m) fallback implementation.
                    return new MultiStringIgnoreCaseSearchValuesFallback(uniqueValues);
                }

                return PickAhoCorasickImplementation<CaseInsensitiveUnicode>(ahoCorasick, uniqueValues);
            }

            if (asciiLettersOnly)
            {
                return PickAhoCorasickImplementation<CaseInsensitiveAsciiLetters>(ahoCorasick, uniqueValues);
            }

            return PickAhoCorasickImplementation<CaseInsensitiveAscii>(ahoCorasick, uniqueValues);

            static SearchValues<string> PickAhoCorasickImplementation<TCaseSensitivity>(AhoCorasick ahoCorasick, HashSet<string> uniqueValues)
                where TCaseSensitivity : struct, ICaseSensitivity
            {
                return ahoCorasick.ShouldUseAsciiFastScan
                    ? new StringSearchValuesAhoCorasick<TCaseSensitivity, AhoCorasick.IndexOfAnyAsciiFastScan>(ahoCorasick, uniqueValues)
                    : new StringSearchValuesAhoCorasick<TCaseSensitivity, AhoCorasick.NoFastScan>(ahoCorasick, uniqueValues);
            }
        }

        private static SearchValues<string>? TryGetTeddyAcceleratedValues(
            ReadOnlySpan<string> values,
            HashSet<string> uniqueValues,
            bool ignoreCase,
            bool allAscii,
            bool asciiLettersOnly,
            bool nonAsciiAffectedByCaseConversion,
            int minLength)
        {
            if (minLength == 1)
            {
                // An 'N=1' implementation is possible, but callers should
                // consider using SearchValues<char> instead in such cases.
                // It can be added if Regex ends up running into this case.
                return null;
            }

            if (values.Length > RabinKarp.MaxValues)
            {
                // The more values we have, the higher the chance of hash/fingerprint collisions.
                // To avoid spending too much time in verification steps, fallback to Aho-Corasick which guarantees O(n).
                // If it turns out that this limit is commonly exceeded, we can tweak the number of buckets
                // in the implementation, or use different variants depending on input.
                return null;
            }

            int n = minLength == 2 ? 2 : 3;

            if (Ssse3.IsSupported)
            {
                foreach (string value in values)
                {
                    if (value.AsSpan(0, n).Contains('\0'))
                    {
                        // If we let null chars through here, Teddy would still work correctly, but it
                        // would hit more false positives that the verification step would have to rule out.
                        // While we could flow a generic flag like Ssse3AndWasmHandleZeroInNeedle through,
                        // we expect such values to be rare enough that introducing more code is not worth it.
                        return null;
                    }
                }
            }

            // Even if the values contain non-ASCII chars, we may be able to use Teddy as long as the
            // first N characters are ASCII.
            if (!allAscii)
            {
                foreach (string value in values)
                {
                    if (!Ascii.IsValid(value.AsSpan(0, n)))
                    {
                        // A vectorized implementation for non-ASCII values is possible.
                        // It can be added if it turns out to be a common enough scenario.
                        return null;
                    }
                }
            }

            if (!ignoreCase)
            {
                return PickTeddyImplementation<CaseSensitive, CaseSensitive>(values, uniqueValues, n);
            }

            if (asciiLettersOnly)
            {
                return PickTeddyImplementation<CaseInsensitiveAsciiLetters, CaseInsensitiveAsciiLetters>(values, uniqueValues, n);
            }

            // Even if the whole value isn't ASCII letters only, we can still use a faster approach
            // for the vectorized part as long as the first N characters are.
            bool asciiStartLettersOnly = true;
            bool asciiStartUnaffectedByCaseConversion = true;

            foreach (string value in values)
            {
                ReadOnlySpan<char> slice = value.AsSpan(0, n);
                asciiStartLettersOnly = asciiStartLettersOnly && !slice.ContainsAnyExcept(s_asciiLetters);
                asciiStartUnaffectedByCaseConversion = asciiStartUnaffectedByCaseConversion && !slice.ContainsAny(s_asciiLetters);
            }

            Debug.Assert(!(asciiStartLettersOnly && asciiStartUnaffectedByCaseConversion));

            // If we still have empty buckets we could use and we're ignoring case, we may be able to
            // generate all possible permutations of the first N characters and switch to case-sensitive searching.
            // E.g. ["ab", "c!"] => ["ab", "Ab" "aB", "AB", "c!", "C!"].
            // This won't apply to inputs with many letters (e.g. "abc" => 8 permutations on its own).
            if (!asciiStartUnaffectedByCaseConversion &&
                values.Length < TeddyBucketCount &&
                TryGenerateAllCasePermutationsForPrefixes(values, n, TeddyBucketCount, out string[]? newValues))
            {
                asciiStartUnaffectedByCaseConversion = true;
                values = newValues;
            }

            if (asciiStartUnaffectedByCaseConversion)
            {
                return nonAsciiAffectedByCaseConversion
                    ? PickTeddyImplementation<CaseSensitive, CaseInsensitiveUnicode>(values, uniqueValues, n)
                    : PickTeddyImplementation<CaseSensitive, CaseInsensitiveAscii>(values, uniqueValues, n);
            }

            if (nonAsciiAffectedByCaseConversion)
            {
                return asciiStartLettersOnly
                    ? PickTeddyImplementation<CaseInsensitiveAsciiLetters, CaseInsensitiveUnicode>(values, uniqueValues, n)
                    : PickTeddyImplementation<CaseInsensitiveAscii, CaseInsensitiveUnicode>(values, uniqueValues, n);
            }

            return asciiStartLettersOnly
                ? PickTeddyImplementation<CaseInsensitiveAsciiLetters, CaseInsensitiveAscii>(values, uniqueValues, n)
                : PickTeddyImplementation<CaseInsensitiveAscii, CaseInsensitiveAscii>(values, uniqueValues, n);
        }

        private static SearchValues<string> PickTeddyImplementation<TStartCaseSensitivity, TCaseSensitivity>(
            ReadOnlySpan<string> values,
            HashSet<string> uniqueValues,
            int n)
            where TStartCaseSensitivity : struct, ICaseSensitivity
            where TCaseSensitivity : struct, ICaseSensitivity
        {
            Debug.Assert(typeof(TStartCaseSensitivity) != typeof(CaseInsensitiveUnicode));
            Debug.Assert(values.Length > 1);
            Debug.Assert(n is 2 or 3);

            if (values.Length > 8)
            {
                string[][] buckets = TeddyBucketizer.Bucketize(values, TeddyBucketCount, n);

                // Potential optimization: We don't have to pick the first N characters for the fingerprint.
                // Different offset selection can noticeably improve throughput (e.g. 2x).

                return n == 2
                    ? new AsciiStringSearchValuesTeddyBucketizedN2<TStartCaseSensitivity, TCaseSensitivity>(buckets, values, uniqueValues)
                    : new AsciiStringSearchValuesTeddyBucketizedN3<TStartCaseSensitivity, TCaseSensitivity>(buckets, values, uniqueValues);
            }
            else
            {
                return n == 2
                    ? new AsciiStringSearchValuesTeddyNonBucketizedN2<TStartCaseSensitivity, TCaseSensitivity>(values, uniqueValues)
                    : new AsciiStringSearchValuesTeddyNonBucketizedN3<TStartCaseSensitivity, TCaseSensitivity>(values, uniqueValues);
            }
        }

        private static bool TryGenerateAllCasePermutationsForPrefixes(ReadOnlySpan<string> values, int n, int maxValues, [NotNullWhen(true)] out string[]? newValues)
        {
            Debug.Assert(n is 2 or 3);
            Debug.Assert(values.Length < maxValues);

            // Count how many possible permutations there are.
            int newValuesCount = 0;

            foreach (string value in values)
            {
                int permutations = 1;

                foreach (char c in value.AsSpan(0, n))
                {
                    Debug.Assert(char.IsAscii(c));

                    if (char.IsAsciiLetter(c))
                    {
                        permutations *= 2;
                    }
                }

                newValuesCount += permutations;
            }

            Debug.Assert(newValuesCount > values.Length, "Shouldn't have been called if there were no letters present");

            if (newValuesCount > maxValues)
            {
                newValues = null;
                return false;
            }

            // Generate the permutations.
            newValues = new string[newValuesCount];
            newValuesCount = 0;

            foreach (string value in values)
            {
                int start = newValuesCount;

                newValues[newValuesCount++] = value;

                for (int i = 0; i < n; i++)
                {
                    char c = value[i];

                    if (char.IsAsciiLetter(c))
                    {
                        // Copy all the previous permutations of this value but change the casing of the i-th character.
                        foreach (string previous in newValues.AsSpan(start, newValuesCount - start))
                        {
                            newValues[newValuesCount++] = $"{previous.AsSpan(0, i)}{(char)(c ^ 0x20)}{previous.AsSpan(i + 1)}";
                        }
                    }
                }
            }

            Debug.Assert(newValuesCount == newValues.Length);
            return true;
        }

        private static SearchValues<string> CreateForSingleValue(
            string value,
            HashSet<string>? uniqueValues,
            bool ignoreCase,
            bool allAscii,
            bool asciiLettersOnly)
        {
            // We make use of optimizations that may overflow on 32bit systems for long values.
            int maxLength = IntPtr.Size == 4 ? 1_000_000_000 : int.MaxValue;

            if (Vector128.IsHardwareAccelerated && value.Length > 1 && value.Length <= maxLength)
            {
                SearchValues<string>? searchValues = value.Length switch
                {
                    < 4 => TryCreateSingleValuesThreeChars<ValueLengthLessThan4>(value, uniqueValues, ignoreCase, allAscii, asciiLettersOnly),
                    < 8 => TryCreateSingleValuesThreeChars<ValueLength4To7>(value, uniqueValues, ignoreCase, allAscii, asciiLettersOnly),
                    _ => TryCreateSingleValuesThreeChars<ValueLength8OrLongerOrUnknown>(value, uniqueValues, ignoreCase, allAscii, asciiLettersOnly),
                };

                if (searchValues is not null)
                {
                    return searchValues;
                }
            }

            uniqueValues ??= new HashSet<string>(1, ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal) { value };

            return ignoreCase
                ? new SingleStringSearchValuesFallback<SearchValues.TrueConst>(value, uniqueValues)
                : new SingleStringSearchValuesFallback<SearchValues.FalseConst>(value, uniqueValues);
        }

        private static SearchValues<string>? TryCreateSingleValuesThreeChars<TValueLength>(
            string value,
            HashSet<string>? uniqueValues,
            bool ignoreCase,
            bool allAscii,
            bool asciiLettersOnly)
            where TValueLength : struct, IValueLength
        {
            if (!ignoreCase)
            {
                return new SingleStringSearchValuesThreeChars<TValueLength, CaseSensitive>(uniqueValues, value);
            }

            if (asciiLettersOnly)
            {
                return new SingleStringSearchValuesThreeChars<TValueLength, CaseInsensitiveAsciiLetters>(uniqueValues, value);
            }

            if (allAscii)
            {
                return new SingleStringSearchValuesThreeChars<TValueLength, CaseInsensitiveAscii>(uniqueValues, value);
            }

            // SingleStringSearchValuesThreeChars doesn't have logic to handle non-ASCII case conversion, so we require that anchor characters are ASCII.
            // Right now we're always selecting the first character as one of the anchors, and we need at least two.
            if (char.IsAscii(value[0]) && value.AsSpan(1).ContainsAnyInRange((char)0, (char)127))
            {
                return new SingleStringSearchValuesThreeChars<TValueLength, CaseInsensitiveUnicode>(uniqueValues, value);
            }

            return null;
        }

        private static void AnalyzeValues(
            ReadOnlySpan<string> values,
            ref bool ignoreCase,
            out bool allAscii,
            out bool asciiLettersOnly,
            out bool nonAsciiAffectedByCaseConversion,
            out int minLength)
        {
            allAscii = true;
            asciiLettersOnly = true;
            minLength = int.MaxValue;

            foreach (string value in values)
            {
                allAscii = allAscii && Ascii.IsValid(value);
                asciiLettersOnly = asciiLettersOnly && !value.AsSpan().ContainsAnyExcept(s_asciiLetters);
                minLength = Math.Min(minLength, value.Length);
            }

            // Potential optimization: Not all characters participate in Unicode case conversion.
            // If we can determine that none of the non-ASCII characters do, we can make searching faster
            // by using the same paths as we do for ASCII-only values.
            nonAsciiAffectedByCaseConversion = ignoreCase && !allAscii;

            // If all the characters in values are unaffected by casing, we can avoid the ignoreCase overhead.
            if (ignoreCase && !nonAsciiAffectedByCaseConversion && !asciiLettersOnly)
            {
                ignoreCase = false;

                foreach (string value in values)
                {
                    if (value.AsSpan().ContainsAny(s_asciiLetters))
                    {
                        ignoreCase = true;
                        break;
                    }
                }
            }
        }

        private static bool ContainsIncompleteSurrogatePairs(ReadOnlySpan<string> values)
        {
            foreach (string value in values)
            {
                int i = value.AsSpan().IndexOfAnyInRange(CharUnicodeInfo.HIGH_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END);
                if (i < 0)
                {
                    continue;
                }

                for (; (uint)i < (uint)value.Length; i++)
                {
                    if (char.IsHighSurrogate(value[i]))
                    {
                        if ((uint)(i + 1) >= (uint)value.Length || !char.IsLowSurrogate(value[i + 1]))
                        {
                            // High surrogate not followed by a low surrogate.
                            return true;
                        }

                        i++;
                    }
                    else if (char.IsLowSurrogate(value[i]))
                    {
                        // Low surrogate not preceded by a high surrogate.
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
