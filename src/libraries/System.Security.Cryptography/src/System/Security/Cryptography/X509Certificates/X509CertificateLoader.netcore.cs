// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace System.Security.Cryptography.X509Certificates
{
    public static partial class X509CertificateLoader
    {
        public static partial X509Certificate2 LoadCertificate(byte[] data)
        {
            ThrowIfNull(data);

            return LoadCertificate(new ReadOnlySpan<byte>(data));
        }

        public static partial X509Certificate2 LoadCertificate(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                ThrowWithHResult(SR.Cryptography_Der_Invalid_Encoding, CRYPT_E_BAD_DECODE);
            }

            ICertificatePal pal = LoadCertificatePal(data);
            Debug.Assert(pal is not null);
            return new X509Certificate2(pal);
        }

        public static partial X509Certificate2 LoadCertificateFromFile(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            ICertificatePal pal = LoadCertificatePalFromFile(path);
            Debug.Assert(pal is not null);
            return new X509Certificate2(pal);
        }

        private static partial ICertificatePal LoadCertificatePal(ReadOnlySpan<byte> data);
        private static partial ICertificatePal LoadCertificatePalFromFile(string path);

        internal static ICertificatePal LoadPkcs12Pal(
            ReadOnlySpan<byte> data,
            ReadOnlySpan<char> password,
            X509KeyStorageFlags keyStorageFlags,
            Pkcs12LoaderLimits loaderLimits)
        {
            Debug.Assert(loaderLimits is not null);

            unsafe
            {
                fixed (byte* pinned = data)
                {
                    using (PointerMemoryManager<byte> manager = new(pinned, data.Length))
                    {
                        return LoadPkcs12(
                            manager.Memory,
                            password,
                            keyStorageFlags,
                            loaderLimits).GetPal();
                    }
                }
            }
        }

        internal static ICertificatePal LoadPkcs12PalFromFile(
            string path,
            ReadOnlySpan<char> password,
            X509KeyStorageFlags keyStorageFlags,
            Pkcs12LoaderLimits loaderLimits)
        {
            Debug.Assert(loaderLimits is not null);

            ThrowIfNullOrEmpty(path);

            return LoadFromFile(
                path,
                password,
                keyStorageFlags,
                loaderLimits,
                LoadPkcs12).GetPal();
        }

        private const X509KeyStorageFlags KeyStorageFlagsAll =
            X509KeyStorageFlags.UserKeySet |
            X509KeyStorageFlags.MachineKeySet |
            X509KeyStorageFlags.Exportable |
            X509KeyStorageFlags.UserProtected |
            X509KeyStorageFlags.PersistKeySet |
            X509KeyStorageFlags.EphemeralKeySet;

        internal static void ValidateKeyStorageFlags(X509KeyStorageFlags keyStorageFlags)
        {
            ValidateKeyStorageFlagsCore(keyStorageFlags);
        }

        static partial void ValidateKeyStorageFlagsCore(X509KeyStorageFlags keyStorageFlags)
        {
            if ((keyStorageFlags & ~KeyStorageFlagsAll) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(keyStorageFlags));
            }

            const X509KeyStorageFlags EphemeralPersist =
                X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.PersistKeySet;

            X509KeyStorageFlags persistenceFlags = keyStorageFlags & EphemeralPersist;

            if (persistenceFlags == EphemeralPersist)
            {
                throw new ArgumentException(
                    SR.Format(SR.Cryptography_X509_InvalidFlagCombination, persistenceFlags),
                    nameof(keyStorageFlags));
            }

            ValidatePlatformKeyStorageFlags(keyStorageFlags);
        }

        static partial void ValidatePlatformKeyStorageFlags(X509KeyStorageFlags keyStorageFlags);

        private readonly partial struct Pkcs12Return
        {
            private readonly ICertificatePal? _pal;

            internal Pkcs12Return(ICertificatePal pal)
            {
                _pal = pal;
            }

            internal ICertificatePal GetPal()
            {
                Debug.Assert(_pal is not null);
                return _pal;
            }

            internal partial bool HasValue() => _pal is not null;

            internal partial X509Certificate2 ToCertificate()
            {
                Debug.Assert(_pal is not null);

                return new X509Certificate2(_pal);
            }
        }
    }
}
