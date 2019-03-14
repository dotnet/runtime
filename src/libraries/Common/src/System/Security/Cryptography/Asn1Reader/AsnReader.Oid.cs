// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace System.Security.Cryptography.Asn1
{
    internal partial class AsnReader
    {
        /// <summary>
        ///   Reads the next value as an OBJECT IDENTIFIER with tag UNIVERSAL 6, returning
        ///   the value in a dotted decimal format string.
        /// </summary>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        public string ReadObjectIdentifierAsString() =>
            ReadObjectIdentifierAsString(Asn1Tag.ObjectIdentifier);

        /// <summary>
        ///   Reads the next value as an OBJECT IDENTIFIER with a specified tag, returning
        ///   the value in a dotted decimal format string.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
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
        public string ReadObjectIdentifierAsString(Asn1Tag expectedTag)
        {
            string oidValue = ReadObjectIdentifierAsString(expectedTag, out int bytesRead);

            _data = _data.Slice(bytesRead);

            return oidValue;
        }

        /// <summary>
        ///   Reads the next value as an OBJECT IDENTIFIER with tag UNIVERSAL 6, returning
        ///   the value as an <see cref="Oid"/>.
        /// </summary>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        public Oid ReadObjectIdentifier() =>
            ReadObjectIdentifier(Asn1Tag.ObjectIdentifier);

        /// <summary>
        ///   Reads the next value as an OBJECT IDENTIFIER with a specified tag, returning
        ///   the value as an <see cref="Oid"/>.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
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
        public Oid ReadObjectIdentifier(Asn1Tag expectedTag)
        {
            string oidValue = ReadObjectIdentifierAsString(expectedTag, out int bytesRead);
            // Specifying null for friendly name makes the lookup deferred until first read
            // of the Oid.FriendlyName property.
            Oid oid = new Oid(oidValue, null);

            // Don't slice until the return object has been created.
            _data = _data.Slice(bytesRead);

            return oid;
        }

        private static void ReadSubIdentifier(
            ReadOnlySpan<byte> source,
            out int bytesRead,
            out long? smallValue,
            out BigInteger? largeValue)
        {
            Debug.Assert(source.Length > 0);

            // T-REC-X.690-201508 sec 8.19.2 (last sentence)
            if (source[0] == 0x80)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            // First, see how long the segment is
            int end = -1;
            int idx;

            for (idx = 0; idx < source.Length; idx++)
            {
                // If the high bit isn't set this marks the end of the sub-identifier.
                bool endOfIdentifier = (source[idx] & 0x80) == 0;

                if (endOfIdentifier)
                {
                    end = idx;
                    break;
                }
            }

            if (end < 0)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            bytesRead = end + 1;
            long accum = 0;

            // Fast path, 9 or fewer bytes => fits in a signed long.
            // (7 semantic bits per byte * 9 bytes = 63 bits, which leaves the sign bit alone)
            if (bytesRead <= 9)
            {
                for (idx = 0; idx < bytesRead; idx++)
                {
                    byte cur = source[idx];
                    accum <<= 7;
                    accum |= (byte)(cur & 0x7F);
                }

                largeValue = null;
                smallValue = accum;
                return;
            }

            // Slow path, needs temporary storage.

            const int SemanticByteCount = 7;
            const int ContentByteCount = 8;

            // Every 8 content bytes turns into 7 integer bytes, so scale the count appropriately.
            // Add one while we're shrunk to account for the needed padding byte or the len%8 discarded bytes.
            int bytesRequired = ((bytesRead / ContentByteCount) + 1) * SemanticByteCount;
            byte[] tmpBytes = ArrayPool<byte>.Shared.Rent(bytesRequired);
            // Ensure all the bytes are zeroed out for BigInteger's parsing.
            Array.Clear(tmpBytes, 0, tmpBytes.Length);

            Span<byte> writeSpan = tmpBytes;
            Span<byte> accumValueBytes = stackalloc byte[sizeof(long)];
            int nextStop = bytesRead;
            idx = bytesRead - ContentByteCount;

            while (nextStop > 0)
            {
                byte cur = source[idx];

                accum <<= 7;
                accum |= (byte)(cur & 0x7F);

                idx++;

                if (idx >= nextStop)
                {
                    Debug.Assert(idx == nextStop);
                    Debug.Assert(writeSpan.Length >= SemanticByteCount);

                    BinaryPrimitives.WriteInt64LittleEndian(accumValueBytes, accum);
                    Debug.Assert(accumValueBytes[7] == 0);
                    accumValueBytes.Slice(0, SemanticByteCount).CopyTo(writeSpan);
                    writeSpan = writeSpan.Slice(SemanticByteCount);

                    accum = 0;
                    nextStop -= ContentByteCount;
                    idx = Math.Max(0, nextStop - ContentByteCount);
                }
            }

            int bytesWritten = tmpBytes.Length - writeSpan.Length;

            // Verify our bytesRequired calculation. There should be at most 7 padding bytes.
            // If the length % 8 is 7 we'll have 0 padding bytes, but the sign bit is still clear.
            //
            // 8 content bytes had a sign bit problem, so we gave it a second 7-byte block, 7 remain.
            // 7 content bytes got a single block but used and wrote 7 bytes, but only 49 of the 56 bits.
            // 6 content bytes have a padding count of 1.
            // 1 content byte has a padding count of 6.
            // 0 content bytes is illegal, but see 8 for the cycle.
            int paddingByteCount = bytesRequired - bytesWritten;
            Debug.Assert(paddingByteCount >= 0 && paddingByteCount < sizeof(long));

            largeValue = new BigInteger(tmpBytes);
            smallValue = null;

            Array.Clear(tmpBytes, 0, bytesWritten);
            ArrayPool<byte>.Shared.Return(tmpBytes);
        }

        private string ReadObjectIdentifierAsString(Asn1Tag expectedTag, out int totalBytesRead)
        {
            Asn1Tag tag = ReadTagAndLength(out int? length, out int headerLength);
            CheckExpectedTag(tag, expectedTag, UniversalTagNumber.ObjectIdentifier);

            // T-REC-X.690-201508 sec 8.19.1
            // T-REC-X.690-201508 sec 8.19.2 says the minimum length is 1
            if (tag.IsConstructed || length < 1)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            ReadOnlyMemory<byte> contentsMemory = Slice(_data, headerLength, length.Value);
            ReadOnlySpan<byte> contents = contentsMemory.Span;

            // Each byte can contribute a 3 digit value and a '.' (e.g. "126."), but usually
            // they convey one digit and a separator.
            //
            // The OID with the most arcs which were found after a 30 minute search is
            // "1.3.6.1.4.1.311.60.2.1.1" (EV cert jurisdiction of incorporation - locality)
            // which has 11 arcs.
            // The longest "known" segment is 16 bytes, a UUID-as-an-arc value.
            // 16 * 11 = 176 bytes for an "extremely long" OID.
            //
            // So pre-allocate the StringBuilder with at most 1020 characters, an input longer than
            // 255 encoded bytes will just have to re-allocate.
            StringBuilder builder = new StringBuilder(((byte)contents.Length) * 4);

            ReadSubIdentifier(contents, out int bytesRead, out long? smallValue, out BigInteger? largeValue);

            // T-REC-X.690-201508 sec 8.19.4
            // The first two subidentifiers (X.Y) are encoded as (X * 40) + Y, because Y is
            // bounded [0, 39] for X in {0, 1}, and only X in {0, 1, 2} are legal.
            // So:
            // * identifier < 40 => X = 0, Y = identifier.
            // * identifier < 80 => X = 1, Y = identifier - 40.
            // * else: X = 2, Y = identifier - 80.
            byte firstArc;

            if (smallValue != null)
            {
                long firstIdentifier = smallValue.Value;

                if (firstIdentifier < 40)
                {
                    firstArc = 0;
                }
                else if (firstIdentifier < 80)
                {
                    firstArc = 1;
                    firstIdentifier -= 40;
                }
                else
                {
                    firstArc = 2;
                    firstIdentifier -= 80;
                }

                builder.Append(firstArc);
                builder.Append('.');
                builder.Append(firstIdentifier);
            }
            else
            {
                Debug.Assert(largeValue != null);
                BigInteger firstIdentifier = largeValue.Value;

                // We're only here because we were bigger than long.MaxValue, so
                // we're definitely on arc 2.
                Debug.Assert(firstIdentifier > long.MaxValue);

                firstArc = 2;
                firstIdentifier -= 80;

                builder.Append(firstArc);
                builder.Append('.');
                builder.Append(firstIdentifier.ToString());
            }

            contents = contents.Slice(bytesRead);

            while (!contents.IsEmpty)
            {
                ReadSubIdentifier(contents, out bytesRead, out smallValue, out largeValue);
                // Exactly one should be non-null.
                Debug.Assert((smallValue == null) != (largeValue == null));

                builder.Append('.');

                if (smallValue != null)
                {
                    builder.Append(smallValue.Value);
                }
                else
                {
                    builder.Append(largeValue.Value.ToString());
                }

                contents = contents.Slice(bytesRead);
            }

            totalBytesRead = headerLength + length.Value;
            return builder.ToString();
        }
    }
}
