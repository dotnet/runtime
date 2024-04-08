// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// <returns>True if the the reader has been buffered with all required data.</returns>
        // AggressiveInlining used since this method is on a hot path and short. The AdvanceWithReadAhead method should not be inlined.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdvanceWithOptionalReadAhead(this scoped ref Utf8JsonReader reader, bool requiresReadAhead)
        {
            // No read-ahead necessary if we're at the final block of JSON data.
            bool readAhead = requiresReadAhead && !reader.IsFinalBlock;
            return readAhead ? TryAdvanceWithReadAhead(ref reader) : reader.Read();

            // The read-ahead method is not inlined
            static bool TryAdvanceWithReadAhead(scoped ref Utf8JsonReader reader)
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
        }

#if !NETCOREAPP
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
#if NETCOREAPP
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

        /// <summary>
        /// Emulates Dictionary(IEnumerable{KeyValuePair}) on netstandard.
        /// </summary>
        public static Dictionary<TKey, TValue> CreateDictionaryFromCollection<TKey, TValue>(
            IEnumerable<KeyValuePair<TKey, TValue>> collection,
            IEqualityComparer<TKey> comparer)
            where TKey : notnull
        {
#if !NETCOREAPP
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
#if NETCOREAPP
            return double.IsFinite(value);
#else
            return !(double.IsNaN(value) || double.IsInfinity(value));
#endif
        }

        public static bool IsFinite(float value)
        {
#if NETCOREAPP
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
        private const string IntegerRegexPattern = @"^\s*(\+|\-)?[0-9]+\s*$";
        private const int IntegerRegexTimeoutMs = 200;

#if NETCOREAPP
        [GeneratedRegex(IntegerRegexPattern, RegexOptions.None, matchTimeoutMilliseconds: IntegerRegexTimeoutMs)]
        private static partial Regex CreateIntegerRegex();
#else
        private static Regex CreateIntegerRegex() => new(IntegerRegexPattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(IntegerRegexTimeoutMs));
#endif
    }
}
