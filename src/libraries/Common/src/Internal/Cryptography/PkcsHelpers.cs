// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Pkcs;
using System.Text;

namespace Internal.Cryptography
{
    internal static partial class PkcsHelpers
    {
#if BUILDING_PKCS
        private static readonly bool s_oidIsInitOnceOnly = DetectInitOnlyOid();

        private static bool DetectInitOnlyOid()
        {
            Oid testOid = new Oid(Oids.Sha256, null);

            try
            {
                testOid.Value = Oids.Sha384;
                return false;
            }
            catch (PlatformNotSupportedException)
            {
                return true;
            }
        }
#endif

        internal static List<AttributeAsn> BuildAttributes(CryptographicAttributeObjectCollection? attributes)
        {
            List<AttributeAsn> signedAttrs = new List<AttributeAsn>();

            if (attributes == null || attributes.Count == 0)
            {
                return signedAttrs;
            }

            foreach (CryptographicAttributeObject attributeObject in attributes)
            {
                AttributeAsn newAttr = new AttributeAsn
                {
                    AttrType = attributeObject.Oid!.Value!,
                    AttrValues = new ReadOnlyMemory<byte>[attributeObject.Values.Count],
                };

                for (int i = 0; i < attributeObject.Values.Count; i++)
                {
                    newAttr.AttrValues[i] = attributeObject.Values[i].RawData;
                }

                signedAttrs.Add(newAttr);
            }

            return signedAttrs;
        }

        public static void EnsureSingleBerValue(ReadOnlySpan<byte> source)
        {
            if (!AsnDecoder.TryReadEncodedValue(source, AsnEncodingRules.BER, out _, out _, out _, out int consumed) ||
                consumed != source.Length)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }
        }

        public static byte[] EncodeOctetString(byte[] octets)
        {
            // Write using DER to support the most readers.
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            writer.WriteOctetString(octets);
            return writer.Encode();
        }

        public static byte[] DecodeOctetString(ReadOnlyMemory<byte> encodedOctets)
        {
            try
            {
                // Read using BER because the CMS specification says the encoding is BER.
                byte[] ret = AsnDecoder.ReadOctetString(encodedOctets.Span, AsnEncodingRules.BER, out int consumed);

                if (consumed != encodedOctets.Length)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                return ret;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        public static string DecodeOid(ReadOnlySpan<byte> encodedOid)
        {
            // Windows compat for a zero length OID.
            if (encodedOid.Length == 2 && encodedOid[0] == 0x06 && encodedOid[1] == 0x00)
            {
                return string.Empty;
            }

            // Read using BER because the CMS specification says the encoding is BER.
            try
            {
                string value = AsnDecoder.ReadObjectIdentifier(
                    encodedOid,
                    AsnEncodingRules.BER,
                    out int consumed);

                if (consumed != encodedOid.Length)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                return value;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        public static int FirstBerValueLength(ReadOnlySpan<byte> source)
        {
            if (!AsnDecoder.TryReadEncodedValue(source, AsnEncodingRules.BER, out _, out _, out _, out int consumed))
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            return consumed;
        }

        public static byte[] UnicodeToOctetString(this string s)
        {
            byte[] octets = new byte[2 * (s.Length + 1)];
            Encoding.Unicode.GetBytes(s, 0, s.Length, octets, 0);
            return octets;
        }

        public static string OctetStringToUnicode(this byte[] octets)
        {
            if (octets.Length < 2)
                return string.Empty;   // .NET Framework compat: 0-length byte array maps to string.empty. 1-length byte array gets passed to Marshal.PtrToStringUni() with who knows what outcome.

            int end = octets.Length;
            int endMinusOne = end - 1;

            // Truncate the string to before the first embedded \0 (probably the last two bytes).
            for (int i = 0; i < endMinusOne; i += 2)
            {
                if (octets[i] == 0 && octets[i + 1] == 0)
                {
                    end = i;
                    break;
                }
            }

            string s = Encoding.Unicode.GetString(octets, 0, end);
            return s;
        }

        public static ReadOnlyMemory<byte> DecodeOctetStringAsMemory(ReadOnlyMemory<byte> encodedOctetString)
        {
#if BUILDING_PKCS
            try
            {
                ReadOnlySpan<byte> input = encodedOctetString.Span;

                if (AsnDecoder.TryReadPrimitiveOctetString(
                    input,
                    AsnEncodingRules.BER,
                    out ReadOnlySpan<byte> primitive,
                    out int consumed))
                {
                    if (consumed != input.Length)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    if (input.Overlaps(primitive, out int offset))
                    {
                        return encodedOctetString.Slice(offset, primitive.Length);
                    }

                    Debug.Fail("input.Overlaps(primitive) failed after TryReadPrimitiveOctetString succeeded");
                }

                byte[] ret = AsnDecoder.ReadOctetString(input, AsnEncodingRules.BER, out consumed);

                if (consumed != input.Length)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                return ret;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
#else
            return Helpers.DecodeOctetStringAsMemory(encodedOctetString);
#endif
        }

        internal static string GetOidFromHashAlgorithm(HashAlgorithmName algName)
        {
            if (algName == HashAlgorithmName.MD5)
                return Oids.Md5;
            if (algName == HashAlgorithmName.SHA1)
                return Oids.Sha1;
            if (algName == HashAlgorithmName.SHA256)
                return Oids.Sha256;
            if (algName == HashAlgorithmName.SHA384)
                return Oids.Sha384;
            if (algName == HashAlgorithmName.SHA512)
                return Oids.Sha512;
#if NET8_0_OR_GREATER
            if (algName == HashAlgorithmName.SHA3_256)
                return Oids.Sha3_256;
            if (algName == HashAlgorithmName.SHA3_384)
                return Oids.Sha3_384;
            if (algName == HashAlgorithmName.SHA3_512)
                return Oids.Sha3_512;
#endif

            throw new CryptographicException(SR.Cryptography_Cms_UnknownAlgorithm, algName.Name);
        }

        // Creates a defensive copy of an OID on platforms where OID
        // is mutable. On platforms where OID is immutable, return the OID as-is.
        [return: NotNullIfNotNull(nameof(oid))]
        public static Oid? CopyOid(this Oid? oid)
        {
#if BUILDING_PKCS
            if (s_oidIsInitOnceOnly)
            {
                return oid;
            }
            else
            {
                return oid is null ? null : new Oid(oid);
            }
#else
            return oid; // If we are in System.Security.Cryptography, then Oids are known to be immutable.
#endif
        }

        internal static CryptographicAttributeObjectCollection MakeAttributeCollection(AttributeAsn[]? attributes)
        {
            var coll = new CryptographicAttributeObjectCollection();

            if (attributes == null)
                return coll;

            foreach (AttributeAsn attribute in attributes)
            {
                coll.AddWithoutMerge(MakeAttribute(attribute));
            }

            return coll;
        }

        internal static CryptographicAttributeObject MakeAttribute(AttributeAsn attribute)
        {
            Oid type = new Oid(attribute.AttrType);
            AsnEncodedDataCollection valueColl = new AsnEncodedDataCollection();

            foreach (ReadOnlyMemory<byte> attrValue in attribute.AttrValues)
            {
                valueColl.Add(CreateBestPkcs9AttributeObjectAvailable(type, attrValue.ToArray()));
            }

            return new CryptographicAttributeObject(type, valueColl);
        }

        public static byte[] EncodeUtcTime(DateTime utcTime)
        {
            const int maxLegalYear = 2049;
            // Write using DER to support the most readers.
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            try
            {
                // Sending the DateTime through ToLocalTime here will cause the right normalization
                // of DateTimeKind.Unknown.
                //
                // Unknown => Local (adjust) => UTC (adjust "back", add Z marker; matches Windows)
                if (utcTime.Kind == DateTimeKind.Unspecified)
                {
                    writer.WriteUtcTime(utcTime.ToLocalTime(), maxLegalYear);
                }
                else
                {
                    writer.WriteUtcTime(utcTime, maxLegalYear);
                }

                return writer.Encode();
            }
            catch (ArgumentException ex)
            {
                throw new CryptographicException(ex.Message, ex);
            }
        }

        public static DateTime DecodeUtcTime(byte[] encodedUtcTime)
        {
            // Read using BER because the CMS specification says the encoding is BER.
            try
            {
                DateTimeOffset value = AsnDecoder.ReadUtcTime(
                    encodedUtcTime,
                    AsnEncodingRules.BER,
                    out int consumed);

                if (consumed != encodedUtcTime.Length)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                return value.UtcDateTime;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        /// <summary>
        /// Useful helper for "upgrading" well-known CMS attributes to type-specific objects such as Pkcs9DocumentName, Pkcs9DocumentDescription, etc.
        /// </summary>
        public static Pkcs9AttributeObject CreateBestPkcs9AttributeObjectAvailable(Oid oid, ReadOnlySpan<byte> encodedAttribute)
        {
            return oid.Value switch
            {
                Oids.DocumentName => new Pkcs9DocumentName(encodedAttribute),
                Oids.DocumentDescription => new Pkcs9DocumentDescription(encodedAttribute),
                Oids.SigningTime => new Pkcs9SigningTime(encodedAttribute),
                Oids.ContentType => new Pkcs9ContentType(encodedAttribute),
                Oids.MessageDigest => new Pkcs9MessageDigest(encodedAttribute),
#if NET || NETSTANDARD2_1
                Oids.LocalKeyId => new Pkcs9LocalKeyId() { RawData = encodedAttribute.ToArray() },
#endif
                _ => new Pkcs9AttributeObject(oid, encodedAttribute),
            };
        }

        public static AttributeAsn[] NormalizeAttributeSet(
            AttributeAsn[] setItems,
            Action<byte[]>? encodedValueProcessor = null)
        {
            byte[] normalizedValue;

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            writer.PushSetOf();

            foreach (AttributeAsn item in setItems)
            {
                item.Encode(writer);
            }

            writer.PopSetOf();
            normalizedValue = writer.Encode();

            encodedValueProcessor?.Invoke(normalizedValue);

            try
            {
                AsnValueReader reader = new AsnValueReader(normalizedValue, AsnEncodingRules.DER);
                AsnValueReader setReader = reader.ReadSetOf();
                AttributeAsn[] decodedSet = new AttributeAsn[setItems.Length];
                int i = 0;
                while (setReader.HasData)
                {
                    AttributeAsn.Decode(ref setReader, normalizedValue, out AttributeAsn item);
                    decodedSet[i] = item;
                    i++;
                }

                return decodedSet;
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }
    }
}
