// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Internal.NativeCrypto;
using Microsoft.Win32.SafeHandles;

using NTSTATUS = Interop.BCrypt.NTSTATUS;

namespace System.Security.Cryptography
{
    internal sealed class X25519DiffieHellmanImplementation : X25519DiffieHellman
    {
        private static readonly SafeBCryptAlgorithmHandle? s_algHandle = OpenAlgorithmHandle();

        private readonly SafeBCryptKeyHandle _key;
        private readonly bool _hasPrivate;
        private readonly byte _privatePreservation;
        private readonly byte[]? _originalPublicKey;

        private X25519DiffieHellmanImplementation(SafeBCryptKeyHandle key, bool hasPrivate, byte privatePreservation, byte[]? originalPublicKey = null)
        {
            _key = key;
            _hasPrivate = hasPrivate;
            _privatePreservation = privatePreservation;
            _originalPublicKey = originalPublicKey;
            Debug.Assert(_hasPrivate || _privatePreservation == 0);
            Debug.Assert(!_hasPrivate || _originalPublicKey is null);
        }

        [MemberNotNullWhen(true, nameof(s_algHandle))]
        internal static new bool IsSupported => s_algHandle is not null;

        protected override unsafe void DeriveRawSecretAgreementCore(X25519DiffieHellman otherParty, Span<byte> destination)
        {
            Debug.Assert(destination.Length == SecretAgreementSizeInBytes);
            ThrowIfPrivateNeeded();
            int written;

            if (otherParty is X25519DiffieHellmanImplementation x25519impl)
            {
                using (SafeBCryptSecretHandle secret = Interop.BCrypt.BCryptSecretAgreement(_key, x25519impl._key))
                {
                    Interop.BCrypt.BCryptDeriveKey(
                        secret,
                        BCryptNative.KeyDerivationFunction.Raw,
                        in Unsafe.NullRef<Interop.BCrypt.BCryptBufferDesc>(),
                        destination,
                        out written);
                }
            }
            else
            {
                Span<byte> publicKeyBytes = stackalloc byte[PublicKeySizeInBytes];
                otherParty.ExportPublicKey(publicKeyBytes);

                using (X25519DiffieHellmanImplementation otherPartyImplementation = ImportPublicKeyImpl(publicKeyBytes))
                using (SafeBCryptSecretHandle secret = Interop.BCrypt.BCryptSecretAgreement(_key, otherPartyImplementation._key))
                {
                    Interop.BCrypt.BCryptDeriveKey(
                        secret,
                        BCryptNative.KeyDerivationFunction.Raw,
                        in Unsafe.NullRef<Interop.BCrypt.BCryptBufferDesc>(),
                        destination,
                        out written);
                }
            }

            if (written != SecretAgreementSizeInBytes)
            {
                destination.Clear();
                Debug.Fail($"Unexpected number of bytes written: {written}.");
                throw new CryptographicException();
            }

            // CNG with BCRYPT_NO_KEY_VALIDATION permits low-order public keys, which produce
            // an all-zero shared secret. Other platforms reject these at
            // derive time per RFC 7748 6.1.
            // We still need BCRYPT_NO_KEY_VALIDATION though because there are small subgroup keys that work, which do
            // not produce all zero shared secrets.
            if (CryptographicOperations.FixedTimeEquals(destination, 0))
            {
                throw new CryptographicException();
            }
            else
            {
                // BCryptDeriveKey exports with the wrong endianness.
                destination.Reverse();
            }
        }

        protected override void ExportPrivateKeyCore(Span<byte> destination)
        {
            ExportKey(true, destination);
            X25519WindowsHelpers.RefixPrivateScalar(destination, _privatePreservation);
        }

        protected override void ExportPublicKeyCore(Span<byte> destination)
        {
            if (_originalPublicKey is not null)
            {
                _originalPublicKey.CopyTo(destination);
            }
            else
            {
                ExportKey(false, destination);
            }
        }

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            ThrowIfPrivateNeeded();
            return TryExportPkcs8PrivateKeyImpl(destination, out bytesWritten);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _key.Dispose();
            }

            base.Dispose(disposing);
        }

        internal static X25519DiffieHellmanImplementation GenerateKeyImpl()
        {
            Debug.Assert(IsSupported);
            SafeBCryptKeyHandle key = Interop.BCrypt.BCryptGenerateKeyPair(s_algHandle, 0);
            Debug.Assert(!key.IsInvalid);

            try
            {
                Interop.BCrypt.BCryptFinalizeKeyPair(key);
                return new X25519DiffieHellmanImplementation(key, hasPrivate: true, privatePreservation: 0);
            }
            catch
            {
                key.Dispose();
                throw;
            }
        }

        internal static X25519DiffieHellmanImplementation ImportPrivateKeyImpl(ReadOnlySpan<byte> source)
        {
            SafeBCryptKeyHandle key = ImportKey(true, source, out byte preservation);
            Debug.Assert(!key.IsInvalid);
            return new X25519DiffieHellmanImplementation(key, hasPrivate: true, privatePreservation: preservation);
        }

        internal static unsafe X25519DiffieHellmanImplementation ImportPublicKeyImpl(ReadOnlySpan<byte> source)
        {
            Span<byte> reducedPublicKey = stackalloc byte[PublicKeySizeInBytes];
            bool requiredReduction = X25519WindowsHelpers.ReducePublicKey(source, reducedPublicKey);

            SafeBCryptKeyHandle key = ImportKey(false, reducedPublicKey, out _);

            Debug.Assert(!key.IsInvalid);
            return new X25519DiffieHellmanImplementation(
                key,
                hasPrivate: false,
                privatePreservation: 0,
                requiredReduction ? source.ToArray() : null);
        }

        private void ExportKey(bool privateKey, Span<byte> destination)
        {
            string blobType = privateKey ?
                Interop.BCrypt.KeyBlobType.BCRYPT_ECCPRIVATE_BLOB :
                Interop.BCrypt.KeyBlobType.BCRYPT_ECCPUBLIC_BLOB;

            ArraySegment<byte> key = Interop.BCrypt.BCryptExportKey(_key, blobType);

            try
            {
                X25519WindowsHelpers.ExportKey(key, privateKey, destination);
            }
            finally
            {
                if (privateKey)
                {
                    CryptoPool.Return(key);
                }
                else
                {
                    CryptoPool.Return(key, clearSize: 0);
                }
            }
        }

        private static SafeBCryptKeyHandle ImportKey(bool privateKey, ReadOnlySpan<byte> key, out byte preservation)
        {
            Debug.Assert(IsSupported);
            string blobType = privateKey ?
                Interop.BCrypt.KeyBlobType.BCRYPT_ECCPRIVATE_BLOB :
                Interop.BCrypt.KeyBlobType.BCRYPT_ECCPUBLIC_BLOB;

            using (CryptoPoolLease lease = X25519WindowsHelpers.CreateCngBlob(key, privateKey, out preservation))
            {

                return Interop.BCrypt.BCryptImportKeyPair(
                    s_algHandle,
                    blobType,
                    lease.Span,
                    Interop.BCrypt.BCryptImportKeyPairFlags.BCRYPT_NO_KEY_VALIDATION);
            }
        }

        private static SafeBCryptAlgorithmHandle? OpenAlgorithmHandle()
        {
            NTSTATUS status = Interop.BCrypt.BCryptOpenAlgorithmProvider(
                out SafeBCryptAlgorithmHandle hAlgorithm,
                BCryptNative.AlgorithmName.ECDH,
                pszImplementation: null,
                Interop.BCrypt.BCryptOpenAlgorithmProviderFlags.None);

            if (status != NTSTATUS.STATUS_SUCCESS)
            {
                hAlgorithm.Dispose();
                return null;
            }

            unsafe
            {
                fixed (char* pbInput = X25519WindowsHelpers.BCRYPT_ECC_CURVE_25519)
                {
                    status = Interop.BCrypt.BCryptSetProperty(
                        hAlgorithm,
                        KeyPropertyName.ECCCurveName,
                        pbInput,
                        ((uint)X25519WindowsHelpers.BCRYPT_ECC_CURVE_25519.Length + 1) * 2,
                        0);
                }
            }

            if (status != NTSTATUS.STATUS_SUCCESS)
            {
                hAlgorithm.Dispose();
                return null;
            }

            return hAlgorithm;
        }

        private void ThrowIfPrivateNeeded()
        {
            if (!_hasPrivate)
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
        }
    }
}
