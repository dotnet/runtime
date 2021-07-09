// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
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
            public AsymmetricAlgorithm DecodePublicKey(Oid oid, byte[] encodedKeyValue, byte[] encodedParameters,
                ICertificatePal? certificatePal)
            {
                if (oid.Value != Oids.Rsa)
                {
                    throw new NotSupportedException(SR.NotSupported_KeyAlgorithm);
                }

                if (certificatePal is AppleCertificatePal applePal)
                {
                    SafeSecKeyRefHandle key = Interop.AppleCrypto.X509GetPublicKey(applePal.CertificateHandle);
                    Debug.Assert(!key.IsInvalid);
                    return new RSAImplementation.RSASecurityTransforms(key);
                }
                else
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

            public X509ContentType GetCertContentType(ReadOnlySpan<byte> rawData)
            {
                const int errSecUnknownFormat = -25257;

                if (rawData == null || rawData.Length == 0)
                {
                    // Throw to match Windows and Unix behavior.
                    throw Interop.AppleCrypto.CreateExceptionForOSStatus(errSecUnknownFormat);
                }

                X509ContentType result = X509ContentType.Unknown;

                AppleCertificatePal.TryDecodePem(
                    rawData,
                    (derData, contentType) =>
                    {
                        result = contentType;
                        return false;
                    });

                if (result == X509ContentType.Unknown)
                {
                    result = AppleCertificatePal.GetDerCertContentType(rawData);
                }

                if (result == X509ContentType.Unknown)
                {
                    // Throw to match Windows and Unix behavior.
                    throw Interop.AppleCrypto.CreateExceptionForOSStatus(errSecUnknownFormat);
                }

                return result;
            }

            public X509ContentType GetCertContentType(string fileName)
            {
                return GetCertContentType(System.IO.File.ReadAllBytes(fileName));
            }
        }
    }
}
