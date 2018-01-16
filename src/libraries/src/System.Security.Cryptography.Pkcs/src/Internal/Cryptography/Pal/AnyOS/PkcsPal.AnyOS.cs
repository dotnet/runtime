// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography
{
    internal abstract partial class PkcsPal
    {
        private static PkcsPal s_instance = new ManagedPkcsPal();

        private class ManagedPkcsPal : PkcsPal
        {
            public override byte[] Encrypt(
                CmsRecipientCollection recipients,
                ContentInfo contentInfo,
                AlgorithmIdentifier contentEncryptionAlgorithm,
                X509Certificate2Collection originatorCerts,
                CryptographicAttributeObjectCollection unprotectedAttributes)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyPkcs);
            }

            public override DecryptorPal Decode(
                byte[] encodedMessage,
                out int version,
                out ContentInfo contentInfo,
                out AlgorithmIdentifier contentEncryptionAlgorithm,
                out X509Certificate2Collection originatorCerts,
                out CryptographicAttributeObjectCollection unprotectedAttributes)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyPkcs);
            }

            public override byte[] EncodeOctetString(byte[] octets)
            {
                // Write using DER to support the most readers.
                using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
                {
                    writer.WriteOctetString(octets);
                    return writer.Encode();
                }
            }

            public override byte[] DecodeOctetString(byte[] encodedOctets)
            {
                // Read using BER because the CMS specification says the encoding is BER.
                AsnReader reader = new AsnReader(encodedOctets, AsnEncodingRules.BER);

                const int ArbitraryStackLimit = 256;
                Span<byte> tmp = stackalloc byte[ArbitraryStackLimit];
                // Use stackalloc 0 so data can later hold a slice of tmp.
                ReadOnlySpan<byte> data = stackalloc byte[0];
                byte[] poolBytes = null;

                try
                {
                    if (!reader.TryGetPrimitiveOctetStringBytes(out var contents))
                    {
                        if (reader.TryCopyOctetStringBytes(tmp, out int bytesWritten))
                        {
                            data = tmp.Slice(0, bytesWritten);
                        }
                        else
                        {
                            poolBytes = ArrayPool<byte>.Shared.Rent(reader.PeekContentBytes().Length);

                            if (!reader.TryCopyOctetStringBytes(poolBytes, out bytesWritten))
                            {
                                Debug.Fail("TryCopyOctetStringBytes failed with a provably-large-enough buffer");
                                throw new CryptographicException();
                            }

                            data = new ReadOnlySpan<byte>(poolBytes, 0, bytesWritten);
                        }
                    }
                    else
                    {
                        data = contents.Span;
                    }

                    if (reader.HasData)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    return data.ToArray();
                }
                finally
                {
                    if (poolBytes != null)
                    {
                        Array.Clear(poolBytes, 0, data.Length);
                        ArrayPool<byte>.Shared.Return(poolBytes);
                    }
                }
            }

            public override byte[] EncodeUtcTime(DateTime utcTime)
            {
                // Write using DER to support the most readers.
                using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
                {
                    // Sending the DateTime through ToLocalTime here will cause the right normalization
                    // of DateTimeKind.Unknown.
                    //
                    // Local => Local (noop) => UTC (in WriteUtcTime) (adjust, correct)
                    // UTC => Local (adjust) => UTC (adjust back, correct)
                    // Unknown => Local (adjust) => UTC (adjust "back", add Z marker; matches Windows)
                    writer.WriteUtcTime(utcTime.ToLocalTime());
                    return writer.Encode();
                }
            }

            public override DateTime DecodeUtcTime(byte[] encodedUtcTime)
            {
                // Read using BER because the CMS specification says the encoding is BER.
                AsnReader reader = new AsnReader(encodedUtcTime, AsnEncodingRules.BER);
                DateTimeOffset value = reader.GetUtcTime();

                if (reader.HasData)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                return value.UtcDateTime;
            }

            public override string DecodeOid(byte[] encodedOid)
            {
                Span<byte> emptyInvalidOid = stackalloc byte[2];
                emptyInvalidOid[0] = 0x06;
                emptyInvalidOid[1] = 0x00;

                // Windows compat.
                if (emptyInvalidOid.SequenceEqual(encodedOid))
                {
                    return string.Empty;
                }

                // Read using BER because the CMS specification says the encoding is BER.
                AsnReader reader = new AsnReader(encodedOid, AsnEncodingRules.BER);
                string value = reader.ReadObjectIdentifierAsString();

                if (reader.HasData)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                return value;
            }

            public override Oid GetEncodedMessageType(byte[] encodedMessage)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyPkcs);
            }

            public override void AddCertsFromStoreForDecryption(X509Certificate2Collection certs)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyPkcs);
            }

            public override Exception CreateRecipientsNotFoundException()
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyPkcs);
            }

            public override Exception CreateRecipientInfosAfterEncryptException()
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyPkcs);
            }

            public override Exception CreateDecryptAfterEncryptException()
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyPkcs);
            }

            public override Exception CreateDecryptTwiceException()
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyPkcs);
            }

            public override byte[] GetSubjectKeyIdentifier(X509Certificate2 certificate)
            {
                return certificate.GetSubjectKeyIdentifier();
            }

            public override T GetPrivateKeyForSigning<T>(X509Certificate2 certificate, bool silent)
            {
                return GetPrivateKey<T>(certificate);
            }

            public override T GetPrivateKeyForDecryption<T>(X509Certificate2 certificate, bool silent)
            {
                return GetPrivateKey<T>(certificate);
            }

            private T GetPrivateKey<T>(X509Certificate2 certificate) where T : AsymmetricAlgorithm
            {
                if (typeof(T) == typeof(RSA))
                    return (T)(object)certificate.GetRSAPrivateKey();
                if (typeof(T) == typeof(ECDsa))
                    return (T)(object)certificate.GetECDsaPrivateKey();
#if netcoreapp
                if (typeof(T) == typeof(DSA))
                    return (T)(object)certificate.GetDSAPrivateKey();
#endif

                Debug.Fail($"Unknown key type requested: {typeof(T).FullName}");
                return null;
            }
        }
    }
}
