// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Security.Cryptography.Apple;
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

            return LoadX509(data);
        }

        private static partial ICertificatePal LoadCertificatePalFromFile(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            using (FileStream stream = File.OpenRead(path))
            {
                int length = (int)long.Min(int.MaxValue, stream.Length);
                byte[]? rented = null;
                MemoryManager<byte>? manager = null;

                try
                {
                    ReadOnlySpan<byte> span;

                    if (length > MemoryMappedFileCutoff)
                    {
                        manager = MemoryMappedFileMemoryManager.CreateFromFileClamped(stream);
                        span = manager.Memory.Span;
                    }
                    else
                    {
                        rented = CryptoPool.Rent(length);
                        stream.ReadAtLeast(rented, length);
                        span = rented.AsSpan(0, length);
                    }

                    return LoadCertificatePal(span);
                }
                finally
                {
                    (manager as IDisposable)?.Dispose();

                    if (rented is not null)
                    {
                        CryptoPool.Return(rented, length);
                    }
                }
            }
        }

        static partial void ValidatePlatformKeyStorageFlags(X509KeyStorageFlags keyStorageFlags)
        {
            if ((keyStorageFlags & X509KeyStorageFlags.EphemeralKeySet) == X509KeyStorageFlags.EphemeralKeySet)
            {
                throw new PlatformNotSupportedException(SR.Cryptography_X509_NoEphemeralPfx);
            }
        }

        static partial void InitializeImportState(ref ImportState importState, X509KeyStorageFlags keyStorageFlags)
        {
            bool exportable = (keyStorageFlags & X509KeyStorageFlags.Exportable) == X509KeyStorageFlags.Exportable;

            bool persist =
                (keyStorageFlags & X509KeyStorageFlags.PersistKeySet) == X509KeyStorageFlags.PersistKeySet;

            SafeKeychainHandle keychain = persist
                ? Interop.AppleCrypto.SecKeychainCopyDefault()
                : Interop.AppleCrypto.CreateTemporaryKeychain();

            importState.Exportable = exportable;
            importState.Persisted = persist;
            importState.Keychain = keychain;
        }

        private static partial Pkcs12Return FromCertAndKey(CertAndKey certAndKey, ImportState importState)
        {
            AppleCertificatePal pal = (AppleCertificatePal)certAndKey.Cert!;
            SafeSecKeyRefHandle? key = null;

            if (certAndKey.Key is not null)
            {
                key = GetPrivateKey(certAndKey.Key);
                certAndKey.Key.Dispose();
            }

            if (key is not null || importState.Persisted)
            {
                if (key is not null && !importState.Exportable)
                {
                    AppleCertificatePal newPal = AppleCertificatePal.ImportPkcs12NonExportable(
                        pal,
                        key,
                        SafePasswordHandle.InvalidHandle,
                        importState.Keychain);

                    pal.Dispose();
                    pal = newPal;
                }
                else
                {
                    AppleCertificatePal? identity = pal.MoveToKeychain(importState.Keychain, key);

                    if (identity is not null)
                    {
                        pal.Dispose();
                        pal = identity;
                    }
                }
            }

            return new Pkcs12Return(pal);
        }

        private static partial Pkcs12Key? CreateKey(string algorithm, ReadOnlySpan<byte> pkcs8)
        {
            switch (algorithm)
            {
                case Oids.Rsa or Oids.RsaPss:
                    return new AsymmetricAlgorithmPkcs12PrivateKey(
                        pkcs8,
                        static () => new RSAImplementation.RSASecurityTransforms());
                case Oids.EcPublicKey or Oids.EcDiffieHellman:
                    return new AsymmetricAlgorithmPkcs12PrivateKey(
                        pkcs8,
                        static () => new ECDsaImplementation.ECDsaSecurityTransforms());
                default:
                    // No DSA or PQC support on macOS.
                    return null;
            }
        }

        internal static SafeSecKeyRefHandle? GetPrivateKey(Pkcs12Key? key)
        {
            if (key is null)
            {
                return null;
            }

            if (key.Key is RSAImplementation.RSASecurityTransforms rsa)
            {
                byte[] rsaPrivateKey = rsa.ExportRSAPrivateKey();
                using (PinAndClear.Track(rsaPrivateKey))
                {
                    return Interop.AppleCrypto.ImportEphemeralKey(rsaPrivateKey, true);
                }
            }

            if (key.Key is ECDsaImplementation.ECDsaSecurityTransforms ecdsa)
            {
                byte[] ecdsaPrivateKey = ecdsa.ExportECPrivateKey();
                using (PinAndClear.Track(ecdsaPrivateKey))
                {
                    return Interop.AppleCrypto.ImportEphemeralKey(ecdsaPrivateKey, true);
                }
            }

            Debug.Fail("Invalid key implementation");
            return null;
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
            SafeSecCertificateHandle certHandle = Interop.AppleCrypto.X509ImportCertificate(
                data,
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

        private partial struct ImportState
        {
            internal bool Exportable;
            internal bool Persisted;
            internal SafeKeychainHandle Keychain;

            partial void DisposeCore()
            {
                Keychain?.Dispose();
                this = default;
            }
        }
    }
}
