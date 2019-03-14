// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Asn1
{
    internal partial class AsnReader
    {
        /// <summary>
        ///   Reads the next value as an Enumerated value with tag UNIVERSAL 10,
        ///   returning the contents as a <see cref="ReadOnlyMemory{T}"/> over the original data.
        /// </summary>
        /// <returns>
        ///   The bytes of the Enumerated value, in signed big-endian form.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <seealso cref="ReadEnumeratedValue{TEnum}()"/>
        public ReadOnlyMemory<byte> ReadEnumeratedBytes() =>
            ReadEnumeratedBytes(Asn1Tag.Enumerated);

        /// <summary>
        ///   Reads the next value as a Enumerated with a specified tag, returning the contents
        ///   as a <see cref="ReadOnlyMemory{T}"/> over the original data.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <returns>
        ///   The bytes of the Enumerated value, in signed big-endian form.
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
        /// <seealso cref="ReadEnumeratedValue{TEnum}(Asn1Tag)"/>
        public ReadOnlyMemory<byte> ReadEnumeratedBytes(Asn1Tag expectedTag)
        {
            // T-REC-X.690-201508 sec 8.4 says the contents are the same as for integers.
            ReadOnlyMemory<byte> contents =
                GetIntegerContents(expectedTag, UniversalTagNumber.Enumerated, out int headerLength);

            _data = _data.Slice(headerLength + contents.Length);
            return contents;
        }

        /// <summary>
        ///   Reads the next value as an Enumerated value with tag UNIVERSAL 10, converting it to
        ///   the non-[<see cref="FlagsAttribute"/>] enum specfied by <typeparamref name="TEnum"/>.
        /// </summary>
        /// <typeparam name="TEnum">Destination enum type</typeparam>
        /// <returns>
        ///   the Enumerated value converted to a <typeparamref name="TEnum"/>.
        /// </returns>
        /// <remarks>
        ///   This method does not validate that the return value is defined within
        ///   <typeparamref name="TEnum"/>.
        /// </remarks>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules --OR--
        ///   the encoded value is too big to fit in a <typeparamref name="TEnum"/> value
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <typeparamref name="TEnum"/> is not an enum type --OR--
        ///   <typeparamref name="TEnum"/> was declared with <see cref="FlagsAttribute"/>
        /// </exception>
        /// <seealso cref="ReadEnumeratedValue{TEnum}(Asn1Tag)"/>
        public TEnum ReadEnumeratedValue<TEnum>() where TEnum : struct
        {
            Type tEnum = typeof(TEnum);

            return (TEnum)Enum.ToObject(tEnum, ReadEnumeratedValue(tEnum));
        }

        /// <summary>
        ///   Reads the next value as an Enumerated with tag UNIVERSAL 10, converting it to the
        ///   non-[<see cref="FlagsAttribute"/>] enum specfied by <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <typeparam name="TEnum">Destination enum type</typeparam>
        /// <returns>
        ///   the Enumerated value converted to a <typeparamref name="TEnum"/>.
        /// </returns>
        /// <remarks>
        ///   This method does not validate that the return value is defined within
        ///   <typeparamref name="TEnum"/>.
        /// </remarks>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules --OR--
        ///   the encoded value is too big to fit in a <typeparamref name="TEnum"/> value
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <typeparamref name="TEnum"/> is not an enum type --OR--
        ///   <typeparamref name="TEnum"/> was declared with <see cref="FlagsAttribute"/>
        ///   --OR--
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        public TEnum ReadEnumeratedValue<TEnum>(Asn1Tag expectedTag) where TEnum : struct
        {
            Type tEnum = typeof(TEnum);

            return (TEnum)Enum.ToObject(tEnum, ReadEnumeratedValue(expectedTag, tEnum));
        }

        /// <summary>
        ///   Reads the next value as an Enumerated value with tag UNIVERSAL 10, converting it to
        ///   the non-[<see cref="FlagsAttribute"/>] enum specfied by <paramref name="tEnum"/>.
        /// </summary>
        /// <param name="tEnum">Type object representing the destination type.</param>
        /// <returns>
        ///   the Enumerated value converted to a <paramref name="tEnum"/>.
        /// </returns>
        /// <remarks>
        ///   This method does not validate that the return value is defined within
        ///   <paramref name="tEnum"/>.
        /// </remarks>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules --OR--
        ///   the encoded value is too big to fit in a <paramref name="tEnum"/> value
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tEnum"/> is not an enum type --OR--
        ///   <paramref name="tEnum"/> was declared with <see cref="FlagsAttribute"/>
        /// </exception>
        /// <seealso cref="ReadEnumeratedValue(Asn1Tag, Type)"/>
        public Enum ReadEnumeratedValue(Type tEnum) =>
            ReadEnumeratedValue(Asn1Tag.Enumerated, tEnum);

        /// <summary>
        ///   Reads the next value as an Enumerated with tag UNIVERSAL 10, converting it to the
        ///   non-[<see cref="FlagsAttribute"/>] enum specfied by <paramref name="tEnum"/>.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="tEnum">Type object representing the destination type.</param>
        /// <returns>
        ///   the Enumerated value converted to a <paramref name="tEnum"/>.
        /// </returns>
        /// <remarks>
        ///   This method does not validate that the return value is defined within
        ///   <paramref name="tEnum"/>.
        /// </remarks>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules --OR--
        ///   the encoded value is too big to fit in a <paramref name="tEnum"/> value
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tEnum"/> is not an enum type --OR--
        ///   <paramref name="tEnum"/> was declared with <see cref="FlagsAttribute"/>
        ///   --OR--
        ///   <paramref name="tEnum"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tEnum"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        public Enum ReadEnumeratedValue(Asn1Tag expectedTag, Type tEnum)
        {
            const UniversalTagNumber tagNumber = UniversalTagNumber.Enumerated;

            // This will throw an ArgumentException if TEnum isn't an enum type,
            // so we don't need to validate it.
            Type backingType = tEnum.GetEnumUnderlyingType();

            if (tEnum.IsDefined(typeof(FlagsAttribute), false))
            {
                throw new ArgumentException(
                    SR.Cryptography_Asn_EnumeratedValueRequiresNonFlagsEnum,
                    nameof(tEnum));
            }

            // T-REC-X.690-201508 sec 8.4 says the contents are the same as for integers.
            int sizeLimit = Marshal.SizeOf(backingType);

            if (backingType == typeof(int) ||
                backingType == typeof(long) ||
                backingType == typeof(short) ||
                backingType == typeof(sbyte))
            {
                if (!TryReadSignedInteger(sizeLimit, expectedTag, tagNumber, out long value))
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                return (Enum)Enum.ToObject(tEnum, value);
            }

            if (backingType == typeof(uint) ||
                backingType == typeof(ulong) ||
                backingType == typeof(ushort) ||
                backingType == typeof(byte))
            {
                if (!TryReadUnsignedInteger(sizeLimit, expectedTag, tagNumber, out ulong value))
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                return (Enum)Enum.ToObject(tEnum, value);
            }

            Debug.Fail($"No handler for type {backingType.Name}");
            throw new CryptographicException();
        }
    }
}
