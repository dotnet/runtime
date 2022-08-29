// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed class OpenSslPkcs12Reader : UnixPkcs12Reader
    {
        private OpenSslPkcs12Reader(ReadOnlySpan<byte> data)
        {
            ParsePkcs12(data);
        }

        protected override ICertificatePalCore ReadX509Der(ReadOnlyMemory<byte> data)
        {
            if (OpenSslX509CertificateReader.TryReadX509Der(data.Span, out ICertificatePal? ret))
            {
                return ret;
            }

            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
        }

        public static bool TryRead(ReadOnlySpan<byte> data, [NotNullWhen(true)] out OpenSslPkcs12Reader? pkcs12Reader) =>
            TryRead(data, out pkcs12Reader, out _, captureException: false);

        public static bool TryRead(ReadOnlySpan<byte> data, [NotNullWhen(true)] out OpenSslPkcs12Reader? pkcs12Reader, [NotNullWhen(false)] out Exception? openSslException) =>
            TryRead(data, out pkcs12Reader, out openSslException!, captureException: true);

        protected override AsymmetricAlgorithm LoadKey(ReadOnlyMemory<byte> pkcs8)
        {
            PrivateKeyInfoAsn privateKeyInfo = PrivateKeyInfoAsn.Decode(pkcs8, AsnEncodingRules.BER);
            AsymmetricAlgorithm key;

            switch (privateKeyInfo.PrivateKeyAlgorithm.Algorithm)
            {
                case Oids.Rsa:
                    key = new RSAOpenSsl();
                    break;
                case Oids.Dsa:
                    key = new DSAOpenSsl();
                    break;
                case Oids.EcDiffieHellman:
                case Oids.EcPublicKey:
                    key = new ECDiffieHellmanOpenSsl();
                    break;
                default:
                    throw new CryptographicException(
                        SR.Cryptography_UnknownAlgorithmIdentifier,
                        privateKeyInfo.PrivateKeyAlgorithm.Algorithm);
            }

            key.ImportPkcs8PrivateKey(pkcs8.Span, out int bytesRead);

            if (bytesRead != pkcs8.Length)
            {
                key.Dispose();
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            return key;
        }

        internal static SafeEvpPKeyHandle GetPrivateKey(AsymmetricAlgorithm key)
        {
            if (key is RSAOpenSsl rsa)
            {
                return rsa.DuplicateKeyHandle();
            }

            if (key is DSAOpenSsl dsa)
            {
                return dsa.DuplicateKeyHandle();
            }

            return ((ECDiffieHellmanOpenSsl)key).DuplicateKeyHandle();
        }

        private static bool TryRead(
            ReadOnlySpan<byte> data,
            [NotNullWhen(true)] out OpenSslPkcs12Reader? pkcs12Reader,
            out Exception? openSslException,
            bool captureException)
        {
            openSslException = null;

            try
            {
                pkcs12Reader = new OpenSslPkcs12Reader(data);
                return true;
            }
            catch (CryptographicException e)
            {
                if (captureException)
                {
                    openSslException = e;
                }

                pkcs12Reader = null;
                return false;
            }
        }
    }
}
