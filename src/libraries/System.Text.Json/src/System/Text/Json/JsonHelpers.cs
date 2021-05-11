// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    internal static partial class JsonHelpers
    {
        // Members accessed by the serializer when deserializing.
        public const DynamicallyAccessedMemberTypes MembersAccessedOnRead =
            DynamicallyAccessedMemberTypes.PublicConstructors |
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields;

        /// <summary>
        /// Returns the span for the given reader.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<byte> GetSpan(this ref Utf8JsonReader reader)
        {
            return reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
        }

#if !BUILDING_INBOX_LIBRARY
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
        /// Calls Encoding.UTF8.GetString that supports netstandard.
        /// </summary>
        /// <param name="bytes">The utf8 bytes to convert.</param>
        /// <returns></returns>
        public static string Utf8GetString(ReadOnlySpan<byte> bytes)
        {
            return Encoding.UTF8.GetString(bytes
#if NETSTANDARD2_0 || NETFRAMEWORK
                        .ToArray()
#endif
                );
        }

        /// <summary>
        /// Emulates Dictionary.TryAdd on netstandard.
        /// </summary>
        public static bool TryAdd<TKey, TValue>(Dictionary<TKey, TValue> dictionary, in TKey key, in TValue value) where TKey : notnull
        {
#if NETSTANDARD2_0 || NETFRAMEWORK
            if (!dictionary.ContainsKey(key))
            {
                dictionary[key] = value;
                return true;
            }

            return false;
#else
            return dictionary.TryAdd(key, value);
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
#if NETSTANDARD2_0 || NETFRAMEWORK
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
#if BUILDING_INBOX_LIBRARY
            return double.IsFinite(value);
#else
            return !(double.IsNaN(value) || double.IsInfinity(value));
#endif
        }

        public static bool IsFinite(float value)
        {
#if BUILDING_INBOX_LIBRARY
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
    }
}
