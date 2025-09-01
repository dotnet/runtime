// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Apple;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Asn1.Pkcs12;
using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    internal static partial class X509Pal
    {
        private static partial IX509Pal BuildSingleton()
        {
            return new AppleX509Pal();
        }

        private sealed partial class AppleX509Pal : IX509Pal
        {
            public AsymmetricAlgorithm DecodePublicKey(Oid oid, byte[] encodedKeyValue, byte[]? encodedParameters,
                ICertificatePal? certificatePal)
            {
                AppleCertificatePal? applePal = certificatePal as AppleCertificatePal;

                if (applePal != null)
                {
                    SafeSecKeyRefHandle key = Interop.AppleCrypto.X509GetPublicKey(applePal.CertificateHandle);

                    if (oid.Value == Oids.Rsa)
                    {
                        Debug.Assert(!key.IsInvalid);
                        return new RSAImplementation.RSASecurityTransforms(key);
                    }

                    key.Dispose();
                }
                else if (oid.Value == Oids.Rsa)
                {
                    return DecodeRsaPublicKey(encodedKeyValue);
                }

                throw new NotSupportedException(SR.NotSupported_KeyAlgorithm);
            }

            private static RSA DecodeRsaPublicKey(byte[] encodedKeyValue)
            {
                RSA rsa = RSA.Create();
                try
                {
                    rsa.ImportRSAPublicKey(new ReadOnlySpan<byte>(encodedKeyValue), out _);
                    return rsa;
                }
                catch (Exception)
                {
                    rsa.Dispose();
                    throw;
                }
            }

            public X509ContentType GetCertContentType(ReadOnlySpan<byte> rawData)
            {
                const int errSecUnknownFormat = -25257;

                if (rawData.IsEmpty)
                {
                    // Throw to match Windows and Unix behavior.
                    throw Interop.AppleCrypto.CreateExceptionForOSStatus(errSecUnknownFormat);
                }

                X509ContentType contentType = Interop.AppleCrypto.X509GetContentType(rawData);

                // Apple's native check can't check for PKCS12, so do a quick decode test to see if it is PKCS12 / PFX.
                if (contentType == X509ContentType.Unknown)
                {
                    try
                    {
                        unsafe
                        {
                            fixed (byte* pin = rawData)
                            {
                                AsnValueReader reader = new AsnValueReader(rawData, AsnEncodingRules.BER);

                                using (var manager = new PointerMemoryManager<byte>(pin, rawData.Length))
                                {
                                    PfxAsn.Decode(ref reader, manager.Memory, out _);
                                }

                                contentType = X509ContentType.Pkcs12;
                            }
                        }
                    }
                    catch (CryptographicException)
                    {
                    }
                }

                if (contentType == X509ContentType.Unknown)
                {
                    // Throw to match Windows and Unix behavior.
                    throw Interop.AppleCrypto.CreateExceptionForOSStatus(errSecUnknownFormat);
                }

                return contentType;
            }

            public X509ContentType GetCertContentType(string fileName)
            {
                return GetCertContentType(System.IO.File.ReadAllBytes(fileName));
            }
        }
    }
}
