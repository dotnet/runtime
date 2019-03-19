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
        ///   Reads the next value as a NamedBitList with tag UNIVERSAL 3, converting it to the
        ///   [<see cref="FlagsAttribute"/>] enum specfied by <typeparamref name="TFlagsEnum"/>.
        /// </summary>
        /// <typeparam name="TFlagsEnum">Destination enum type</typeparam>
        /// <returns>
        ///   the NamedBitList value converted to a <typeparamref name="TFlagsEnum"/>.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules --OR--
        ///   the encoded value is too big to fit in a <typeparamref name="TFlagsEnum"/> value
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <typeparamref name="TFlagsEnum"/> is not an enum type --OR--
        ///   <typeparamref name="TFlagsEnum"/> was not declared with <see cref="FlagsAttribute"/>
        /// </exception>
        /// <seealso cref="ReadNamedBitListValue{TFlagsEnum}(Asn1Tag)"/>
        public TFlagsEnum ReadNamedBitListValue<TFlagsEnum>() where TFlagsEnum : struct =>
            ReadNamedBitListValue<TFlagsEnum>(Asn1Tag.PrimitiveBitString);

        /// <summary>
        ///   Reads the next value as a NamedBitList with tag UNIVERSAL 3, converting it to the
        ///   [<see cref="FlagsAttribute"/>] enum specfied by <typeparamref name="TFlagsEnum"/>.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <typeparam name="TFlagsEnum">Destination enum type</typeparam>
        /// <returns>
        ///   the NamedBitList value converted to a <typeparamref name="TFlagsEnum"/>.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules --OR--
        ///   the encoded value is too big to fit in a <typeparamref name="TFlagsEnum"/> value
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <typeparamref name="TFlagsEnum"/> is not an enum type --OR--
        ///   <typeparamref name="TFlagsEnum"/> was not declared with <see cref="FlagsAttribute"/>
        ///   --OR--
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
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
        public TFlagsEnum ReadNamedBitListValue<TFlagsEnum>(Asn1Tag expectedTag) where TFlagsEnum : struct
        {
            Type tFlagsEnum = typeof(TFlagsEnum);

            return (TFlagsEnum)Enum.ToObject(tFlagsEnum, ReadNamedBitListValue(expectedTag, tFlagsEnum));
        }

        /// <summary>
        ///   Reads the next value as a NamedBitList with tag UNIVERSAL 3, converting it to the
        ///   [<see cref="FlagsAttribute"/>] enum specfied by <paramref name="tFlagsEnum"/>.
        /// </summary>
        /// <param name="tFlagsEnum">Type object representing the destination type.</param>
        /// <returns>
        ///   the NamedBitList value converted to a <paramref name="tFlagsEnum"/>.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules --OR--
        ///   the encoded value is too big to fit in a <paramref name="tFlagsEnum"/> value
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tFlagsEnum"/> is not an enum type --OR--
        ///   <paramref name="tFlagsEnum"/> was not declared with <see cref="FlagsAttribute"/>
        /// </exception>
        /// <seealso cref="ReadNamedBitListValue{TFlagsEnum}(Asn1Tag)"/>
        public Enum ReadNamedBitListValue(Type tFlagsEnum) =>
            ReadNamedBitListValue(Asn1Tag.PrimitiveBitString, tFlagsEnum);

        /// <summary>
        ///   Reads the next value as a NamedBitList with tag UNIVERSAL 3, converting it to the
        ///   [<see cref="FlagsAttribute"/>] enum specfied by <paramref name="tFlagsEnum"/>.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="tFlagsEnum">Type object representing the destination type.</param>
        /// <returns>
        ///   the NamedBitList value converted to a <paramref name="tFlagsEnum"/>.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules --OR---
        ///   the encoded value is too big to fit in a <paramref name="tFlagsEnum"/> value
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tFlagsEnum"/> is not an enum type --OR--
        ///   <paramref name="tFlagsEnum"/> was not declared with <see cref="FlagsAttribute"/>
        ///   --OR--
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        /// <seealso cref="ReadNamedBitListValue{TFlagsEnum}(Asn1Tag)"/>
        public Enum ReadNamedBitListValue(Asn1Tag expectedTag, Type tFlagsEnum)
        {
            // This will throw an ArgumentException if TEnum isn't an enum type,
            // so we don't need to validate it.
            Type backingType = tFlagsEnum.GetEnumUnderlyingType();

            if (!tFlagsEnum.IsDefined(typeof(FlagsAttribute), false))
            {
                throw new ArgumentException(
                    SR.Cryptography_Asn_NamedBitListRequiresFlagsEnum,
                    nameof(tFlagsEnum));
            }

            int sizeLimit = Marshal.SizeOf(backingType);
            Span<byte> stackSpan = stackalloc byte[sizeLimit];
            ReadOnlyMemory<byte> saveData = _data;

            // If TryCopyBitStringBytes succeeds but anything else fails _data will have moved,
            // so if anything throws here just move _data back to what it was.
            try
            {
                if (!TryCopyBitStringBytes(expectedTag, stackSpan, out int unusedBitCount, out int bytesWritten))
                {
                    throw new CryptographicException(
                        SR.Format(SR.Cryptography_Asn_NamedBitListValueTooBig, tFlagsEnum.Name));
                }

                if (bytesWritten == 0)
                {
                    // The mode isn't relevant, zero is always zero.
                    return (Enum)Enum.ToObject(tFlagsEnum, 0);
                }

                ReadOnlySpan<byte> valueSpan = stackSpan.Slice(0, bytesWritten);

                // Now that the 0-bounds check is out of the way:
                // 
                // T-REC-X.690-201508 sec 11.2.2
                if (RuleSet == AsnEncodingRules.DER ||
                    RuleSet == AsnEncodingRules.CER)
                {
                    byte lastByte = valueSpan[bytesWritten - 1];

                    // No unused bits tests 0x01, 1 is 0x02, 2 is 0x04, etc.
                    // We already know that TryCopyBitStringBytes checked that the
                    // declared unused bits were 0, this checks that the last "used" bit
                    // isn't also zero.
                    byte testBit = (byte)(1 << unusedBitCount);

                    if ((lastByte & testBit) == 0)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
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
                return (Enum)Enum.ToObject(tFlagsEnum, InterpretNamedBitListReversed(valueSpan));
            }
            catch
            {
                _data = saveData;
                throw;
            }
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
    }
}
