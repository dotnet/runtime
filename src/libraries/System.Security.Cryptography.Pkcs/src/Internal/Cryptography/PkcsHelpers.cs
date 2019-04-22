// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.Pkcs.Asn1;
using System.Security.Cryptography.X509Certificates;
using X509IssuerSerial = System.Security.Cryptography.Xml.X509IssuerSerial;

namespace Internal.Cryptography
{
    internal static partial class PkcsHelpers
    {
#if !netcoreapp
        // Compatibility API.
        internal static void AppendData(this IncrementalHash hasher, ReadOnlySpan<byte> data)
        {
            hasher.AppendData(data.ToArray());
        }
#endif

        internal static HashAlgorithmName GetDigestAlgorithm(Oid oid)
        {
            Debug.Assert(oid != null);
            return GetDigestAlgorithm(oid.Value);
        }

        internal static HashAlgorithmName GetDigestAlgorithm(string oidValue)
        {
            switch (oidValue)
            {
                case Oids.Md5:
                    return HashAlgorithmName.MD5;
                case Oids.Sha1:
                    return HashAlgorithmName.SHA1;
                case Oids.Sha256:
                    return HashAlgorithmName.SHA256;
                case Oids.Sha384:
                    return HashAlgorithmName.SHA384;
                case Oids.Sha512:
                    return HashAlgorithmName.SHA512;
                default:
                    throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, oidValue);
            }
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

            throw new CryptographicException(SR.Cryptography_Cms_UnknownAlgorithm, algName.Name);
        }

        /// <summary>
        /// This is not just a convenience wrapper for Array.Resize(). In DEBUG builds, it forces the array to move in memory even if no resize is needed. This should be used by
        /// helper methods that do anything of the form "call a native api once to get the estimated size, call it again to get the data and return the data in a byte[] array."
        /// Sometimes, that data consist of a native data structure containing pointers to other parts of the block. Using such a helper to retrieve such a block results in an intermittent
        /// AV. By using this helper, you make that AV repro every time.
        /// </summary>
        public static byte[] Resize(this byte[] a, int size)
        {
            Array.Resize(ref a, size);
#if DEBUG
            a = a.CloneByteArray();
#endif
            return a;
        }

        public static void RemoveAt<T>(ref T[] arr, int idx)
        {
            Debug.Assert(arr != null);
            Debug.Assert(idx >= 0);
            Debug.Assert(idx < arr.Length);

            if (arr.Length == 1)
            {
                arr = Array.Empty<T>();
                return;
            }

            T[] tmp = new T[arr.Length - 1];

            if (idx != 0)
            {
                Array.Copy(arr, 0, tmp, 0, idx);
            }

            if (idx < tmp.Length)
            {
                Array.Copy(arr, idx + 1, tmp, idx, tmp.Length - idx);
            }

            arr = tmp;
        }

        public static AttributeAsn[] NormalizeAttributeSet(
            AttributeAsn[] setItems,
            Action<byte[]> encodedValueProcessor=null)
        {
            byte[] normalizedValue;

            using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
            {
                writer.PushSetOf();
                foreach (AttributeAsn item in setItems)
                {
                    item.Encode(writer);
                }
                writer.PopSetOf();
                normalizedValue = writer.Encode();
                if (encodedValueProcessor != null)
                {
                    encodedValueProcessor(normalizedValue);
                }
            }

            AsnReader reader = new AsnReader(normalizedValue, AsnEncodingRules.DER);
            AsnReader setReader = reader.ReadSetOf();
            AttributeAsn[] decodedSet = new AttributeAsn[setItems.Length];
            int i = 0;
            while (setReader.HasData)
            {
                AttributeAsn.Decode(setReader, out AttributeAsn item);
                decodedSet[i] = item;
                i++;
            }
            return decodedSet;
        }

        internal static byte[] EncodeContentInfo(
            ReadOnlyMemory<byte> content,
            string contentType,
            AsnEncodingRules ruleSet = AsnEncodingRules.DER)
        {
            ContentInfoAsn contentInfo = new ContentInfoAsn
            {
                ContentType = contentType,
                Content = content,
            };
            
            using (AsnWriter writer = new AsnWriter(ruleSet))
            {
                contentInfo.Encode(writer);
                return writer.Encode();
            }
        }

        public static CmsRecipientCollection DeepCopy(this CmsRecipientCollection recipients)
        {
            CmsRecipientCollection recipientsCopy = new CmsRecipientCollection();
            foreach (CmsRecipient recipient in recipients)
            {
                X509Certificate2 originalCert = recipient.Certificate;
                X509Certificate2 certCopy = new X509Certificate2(originalCert.Handle);
                CmsRecipient recipientCopy;

                if (recipient.RSAEncryptionPadding is null)
                {
                    recipientCopy = new CmsRecipient(recipient.RecipientIdentifierType, certCopy);
                }
                else
                {
                    recipientCopy = new CmsRecipient(recipient.RecipientIdentifierType, certCopy, recipient.RSAEncryptionPadding);
                }

                recipientsCopy.Add(recipientCopy);
                GC.KeepAlive(originalCert);
            }
            return recipientsCopy;
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
                return string.Empty;   // Desktop compat: 0-length byte array maps to string.empty. 1-length byte array gets passed to Marshal.PtrToStringUni() with who knows what outcome.

            string s = Encoding.Unicode.GetString(octets, 0, octets.Length - 2);
            return s;
        }

        public static X509Certificate2Collection GetStoreCertificates(StoreName storeName, StoreLocation storeLocation, bool openExistingOnly)
        {
            using (X509Store store = new X509Store(storeName, storeLocation))
            {
                OpenFlags flags = OpenFlags.ReadOnly | OpenFlags.IncludeArchived;
                if (openExistingOnly)
                    flags |= OpenFlags.OpenExistingOnly;

                store.Open(flags);
                X509Certificate2Collection certificates = store.Certificates;
                return certificates;
            }
        }

        /// <summary>
        /// Desktop compat: We do not complain about multiple matches. Just take the first one and ignore the rest.
        /// </summary>
        public static X509Certificate2 TryFindMatchingCertificate(this X509Certificate2Collection certs, SubjectIdentifier recipientIdentifier)
        {
            //
            // Note: SubjectIdentifier has no public constructor so the only one that can construct this type is this assembly.
            //       Therefore, we trust that the string-ized byte array (serial or ski) in it is correct and canonicalized.
            //

            SubjectIdentifierType recipientIdentifierType = recipientIdentifier.Type;
            switch (recipientIdentifierType)
            {
                case SubjectIdentifierType.IssuerAndSerialNumber:
                    {
                        X509IssuerSerial issuerSerial = (X509IssuerSerial)(recipientIdentifier.Value);
                        byte[] serialNumber = issuerSerial.SerialNumber.ToSerialBytes();
                        string issuer = issuerSerial.IssuerName;
                        foreach (X509Certificate2 candidate in certs)
                        {
                            byte[] candidateSerialNumber = candidate.GetSerialNumber();
                            if (AreByteArraysEqual(candidateSerialNumber, serialNumber) && candidate.Issuer == issuer)
                                return candidate;
                        }
                    }
                    break;

                case SubjectIdentifierType.SubjectKeyIdentifier:
                    {
                        string skiString = (string)(recipientIdentifier.Value);
                        byte[] ski = skiString.ToSkiBytes();
                        foreach (X509Certificate2 cert in certs)
                        {
                            byte[] candidateSki = PkcsPal.Instance.GetSubjectKeyIdentifier(cert);
                            if (AreByteArraysEqual(ski, candidateSki))
                                return cert;
                        }
                    }
                    break;

                default:
                    // RecipientInfo's can only be created by this package so if this an invalid type, it's the package's fault.
                    Debug.Fail($"Invalid recipientIdentifier type: {recipientIdentifierType}");
                    throw new CryptographicException();
            }
            return null;
        }

        internal static bool AreByteArraysEqual(byte[] ba1, byte[] ba2)
        {
            if (ba1.Length != ba2.Length)
                return false;

            for (int i = 0; i < ba1.Length; i++)
            {
                if (ba1[i] != ba2[i])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Asserts on bad or non-canonicalized input. Input must come from trusted sources.
        /// 
        /// Subject Key Identifier is string-ized as an upper case hex string. This format is part of the public api behavior and cannot be changed.
        /// </summary>
        internal static byte[] ToSkiBytes(this string skiString)
        {
            return skiString.UpperHexStringToByteArray();
        }

        public static string ToSkiString(this byte[] skiBytes)
        {
            return ToUpperHexString(skiBytes);
        }

        public static string ToBigEndianHex(this ReadOnlySpan<byte> bytes)
        {
            return ToUpperHexString(bytes);
        }

        /// <summary>
        /// Asserts on bad or non-canonicalized input. Input must come from trusted sources.
        /// 
        /// Serial number is string-ized as a reversed upper case hex string. This format is part of the public api behavior and cannot be changed.
        /// </summary>
        internal static byte[] ToSerialBytes(this string serialString)
        {
            byte[] ba = serialString.UpperHexStringToByteArray();
            Array.Reverse(ba);
            return ba;
        }

        public static string ToSerialString(this byte[] serialBytes)
        {
            serialBytes = serialBytes.CloneByteArray();
            Array.Reverse(serialBytes);
            return ToUpperHexString(serialBytes);
        }

#if netcoreapp
        private static unsafe string ToUpperHexString(ReadOnlySpan<byte> ba)
        {
            fixed (byte* baPtr = ba)
            {
                return string.Create(ba.Length * 2, (new IntPtr(baPtr), ba.Length), (span, args) =>
                {
                    const string HexValues = "0123456789ABCDEF";
                    int p = 0;
                    foreach (byte b in new ReadOnlySpan<byte>((byte*)args.Item1, args.Item2))
                    {
                        span[p++] = HexValues[b >> 4];
                        span[p++] = HexValues[b & 0xF];
                    }
                });
            }
        }
#else
        private static string ToUpperHexString(ReadOnlySpan<byte> ba)
        {
            StringBuilder sb = new StringBuilder(ba.Length * 2);

            for (int i = 0; i < ba.Length; i++)
            {
                sb.Append(ba[i].ToString("X2"));
            }

            return sb.ToString();
        }
#endif

        /// <summary>
        /// Asserts on bad input. Input must come from trusted sources.
        /// </summary>
        private static byte[] UpperHexStringToByteArray(this string normalizedString)
        {
            Debug.Assert((normalizedString.Length & 0x1) == 0);

            byte[] ba = new byte[normalizedString.Length / 2];
            for (int i = 0; i < ba.Length; i++)
            {
                char c = normalizedString[i * 2];
                byte b = (byte)(UpperHexCharToNybble(c) << 4);
                c = normalizedString[i * 2 + 1];
                b |= UpperHexCharToNybble(c);
                ba[i] = b;
            }
            return ba;
        }

        /// <summary>
        /// Asserts on bad input. Input must come from trusted sources.
        /// </summary>
        private static byte UpperHexCharToNybble(char c)
        {
            if (c >= '0' && c <= '9')
                return (byte)(c - '0');
            if (c >= 'A' && c <= 'F')
                return (byte)(c - 'A' + 10);

            Debug.Fail($"Invalid hex character: {c}");
            throw new CryptographicException();  // This just keeps the compiler happy. We don't expect to reach this.
        }

        /// <summary>
        /// Useful helper for "upgrading" well-known CMS attributes to type-specific objects such as Pkcs9DocumentName, Pkcs9DocumentDescription, etc. 
        /// </summary>
        public static Pkcs9AttributeObject CreateBestPkcs9AttributeObjectAvailable(Oid oid, byte[] encodedAttribute)
        {
            Pkcs9AttributeObject attributeObject = new Pkcs9AttributeObject(oid, encodedAttribute);
            switch (oid.Value)
            {
                case Oids.DocumentName:
                    attributeObject = Upgrade<Pkcs9DocumentName>(attributeObject);
                    break;

                case Oids.DocumentDescription:
                    attributeObject = Upgrade<Pkcs9DocumentDescription>(attributeObject);
                    break;

                case Oids.SigningTime:
                    attributeObject = Upgrade<Pkcs9SigningTime>(attributeObject);
                    break;

                case Oids.ContentType:
                    attributeObject = Upgrade<Pkcs9ContentType>(attributeObject);
                    break;

                case Oids.MessageDigest:
                    attributeObject = Upgrade<Pkcs9MessageDigest>(attributeObject);
                    break;

#if netcoreapp
                case Oids.LocalKeyId:
                    attributeObject = Upgrade<Pkcs9LocalKeyId>(attributeObject);
                    break;
#endif

                default:
                    break;
            }
            return attributeObject;
        }

        private static T Upgrade<T>(Pkcs9AttributeObject basicAttribute) where T : Pkcs9AttributeObject, new()
        {
            T enhancedAttribute = new T();
            enhancedAttribute.CopyFrom(basicAttribute);
            return enhancedAttribute;
        }

        internal static byte[] OneShot(this ICryptoTransform transform, byte[] data)
        {
            return OneShot(transform, data, 0, data.Length);
        }

        internal static byte[] OneShot(this ICryptoTransform transform, byte[] data, int offset, int length)
        {
            if (transform.CanTransformMultipleBlocks)
            {
                return transform.TransformFinalBlock(data, offset, length);
            }

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (var cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(data, offset, length);
                }

                return memoryStream.ToArray();
            }
        }

        public static ReadOnlyMemory<byte> DecodeOctetString(ReadOnlyMemory<byte> encodedOctetString)
        {
            AsnReader reader = new AsnReader(encodedOctetString, AsnEncodingRules.BER);

            if (reader.PeekEncodedValue().Length != encodedOctetString.Length)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            if (reader.TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> primitiveContents))
            {
                return primitiveContents;
            }

            byte[] tooBig = new byte[encodedOctetString.Length];

            if (reader.TryCopyOctetStringBytes(tooBig, out int bytesWritten))
            {
                return tooBig.AsMemory(0, bytesWritten);
            }

            Debug.Fail("TryCopyOctetStringBytes failed with an over-allocated array");
            throw new CryptographicException();
        }
    }
}
