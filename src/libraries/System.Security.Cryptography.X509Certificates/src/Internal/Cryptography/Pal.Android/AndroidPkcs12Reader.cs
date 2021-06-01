// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed class AndroidPkcs12Reader : UnixPkcs12Reader
    {
        internal AndroidPkcs12Reader(ReadOnlySpan<byte> data)
        {
            ParsePkcs12(data);
        }

        public static bool IsPkcs12(ReadOnlySpan<byte> data)
        {
            try
            {
                using (var reader = new AndroidPkcs12Reader(data))
                {
                    return true;
                }
            }
            catch (CryptographicException)
            {
            }

            return false;
        }

        protected override ICertificatePalCore ReadX509Der(ReadOnlyMemory<byte> data)
        {
            ICertificatePal? cert;
            if (!AndroidCertificatePal.TryReadX509(data.Span, out cert))
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);

            return cert;
        }

        protected override AsymmetricAlgorithm LoadKey(ReadOnlyMemory<byte> pkcs8)
        {
            PrivateKeyInfoAsn privateKeyInfo = PrivateKeyInfoAsn.Decode(pkcs8, AsnEncodingRules.BER);
            AsymmetricAlgorithm key;

            string algorithm = privateKeyInfo.PrivateKeyAlgorithm.Algorithm;
            switch (algorithm)
            {
                case Oids.Rsa:
                    key = new RSAImplementation.RSAAndroid();
                    break;
                case Oids.Dsa:
                    key = new DSAImplementation.DSAAndroid();
                    break;
                case Oids.EcDiffieHellman:
                case Oids.EcPublicKey:
                    key = new ECDsaImplementation.ECDsaAndroid();
                    break;
                default:
                    throw new CryptographicException(SR.Cryptography_UnknownAlgorithmIdentifier, algorithm);
            }

            key.ImportPkcs8PrivateKey(pkcs8.Span, out int bytesRead);
            if (bytesRead != pkcs8.Length)
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);

            return key;
        }

        internal static SafeKeyHandle GetPrivateKey(AsymmetricAlgorithm key)
        {
            if (key is ECDsaImplementation.ECDsaAndroid ecdsa)
            {
                return ecdsa.DuplicateKeyHandle();
            }

            if (key is RSAImplementation.RSAAndroid rsa)
            {
                return rsa.DuplicateKeyHandle();
            }

            if (key is DSAImplementation.DSAAndroid dsa)
            {
                return dsa.DuplicateKeyHandle();
            }

            throw new NotImplementedException($"{nameof(GetPrivateKey)} ({key.GetType()})");
        }
    }
}
