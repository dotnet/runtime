// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.IO;

namespace System.Security.Cryptography.X509Certificates
{
    public static partial class X509CertificateLoader
    {
        private static partial ICertificatePal LoadCertificatePal(ReadOnlySpan<byte> data)
        {
            if (!AndroidCertificatePal.TryReadX509(data, out ICertificatePal? cert))
            {
                cert?.Dispose();
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            return cert;
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
            if ((keyStorageFlags & X509KeyStorageFlags.PersistKeySet) == X509KeyStorageFlags.PersistKeySet)
            {
                throw new PlatformNotSupportedException(SR.Cryptography_X509_PKCS12_PersistKeySetNotSupported);
            }
        }

        private static partial Pkcs12Return FromCertAndKey(CertAndKey certAndKey, ImportState importState)
        {
            AndroidCertificatePal pal = (AndroidCertificatePal)certAndKey.Cert!;

            if (certAndKey.Key != null)
            {
                pal.SetPrivateKey(GetPrivateKey(certAndKey.Key));
                certAndKey.Key.Dispose();
            }

            return new Pkcs12Return(pal);
        }

        private static partial AsymmetricAlgorithm? CreateKey(string algorithm)
        {
            return algorithm switch
            {
                Oids.Rsa or Oids.RsaPss => new RSAImplementation.RSAAndroid(),
                Oids.EcPublicKey or Oids.EcDiffieHellman => new ECDsaImplementation.ECDsaAndroid(),
                Oids.Dsa => new DSAImplementation.DSAAndroid(),
                _ => null,
            };
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

        private static partial ICertificatePalCore LoadX509Der(ReadOnlyMemory<byte> data)
        {
            ReadOnlySpan<byte> span = data.Span;

            AsnValueReader reader = new AsnValueReader(span, AsnEncodingRules.DER);
            reader.ReadSequence();
            reader.ThrowIfNotEmpty();

            if (!AndroidCertificatePal.TryReadX509(span, out ICertificatePal? cert))
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            return cert;
        }
    }
}
