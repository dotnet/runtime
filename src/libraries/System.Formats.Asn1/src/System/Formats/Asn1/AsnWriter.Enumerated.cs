// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Asn1
{
    internal sealed partial class AsnWriter
    {
        /// <summary>
        ///   Write a non-[<see cref="FlagsAttribute"/>] enum value as an Enumerated with
        ///   tag UNIVERSAL 10.
        /// </summary>
        /// <param name="enumValue">The boxed enumeration value to write</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="enumValue"/> is <c>null</c>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="enumValue"/> is not a boxed enum value --OR--
        ///   the unboxed type of <paramref name="enumValue"/> is declared [<see cref="FlagsAttribute"/>]
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        /// <seealso cref="WriteEnumeratedValue(Asn1Tag,object)"/>
        /// <seealso cref="WriteEnumeratedValue{T}(T)"/>
        public void WriteEnumeratedValue(object enumValue)
        {
            if (enumValue == null)
                throw new ArgumentNullException(nameof(enumValue));

            WriteEnumeratedValue(Asn1Tag.Enumerated, enumValue);
        }

        /// <summary>
        ///   Write a non-[<see cref="FlagsAttribute"/>] enum value as an Enumerated with
        ///   tag UNIVERSAL 10.
        /// </summary>
        /// <param name="enumValue">The boxed enumeration value to write</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="enumValue"/> is <c>null</c>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <typeparamref name="TEnum"/> is not a boxed enum value --OR--
        ///   <typeparamref name="TEnum"/> is declared [<see cref="FlagsAttribute"/>]
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        /// <seealso cref="WriteEnumeratedValue(Asn1Tag,object)"/>
        /// <seealso cref="WriteEnumeratedValue{TEnum}(TEnum)"/>
        public void WriteEnumeratedValue<TEnum>(TEnum enumValue) where TEnum : struct
        {
            WriteEnumeratedValue(Asn1Tag.Enumerated, typeof(TEnum), enumValue);
        }

        /// <summary>
        ///   Write a non-[<see cref="FlagsAttribute"/>] enum value as an Enumerated with
        ///   tag UNIVERSAL 10.
        /// </summary>
        /// <param name="tag">The tag to write.</param>
        /// <param name="enumValue">The boxed enumeration value to write.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="enumValue"/> is <c>null</c>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method --OR--
        ///   <paramref name="enumValue"/> is not a boxed enum value --OR--
        ///   the unboxed type of <paramref name="enumValue"/> is declared [<see cref="FlagsAttribute"/>]
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        /// <seealso cref="WriteEnumeratedValue(System.Security.Cryptography.Asn1.Asn1Tag,object)"/>
        /// <seealso cref="WriteEnumeratedValue{T}(T)"/>
        public void WriteEnumeratedValue(Asn1Tag tag, object enumValue)
        {
            if (enumValue == null)
                throw new ArgumentNullException(nameof(enumValue));

            WriteEnumeratedValue(tag.AsPrimitive(), enumValue.GetType(), enumValue);
        }

        /// <summary>
        ///   Write a non-[<see cref="FlagsAttribute"/>] enum value as an Enumerated with
        ///   tag UNIVERSAL 10.
        /// </summary>
        /// <param name="tag">The tag to write.</param>
        /// <param name="enumValue">The boxed enumeration value to write.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="enumValue"/> is <c>null</c>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="tag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method --OR--
        ///   <typeparamref name="TEnum"/> is not an enum --OR--
        ///   <typeparamref name="TEnum"/> is declared [<see cref="FlagsAttribute"/>]
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been Disposed.</exception>
        /// <seealso cref="WriteEnumeratedValue(Asn1Tag,object)"/>
        /// <seealso cref="WriteEnumeratedValue{T}(T)"/>
        public void WriteEnumeratedValue<TEnum>(Asn1Tag tag, TEnum enumValue) where TEnum : struct
        {
            WriteEnumeratedValue(tag.AsPrimitive(), typeof(TEnum), enumValue);
        }

        // T-REC-X.690-201508 sec 8.4
        private void WriteEnumeratedValue(Asn1Tag tag, Type tEnum, object enumValue)
        {
            CheckUniversalTag(tag, UniversalTagNumber.Enumerated);

            Type backingType = tEnum.GetEnumUnderlyingType();

            if (tEnum.IsDefined(typeof(FlagsAttribute), false))
            {
                throw new ArgumentException(
                    SR.Cryptography_Asn_EnumeratedValueRequiresNonFlagsEnum,
                    nameof(tEnum));
            }

            if (backingType == typeof(ulong))
            {
                ulong numericValue = Convert.ToUInt64(enumValue);
                // T-REC-X.690-201508 sec 8.4
                WriteNonNegativeIntegerCore(tag, numericValue);
            }
            else
            {
                // All other types fit in a (signed) long.
                long numericValue = Convert.ToInt64(enumValue);
                // T-REC-X.690-201508 sec 8.4
                WriteIntegerCore(tag, numericValue);
            }
        }
    }
}
