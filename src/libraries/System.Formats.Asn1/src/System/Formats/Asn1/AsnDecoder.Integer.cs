// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;

namespace System.Formats.Asn1
{
    public static partial class AsnDecoder
    {
        /// <summary>
        ///   Reads an Integer value from <paramref name="source"/> with a specified tag under
        ///   the specified encoding rules, returning the contents as a slice of the buffer.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 2).
        /// </param>
        /// <returns>
        ///   The slice of the buffer containing the bytes of the Integer value, in signed big-endian form.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="ruleSet"/> is not defined.
        /// </exception>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        public static ReadOnlySpan<byte> ReadIntegerBytes(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out int bytesConsumed,
            Asn1Tag? expectedTag = null)
        {
            return GetIntegerContents(
                source,
                ruleSet,
                expectedTag ?? Asn1Tag.Integer,
                UniversalTagNumber.Integer,
                out bytesConsumed);
        }

        /// <summary>
        ///   Reads an Integer value from <paramref name="source"/> with a specified tag under
        ///   the specified encoding rules.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 2).
        /// </param>
        /// <returns>
        ///   The decoded numeric value.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="ruleSet"/> is not defined.
        /// </exception>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        public static BigInteger ReadInteger(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out int bytesConsumed,
            Asn1Tag? expectedTag = null)
        {
            ReadOnlySpan<byte> contents = ReadIntegerBytes(source, ruleSet, out int consumed, expectedTag);

#if NETCOREAPP2_1_OR_GREATER
            BigInteger value = new BigInteger(contents, isBigEndian: true);
#else
            byte[] tmp = CryptoPool.Rent(contents.Length);
            BigInteger value;

            try
            {
                byte fill = (contents[0] & 0x80) == 0 ? (byte)0 : (byte)0xFF;
                // Fill the unused portions of tmp with positive or negative padding.
                tmp.AsSpan(contents.Length, tmp.Length - contents.Length).Fill(fill);
                contents.CopyTo(tmp);
                // Convert to Little-Endian.
                tmp.AsSpan(0, contents.Length).Reverse();
                value = new BigInteger(tmp);
            }
            finally
            {
                // Let CryptoPool.Return clear the whole tmp so that not even the sign bit
                // is returned to the array pool.
                CryptoPool.Return(tmp);
            }
#endif

            bytesConsumed = consumed;
            return value;
        }

        /// <summary>
        ///   Attempts to read an Integer value from <paramref name="source"/> with a specified tag under
        ///   the specified encoding rules as a signed 32-bit value.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="value">
        ///   On success, receives the interpreted numeric value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 2).
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the Integer represents value is between
        ///   <see cref="int.MinValue"/> and <see cref="int.MaxValue"/>, inclusive; otherwise,
        ///   <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="ruleSet"/> is not defined.
        /// </exception>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        public static bool TryReadInt32(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out int value,
            out int bytesConsumed,
            Asn1Tag? expectedTag = null)
        {
            if (TryReadSignedInteger(
                source,
                ruleSet,
                sizeof(int),
                expectedTag ?? Asn1Tag.Integer,
                UniversalTagNumber.Integer,
                out long longValue,
                out bytesConsumed))
            {
                value = (int)longValue;
                return true;
            }

            value = 0;
            return false;
        }

        /// <summary>
        ///   Attempts to read an Integer value from <paramref name="source"/> with a specified tag under
        ///   the specified encoding rules as an unsigned 32-bit value.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="value">
        ///   On success, receives the interpreted numeric value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 2).
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the Integer represents value is between
        ///   <see cref="uint.MinValue"/> and <see cref="uint.MaxValue"/>, inclusive; otherwise,
        ///   <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="ruleSet"/> is not defined.
        /// </exception>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        [CLSCompliant(false)]
        public static bool TryReadUInt32(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out uint value,
            out int bytesConsumed,
            Asn1Tag? expectedTag = null)
        {
            if (TryReadUnsignedInteger(
                source,
                ruleSet,
                sizeof(uint),
                expectedTag ?? Asn1Tag.Integer,
                UniversalTagNumber.Integer,
                out ulong ulongValue,
                out bytesConsumed))
            {
                value = (uint)ulongValue;
                return true;
            }

            value = 0;
            return false;
        }

        /// <summary>
        ///   Attempts to read an Integer value from <paramref name="source"/> with a specified tag under
        ///   the specified encoding rules as a signed 64-bit value.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="value">
        ///   On success, receives the interpreted numeric value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 2).
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the Integer represents value is between
        ///   <see cref="long.MinValue"/> and <see cref="long.MaxValue"/>, inclusive; otherwise,
        ///   <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="ruleSet"/> is not defined.
        /// </exception>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        public static bool TryReadInt64(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out long value,
            out int bytesConsumed,
            Asn1Tag? expectedTag = null)
        {
            return TryReadSignedInteger(
                source,
                ruleSet,
                sizeof(long),
                expectedTag ?? Asn1Tag.Integer,
                UniversalTagNumber.Integer,
                out value,
                out bytesConsumed);
        }

        /// <summary>
        ///   Attempts to read an Integer value from <paramref name="source"/> with a specified tag under
        ///   the specified encoding rules as an unsigned 64-bit value.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="value">
        ///   On success, receives the interpreted numeric value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 2).
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the Integer represents value is between
        ///   <see cref="ulong.MinValue"/> and <see cref="ulong.MaxValue"/>, inclusive; otherwise,
        ///   <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="ruleSet"/> is not defined.
        /// </exception>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        [CLSCompliant(false)]
        public static bool TryReadUInt64(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out ulong value,
            out int bytesConsumed,
            Asn1Tag? expectedTag = null)
        {
            return TryReadUnsignedInteger(
                source,
                ruleSet,
                sizeof(ulong),
                expectedTag ?? Asn1Tag.Integer,
                UniversalTagNumber.Integer,
                out value,
                out bytesConsumed);
        }

        private static ReadOnlySpan<byte> GetIntegerContents(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            Asn1Tag expectedTag,
            UniversalTagNumber tagNumber,
            out int bytesConsumed)
        {
            // T-REC-X.690-201508 sec 8.3.1
            ReadOnlySpan<byte> contents = GetPrimitiveContentSpan(
                source,
                ruleSet,
                expectedTag,
                tagNumber,
                out int consumed);

            // T-REC-X.690-201508 sec 8.3.1
            if (contents.IsEmpty)
            {
                throw new AsnContentException();
            }

            // T-REC-X.690-201508 sec 8.3.2
            if (BinaryPrimitives.TryReadUInt16BigEndian(contents, out ushort bigEndianValue))
            {
                const ushort RedundancyMask = 0b1111_1111_1000_0000;
                ushort masked = (ushort)(bigEndianValue & RedundancyMask);

                // If the first 9 bits are all 0 or are all 1, the value is invalid.
                if (masked == 0 || masked == RedundancyMask)
                {
                    throw new AsnContentException();
                }
            }

            bytesConsumed = consumed;
            return contents;
        }

        private static bool TryReadSignedInteger(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            int sizeLimit,
            Asn1Tag expectedTag,
            UniversalTagNumber tagNumber,
            out long value,
            out int bytesConsumed)
        {
            Debug.Assert(sizeLimit <= sizeof(long));

            ReadOnlySpan<byte> contents = GetIntegerContents(
                source,
                ruleSet,
                expectedTag,
                tagNumber,
                out int consumed);

            if (contents.Length > sizeLimit)
            {
                value = 0;
                bytesConsumed = 0;
                return false;
            }

            bool isNegative = (contents[0] & 0x80) != 0;
            long accum = isNegative ? -1 : 0;

            for (int i = 0; i < contents.Length; i++)
            {
                accum <<= 8;
                accum |= contents[i];
            }

            bytesConsumed = consumed;
            value = accum;
            return true;
        }

        private static bool TryReadUnsignedInteger(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            int sizeLimit,
            Asn1Tag expectedTag,
            UniversalTagNumber tagNumber,
            out ulong value,
            out int bytesConsumed)
        {
            Debug.Assert(sizeLimit <= sizeof(ulong));

            ReadOnlySpan<byte> contents = GetIntegerContents(
                source,
                ruleSet,
                expectedTag,
                tagNumber,
                out int consumed);

            bool isNegative = (contents[0] & 0x80) != 0;

            if (isNegative)
            {
                bytesConsumed = 0;
                value = 0;
                return false;
            }

            // Ignore any padding zeros.
            if (contents.Length > 1 && contents[0] == 0)
            {
                contents = contents.Slice(1);
            }

            if (contents.Length > sizeLimit)
            {
                bytesConsumed = 0;
                value = 0;
                return false;
            }

            ulong accum = 0;

            for (int i = 0; i < contents.Length; i++)
            {
                accum <<= 8;
                accum |= contents[i];
            }

            bytesConsumed = consumed;
            value = accum;
            return true;
        }
    }

    public partial class AsnReader
    {
        /// <summary>
        ///   Reads the next value as a Integer with a specified tag, returning the contents
        ///   as a <see cref="ReadOnlyMemory{T}"/> over the original data.
        /// </summary>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 2).
        /// </param>
        /// <returns>
        ///   The bytes of the Integer value, in signed big-endian form.
        /// </returns>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        public ReadOnlyMemory<byte> ReadIntegerBytes(Asn1Tag? expectedTag = null)
        {
            ReadOnlySpan<byte> bytes =
                AsnDecoder.ReadIntegerBytes(_data.Span, RuleSet, out int consumed, expectedTag);

            ReadOnlyMemory<byte> ret = AsnDecoder.Slice(_data, bytes);

            _data = _data.Slice(consumed);
            return ret;
        }

        /// <summary>
        ///   Reads the next value as an Integer with a specified tag.
        /// </summary>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 2).
        /// </param>
        /// <returns>
        ///   The decoded value.
        /// </returns>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        public BigInteger ReadInteger(Asn1Tag? expectedTag = null)
        {
            BigInteger ret = AsnDecoder.ReadInteger(_data.Span, RuleSet, out int consumed, expectedTag);
            _data = _data.Slice(consumed);
            return ret;
        }

        /// <summary>
        ///   Attempts to read the next value as an Integer with a specified tag,
        ///   as a signed 32-bit value.
        /// </summary>
        /// <param name="value">
        ///   On success, receives the decoded value.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 2).
        /// </param>
        /// <returns>
        ///   <see langword="false"/> and does not advance the reader if the value is not between
        ///   <see cref="int.MinValue"/> and <see cref="int.MaxValue"/>, inclusive; otherwise
        ///   <see langword="true"/> is returned and the reader advances.
        /// </returns>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        public bool TryReadInt32(out int value, Asn1Tag? expectedTag = null)
        {
            bool ret = AsnDecoder.TryReadInt32(_data.Span, RuleSet, out value, out int read, expectedTag);
            _data = _data.Slice(read);
            return ret;
        }

        /// <summary>
        ///   Attempts to read the next value as an Integer with a specified tag,
        ///   as an unsigned 32-bit value.
        /// </summary>
        /// <param name="value">
        ///   On success, receives the decoded value.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 2).
        /// </param>
        /// <returns>
        ///   <see langword="false"/> and does not advance the reader if the value is not between
        ///   <see cref="uint.MinValue"/> and <see cref="uint.MaxValue"/>, inclusive; otherwise
        ///   <see langword="true"/> is returned and the reader advances.
        /// </returns>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        [CLSCompliant(false)]
        public bool TryReadUInt32(out uint value, Asn1Tag? expectedTag = null)
        {
            bool ret = AsnDecoder.TryReadUInt32(_data.Span, RuleSet, out value, out int read, expectedTag);
            _data = _data.Slice(read);
            return ret;
        }

        /// <summary>
        ///   Attempts to read the next value as an Integer with a specified tag,
        ///   as a signed 64-bit value.
        /// </summary>
        /// <param name="value">
        ///   On success, receives the decoded value.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 2).
        /// </param>
        /// <returns>
        ///   <see langword="false"/> and does not advance the reader if the value is not between
        ///   <see cref="long.MinValue"/> and <see cref="long.MaxValue"/>, inclusive; otherwise
        ///   <see langword="true"/> is returned and the reader advances.
        /// </returns>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        public bool TryReadInt64(out long value, Asn1Tag? expectedTag = null)
        {
            bool ret = AsnDecoder.TryReadInt64(_data.Span, RuleSet, out value, out int read, expectedTag);
            _data = _data.Slice(read);
            return ret;
        }

        /// <summary>
        ///   Attempts to read the next value as an Integer with a specified tag,
        ///   as an unsigned 64-bit value.
        /// </summary>
        /// <param name="value">
        ///   On success, receives the decoded value.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 2).
        /// </param>
        /// <returns>
        ///   <see langword="false"/> and does not advance the reader if the value is not between
        ///   <see cref="ulong.MinValue"/> and <see cref="ulong.MaxValue"/>, inclusive; otherwise
        ///   <see langword="true"/> is returned and the reader advances.
        /// </returns>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        [CLSCompliant(false)]
        public bool TryReadUInt64(out ulong value, Asn1Tag? expectedTag = null)
        {
            bool ret = AsnDecoder.TryReadUInt64(_data.Span, RuleSet, out value, out int read, expectedTag);
            _data = _data.Slice(read);
            return ret;
        }
    }
}
