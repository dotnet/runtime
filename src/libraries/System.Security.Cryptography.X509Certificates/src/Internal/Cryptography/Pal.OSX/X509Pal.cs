// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Asn1.Pkcs12;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class X509Pal
    {
        public static IX509Pal Instance = new AppleX509Pal();

        private X509Pal()
        {
        }

        private sealed partial class AppleX509Pal : ManagedX509ExtensionProcessor, IX509Pal
        {
            public ECDsa DecodeECDsaPublicKey(ICertificatePal? certificatePal)
            {
                return new ECDsaImplementation.ECDsaSecurityTransforms(DecodeECPublicKey(certificatePal));
            }

            public ECDiffieHellman DecodeECDiffieHellmanPublicKey(ICertificatePal? certificatePal)
            {
                return new ECDiffieHellmanImplementation.ECDiffieHellmanSecurityTransforms(DecodeECPublicKey(certificatePal));
            }

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

            private static SafeSecKeyRefHandle DecodeECPublicKey(ICertificatePal? certificatePal)
            {
                const int errSecInvalidKeyRef = -67712;
                const int errSecUnsupportedKeySize = -67735;

                if (certificatePal is null)
                    throw new NotSupportedException(SR.NotSupported_KeyAlgorithm);

                AppleCertificatePal applePal = (AppleCertificatePal)certificatePal;
                SafeSecKeyRefHandle key = Interop.AppleCrypto.X509GetPublicKey(applePal.CertificateHandle);

                // If X509GetPublicKey uses the new SecCertificateCopyKey API it can return an invalid
                // key reference for unsupported algorithms. This currently happens for the BrainpoolP160r1
                // algorithm in the test suite (as of macOS Mojave Developer Preview 4).
                if (key.IsInvalid)
                {
                    throw Interop.AppleCrypto.CreateExceptionForOSStatus(errSecInvalidKeyRef);
                }
                // EccGetKeySizeInBits can fail for two reasons. First, the Apple implementation has changed
                // and we receive values from API that were not previously handled. In that case the
                // implementation will need to be adjusted to handle these values. Second, we deliberately
                // return 0 from the native code to prevent hitting buggy API implementations in Apple code
                // later.
                if (Interop.AppleCrypto.EccGetKeySizeInBits(key) == 0)
                {
                    key.Dispose();
                    throw Interop.AppleCrypto.CreateExceptionForOSStatus(errSecUnsupportedKeySize);
                }

                return key;
            }

            private static AsymmetricAlgorithm DecodeRsaPublicKey(byte[] encodedKeyValue)
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

            private static AsymmetricAlgorithm DecodeDsaPublicKey(byte[] encodedKeyValue, byte[] encodedParameters)
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
                IDisposable? toDispose = dsa;

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

            public string X500DistinguishedNameDecode(byte[] encodedDistinguishedName, X500DistinguishedNameFlags flag)
            {
                return X500NameEncoder.X500DistinguishedNameDecode(encodedDistinguishedName, true, flag);
            }

            public byte[] X500DistinguishedNameEncode(string distinguishedName, X500DistinguishedNameFlags flag)
            {
                return X500NameEncoder.X500DistinguishedNameEncode(distinguishedName, flag);
            }

            public string X500DistinguishedNameFormat(byte[] encodedDistinguishedName, bool multiLine)
            {
                return X500NameEncoder.X500DistinguishedNameDecode(
                    encodedDistinguishedName,
                    true,
                    multiLine ? X500DistinguishedNameFlags.UseNewLines : X500DistinguishedNameFlags.None,
                    multiLine);
            }

            public X509ContentType GetCertContentType(ReadOnlySpan<byte> rawData)
            {
                const int errSecUnknownFormat = -25257;
                if (rawData == null || rawData.Length == 0)
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
