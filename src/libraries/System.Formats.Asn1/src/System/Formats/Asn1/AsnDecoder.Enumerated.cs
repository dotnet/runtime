// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Formats.Asn1
{
    public static partial class AsnDecoder
    {
        /// <summary>
        ///   Reads an Enumerated value from <paramref name="source"/> with a specified tag under
        ///   the specified encoding rules, returning the contents as a slice of the buffer.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 10).
        /// </param>
        /// <returns>
        ///   The slice of the buffer containing the bytes of the Enumerated value,
        ///   in signed big-endian form.
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
        public static ReadOnlySpan<byte> ReadEnumeratedBytes(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out int bytesConsumed,
            Asn1Tag? expectedTag = null)
        {
            return GetIntegerContents(
                source,
                ruleSet,
                expectedTag ?? Asn1Tag.Enumerated,
                UniversalTagNumber.Enumerated,
                out bytesConsumed);
        }

        /// <summary>
        ///   Reads an Enumerated from <paramref name="source"/> with a specified tag under
        ///   the specified encoding rules, converting it to the
        ///   non-[<see cref="FlagsAttribute"/>] enum specified by <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 10).
        /// </param>
        /// <typeparam name="TEnum">Destination enum type</typeparam>
        /// <returns>
        ///   The Enumerated value converted to a <typeparamref name="TEnum"/>.
        /// </returns>
        /// <remarks>
        ///   This method does not validate that the return value is defined within
        ///   <typeparamref name="TEnum"/>.
        /// </remarks>
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
        ///
        ///   -or-
        ///
        ///   the encoded value is too big to fit in a <typeparamref name="TEnum"/> value.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <typeparamref name="TEnum"/> is not an enum type.
        ///
        ///   -or-
        ///
        ///   <typeparamref name="TEnum"/> was declared with <see cref="FlagsAttribute"/>.
        ///
        ///   -or-
        ///
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        public static TEnum ReadEnumeratedValue<TEnum>(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out int bytesConsumed,
            Asn1Tag? expectedTag = null)
            where TEnum : Enum
        {
            Type tEnum = typeof(TEnum);

            return (TEnum)Enum.ToObject(
                tEnum,
                ReadEnumeratedValue(
                    source,
                    ruleSet,
                    tEnum,
                    out bytesConsumed,
                    expectedTag));
        }

        /// <summary>
        ///   Reads an Enumerated from <paramref name="source"/> with a specified tag under
        ///   the specified encoding rules, converting it to the
        ///   non-[<see cref="FlagsAttribute"/>] enum specified by <paramref name="enumType"/>.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="enumType">Type object representing the destination type.</param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 10).
        /// </param>
        /// <returns>
        ///   The Enumerated value converted to a <paramref name="enumType"/>.
        /// </returns>
        /// <remarks>
        ///   This method does not validate that the return value is defined within
        ///   <paramref name="enumType"/>.
        /// </remarks>
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
        ///
        ///   -or-
        ///
        ///   the encoded value is too big to fit in a <paramref name="enumType"/> value.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="enumType"/> is not an enum type.
        ///
        ///   -or-
        ///
        ///   <paramref name="enumType"/> was declared with <see cref="FlagsAttribute"/>.
        ///
        ///   -or-
        ///
        ///   <paramref name="enumType"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="enumType"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="enumType"/> is <see langword="null" />.
        /// </exception>
        public static Enum ReadEnumeratedValue(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            Type enumType,
            out int bytesConsumed,
            Asn1Tag? expectedTag = null)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));

            const UniversalTagNumber TagNumber = UniversalTagNumber.Enumerated;
            Asn1Tag localTag = expectedTag ?? Asn1Tag.Enumerated;

            // This will throw an ArgumentException if TEnum isn't an enum type,
            // so we don't need to validate it.
            Type backingType = enumType.GetEnumUnderlyingType();

            if (enumType.IsDefined(typeof(FlagsAttribute), false))
            {
                throw new ArgumentException(
                    SR.Argument_EnumeratedValueRequiresNonFlagsEnum,
                    nameof(enumType));
            }

            // T-REC-X.690-201508 sec 8.4 says the contents are the same as for integers.
            int sizeLimit = GetPrimitiveIntegerSize(backingType);

            if (backingType == typeof(int) ||
                backingType == typeof(long) ||
                backingType == typeof(short) ||
                backingType == typeof(sbyte))
            {
                if (!TryReadSignedInteger(
                    source,
                    ruleSet,
                    sizeLimit,
                    localTag,
                    TagNumber,
                    out long value,
                    out int consumed))
                {
                    throw new AsnContentException(SR.ContentException_EnumeratedValueTooBig);
                }

                bytesConsumed = consumed;
                return (Enum)Enum.ToObject(enumType, value);
            }

            if (backingType == typeof(uint) ||
                backingType == typeof(ulong) ||
                backingType == typeof(ushort) ||
                backingType == typeof(byte))
            {
                if (!TryReadUnsignedInteger(
                    source,
                    ruleSet,
                    sizeLimit,
                    localTag,
                    TagNumber,
                    out ulong value,
                    out int consumed))
                {
                    throw new AsnContentException(SR.ContentException_EnumeratedValueTooBig);
                }

                bytesConsumed = consumed;
                return (Enum)Enum.ToObject(enumType, value);
            }

            throw new AsnContentException(
                SR.Format(
                    SR.Argument_EnumeratedValueBackingTypeNotSupported,
                    backingType.FullName));
        }
    }

    public partial class AsnReader
    {
        /// <summary>
        ///   Reads the next value as a Enumerated with a specified tag, returning the contents
        ///   as a <see cref="ReadOnlyMemory{T}"/> over the original data.
        /// </summary>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 10).
        /// </param>
        /// <returns>
        ///   The bytes of the Enumerated value, in signed big-endian form.
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
        /// <seealso cref="ReadEnumeratedValue{TEnum}"/>
        public ReadOnlyMemory<byte> ReadEnumeratedBytes(Asn1Tag? expectedTag = null)
        {
            ReadOnlySpan<byte> bytes =
                AsnDecoder.ReadEnumeratedBytes(_data.Span, RuleSet, out int consumed, expectedTag);

            ReadOnlyMemory<byte> memory = AsnDecoder.Slice(_data, bytes);

            _data = _data.Slice(consumed);
            return memory;
        }

        /// <summary>
        ///   Reads the next value as an Enumerated with a specified tag, converting it to the
        ///   non-[<see cref="FlagsAttribute"/>] enum specified by <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 10).
        /// </param>
        /// <typeparam name="TEnum">Destination enum type</typeparam>
        /// <returns>
        ///   The Enumerated value converted to a <typeparamref name="TEnum"/>.
        /// </returns>
        /// <remarks>
        ///   This method does not validate that the return value is defined within
        ///   <typeparamref name="TEnum"/>.
        /// </remarks>
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
        ///
        ///   -or-
        ///
        ///   the encoded value is too big to fit in a <typeparamref name="TEnum"/> value.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <typeparamref name="TEnum"/> is not an enum type.
        ///
        ///   -or-
        ///
        ///   <typeparamref name="TEnum"/> was declared with <see cref="FlagsAttribute"/>.
        ///
        ///   -or-
        ///
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        public TEnum ReadEnumeratedValue<TEnum>(Asn1Tag? expectedTag = null) where TEnum : Enum
        {
            TEnum ret = AsnDecoder.ReadEnumeratedValue<TEnum>(_data.Span, RuleSet, out int consumed, expectedTag);
            _data = _data.Slice(consumed);
            return ret;
        }

        /// <summary>
        ///   Reads the next value as an Enumerated with a specified tag, converting it to the
        ///   non-[<see cref="FlagsAttribute"/>] enum specified by <paramref name="enumType"/>.
        /// </summary>
        /// <param name="enumType">Type object representing the destination type.</param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 10).
        /// </param>
        /// <returns>
        ///   The Enumerated value converted to a <paramref name="enumType"/>.
        /// </returns>
        /// <remarks>
        ///   This method does not validate that the return value is defined within
        ///   <paramref name="enumType"/>.
        /// </remarks>
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
        ///
        ///   -or-
        ///
        ///   the encoded value is too big to fit in a <paramref name="enumType"/> value.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="enumType"/> is not an enum type.
        ///
        ///   -or-
        ///
        ///   <paramref name="enumType"/> was declared with <see cref="FlagsAttribute"/>.
        ///
        ///   -or-
        ///
        ///   <paramref name="enumType"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="enumType"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="enumType"/> is <see langword="null" />.
        /// </exception>
        public Enum ReadEnumeratedValue(Type enumType, Asn1Tag? expectedTag = null)
        {
            Enum ret = AsnDecoder.ReadEnumeratedValue(_data.Span, RuleSet, enumType, out int consumed, expectedTag);
            _data = _data.Slice(consumed);
            return ret;
        }
    }
}
