// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed class ApplePkcs12Reader : UnixPkcs12Reader
    {
        internal ApplePkcs12Reader(ReadOnlySpan<byte> data)
        {
            ParsePkcs12(data);
        }

        protected override ICertificatePalCore ReadX509Der(ReadOnlyMemory<byte> data)
        {
            SafeSecCertificateHandle certHandle = Interop.AppleCrypto.X509ImportCertificate(
                data.Span,
                X509ContentType.Cert,
                SafePasswordHandle.InvalidHandle,
                SafeTemporaryKeychainHandle.InvalidHandle,
                exportable: true,
                out SafeSecIdentityHandle identityHandle);

            if (identityHandle.IsInvalid)
            {
                identityHandle.Dispose();
                return new AppleCertificatePal(certHandle);
            }

            Debug.Fail("Non-PKCS12 import produced an identity handle");

            identityHandle.Dispose();
            certHandle.Dispose();
            throw new CryptographicException();
        }

        protected override AsymmetricAlgorithm LoadKey(ReadOnlyMemory<byte> pkcs8)
        {
            PrivateKeyInfoAsn privateKeyInfo = PrivateKeyInfoAsn.Decode(pkcs8, AsnEncodingRules.BER);
            AsymmetricAlgorithm key;

            switch (privateKeyInfo.PrivateKeyAlgorithm.Algorithm)
            {
                case Oids.Rsa:
                    key = new RSAImplementation.RSASecurityTransforms();
                    break;
                case Oids.Dsa:
                    key = new DSAImplementation.DSASecurityTransforms();
                    break;
                case Oids.EcDiffieHellman:
                case Oids.EcPublicKey:
                    key = new ECDsaImplementation.ECDsaSecurityTransforms();
                    break;
                default:
                    throw new CryptographicException(
                        SR.Cryptography_UnknownAlgorithmIdentifier,
                        privateKeyInfo.PrivateKeyAlgorithm.Algorithm);
            }

            key.ImportPkcs8PrivateKey(pkcs8.Span, out int bytesRead);

            if (bytesRead != pkcs8.Length)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            return key;
        }

        internal static SafeSecKeyRefHandle? GetPrivateKey(AsymmetricAlgorithm? key)
        {
            if (key == null)
            {
                return null;
            }

            if (key is RSAImplementation.RSASecurityTransforms rsa)
            {
                // Convert data key to legacy CSSM key that can be imported into keychain
                byte[] rsaPrivateKey = rsa.ExportRSAPrivateKey();
                using (PinAndClear.Track(rsaPrivateKey))
                {
                    return Interop.AppleCrypto.ImportEphemeralKey(rsaPrivateKey, true);
                }
            }

            if (key is DSAImplementation.DSASecurityTransforms dsa)
            {
                // DSA always uses legacy CSSM keys do no need to convert
                return dsa.GetKeys().PrivateKey;
            }

            if (key is ECDsaImplementation.ECDsaSecurityTransforms ecdsa)
            {
                // Convert data key to legacy CSSM key that can be imported into keychain
                byte[] ecdsaPrivateKey = ecdsa.ExportECPrivateKey();
                using (PinAndClear.Track(ecdsaPrivateKey))
                {
                    return Interop.AppleCrypto.ImportEphemeralKey(ecdsaPrivateKey, true);
                }
            }

            Debug.Fail("Invalid key implementation");
            return null;
        }
    }
}
