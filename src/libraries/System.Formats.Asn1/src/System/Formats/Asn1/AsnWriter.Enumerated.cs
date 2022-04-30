// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Asn1
{
    public sealed partial class AsnWriter
    {
        /// <summary>
        ///   Write a non-[<see cref="FlagsAttribute"/>] enum value as an Enumerated with
        ///   tag UNIVERSAL 10.
        /// </summary>
        /// <param name="value">The boxed enumeration value to write.</param>
        /// <param name="tag">The tag to write, or <see langword="null"/> for the default tag (Universal 10).</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
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
        ///   the unboxed type of <paramref name="value"/> is declared [<see cref="FlagsAttribute"/>].
        /// </exception>
        /// <seealso cref="WriteEnumeratedValue(Enum,Asn1Tag?)"/>
        /// <seealso cref="WriteEnumeratedValue{T}(T,Asn1Tag?)"/>
        public void WriteEnumeratedValue(Enum value, Asn1Tag? tag = null)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            WriteEnumeratedValue(tag?.AsPrimitive() ?? Asn1Tag.Enumerated, value.GetType(), value);
        }

        /// <summary>
        ///   Write a non-[<see cref="FlagsAttribute"/>] enum value as an Enumerated with
        ///   tag UNIVERSAL 10.
        /// </summary>
        /// <param name="tag">The tag to write.</param>
        /// <param name="value">The boxed enumeration value to write.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        ///
        ///   -or-
        ///
        ///   <typeparamref name="TEnum"/> is not an enum.
        ///
        ///   -or-
        ///
        ///   <typeparamref name="TEnum"/> is declared [<see cref="FlagsAttribute"/>].
        /// </exception>
        /// <seealso cref="WriteEnumeratedValue(Enum,Nullable{Asn1Tag})"/>
        public void WriteEnumeratedValue<TEnum>(TEnum value, Asn1Tag? tag = null) where TEnum : Enum
        {
            WriteEnumeratedValue(tag?.AsPrimitive() ?? Asn1Tag.Enumerated, typeof(TEnum), value);
        }

        // T-REC-X.690-201508 sec 8.4
        private void WriteEnumeratedValue(Asn1Tag tag, Type tEnum, object value)
        {
            CheckUniversalTag(tag, UniversalTagNumber.Enumerated);

            Type backingType = tEnum.GetEnumUnderlyingType();

            if (tEnum.IsDefined(typeof(FlagsAttribute), false))
            {
                throw new ArgumentException(
                    SR.Argument_EnumeratedValueRequiresNonFlagsEnum,
                    nameof(tEnum));
            }

            if (backingType == typeof(ulong))
            {
                ulong numericValue = Convert.ToUInt64(value);
                // T-REC-X.690-201508 sec 8.4
                WriteNonNegativeIntegerCore(tag, numericValue);
            }
            else
            {
                // All other types fit in a (signed) long.
                long numericValue = Convert.ToInt64(value);
                // T-REC-X.690-201508 sec 8.4
                WriteIntegerCore(tag, numericValue);
            }
        }
    }
}
