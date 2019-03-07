// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Asn1
{
    internal sealed partial class AsnWriter
    {
        /// <summary>
        ///   Write a [<see cref="FlagsAttribute"/>] enum value as a NamedBitList with
        ///   tag UNIVERSAL 3.
        /// </summary>
        /// <param name="enumValue">The boxed enumeration value to write</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="enumValue"/> is <c>null</c>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="enumValue"/> is not a boxed enum value --OR--
        ///   the unboxed type of <paramref name="enumValue"/> is not declared [<see cref="FlagsAttribute"/>]
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        /// <seealso cref="WriteNamedBitList(Asn1Tag,object)"/>
        /// <seealso cref="WriteNamedBitList{T}(T)"/>
        public void WriteNamedBitList(object enumValue)
        {
            if (enumValue == null)
                throw new ArgumentNullException(nameof(enumValue));

            WriteNamedBitList(Asn1Tag.PrimitiveBitString, enumValue);
        }

        /// <summary>
        ///   Write a [<see cref="FlagsAttribute"/>] enum value as a NamedBitList with
        ///   tag UNIVERSAL 3.
        /// </summary>
        /// <param name="enumValue">The enumeration value to write</param>
        /// <exception cref="ArgumentException">
        ///   <typeparamref name="TEnum"/> is not an enum value --OR--
        ///   <typeparamref name="TEnum"/> is not declared [<see cref="FlagsAttribute"/>]
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        /// <seealso cref="WriteNamedBitList{T}(Asn1Tag,T)"/>
        public void WriteNamedBitList<TEnum>(TEnum enumValue) where TEnum : struct
        {
            WriteNamedBitList(Asn1Tag.PrimitiveBitString, enumValue);
        }

        /// <summary>
        ///   Write a [<see cref="FlagsAttribute"/>] enum value as a NamedBitList with
        ///   a specified tag.
        /// </summary>
        /// <param name="tag">The tag to write.</param>
        /// <param name="enumValue">The boxed enumeration value to write</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method --OR--
        ///   <paramref name="enumValue"/> is not a boxed enum value --OR--
        ///   the unboxed type of <paramref name="enumValue"/> is not declared [<see cref="FlagsAttribute"/>]
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="enumValue"/> is <c>null</c>
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        public void WriteNamedBitList(Asn1Tag tag, object enumValue)
        {
            if (enumValue == null)
                throw new ArgumentNullException(nameof(enumValue));

            CheckUniversalTag(tag, UniversalTagNumber.BitString);

            WriteNamedBitList(tag, enumValue.GetType(), enumValue);
        }

        /// <summary>
        ///   Write a [<see cref="FlagsAttribute"/>] enum value as a NamedBitList with
        ///   a specified tag.
        /// </summary>
        /// <param name="tag">The tag to write.</param>
        /// <param name="enumValue">The enumeration value to write</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method --OR--
        ///   <typeparamref name="TEnum"/> is not an enum value --OR--
        ///   <typeparamref name="TEnum"/> is not declared [<see cref="FlagsAttribute"/>]
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        public void WriteNamedBitList<TEnum>(Asn1Tag tag, TEnum enumValue) where TEnum : struct
        {
            CheckUniversalTag(tag, UniversalTagNumber.BitString);

            WriteNamedBitList(tag, typeof(TEnum), enumValue);
        }

        private void WriteNamedBitList(Asn1Tag tag, Type tEnum, object enumValue)
        {
            Type backingType = tEnum.GetEnumUnderlyingType();

            if (!tEnum.IsDefined(typeof(FlagsAttribute), false))
            {
                throw new ArgumentException(
                    SR.Cryptography_Asn_NamedBitListRequiresFlagsEnum,
                    nameof(tEnum));
            }

            ulong integralValue;

            if (backingType == typeof(ulong))
            {
                integralValue = Convert.ToUInt64(enumValue);
            }
            else
            {
                // All other types fit in a (signed) long.
                long numericValue = Convert.ToInt64(enumValue);
                integralValue = unchecked((ulong)numericValue);
            }

            WriteNamedBitList(tag, integralValue);
        }

        // T-REC-X.680-201508 sec 22
        // T-REC-X.690-201508 sec 8.6, 11.2.2
        private void WriteNamedBitList(Asn1Tag tag, ulong integralValue)
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
                WriteBitString(tag, ReadOnlySpan<byte>.Empty);
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
                    tag,
                    temp.Slice(0, byteLen),
                    unusedBitCount);
            }
        }
    }
}
