// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class X509Pal
    {
        public static IX509Pal Instance = new AppleX509Pal();

        private X509Pal()
        {
        }

        private partial class AppleX509Pal : ManagedX509ExtensionProcessor, IX509Pal
        {
            public AsymmetricAlgorithm DecodePublicKey(Oid oid, byte[] encodedKeyValue, byte[] encodedParameters,
                ICertificatePal certificatePal)
            {
                const int errSecInvalidKeyRef = -67712;
                const int errSecUnsupportedKeySize = -67735;
                AppleCertificatePal applePal = certificatePal as AppleCertificatePal;

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
                        case Oids.EcPublicKey:
                            // If X509GetPublicKey uses the new SecCertificateCopyKey API it can return an invalid
                            // key reference for unsupported algorithms. This currently happens for the BrainpoolP160r1
                            // algorithm in the test suite (as of macOS Mojave Developer Preview 4).
                            if (key.IsInvalid)
                            {
                                throw Interop.AppleCrypto.CreateExceptionForOSStatus(errSecInvalidKeyRef);
                            }
                            // EccGetKeySizeInBits can fail for two reasons. First, the Apple implementation has changed
                            // and we receive values from API that were not previously handled. In that case the CoreFX
                            // implementation will need to be adjusted to handle these values. Second, we deliberately
                            // return 0 from the native code to prevent hitting buggy API implementations in Apple code
                            // later.
                            if (Interop.AppleCrypto.EccGetKeySizeInBits(key) == 0)
                            {
                                key.Dispose();
                                throw Interop.AppleCrypto.CreateExceptionForOSStatus(errSecUnsupportedKeySize);
                            }
                            return new ECDsaImplementation.ECDsaSecurityTransforms(key);
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
                    Algorithm = new AlgorithmIdentifierAsn { Algorithm = new Oid(Oids.Dsa, null), Parameters = encodedParameters },
                    SubjectPublicKey = encodedKeyValue,
                };

                using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
                {
                    spki.Encode(writer);
                    DSA dsa = DSA.Create();
                    try
                    {
                        dsa.ImportSubjectPublicKeyInfo(writer.EncodeAsSpan(), out _);
                        return dsa;
                    }
                    catch (Exception)
                    {
                        dsa.Dispose();
                        throw;
                    }
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

            public X509ContentType GetCertContentType(byte[] rawData)
            {
                const int errSecUnknownFormat = -25257;
                if (rawData == null || rawData.Length == 0)
                {
                    // Throw to match Windows and Unix behavior.
                    throw Interop.AppleCrypto.CreateExceptionForOSStatus(errSecUnknownFormat);
                }
                
                X509ContentType contentType = Interop.AppleCrypto.X509GetContentType(rawData, rawData.Length);

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
