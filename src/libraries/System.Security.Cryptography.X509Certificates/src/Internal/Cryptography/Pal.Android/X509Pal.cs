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
                throw new NotImplementedException(nameof(DecodeECDsaPublicKey));
            }

            public ECDiffieHellman DecodeECDiffieHellmanPublicKey(ICertificatePal? certificatePal)
            {
                throw new NotImplementedException(nameof(DecodeECDiffieHellmanPublicKey));
            }

            public AsymmetricAlgorithm DecodePublicKey(Oid oid, byte[] encodedKeyValue, byte[] encodedParameters,
                ICertificatePal? certificatePal)
            {
                throw new NotImplementedException(nameof(DecodePublicKey));
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

                // TODO: [AndroidCrypto] Handle PKCS#12
                if (contentType == X509ContentType.Unknown)
                    throw new CryptographicException();

                return contentType;
            }

            public X509ContentType GetCertContentType(string fileName)
            {
                return GetCertContentType(File.ReadAllBytes(fileName));
            }
        }
    }
}
