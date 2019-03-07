// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Numerics;

namespace System.Security.Cryptography.Asn1
{
    internal partial class AsnReader
    {
        /// <summary>
        ///   Reads the next value as an Integer with tag UNIVERSAL 2, returning the contents
        ///   as a <see cref="ReadOnlyMemory{T}"/> over the original data.
        /// </summary>
        /// <returns>
        ///   The bytes of the Integer value, in signed big-endian form.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        public ReadOnlyMemory<byte> ReadIntegerBytes() =>
            ReadIntegerBytes(Asn1Tag.Integer);

        /// <summary>
        ///   Reads the next value as a Integer with a specified tag, returning the contents
        ///   as a <see cref="ReadOnlyMemory{T}"/> over the original data.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <returns>
        ///   The bytes of the Integer value, in signed big-endian form.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        public ReadOnlyMemory<byte> ReadIntegerBytes(Asn1Tag expectedTag)
        {
            ReadOnlyMemory<byte> contents =
                GetIntegerContents(expectedTag, UniversalTagNumber.Integer, out int headerLength);

            _data = _data.Slice(headerLength + contents.Length);
            return contents;
        }

        /// <summary>
        ///   Reads the next value as an Integer with tag UNIVERSAL 2, returning the contents
        ///   as a <see cref="BigInteger"/>.
        /// </summary>
        /// <returns>
        ///   The bytes of the Integer value, in signed big-endian form.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        public BigInteger ReadInteger() => ReadInteger(Asn1Tag.Integer);

        /// <summary>
        ///   Reads the next value as a Integer with a specified tag, returning the contents
        ///   as a <see cref="BigInteger"/> over the original data.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <returns>
        ///   The bytes of the Integer value, in signed big-endian form.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        public BigInteger ReadInteger(Asn1Tag expectedTag)
        {
            ReadOnlyMemory<byte> contents =
                GetIntegerContents(expectedTag, UniversalTagNumber.Integer, out int headerLength);

            // TODO: Split this for netcoreapp/netstandard to use the Big-Endian BigInteger parsing
            byte[] tmp = ArrayPool<byte>.Shared.Rent(contents.Length);
            BigInteger value;

            try
            {
                byte fill = (contents.Span[0] & 0x80) == 0 ? (byte)0 : (byte)0xFF;
                // Fill the unused portions of tmp with positive or negative padding.
                new Span<byte>(tmp, contents.Length, tmp.Length - contents.Length).Fill(fill);
                contents.CopyTo(tmp);
                // Convert to Little-Endian.
                AsnWriter.Reverse(new Span<byte>(tmp, 0, contents.Length));
                value = new BigInteger(tmp);
            }
            finally
            {
                // Clear the whole tmp so that not even the sign bit is returned to the array pool.
                Array.Clear(tmp, 0, tmp.Length);
                ArrayPool<byte>.Shared.Return(tmp);
            }

            _data = _data.Slice(headerLength + contents.Length);
            return value;
        }

        /// <summary>
        ///   Reads the next value as an Integer with tag UNIVERSAL 2, interpreting the contents
        ///   as an <see cref="int"/>.
        /// </summary>
        /// <param name="value">
        ///   On success, receives the <see cref="int"/> value represented
        /// </param>
        /// <returns>
        ///   <c>false</c> and does not advance the reader if the value is not between
        ///   <see cref="int.MinValue"/> and <see cref="int.MaxValue"/>, inclusive; otherwise
        ///   <c>true</c> is returned and the reader advances.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        public bool TryReadInt32(out int value) =>
            TryReadInt32(Asn1Tag.Integer, out value);

        /// <summary>
        ///   Reads the next value as a Integer with a specified tag, interpreting the contents
        ///   as an <see cref="int"/>.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="value">
        ///   On success, receives the <see cref="int"/> value represented
        /// </param>
        /// <returns>
        ///   <c>false</c> and does not advance the reader if the value is not between
        ///   <see cref="int.MinValue"/> and <see cref="int.MaxValue"/>, inclusive; otherwise
        ///   <c>true</c> is returned and the reader advances.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        public bool TryReadInt32(Asn1Tag expectedTag, out int value)
        {
            if (TryReadSignedInteger(sizeof(int), expectedTag, UniversalTagNumber.Integer, out long longValue))
            {
                value = (int)longValue;
                return true;
            }

            value = 0;
            return false;
        }

        /// <summary>
        ///   Reads the next value as an Integer with tag UNIVERSAL 2, interpreting the contents
        ///   as a <see cref="uint"/>.
        /// </summary>
        /// <param name="value">
        ///   On success, receives the <see cref="uint"/> value represented
        /// </param>
        /// <returns>
        ///   <c>false</c> and does not advance the reader if the value is not between
        ///   <see cref="uint.MinValue"/> and <see cref="uint.MaxValue"/>, inclusive; otherwise
        ///   <c>true</c> is returned and the reader advances.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        public bool TryReadUInt32(out uint value) =>
            TryReadUInt32(Asn1Tag.Integer, out value);

        /// <summary>
        ///   Reads the next value as a Integer with a specified tag, interpreting the contents
        ///   as a <see cref="uint"/>.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="value">
        ///   On success, receives the <see cref="uint"/> value represented
        /// </param>
        /// <returns>
        ///   <c>false</c> and does not advance the reader if the value is not between
        ///   <see cref="uint.MinValue"/> and <see cref="uint.MaxValue"/>, inclusive; otherwise
        ///   <c>true</c> is returned and the reader advances.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        public bool TryReadUInt32(Asn1Tag expectedTag, out uint value)
        {
            if (TryReadUnsignedInteger(sizeof(uint), expectedTag, UniversalTagNumber.Integer, out ulong ulongValue))
            {
                value = (uint)ulongValue;
                return true;
            }

            value = 0;
            return false;
        }

        /// <summary>
        ///   Reads the next value as an Integer with tag UNIVERSAL 2, interpreting the contents
        ///   as a <see cref="long"/>.
        /// </summary>
        /// <param name="value">
        ///   On success, receives the <see cref="long"/> value represented
        /// </param>
        /// <returns>
        ///   <c>false</c> and does not advance the reader if the value is not between
        ///   <see cref="long.MinValue"/> and <see cref="long.MaxValue"/>, inclusive; otherwise
        ///   <c>true</c> is returned and the reader advances.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        public bool TryReadInt64(out long value) =>
            TryReadInt64(Asn1Tag.Integer, out value);

        /// <summary>
        ///   Reads the next value as a Integer with a specified tag, interpreting the contents
        ///   as an <see cref="long"/>.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="value">
        ///   On success, receives the <see cref="long"/> value represented
        /// </param>
        /// <returns>
        ///   <c>false</c> and does not advance the reader if the value is not between
        ///   <see cref="long.MinValue"/> and <see cref="long.MaxValue"/>, inclusive; otherwise
        ///   <c>true</c> is returned and the reader advances.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        public bool TryReadInt64(Asn1Tag expectedTag, out long value)
        {
            return TryReadSignedInteger(sizeof(long), expectedTag, UniversalTagNumber.Integer, out value);
        }

        /// <summary>
        ///   Reads the next value as an Integer with tag UNIVERSAL 2, interpreting the contents
        ///   as a <see cref="ulong"/>.
        /// </summary>
        /// <param name="value">
        ///   On success, receives the <see cref="ulong"/> value represented
        /// </param>
        /// <returns>
        ///   <c>false</c> and does not advance the reader if the value is not between
        ///   <see cref="ulong.MinValue"/> and <see cref="ulong.MaxValue"/>, inclusive; otherwise
        ///   <c>true</c> is returned and the reader advances.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        public bool TryReadUInt64(out ulong value) =>
            TryReadUInt64(Asn1Tag.Integer, out value);

        /// <summary>
        ///   Reads the next value as a Integer with a specified tag, interpreting the contents
        ///   as a <see cref="ulong"/>.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="value">
        ///   On success, receives the <see cref="ulong"/> value represented
        /// </param>
        /// <returns>
        ///   <c>false</c> and does not advance the reader if the value is not between
        ///   <see cref="ulong.MinValue"/> and <see cref="ulong.MaxValue"/>, inclusive; otherwise
        ///   <c>true</c> is returned and the reader advances.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        public bool TryReadUInt64(Asn1Tag expectedTag, out ulong value)
        {
            return TryReadUnsignedInteger(sizeof(ulong), expectedTag, UniversalTagNumber.Integer, out value);
        }

        /// <summary>
        ///   Reads the next value as an Integer with tag UNIVERSAL 2, interpreting the contents
        ///   as a <see cref="short"/>.
        /// </summary>
        /// <param name="value">
        ///   On success, receives the <see cref="short"/> value represented
        /// </param>
        /// <returns>
        ///   <c>false</c> and does not advance the reader if the value is not between
        ///   <see cref="short.MinValue"/> and <see cref="short.MaxValue"/>, inclusive; otherwise
        ///   <c>true</c> is returned and the reader advances.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        public bool TryReadInt16(out short value) =>
            TryReadInt16(Asn1Tag.Integer, out value);

        /// <summary>
        ///   Reads the next value as a Integer with a specified tag, interpreting the contents
        ///   as an <see cref="short"/>.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="value">
        ///   On success, receives the <see cref="short"/> value represented
        /// </param>
        /// <returns>
        ///   <c>false</c> and does not advance the reader if the value is not between
        ///   <see cref="short.MinValue"/> and <see cref="short.MaxValue"/>, inclusive; otherwise
        ///   <c>true</c> is returned and the reader advances.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        public bool TryReadInt16(Asn1Tag expectedTag, out short value)
        {
            if (TryReadSignedInteger(sizeof(short), expectedTag, UniversalTagNumber.Integer, out long longValue))
            {
                value = (short)longValue;
                return true;
            }

            value = 0;
            return false;
        }

        /// <summary>
        ///   Reads the next value as an Integer with tag UNIVERSAL 2, interpreting the contents
        ///   as a <see cref="ushort"/>.
        /// </summary>
        /// <param name="value">
        ///   On success, receives the <see cref="ushort"/> value represented
        /// </param>
        /// <returns>
        ///   <c>false</c> and does not advance the reader if the value is not between
        ///   <see cref="ushort.MinValue"/> and <see cref="ushort.MaxValue"/>, inclusive; otherwise
        ///   <c>true</c> is returned and the reader advances.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        public bool TryReadUInt16(out ushort value) =>
            TryReadUInt16(Asn1Tag.Integer, out value);

        /// <summary>
        ///   Reads the next value as a Integer with a specified tag, interpreting the contents
        ///   as a <see cref="ushort"/>.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="value">
        ///   On success, receives the <see cref="ushort"/> value represented
        /// </param>
        /// <returns>
        ///   <c>false</c> and does not advance the reader if the value is not between
        ///   <see cref="ushort.MinValue"/> and <see cref="ushort.MaxValue"/>, inclusive; otherwise
        ///   <c>true</c> is returned and the reader advances.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        public bool TryReadUInt16(Asn1Tag expectedTag, out ushort value)
        {
            if (TryReadUnsignedInteger(sizeof(ushort), expectedTag, UniversalTagNumber.Integer, out ulong ulongValue))
            {
                value = (ushort)ulongValue;
                return true;
            }

            value = 0;
            return false;
        }

        /// <summary>
        ///   Reads the next value as an Integer with tag UNIVERSAL 2, interpreting the contents
        ///   as an <see cref="sbyte"/>.
        /// </summary>
        /// <param name="value">
        ///   On success, receives the <see cref="sbyte"/> value represented
        /// </param>
        /// <returns>
        ///   <c>false</c> and does not advance the reader if the value is not between
        ///   <see cref="sbyte.MinValue"/> and <see cref="sbyte.MaxValue"/>, inclusive; otherwise
        ///   <c>true</c> is returned and the reader advances.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        public bool TryReadInt8(out sbyte value) =>
            TryReadInt8(Asn1Tag.Integer, out value);

        /// <summary>
        ///   Reads the next value as a Integer with a specified tag, interpreting the contents
        ///   as an <see cref="sbyte"/>.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="value">
        ///   On success, receives the <see cref="sbyte"/> value represented
        /// </param>
        /// <returns>
        ///   <c>false</c> and does not advance the reader if the value is not between
        ///   <see cref="sbyte.MinValue"/> and <see cref="sbyte.MaxValue"/>, inclusive; otherwise
        ///   <c>true</c> is returned and the reader advances.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the Integer value is not valid
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        public bool TryReadInt8(Asn1Tag expectedTag, out sbyte value)
        {
            if (TryReadSignedInteger(sizeof(sbyte), expectedTag, UniversalTagNumber.Integer, out long longValue))
            {
                value = (sbyte)longValue;
                return true;
            }

            value = 0;
            return false;
        }

        /// <summary>
        ///   Reads the next value as an Integer with tag UNIVERSAL 2, interpreting the contents
        ///   as a <see cref="byte"/>.
        /// </summary>
        /// <param name="value">
        ///   On success, receives the <see cref="byte"/> value represented
        /// </param>
        /// <returns>
        ///   <c>false</c> and does not advance the reader if the value is not between
        ///   <see cref="byte.MinValue"/> and <see cref="byte.MaxValue"/>, inclusive; otherwise
        ///   <c>true</c> is returned and the reader advances.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        public bool TryReadUInt8(out byte value) =>
            TryReadUInt8(Asn1Tag.Integer, out value);

        /// <summary>
        ///   Reads the next value as a Integer with a specified tag, interpreting the contents
        ///   as a <see cref="byte"/>.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="value">
        ///   On success, receives the <see cref="byte"/> value represented
        /// </param>
        /// <returns>
        ///   <c>false</c> and does not advance the reader if the value is not between
        ///   <see cref="byte.MinValue"/> and <see cref="byte.MaxValue"/>, inclusive; otherwise
        ///   <c>true</c> is returned and the reader advances.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        public bool TryReadUInt8(Asn1Tag expectedTag, out byte value)
        {
            if (TryReadUnsignedInteger(sizeof(byte), expectedTag, UniversalTagNumber.Integer, out ulong ulongValue))
            {
                value = (byte)ulongValue;
                return true;
            }

            value = 0;
            return false;
        }

        private ReadOnlyMemory<byte> GetIntegerContents(
            Asn1Tag expectedTag,
            UniversalTagNumber tagNumber,
            out int headerLength)
        {
            Asn1Tag tag = ReadTagAndLength(out int? length, out headerLength);
            CheckExpectedTag(tag, expectedTag, tagNumber);

            // T-REC-X.690-201508 sec 8.3.1
            if (tag.IsConstructed || length < 1)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            // Slice first so that an out of bounds value triggers a CryptographicException.
            ReadOnlyMemory<byte> contents = Slice(_data, headerLength, length.Value);
            ReadOnlySpan<byte> contentSpan = contents.Span;

            // T-REC-X.690-201508 sec 8.3.2
            if (contents.Length > 1)
            {
                ushort bigEndianValue = (ushort)(contentSpan[0] << 8 | contentSpan[1]);
                const ushort RedundancyMask = 0b1111_1111_1000_0000;
                ushort masked = (ushort)(bigEndianValue & RedundancyMask);

                // If the first 9 bits are all 0 or are all 1, the value is invalid.
                if (masked == 0 || masked == RedundancyMask)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }
            }

            return contents;
        }

        private bool TryReadSignedInteger(
            int sizeLimit,
            Asn1Tag expectedTag,
            UniversalTagNumber tagNumber,
            out long value)
        {
            Debug.Assert(sizeLimit <= sizeof(long));

            ReadOnlyMemory<byte> contents = GetIntegerContents(expectedTag, tagNumber, out int headerLength);

            if (contents.Length > sizeLimit)
            {
                value = 0;
                return false;
            }

            ReadOnlySpan<byte> contentSpan = contents.Span;

            bool isNegative = (contentSpan[0] & 0x80) != 0;
            long accum = isNegative ? -1 : 0;

            for (int i = 0; i < contents.Length; i++)
            {
                accum <<= 8;
                accum |= contentSpan[i];
            }

            _data = _data.Slice(headerLength + contents.Length);
            value = accum;
            return true;
        }

        private bool TryReadUnsignedInteger(
            int sizeLimit,
            Asn1Tag expectedTag,
            UniversalTagNumber tagNumber,
            out ulong value)
        {
            Debug.Assert(sizeLimit <= sizeof(ulong));

            ReadOnlyMemory<byte> contents = GetIntegerContents(expectedTag, tagNumber, out int headerLength);
            ReadOnlySpan<byte> contentSpan = contents.Span;
            int contentLength = contents.Length;

            bool isNegative = (contentSpan[0] & 0x80) != 0;

            if (isNegative)
            {
                value = 0;
                return false;
            }

            // Ignore any padding zeros.
            if (contentSpan.Length > 1 && contentSpan[0] == 0)
            {
                contentSpan = contentSpan.Slice(1);
            }

            if (contentSpan.Length > sizeLimit)
            {
                value = 0;
                return false;
            }

            ulong accum = 0;

            for (int i = 0; i < contentSpan.Length; i++)
            {
                accum <<= 8;
                accum |= contentSpan[i];
            }

            _data = _data.Slice(headerLength + contentLength);
            value = accum;
            return true;
        }
    }
}
