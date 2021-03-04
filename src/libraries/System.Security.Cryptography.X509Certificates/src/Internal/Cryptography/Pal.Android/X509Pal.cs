// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Formats.Asn1;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class X509Pal
    {
        public static IX509Pal Instance = new AndroidX509Pal();

        private X509Pal()
        {
        }

        private partial class AndroidX509Pal : ManagedX509ExtensionProcessor, IX509Pal
        {
            public ECDsa DecodeECDsaPublicKey(ICertificatePal? certificatePal)
            {
                if (certificatePal == null)
                    throw new NotSupportedException(SR.NotSupported_KeyAlgorithm);

                return new ECDsaImplementation.ECDsaAndroid(DecodeECPublicKey(certificatePal));
            }

            public ECDiffieHellman DecodeECDiffieHellmanPublicKey(ICertificatePal? certificatePal)
            {
                if (certificatePal == null)
                    throw new NotSupportedException(SR.NotSupported_KeyAlgorithm);

                return new ECDiffieHellmanImplementation.ECDiffieHellmanAndroid(DecodeECPublicKey(certificatePal));
            }

            public AsymmetricAlgorithm DecodePublicKey(Oid oid, byte[] encodedKeyValue, byte[] encodedParameters,
                ICertificatePal? certificatePal)
            {
                switch (oid.Value)
                {
                    case Oids.Dsa:
                        throw new NotImplementedException($"{nameof(DecodePublicKey)} (DSA)");
                    case Oids.Rsa:
                        if (certificatePal != null)
                        {
                            var handle = new SafeRsaHandle(GetPublicKey(certificatePal, Interop.AndroidCrypto.PAL_KeyAlgorithm.RSA));
                            return new RSAImplementation.RSAAndroid(handle);
                        }
                        else
                        {
                            return DecodeRsaPublicKey(encodedKeyValue);
                        }
                    default:
                        throw new NotSupportedException(SR.NotSupported_KeyAlgorithm);
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
                if (rawData == null || rawData.Length == 0)
                    throw new CryptographicException();

                X509ContentType contentType = Interop.AndroidCrypto.X509GetContentType(rawData);
                if (contentType != X509ContentType.Unknown)
                {
                    return contentType;
                }

                if (AndroidPkcs12Reader.IsPkcs12(rawData))
                {
                    return X509ContentType.Pkcs12;
                }

                // Throw on unknown type to match Unix and Windows
                throw new CryptographicException();
            }

            public X509ContentType GetCertContentType(string fileName)
            {
                return GetCertContentType(File.ReadAllBytes(fileName));
            }

            private static SafeEcKeyHandle DecodeECPublicKey(ICertificatePal pal)
            {
                return new SafeEcKeyHandle(GetPublicKey(pal, Interop.AndroidCrypto.PAL_KeyAlgorithm.EC));
            }

            private static IntPtr GetPublicKey(ICertificatePal pal, Interop.AndroidCrypto.PAL_KeyAlgorithm algorithm)
            {
                AndroidCertificatePal certPal = (AndroidCertificatePal)pal;
                IntPtr ptr = Interop.AndroidCrypto.X509GetPublicKey(certPal.SafeHandle, algorithm);
                if (ptr == IntPtr.Zero)
                    throw new CryptographicException();

                return ptr;
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
        }
    }
}
