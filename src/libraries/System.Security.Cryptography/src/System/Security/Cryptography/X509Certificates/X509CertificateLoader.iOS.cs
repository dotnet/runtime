// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    public static partial class X509CertificateLoader
    {
        private static partial ICertificatePal LoadCertificatePal(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            if (X509Certificate2.GetCertContentType(data) != X509ContentType.Cert)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            ICertificatePal? result = null;

            // If the data starts with 0x30, only try the DER loader.
            // Otherwise, try PEM.
            // If it's not PEM and not 0x30, still call the DER loader to get the system error.
            if (data[0] != 0x30)
            {
                AppleCertificatePal.TryDecodePem(
                    data,
                    (derData, contentType) =>
                    {
                        if (contentType != X509ContentType.Cert)
                        {
                            // true: keep looking
                            return true;
                        }

                        result = LoadX509(derData);
                        return false;
                    });
            }

            return result ?? LoadX509(data);
        }

        private static partial ICertificatePal LoadCertificatePalFromFile(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            using (FileStream stream = File.OpenRead(path))
            {
                int length = (int)long.Min(int.MaxValue, stream.Length);
                byte[] buf = CryptoPool.Rent(length);

                try
                {
                    stream.ReadAtLeast(buf, length);
                    return LoadCertificatePal(buf.AsSpan(0, length));
                }
                finally
                {
                    CryptoPool.Return(buf, length);
                }
            }
        }

        static partial void ValidatePlatformKeyStorageFlags(X509KeyStorageFlags keyStorageFlags)
        {
            // Unlike macOS, iOS does support EphemeralKeySet.

            if ((keyStorageFlags & X509KeyStorageFlags.Exportable) == X509KeyStorageFlags.Exportable)
            {
                throw new PlatformNotSupportedException(SR.Cryptography_X509_PKCS12_ExportableNotSupported);
            }

            if ((keyStorageFlags & X509KeyStorageFlags.PersistKeySet) == X509KeyStorageFlags.PersistKeySet)
            {
                throw new PlatformNotSupportedException(SR.Cryptography_X509_PKCS12_PersistKeySetNotSupported);
            }
        }

        private static partial Pkcs12Return FromCertAndKey(CertAndKey certAndKey, ImportState importState)
        {
            AppleCertificatePal pal = (AppleCertificatePal)certAndKey.Cert!;

            if (certAndKey.Key is not null)
            {
                AppleCertificatePal newPal = AppleCertificatePal.ImportPkcs12(pal, certAndKey.Key);
                pal.Dispose();
                pal = newPal;
            }

            return new Pkcs12Return(pal);
        }

        private static partial AsymmetricAlgorithm? CreateKey(string algorithm)
        {
            return algorithm switch
            {
                Oids.Rsa or Oids.RsaPss => new RSAImplementation.RSASecurityTransforms(),
                Oids.EcPublicKey or Oids.EcDiffieHellman => new ECDsaImplementation.ECDsaSecurityTransforms(),
                // There's no DSA support on iOS/tvOS.
                _ => null,
            };
        }

        private static partial ICertificatePalCore LoadX509Der(ReadOnlyMemory<byte> data)
        {
            ReadOnlySpan<byte> span = data.Span;

            AsnValueReader reader = new AsnValueReader(span, AsnEncodingRules.DER);
            reader.ReadSequence();
            reader.ThrowIfNotEmpty();

            return LoadX509(span);
        }

        private static AppleCertificatePal LoadX509(ReadOnlySpan<byte> data)
        {
            SafeSecIdentityHandle identityHandle;
            SafeSecCertificateHandle certHandle = Interop.AppleCrypto.X509ImportCertificate(
                data,
                X509ContentType.Cert,
                SafePasswordHandle.InvalidHandle,
                out identityHandle);

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
    }
}
