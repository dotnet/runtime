// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace System.Formats.Asn1
{
    public static partial class AsnDecoder
    {
        /// <summary>
        ///   Reads a NamedBitList from <paramref name="source"/> with a specified tag under
        ///   the specified encoding rules, converting it to the
        ///   [<see cref="FlagsAttribute"/>] enum specified by <typeparamref name="TFlagsEnum"/>.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 3).
        /// </param>
        /// <typeparam name="TFlagsEnum">Destination enum type</typeparam>
        /// <returns>
        ///   The NamedBitList value converted to a <typeparamref name="TFlagsEnum"/>.
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
        ///
        ///   -or-
        ///
        ///   the encoded value is too big to fit in a <typeparamref name="TFlagsEnum"/> value.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <typeparamref name="TFlagsEnum"/> is not an enum type.
        ///
        ///   -or-
        ///
        ///   <typeparamref name="TFlagsEnum"/> was not declared with <see cref="FlagsAttribute"/>
        ///
        ///   -or-
        ///
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        /// <remarks>
        ///   The bit alignment performed by this method is to interpret the most significant bit
        ///   in the first byte of the value as the least significant bit in <typeparamref name="TFlagsEnum"/>,
        ///   with bits increasing in value until the least significant bit of the first byte, proceeding
        ///   with the most significant bit of the second byte, and so on. Under this scheme, the following
        ///   ASN.1 type declaration and C# enumeration can be used together:
        ///
        ///   <code>
        ///     KeyUsage ::= BIT STRING {
        ///       digitalSignature   (0),
        ///       nonRepudiation     (1),
        ///       keyEncipherment    (2),
        ///       dataEncipherment   (3),
        ///       keyAgreement       (4),
        ///       keyCertSign        (5),
        ///       cRLSign            (6),
        ///       encipherOnly       (7),
        ///       decipherOnly       (8) }
        ///   </code>
        ///
        ///   <code>
        ///     [Flags]
        ///     enum KeyUsage
        ///     {
        ///         None              = 0,
        ///         DigitalSignature  = 1 &lt;&lt; (0),
        ///         NonRepudiation    = 1 &lt;&lt; (1),
        ///         KeyEncipherment   = 1 &lt;&lt; (2),
        ///         DataEncipherment  = 1 &lt;&lt; (3),
        ///         KeyAgreement      = 1 &lt;&lt; (4),
        ///         KeyCertSign       = 1 &lt;&lt; (5),
        ///         CrlSign           = 1 &lt;&lt; (6),
        ///         EncipherOnly      = 1 &lt;&lt; (7),
        ///         DecipherOnly      = 1 &lt;&lt; (8),
        ///     }
        ///   </code>
        ///
        ///   Note that while the example here uses the KeyUsage NamedBitList from
        ///   <a href="https://tools.ietf.org/html/rfc3280#section-4.2.1.3">RFC 3280 (4.2.1.3)</a>,
        ///   the example enum uses values thar are different from
        ///   System.Security.Cryptography.X509Certificates.X509KeyUsageFlags.
        /// </remarks>
        public static TFlagsEnum ReadNamedBitListValue<TFlagsEnum>(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out int bytesConsumed,
            Asn1Tag? expectedTag = null)
            where TFlagsEnum : Enum
        {
            Type tFlagsEnum = typeof(TFlagsEnum);

            TFlagsEnum ret = (TFlagsEnum)Enum.ToObject(
                tFlagsEnum,
                ReadNamedBitListValue(source, ruleSet, tFlagsEnum, out int consumed, expectedTag));

            // Now that there's nothing left to throw, assign bytesConsumed.
            bytesConsumed = consumed;
            return ret;
        }

        /// <summary>
        ///   Reads a NamedBitList from <paramref name="source"/> with a specified tag under
        ///   the specified encoding rules, converting it to the
        ///   [<see cref="FlagsAttribute"/>] enum specified by <paramref name="flagsEnumType"/>.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="flagsEnumType">Type object representing the destination type.</param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 3).
        /// </param>
        /// <returns>
        ///   The NamedBitList value converted to a <paramref name="flagsEnumType"/>.
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
        ///
        ///   -or-
        ///-
        ///   the encoded value is too big to fit in a <paramref name="flagsEnumType"/> value.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="flagsEnumType"/> is not an enum type.
        ///
        ///   -or-
        ///
        ///   <paramref name="flagsEnumType"/> was not declared with <see cref="FlagsAttribute"/>
        ///
        ///   -or-
        ///
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="flagsEnumType"/> is <see langword="null" />
        /// </exception>
        /// <seealso cref="ReadNamedBitListValue{TFlagsEnum}"/>
        public static Enum ReadNamedBitListValue(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            Type flagsEnumType,
            out int bytesConsumed,
            Asn1Tag? expectedTag = null)
        {
            if (flagsEnumType == null)
                throw new ArgumentNullException(nameof(flagsEnumType));

            // This will throw an ArgumentException if TEnum isn't an enum type,
            // so we don't need to validate it.
            Type backingType = flagsEnumType.GetEnumUnderlyingType();

            if (!flagsEnumType.IsDefined(typeof(FlagsAttribute), false))
            {
                throw new ArgumentException(
                    SR.Argument_NamedBitListRequiresFlagsEnum,
                    nameof(flagsEnumType));
            }

            Span<byte> stackSpan = stackalloc byte[sizeof(ulong)];
            int sizeLimit = GetPrimitiveIntegerSize(backingType);
            stackSpan = stackSpan.Slice(0, sizeLimit);

            bool read = TryReadBitString(
                source,
                stackSpan,
                ruleSet,
                out int unusedBitCount,
                out int consumed,
                out int bytesWritten,
                expectedTag);

            if (!read)
            {
                throw new AsnContentException(
                    SR.Format(SR.ContentException_NamedBitListValueTooBig, flagsEnumType.Name));
            }

            Enum ret;

            if (bytesWritten == 0)
            {
                // The mode isn't relevant, zero is always zero.
                ret = (Enum)Enum.ToObject(flagsEnumType, 0);
                bytesConsumed = consumed;
                return ret;
            }

            ReadOnlySpan<byte> valueSpan = stackSpan.Slice(0, bytesWritten);

            // Now that the 0-bounds check is out of the way:
            //
            // T-REC-X.690-201508 sec 11.2.2
            if (ruleSet == AsnEncodingRules.DER ||
                ruleSet == AsnEncodingRules.CER)
            {
                byte lastByte = valueSpan[bytesWritten - 1];

                // No unused bits tests 0x01, 1 is 0x02, 2 is 0x04, etc.
                // We already know that TryCopyBitStringBytes checked that the
                // declared unused bits were 0, this checks that the last "used" bit
                // isn't also zero.
                byte testBit = (byte)(1 << unusedBitCount);

                if ((lastByte & testBit) == 0)
                {
                    throw new AsnContentException(SR.ContentException_InvalidUnderCerOrDer_TryBer);
                }
            }

            // Consider a NamedBitList defined as
            //
            //   SomeList ::= BIT STRING {
            //     a(0), b(1), c(2), d(3), e(4), f(5), g(6), h(7), i(8), j(9), k(10)
            //   }
            //
            // The BIT STRING encoding of (a | j) is
            //   unusedBitCount = 6,
            //   contents: 0x80 0x40  (0b10000000_01000000)
            //
            // A the C# exposure of this structure we adhere to is
            //
            // [Flags]
            // enum SomeList
            // {
            //     A = 1,
            //     B = 1 << 1,
            //     C = 1 << 2,
            //     ...
            // }
            //
            // Which happens to be exactly backwards from how the bits are encoded, but the complexity
            // only needs to live here.
            ret = (Enum)Enum.ToObject(flagsEnumType, InterpretNamedBitListReversed(valueSpan));
            bytesConsumed = consumed;
            return ret;
        }

        /// <summary>
        ///   Reads a NamedBitList from <paramref name="source"/> with a specified tag under
        ///   the specified encoding rules.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 3).
        /// </param>
        /// <returns>
        ///   The bits from the encoded value.
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
        /// <remarks>
        ///   The bit alignment performed by this method is to interpret the most significant bit
        ///   in the first byte of the value as bit 0,
        ///   with bits increasing in value until the least significant bit of the first byte, proceeding
        ///   with the most significant bit of the second byte, and so on.
        ///   This means that the number used in an ASN.1 NamedBitList construction is the index in the
        ///   return value.
        /// </remarks>
        public static BitArray ReadNamedBitList(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out int bytesConsumed,
            Asn1Tag? expectedTag = null)
        {
            Asn1Tag actualTag = ReadEncodedValue(source, ruleSet, out _, out int contentLength, out _);

            // Get the last ArgumentException out of the way before we rent arrays
            if (expectedTag != null)
            {
                CheckExpectedTag(actualTag, expectedTag.Value, UniversalTagNumber.BitString);
            }

            // The number of interpreted bytes is at most contentLength - 1, just ask for contentLength.
            byte[] rented = CryptoPool.Rent(contentLength);

            if (!TryReadBitString(
                source,
                rented,
                ruleSet,
                out int unusedBitCount,
                out int consumed,
                out int written,
                expectedTag))
            {
                Debug.Fail("TryReadBitString failed with an over-allocated buffer");
                throw new InvalidOperationException();
            }

            int validBitCount = checked(written * 8 - unusedBitCount);

            Span<byte> valueSpan = rented.AsSpan(0, written);
            ReverseBitsPerByte(valueSpan);

            BitArray ret = new BitArray(rented);
            CryptoPool.Return(rented, written);

            // Trim off all of the unnecessary parts.
            ret.Length = validBitCount;

            bytesConsumed = consumed;
            return ret;
        }

        private static long InterpretNamedBitListReversed(ReadOnlySpan<byte> valueSpan)
        {
            Debug.Assert(valueSpan.Length <= sizeof(long));

            long accum = 0;
            long currentBitValue = 1;

            for (int byteIdx = 0; byteIdx < valueSpan.Length; byteIdx++)
            {
                byte byteVal = valueSpan[byteIdx];

                for (int bitIndex = 7; bitIndex >= 0; bitIndex--)
                {
                    int test = 1 << bitIndex;

                    if ((byteVal & test) != 0)
                    {
                        accum |= currentBitValue;
                    }

                    currentBitValue <<= 1;
                }
            }

            return accum;
        }

        internal static void ReverseBitsPerByte(Span<byte> value)
        {
            for (int byteIdx = 0; byteIdx < value.Length; byteIdx++)
            {
                byte cur = value[byteIdx];
                byte mask = 0b1000_0000;
                byte next = 0;

                for (; cur != 0; cur >>= 1, mask >>= 1)
                {
                    next |= (byte)((cur & 1) * mask);
                }

                value[byteIdx] = next;
            }
        }
    }

    public partial class AsnReader
    {
        /// <summary>
        ///   Reads the next value as a NamedBitList with a specified tag, converting it to the
        ///   [<see cref="FlagsAttribute"/>] enum specified by <typeparamref name="TFlagsEnum"/>.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <typeparam name="TFlagsEnum">Destination enum type</typeparam>
        /// <returns>
        ///   The NamedBitList value converted to a <typeparamref name="TFlagsEnum"/>.
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
        ///
        ///   -or-
        ///
        ///   the encoded value is too big to fit in a <typeparamref name="TFlagsEnum"/> value.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <typeparamref name="TFlagsEnum"/> is not an enum type.
        ///
        ///   -or-
        ///
        ///   <typeparamref name="TFlagsEnum"/> was not declared with <see cref="FlagsAttribute"/>
        ///
        ///   -or-
        ///
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        /// <remarks>
        ///   The bit alignment performed by this method is to interpret the most significant bit
        ///   in the first byte of the value as the least significant bit in <typeparamref name="TFlagsEnum"/>,
        ///   with bits increasing in value until the least significant bit of the first byte, proceeding
        ///   with the most significant bit of the second byte, and so on. Under this scheme, the following
        ///   ASN.1 type declaration and C# enumeration can be used together:
        ///
        ///   <code>
        ///     KeyUsage ::= BIT STRING {
        ///       digitalSignature   (0),
        ///       nonRepudiation     (1),
        ///       keyEncipherment    (2),
        ///       dataEncipherment   (3),
        ///       keyAgreement       (4),
        ///       keyCertSign        (5),
        ///       cRLSign            (6),
        ///       encipherOnly       (7),
        ///       decipherOnly       (8) }
        ///   </code>
        ///
        ///   <code>
        ///     [Flags]
        ///     enum KeyUsage
        ///     {
        ///         None              = 0,
        ///         DigitalSignature  = 1 &lt;&lt; (0),
        ///         NonRepudiation    = 1 &lt;&lt; (1),
        ///         KeyEncipherment   = 1 &lt;&lt; (2),
        ///         DataEncipherment  = 1 &lt;&lt; (3),
        ///         KeyAgreement      = 1 &lt;&lt; (4),
        ///         KeyCertSign       = 1 &lt;&lt; (5),
        ///         CrlSign           = 1 &lt;&lt; (6),
        ///         EncipherOnly      = 1 &lt;&lt; (7),
        ///         DecipherOnly      = 1 &lt;&lt; (8),
        ///     }
        ///   </code>
        ///
        ///   Note that while the example here uses the KeyUsage NamedBitList from
        ///   <a href="https://tools.ietf.org/html/rfc3280#section-4.2.1.3">RFC 3280 (4.2.1.3)</a>,
        ///   the example enum uses values thar are different from
        ///   System.Security.Cryptography.X509Certificates.X509KeyUsageFlags.
        /// </remarks>
        public TFlagsEnum ReadNamedBitListValue<TFlagsEnum>(Asn1Tag? expectedTag = null) where TFlagsEnum : Enum
        {
            TFlagsEnum ret = AsnDecoder.ReadNamedBitListValue<TFlagsEnum>(
                _data.Span,
                RuleSet,
                out int consumed,
                expectedTag);

            _data = _data.Slice(consumed);
            return ret;
        }

        /// <summary>
        ///   Reads the next value as a NamedBitList with a specified tag, converting it to the
        ///   [<see cref="FlagsAttribute"/>] enum specified by <paramref name="flagsEnumType"/>.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="flagsEnumType">Type object representing the destination type.</param>
        /// <returns>
        ///   The NamedBitList value converted to a <paramref name="flagsEnumType"/>.
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
        ///
        ///   -or-
        ///
        ///   the encoded value is too big to fit in a <paramref name="flagsEnumType"/> value.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="flagsEnumType"/> is not an enum type.
        ///
        ///   -or-
        ///
        ///   <paramref name="flagsEnumType"/> was not declared with <see cref="FlagsAttribute"/>
        ///
        ///   -or-
        ///
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="flagsEnumType"/> is <see langword="null" />
        /// </exception>
        /// <seealso cref="ReadNamedBitListValue{TFlagsEnum}"/>
        public Enum ReadNamedBitListValue(Type flagsEnumType, Asn1Tag? expectedTag = null)
        {
            Enum ret = AsnDecoder.ReadNamedBitListValue(
                _data.Span,
                RuleSet,
                flagsEnumType,
                out int consumed,
                expectedTag);

            _data = _data.Slice(consumed);
            return ret;
        }

        /// <summary>
        ///   Reads the next value as a NamedBitList with a specified tag.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <returns>
        ///   The bits from the encoded value.
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
        /// <seealso cref="ReadNamedBitListValue{TFlagsEnum}"/>
        public BitArray ReadNamedBitList(Asn1Tag? expectedTag = null)
        {
            BitArray ret = AsnDecoder.ReadNamedBitList(_data.Span, RuleSet, out int consumed, expectedTag);
            _data = _data.Slice(consumed);
            return ret;
        }
    }
}
