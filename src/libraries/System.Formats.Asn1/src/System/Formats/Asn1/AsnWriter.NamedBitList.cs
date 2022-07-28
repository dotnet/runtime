// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Security.Cryptography;

namespace System.Formats.Asn1
{
    public sealed partial class AsnWriter
    {
        /// <summary>
        ///   Write a [<see cref="FlagsAttribute"/>] enum value as a NamedBitList with
        ///   a specified tag.
        /// </summary>
        /// <param name="value">The boxed enumeration value to write</param>
        /// <param name="tag">The tag to write, or <see langword="null"/> for the default tag (Universal 3).</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        ///
        ///   -or-
        ///
        ///   <paramref name="value"/> is not a boxed enum value.
        ///
        ///   -or-
        ///
        ///   the unboxed type of <paramref name="value"/> is not declared [<see cref="FlagsAttribute"/>].
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public void WriteNamedBitList(Enum value, Asn1Tag? tag = null)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            CheckUniversalTag(tag, UniversalTagNumber.BitString);

            WriteNamedBitList(tag, value.GetType(), value);
        }

        /// <summary>
        ///   Write a [<see cref="FlagsAttribute"/>] enum value as a NamedBitList with
        ///   a specified tag.
        /// </summary>
        /// <param name="value">The enumeration value to write</param>
        /// <param name="tag">The tag to write, or <see langword="null"/> for the default tag (Universal 3).</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        ///
        ///   -or-
        ///
        ///   <typeparamref name="TEnum"/> is not an enum value.
        ///
        ///   -or-
        ///
        ///   <typeparamref name="TEnum"/> is not declared [<see cref="FlagsAttribute"/>].
        /// </exception>
        public void WriteNamedBitList<TEnum>(TEnum value, Asn1Tag? tag = null) where TEnum : Enum
        {
            CheckUniversalTag(tag, UniversalTagNumber.BitString);

            WriteNamedBitList(tag, typeof(TEnum), value);
        }

        /// <summary>
        ///   Write a bit array value as a NamedBitList with a specified tag.
        /// </summary>
        /// <param name="value">The bits to write</param>
        /// <param name="tag">The tag to write, or <see langword="null"/> for the default tag (Universal 3).</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        ///   The index of the bit array corresponds to the bit number in the encoded format, which is
        ///   different than the value produced by <see cref="BitArray.CopyTo"/> with a byte array.
        ///   For example, the bit array <c>{ false, true, true }</c> encodes as <c>0b0110_0000</c> with 5
        ///   unused bits.
        /// </remarks>
        public void WriteNamedBitList(BitArray value, Asn1Tag? tag = null)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            CheckUniversalTag(tag, UniversalTagNumber.BitString);

            WriteBitArray(value, tag);
        }

        private void WriteNamedBitList(Asn1Tag? tag, Type tEnum, Enum value)
        {
            Type backingType = tEnum.GetEnumUnderlyingType();

            if (!tEnum.IsDefined(typeof(FlagsAttribute), false))
            {
                throw new ArgumentException(
                    SR.Argument_NamedBitListRequiresFlagsEnum,
                    nameof(tEnum));
            }

            ulong integralValue;

            if (backingType == typeof(ulong))
            {
                integralValue = Convert.ToUInt64(value);
            }
            else
            {
                // All other types fit in a (signed) long.
                long numericValue = Convert.ToInt64(value);
                integralValue = unchecked((ulong)numericValue);
            }

            WriteNamedBitList(tag, integralValue);
        }

        // T-REC-X.680-201508 sec 22
        // T-REC-X.690-201508 sec 8.6, 11.2.2
        private void WriteNamedBitList(Asn1Tag? tag, ulong integralValue)
        {
            Span<byte> temp = stackalloc byte[sizeof(ulong)];
            // Reset to all zeros, since we're just going to or-in bits we need.
            temp.Clear();

            int indexOfHighestSetBit = -1;

            for (int i = 0; integralValue != 0; integralValue >>= 1, i++)
            {
                if ((integralValue & 1) != 0)
                {
                    temp[i / 8] |= (byte)(0x80 >> (i % 8));
                    indexOfHighestSetBit = i;
                }
            }

            if (indexOfHighestSetBit < 0)
            {
                // No bits were set; this is an empty bit string.
                // T-REC-X.690-201508 sec 11.2.2-note2
                WriteBitString(ReadOnlySpan<byte>.Empty, tag: tag);
            }
            else
            {
                // At least one bit was set.
                // Determine the shortest length necessary to represent the bit string.

                // Since "bit 0" gets written down 0 => 1.
                // Since "bit 8" is in the second byte 8 => 2.
                // That makes the formula ((bit / 8) + 1) instead of ((bit + 7) / 8).
                int byteLen = (indexOfHighestSetBit / 8) + 1;
                int unusedBitCount = 7 - (indexOfHighestSetBit % 8);

                WriteBitString(
                    temp.Slice(0, byteLen),
                    unusedBitCount,
                    tag);
            }
        }

        private void WriteBitArray(BitArray value, Asn1Tag? tag)
        {
            if (value.Count == 0)
            {
                // No bits were set; this is an empty bit string.
                // T-REC-X.690-201508 sec 11.2.2-note2
                WriteBitString(ReadOnlySpan<byte>.Empty, tag: tag);
                return;
            }

            int requiredBytes = checked((value.Count + 7) / 8);
            int unusedBits = requiredBytes * 8 - value.Count;
            byte[] rented = CryptoPool.Rent(requiredBytes);

            // Export the BitArray to a byte array.
            // While bits 0-7 are in the first byte, they are numbered 76543210,
            // but our wire form is 01234567, so we'll need to reverse the bits on each byte.
            value.CopyTo(rented, 0);

            Span<byte> valueSpan = rented.AsSpan(0, requiredBytes);
            AsnDecoder.ReverseBitsPerByte(valueSpan);

            WriteBitString(valueSpan, unusedBits, tag);
            CryptoPool.Return(rented, requiredBytes);
        }
    }
}
