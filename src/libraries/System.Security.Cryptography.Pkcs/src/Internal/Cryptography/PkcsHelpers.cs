// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Asn1.Pkcs7;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using X509IssuerSerial = System.Security.Cryptography.Xml.X509IssuerSerial;

namespace Internal.Cryptography
{
    internal static partial class PkcsHelpers
    {
#if !NET && !NETSTANDARD2_1
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

        internal static HashAlgorithmName GetDigestAlgorithm(string? oidValue, bool forVerification = false)
        {
            switch (oidValue)
            {
                case Oids.Md5:
                case Oids.RsaPkcs1Md5 when forVerification:
                    return HashAlgorithmName.MD5;
                case Oids.Sha1:
                case Oids.RsaPkcs1Sha1 when forVerification:
                    return HashAlgorithmName.SHA1;
                case Oids.Sha256:
                case Oids.RsaPkcs1Sha256 when forVerification:
                    return HashAlgorithmName.SHA256;
                case Oids.Sha384:
                case Oids.RsaPkcs1Sha384 when forVerification:
                    return HashAlgorithmName.SHA384;
                case Oids.Sha512:
                case Oids.RsaPkcs1Sha512 when forVerification:
                    return HashAlgorithmName.SHA512;
#if NET8_0_OR_GREATER
                case Oids.Sha3_256:
                case Oids.RsaPkcs1Sha3_256 when forVerification:
                    return HashAlgorithmName.SHA3_256;
                case Oids.Sha3_384:
                case Oids.RsaPkcs1Sha3_384 when forVerification:
                    return HashAlgorithmName.SHA3_384;
                case Oids.Sha3_512:
                case Oids.RsaPkcs1Sha3_512 when forVerification:
                    return HashAlgorithmName.SHA3_512;
#endif
                default:
                    throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, oidValue);
            }
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
                Array.Copy(arr, tmp, idx);
            }

            if (idx < tmp.Length)
            {
                Array.Copy(arr, idx + 1, tmp, idx, tmp.Length - idx);
            }

            arr = tmp;
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

            AsnWriter writer = new AsnWriter(ruleSet);
            contentInfo.Encode(writer);
            return writer.Encode();
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
        /// .NET Framework compat: We do not complain about multiple matches. Just take the first one and ignore the rest.
        /// </summary>
        public static X509Certificate2? TryFindMatchingCertificate(this X509Certificate2Collection certs, SubjectIdentifier recipientIdentifier)
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
                        X509IssuerSerial issuerSerial = (X509IssuerSerial)(recipientIdentifier.Value!);
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
                        string skiString = (string)(recipientIdentifier.Value!);
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

        internal static bool AreByteArraysEqual(byte[] ba1, byte[] ba2) =>
            ba1.AsSpan().SequenceEqual(ba2.AsSpan());

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

#if NET
        private static string ToUpperHexString(ReadOnlySpan<byte> ba)
        {
            return Convert.ToHexString(ba);
        }
#elif NETSTANDARD2_1
        private static string ToUpperHexString(ReadOnlySpan<byte> ba)
        {
            return HexConverter.ToString(ba, HexConverter.Casing.Upper);
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

            using (MemoryStream memoryStream = new MemoryStream(length))
            {
                using (var cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(data, offset, length);
                }

                return memoryStream.ToArray();
            }
        }

        public static bool TryGetRsaOaepEncryptionPadding(
            ReadOnlyMemory<byte>? parameters,
            [NotNullWhen(true)] out RSAEncryptionPadding? rsaEncryptionPadding,
            [NotNullWhen(false)] out Exception? exception)
        {
            exception = null;
            rsaEncryptionPadding = null;

            if (parameters == null || parameters.Value.IsEmpty)
            {
                exception = new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                return false;
            }

            try
            {
                OaepParamsAsn oaepParameters = OaepParamsAsn.Decode(parameters.Value, AsnEncodingRules.DER);

                if (oaepParameters.MaskGenFunc.Algorithm != Oids.Mgf1 ||
                    oaepParameters.MaskGenFunc.Parameters == null ||
                    oaepParameters.PSourceFunc.Algorithm != Oids.PSpecified
                    )
                {
                    exception = new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    return false;
                }

                AlgorithmIdentifierAsn mgf1AlgorithmIdentifier = AlgorithmIdentifierAsn.Decode(oaepParameters.MaskGenFunc.Parameters.Value, AsnEncodingRules.DER);

                if (mgf1AlgorithmIdentifier.Algorithm != oaepParameters.HashFunc.Algorithm)
                {
                    exception = new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    return false;
                }

                ReadOnlySpan<byte> pSpecifiedDefaultParameters = [0x04, 0x00];

                if (oaepParameters.PSourceFunc.Parameters != null &&
                    !oaepParameters.PSourceFunc.Parameters.Value.Span.SequenceEqual(pSpecifiedDefaultParameters))
                {
                    exception = new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    return false;
                }

                switch (oaepParameters.HashFunc.Algorithm)
                {
                    case Oids.Sha1:
                        rsaEncryptionPadding = RSAEncryptionPadding.OaepSHA1;
                        return true;
                    case Oids.Sha256:
                        rsaEncryptionPadding = RSAEncryptionPadding.OaepSHA256;
                        return true;
                    case Oids.Sha384:
                        rsaEncryptionPadding = RSAEncryptionPadding.OaepSHA384;
                        return true;
                    case Oids.Sha512:
                        rsaEncryptionPadding = RSAEncryptionPadding.OaepSHA512;
                        return true;
                    default:
                        exception = new CryptographicException(
                            SR.Cryptography_Cms_UnknownAlgorithm,
                            oaepParameters.HashFunc.Algorithm);
                        return false;
                }
            }
            catch (CryptographicException e)
            {
                exception = e;
                return false;
            }
        }
    }
}
