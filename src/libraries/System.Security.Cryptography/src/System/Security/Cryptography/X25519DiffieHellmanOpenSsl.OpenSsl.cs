// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    public sealed partial class X25519DiffieHellmanOpenSsl
    {
        private readonly SafeEvpPKeyHandle _key;
        private readonly bool _hasExportablePrivate;

        public partial X25519DiffieHellmanOpenSsl(SafeEvpPKeyHandle pkeyHandle)
        {
            ArgumentNullException.ThrowIfNull(pkeyHandle);

            if (pkeyHandle.IsInvalid)
            {
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(pkeyHandle));
            }

            _key = pkeyHandle.DuplicateHandle();
            bool isValid = Interop.Crypto.X25519IsValidHandle(_key, out _hasExportablePrivate);

            if (!isValid)
            {
                _key.Dispose();
                throw new CryptographicException(SR.Cryptography_X25519InvalidAlgorithmHandle);
            }
        }

        public partial SafeEvpPKeyHandle DuplicateKeyHandle()
        {
            ThrowIfDisposed();
            return _key.DuplicateHandle();
        }

        protected override unsafe void DeriveRawSecretAgreementCore(X25519DiffieHellman otherParty, Span<byte> destination)
        {
            Debug.Assert(destination.Length == SecretAgreementSizeInBytes);

            int written;

            if (otherParty is X25519DiffieHellmanOpenSsl x25519OpenSsl)
            {
                written = Interop.Crypto.EvpPKeyDeriveSecretAgreement(_key, x25519OpenSsl._key, destination);
            }
            else if (otherParty is X25519DiffieHellmanImplementation x25519Impl)
            {
                written = Interop.Crypto.EvpPKeyDeriveSecretAgreement(_key, x25519Impl.Key, destination);
            }
            else
            {
                Span<byte> publicKey = stackalloc byte[PublicKeySizeInBytes];
                otherParty.ExportPublicKey(publicKey);
                written = Interop.Crypto.X25519DeriveSecretAgreementWithBytes(_key, publicKey, destination);
            }

            if (written != SecretAgreementSizeInBytes)
            {
                Debug.Fail($"{nameof(Interop.Crypto.EvpPKeyDeriveSecretAgreement)} wrote an unexpected number of bytes: {written}.");
                throw new CryptographicException();
            }
        }

        protected override void DeriveRawSecretAgreementCore(ReadOnlySpan<byte> otherPartyPublicKey, Span<byte> destination)
        {
            Debug.Assert(otherPartyPublicKey.Length == PublicKeySizeInBytes);
            Debug.Assert(destination.Length == SecretAgreementSizeInBytes);

            int written = Interop.Crypto.X25519DeriveSecretAgreementWithBytes(_key, otherPartyPublicKey, destination);

            if (written != SecretAgreementSizeInBytes)
            {
                Debug.Fail($"{nameof(Interop.Crypto.X25519DeriveSecretAgreementWithBytes)} wrote an unexpected number of bytes: {written}.");
                throw new CryptographicException();
            }
        }

        protected override void ExportPrivateKeyCore(Span<byte> destination)
        {
            Debug.Assert(destination.Length == PrivateKeySizeInBytes);
            ThrowIfExportablePrivateNeeded();
            Interop.Crypto.X25519ExportPrivateKey(_key, destination);
        }

        protected override void ExportPublicKeyCore(Span<byte> destination)
        {
            Debug.Assert(destination.Length == PublicKeySizeInBytes);
            Interop.Crypto.X25519ExportPublicKey(_key, destination);
        }

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            ThrowIfExportablePrivateNeeded();
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

        private void ThrowIfExportablePrivateNeeded()
        {
            if (!_hasExportablePrivate)
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
        }
    }
}
