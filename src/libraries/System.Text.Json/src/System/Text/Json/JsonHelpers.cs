// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace System.Text.Json
{
    internal static partial class JsonHelpers
    {
        /// <summary>
        /// Returns the unescaped span for the given reader.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<byte> GetUnescapedSpan(this scoped ref Utf8JsonReader reader)
        {
            Debug.Assert(reader.TokenType is JsonTokenType.String or JsonTokenType.PropertyName);
            ReadOnlySpan<byte> span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
            return reader.ValueIsEscaped ? JsonReaderHelper.GetUnescapedSpan(span) : span;
        }

        /// <summary>
        /// Attempts to perform a Read() operation and optionally checks that the full JSON value has been buffered.
        /// The reader will be reset if the operation fails.
        /// </summary>
        /// <param name="reader">The reader to advance.</param>
        /// <param name="requiresReadAhead">If reading a partial payload, read ahead to ensure that the full JSON value has been buffered.</param>
        /// <returns>True if the reader has been buffered with all required data.</returns>
        // AggressiveInlining used since this method is on a hot path and short. The AdvanceWithReadAhead method should not be inlined.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdvanceWithOptionalReadAhead(this scoped ref Utf8JsonReader reader, bool requiresReadAhead)
        {
            // No read-ahead necessary if we're at the final block of JSON data.
            bool readAhead = requiresReadAhead && !reader.IsFinalBlock;
            return readAhead ? TryAdvanceWithReadAhead(ref reader) : reader.Read();
        }

        /// <summary>
        /// Attempts to read ahead to the next root-level JSON value, if it exists.
        /// </summary>
        public static bool TryAdvanceToNextRootLevelValueWithOptionalReadAhead(this scoped ref Utf8JsonReader reader, bool requiresReadAhead, out bool isAtEndOfStream)
        {
            Debug.Assert(reader.AllowMultipleValues, "only supported by readers that support multiple values.");
            Debug.Assert(reader.CurrentDepth == 0, "should only invoked for top-level values.");

            Utf8JsonReader checkpoint = reader;
            if (!reader.Read())
            {
                // If the reader didn't return any tokens and it's the final block,
                // then there are no other JSON values to be read.
                isAtEndOfStream = reader.IsFinalBlock;
                reader = checkpoint;
                return false;
            }

            // We found another JSON value, read ahead accordingly.
            isAtEndOfStream = false;
            if (requiresReadAhead && !reader.IsFinalBlock)
            {
                // Perform full read-ahead to ensure the full JSON value has been buffered.
                reader = checkpoint;
                return TryAdvanceWithReadAhead(ref reader);
            }

            return true;
        }

        private static bool TryAdvanceWithReadAhead(scoped ref Utf8JsonReader reader)
        {
            // When we're reading ahead we always have to save the state
            // as we don't know if the next token is a start object or array.
            Utf8JsonReader restore = reader;

            if (!reader.Read())
            {
                return false;
            }

            // Perform the actual read-ahead.
            JsonTokenType tokenType = reader.TokenType;
            if (tokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                // Attempt to skip to make sure we have all the data we need.
                bool complete = reader.TrySkipPartial();

                // We need to restore the state in all cases as we need to be positioned back before
                // the current token to either attempt to skip again or to actually read the value.
                reader = restore;

                if (!complete)
                {
                    // Couldn't read to the end of the object, exit out to get more data in the buffer.
                    return false;
                }

                // Success, requeue the reader to the start token.
                reader.ReadWithVerify();
                Debug.Assert(tokenType == reader.TokenType);
            }

            return true;
        }

#if !NET
        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is a valid Unicode scalar
        /// value, i.e., is in [ U+0000..U+D7FF ], inclusive; or [ U+E000..U+10FFFF ], inclusive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidUnicodeScalar(uint value)
        {
            // By XORing the incoming value with 0xD800, surrogate code points
            // are moved to the range [ U+0000..U+07FF ], and all valid scalar
            // values are clustered into the single range [ U+0800..U+10FFFF ],
            // which allows performing a single fast range check.

            return IsInRangeInclusive(value ^ 0xD800U, 0x800U, 0x10FFFFU);
        }
#endif

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is between
        /// <paramref name="lowerBound"/> and <paramref name="upperBound"/>, inclusive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInRangeInclusive(uint value, uint lowerBound, uint upperBound)
            => (value - lowerBound) <= (upperBound - lowerBound);

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is between
        /// <paramref name="lowerBound"/> and <paramref name="upperBound"/>, inclusive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInRangeInclusive(int value, int lowerBound, int upperBound)
            => (uint)(value - lowerBound) <= (uint)(upperBound - lowerBound);

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is between
        /// <paramref name="lowerBound"/> and <paramref name="upperBound"/>, inclusive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInRangeInclusive(long value, long lowerBound, long upperBound)
            => (ulong)(value - lowerBound) <= (ulong)(upperBound - lowerBound);

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is between
        /// <paramref name="lowerBound"/> and <paramref name="upperBound"/>, inclusive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInRangeInclusive(JsonTokenType value, JsonTokenType lowerBound, JsonTokenType upperBound)
            => (value - lowerBound) <= (upperBound - lowerBound);

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is in the range [0..9].
        /// Otherwise, returns <see langword="false"/>.
        /// </summary>
        public static bool IsDigit(byte value) => (uint)(value - '0') <= '9' - '0';

        /// <summary>
        /// Perform a Read() with a Debug.Assert verifying the reader did not return false.
        /// This should be called when the Read() return value is not used, such as non-Stream cases where there is only one buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadWithVerify(this ref Utf8JsonReader reader)
        {
            bool result = reader.Read();
            Debug.Assert(result);
        }

        /// <summary>
        /// Performs a TrySkip() with a Debug.Assert verifying the reader did not return false.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SkipWithVerify(this ref Utf8JsonReader reader)
        {
            bool success = reader.TrySkipPartial(reader.CurrentDepth);
            Debug.Assert(success, "The skipped value should have already been buffered.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TrySkipPartial(this ref Utf8JsonReader reader)
        {
            return reader.TrySkipPartial(reader.CurrentDepth);
        }

        /// <summary>
        /// Calls Encoding.UTF8.GetString that supports netstandard.
        /// </summary>
        /// <param name="bytes">The utf8 bytes to convert.</param>
        /// <returns></returns>
        public static string Utf8GetString(ReadOnlySpan<byte> bytes)
        {
#if NET
            return Encoding.UTF8.GetString(bytes);
#else
            if (bytes.Length == 0)
            {
                return string.Empty;
            }

            unsafe
            {
                fixed (byte* bytesPtr = bytes)
                {
                    return Encoding.UTF8.GetString(bytesPtr, bytes.Length);
                }
            }
#endif
        }

        public static bool TryLookupUtf8Key<TValue>(
            this Dictionary<string, TValue> dictionary,
            ReadOnlySpan<byte> utf8Key,
            [MaybeNullWhen(false)] out TValue result)
        {
#if NET9_0_OR_GREATER
            Debug.Assert(dictionary.Comparer is IAlternateEqualityComparer<ReadOnlySpan<char>, string>);

            Dictionary<string, TValue>.AlternateLookup<ReadOnlySpan<char>> spanLookup =
                dictionary.GetAlternateLookup<string, TValue, ReadOnlySpan<char>>();

            char[]? rentedBuffer = null;

            Span<char> charBuffer = utf8Key.Length <= JsonConstants.StackallocCharThreshold ?
                stackalloc char[JsonConstants.StackallocCharThreshold] :
                (rentedBuffer = ArrayPool<char>.Shared.Rent(utf8Key.Length));

            int charsWritten = Encoding.UTF8.GetChars(utf8Key, charBuffer);
            Span<char> decodedKey = charBuffer[0..charsWritten];

            bool success = spanLookup.TryGetValue(decodedKey, out result);

            if (rentedBuffer != null)
            {
                decodedKey.Clear();
                ArrayPool<char>.Shared.Return(rentedBuffer);
            }

            return success;
#else
            string key = Utf8GetString(utf8Key);
            return dictionary.TryGetValue(key, out result);
#endif
        }

        /// <summary>
        /// Emulates Dictionary(IEnumerable{KeyValuePair}) on netstandard.
        /// </summary>
        public static Dictionary<TKey, TValue> CreateDictionaryFromCollection<TKey, TValue>(
            IEnumerable<KeyValuePair<TKey, TValue>> collection,
            IEqualityComparer<TKey> comparer)
            where TKey : notnull
        {
#if !NET
            var dictionary = new Dictionary<TKey, TValue>(comparer);

            foreach (KeyValuePair<TKey, TValue> item in collection)
            {
                dictionary.Add(item.Key, item.Value);
            }

            return dictionary;
#else
            return new Dictionary<TKey, TValue>(collection: collection, comparer);
#endif
        }

        public static bool IsFinite(double value)
        {
#if NET
            return double.IsFinite(value);
#else
            return !(double.IsNaN(value) || double.IsInfinity(value));
#endif
        }

        public static bool IsFinite(float value)
        {
#if NET
            return float.IsFinite(value);
#else
            return !(float.IsNaN(value) || float.IsInfinity(value));
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidateInt32MaxArrayLength(uint length)
        {
            if (length > 0X7FEFFFFF) // prior to .NET 6, max array length for sizeof(T) != 1 (size == 1 is larger)
            {
                ThrowHelper.ThrowOutOfMemoryException(length);
            }
        }

#if !NET8_0_OR_GREATER
        public static bool HasAllSet(this BitArray bitArray)
        {
            for (int i = 0; i < bitArray.Count; i++)
            {
                if (!bitArray[i])
                {
                    return false;
                }
            }

            return true;
        }
#endif

        /// <summary>
        /// Gets a Regex instance for recognizing integer representations of enums.
        /// </summary>
        public static readonly Regex IntegerRegex = CreateIntegerRegex();
        private const string IntegerRegexPattern = @"^\s*(?:\+|\-)?[0-9]+\s*$";
        private const int IntegerRegexTimeoutMs = 200;

#if NET
        [GeneratedRegex(IntegerRegexPattern, RegexOptions.None, matchTimeoutMilliseconds: IntegerRegexTimeoutMs)]
        private static partial Regex CreateIntegerRegex();
#else
        private static Regex CreateIntegerRegex() => new(IntegerRegexPattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(IntegerRegexTimeoutMs));
#endif

        /// <summary>
        /// Compares two valid UTF-8 encoded JSON numbers for decimal equality.
        /// </summary>
        public static bool AreEqualJsonNumbers(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
        {
            Debug.Assert(left.Length > 0 && right.Length > 0);

            ParseNumber(left,
                out bool leftIsNegative,
                out ReadOnlySpan<byte> leftIntegral,
                out ReadOnlySpan<byte> leftFractional,
                out int leftExponent);

            ParseNumber(right,
                out bool rightIsNegative,
                out ReadOnlySpan<byte> rightIntegral,
                out ReadOnlySpan<byte> rightFractional,
                out int rightExponent);

            int nDigits;
            if (leftIsNegative != rightIsNegative ||
                leftExponent != rightExponent ||
                (nDigits = (leftIntegral.Length + leftFractional.Length)) !=
                            rightIntegral.Length + rightFractional.Length)
            {
                return false;
            }

            if (leftIntegral.Length == rightIntegral.Length)
            {
                return leftIntegral.SequenceEqual(rightIntegral) &&
                    leftFractional.SequenceEqual(rightFractional);
            }

            // There is differentiation in the integral and fractional lengths,
            // concatenate both into singular buffers and compare them.
            scoped Span<byte> leftDigits;
            scoped Span<byte> rightDigits;
            byte[]? rentedLeftBuffer;
            byte[]? rentedRightBuffer;

            if (nDigits <= JsonConstants.StackallocByteThreshold)
            {
                leftDigits = stackalloc byte[JsonConstants.StackallocByteThreshold];
                rightDigits = stackalloc byte[JsonConstants.StackallocByteThreshold];
                rentedLeftBuffer = rentedRightBuffer = null;
            }
            else
            {
                leftDigits = (rentedLeftBuffer = ArrayPool<byte>.Shared.Rent(nDigits));
                rightDigits = (rentedRightBuffer = ArrayPool<byte>.Shared.Rent(nDigits));
            }

            leftIntegral.CopyTo(leftDigits);
            leftFractional.CopyTo(leftDigits.Slice(leftIntegral.Length));
            rightIntegral.CopyTo(rightDigits);
            rightFractional.CopyTo(rightDigits.Slice(rightIntegral.Length));

            bool result = leftDigits.Slice(0, nDigits).SequenceEqual(rightDigits.Slice(0, nDigits));

            if (rentedLeftBuffer != null)
            {
                Debug.Assert(rentedRightBuffer != null);
                rentedLeftBuffer.AsSpan(0, nDigits).Clear();
                rentedRightBuffer.AsSpan(0, nDigits).Clear();
                ArrayPool<byte>.Shared.Return(rentedLeftBuffer);
                ArrayPool<byte>.Shared.Return(rentedRightBuffer);
            }

            return result;

            static void ParseNumber(
                ReadOnlySpan<byte> span,
                out bool isNegative,
                out ReadOnlySpan<byte> integral,
                out ReadOnlySpan<byte> fractional,
                out int exponent)
            {
                // Parses a JSON number into its integral, fractional, and exponent parts.
                // The returned components use a normal-form decimal representation:
                //
                //   Number := sign * <integral + fractional> * 10^exponent
                //
                // where integral and fractional are sequences of digits whose concatenation
                // represents the significand of the number without leading or trailing zeros.
                // Two such normal-form numbers are treated as equal if and only if they have
                // equal signs, significands, and exponents.

                bool neg;
                ReadOnlySpan<byte> intg;
                ReadOnlySpan<byte> frac;
                int exp;

                Debug.Assert(span.Length > 0);

                if (span[0] == '-')
                {
                    neg = true;
                    span = span.Slice(1);
                }
                else
                {
                    Debug.Assert(char.IsDigit((char)span[0]), "leading plus not allowed in valid JSON numbers.");
                    neg = false;
                }

                int i = span.IndexOfAny((byte)'.', (byte)'e', (byte)'E');
                if (i < 0)
                {
                    intg = span;
                    frac = default;
                    exp = 0;
                    goto Normalize;
                }

                intg = span.Slice(0, i);

                if (span[i] == '.')
                {
                    span = span.Slice(i + 1);
                    i = span.IndexOfAny((byte)'e', (byte)'E');
                    if (i < 0)
                    {
                        frac = span;
                        exp = 0;
                        goto Normalize;
                    }

                    frac = span.Slice(0, i);
                }
                else
                {
                    frac = default;
                }

                Debug.Assert(span[i] is (byte)'e' or (byte)'E');
                if (!Utf8Parser.TryParse(span.Slice(i + 1), out exp, out _))
                {
                    Debug.Assert(span.Length >= 10);
                    ThrowHelper.ThrowArgumentOutOfRangeException_JsonNumberExponentTooLarge(nameof(exponent));
                }

            Normalize: // Calculates the normal form of the number.

                if (IndexOfFirstTrailingZero(frac) is >= 0 and int iz)
                {
                    // Trim trailing zeros from the fractional part.
                    // e.g. 3.1400 -> 3.14
                    frac = frac.Slice(0, iz);
                }

                if (intg[0] == '0')
                {
                    Debug.Assert(intg.Length == 1, "Leading zeros not permitted in JSON numbers.");

                    if (IndexOfLastLeadingZero(frac) is >= 0 and int lz)
                    {
                        // Trim leading zeros from the fractional part
                        // and update the exponent accordingly.
                        // e.g. 0.000123 -> 0.123e-3
                        frac = frac.Slice(lz + 1);
                        exp -= lz + 1;
                    }

                    // Normalize "0" to the empty span.
                    intg = default;
                }

                if (frac.IsEmpty && IndexOfFirstTrailingZero(intg) is >= 0 and int fz)
                {
                    // There is no fractional part, trim trailing zeros from
                    // the integral part and increase the exponent accordingly.
                    // e.g. 1000 -> 1e3
                    exp += intg.Length - fz;
                    intg = intg.Slice(0, fz);
                }

                // Normalize the exponent by subtracting the length of the fractional part.
                // e.g. 3.14 -> 314e-2
                exp -= frac.Length;

                if (intg.IsEmpty && frac.IsEmpty)
                {
                    // Normalize zero representations.
                    neg = false;
                    exp = 0;
                }

                // Copy to out parameters.
                isNegative = neg;
                integral = intg;
                fractional = frac;
                exponent = exp;

                static int IndexOfLastLeadingZero(ReadOnlySpan<byte> span)
                {
#if NET
                    int firstNonZero = span.IndexOfAnyExcept((byte)'0');
                    return firstNonZero < 0 ? span.Length - 1 : firstNonZero - 1;
#else
                    for (int i = 0; i < span.Length; i++)
                    {
                        if (span[i] != '0')
                        {
                            return i - 1;
                        }
                    }

                    return span.Length - 1;
#endif
                }

                static int IndexOfFirstTrailingZero(ReadOnlySpan<byte> span)
                {
#if NET
                    int lastNonZero = span.LastIndexOfAnyExcept((byte)'0');
                    return lastNonZero == span.Length - 1 ? -1 : lastNonZero + 1;
#else
                    if (span.IsEmpty)
                    {
                        return -1;
                    }

                    for (int i = span.Length - 1; i >= 0; i--)
                    {
                        if (span[i] != '0')
                        {
                            return i == span.Length - 1 ? -1 : i + 1;
                        }
                    }

                    return 0;
#endif
                }
            }
        }

#if !NET
        // Polyfill for MemoryExtensions.Trim
        public static ReadOnlySpan<char> Trim(this ReadOnlySpan<char> source)
        {
            int start = 0;
            int end = source.Length - 1;

            while (start <= end && char.IsWhiteSpace(source[start]))
            {
                start++;
            }

            while (end >= start && char.IsWhiteSpace(source[end]))
            {
                end--;
            }

            return source.Slice(start, end - start + 1);
        }

        public static ReadOnlySpan<char> TrimStart(this ReadOnlySpan<char> source)
        {
            int start = 0;
            int end = source.Length - 1;

            while (start <= end && char.IsWhiteSpace(source[start]))
            {
                start++;
            }

            return source.Slice(start, end - start + 1);
        }

        public static ReadOnlySpan<char> TrimEnd(this ReadOnlySpan<char> source)
        {
            int start = 0;
            int end = source.Length - 1;

            while (end >= start && char.IsWhiteSpace(source[end]))
            {
                end--;
            }

            return source.Slice(start, end - start + 1);
        }
#endif
    }
}
