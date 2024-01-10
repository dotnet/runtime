// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Apple;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Asn1.Pkcs12;

namespace System.Security.Cryptography.X509Certificates
{
    internal static partial class X509Pal
    {
        private static partial IX509Pal BuildSingleton()
        {
            return new AppleX509Pal();
        }

        private sealed partial class AppleX509Pal : ManagedX509ExtensionProcessor, IX509Pal
        {
            public AsymmetricAlgorithm DecodePublicKey(Oid oid, byte[] encodedKeyValue, byte[] encodedParameters,
                ICertificatePal? certificatePal)
            {
                AppleCertificatePal? applePal = certificatePal as AppleCertificatePal;

                if (applePal != null)
                {
                    SafeSecKeyRefHandle key = Interop.AppleCrypto.X509GetPublicKey(applePal.CertificateHandle);

                    switch (oid.Value)
                    {
                        case Oids.Rsa:
                            Debug.Assert(!key.IsInvalid);
                            return new RSAImplementation.RSASecurityTransforms(key);
                        case Oids.Dsa:
                            if (key.IsInvalid)
                            {
                                // SecCertificateCopyKey returns null for DSA, so fall back to manually building it.
                                return DecodeDsaPublicKey(encodedKeyValue, encodedParameters);
                            }
                            return new DSAImplementation.DSASecurityTransforms(key);
                    }

                    key.Dispose();
                }
                else
                {
                    switch (oid.Value)
                    {
                        case Oids.Rsa:
                            return DecodeRsaPublicKey(encodedKeyValue);
                        case Oids.Dsa:
                            return DecodeDsaPublicKey(encodedKeyValue, encodedParameters);
                    }
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

            private static DSA DecodeDsaPublicKey(byte[] encodedKeyValue, byte[] encodedParameters)
            {
                SubjectPublicKeyInfoAsn spki = new SubjectPublicKeyInfoAsn
                {
                    Algorithm = new AlgorithmIdentifierAsn { Algorithm = Oids.Dsa, Parameters = encodedParameters },
                    SubjectPublicKey = encodedKeyValue,
                };

                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                spki.Encode(writer);

                byte[] rented = CryptoPool.Rent(writer.GetEncodedLength());

                if (!writer.TryEncode(rented, out int written))
                {
                    Debug.Fail("TryEncode failed with a pre-allocated buffer");
                    throw new InvalidOperationException();
                }

                DSA dsa = DSA.Create();
                DSA? toDispose = dsa;

                try
                {
                    dsa.ImportSubjectPublicKeyInfo(rented.AsSpan(0, written), out _);
                    toDispose = null;
                    return dsa;
                }
                finally
                {
                    toDispose?.Dispose();
                    CryptoPool.Return(rented, written);
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

                // Apple doesn't seem to recognize PFX files with no MAC, so try a quick maybe-it's-a-PFX test
                if (contentType == X509ContentType.Unknown)
                {
                    try
                    {
                        unsafe
                        {
                            fixed (byte* pin = rawData)
                            {
                                using (var manager = new PointerMemoryManager<byte>(pin, rawData.Length))
                                {
                                    PfxAsn.Decode(manager.Memory, AsnEncodingRules.BER);
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
