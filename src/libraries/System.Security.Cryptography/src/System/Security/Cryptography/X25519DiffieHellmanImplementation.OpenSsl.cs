// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal sealed class X25519DiffieHellmanImplementation : X25519DiffieHellman
    {
        private readonly SafeEvpPKeyHandle _key;
        private readonly bool _hasPrivate;

        internal static new bool IsSupported { get; } = Interop.Crypto.X25519Available();
        internal SafeEvpPKeyHandle Key => _key;

        private X25519DiffieHellmanImplementation(SafeEvpPKeyHandle key, bool hasPrivate)
        {
            _key = key;
            _hasPrivate = hasPrivate;
        }

        protected override void DeriveRawSecretAgreementCore(X25519DiffieHellman otherParty, Span<byte> destination)
        {
            Debug.Assert(destination.Length == SecretAgreementSizeInBytes);
            ThrowIfPrivateNeeded();

            int written;

            if (otherParty is X25519DiffieHellmanImplementation x25519Impl)
            {
                written = Interop.Crypto.EvpPKeyDeriveSecretAgreement(_key, x25519Impl._key, destination);
            }
            else
            {
                Span<byte> publicKey = stackalloc byte[PublicKeySizeInBytes];
                otherParty.ExportPublicKey(publicKey);

                using (SafeEvpPKeyHandle peerKeyHandle = Interop.Crypto.X25519ImportPublicKey(publicKey))
                {
                    written = Interop.Crypto.EvpPKeyDeriveSecretAgreement(_key, peerKeyHandle, destination);
                }
            }

            if (written != SecretAgreementSizeInBytes)
            {
                Debug.Fail($"{nameof(Interop.Crypto.EvpPKeyDeriveSecretAgreement)} wrote an unexpected number of bytes: {written}.");
                throw new CryptographicException();
            }
        }

        protected override void ExportPrivateKeyCore(Span<byte> destination)
        {
            Debug.Assert(destination.Length == PrivateKeySizeInBytes);
            ThrowIfPrivateNeeded();
            Interop.Crypto.X25519ExportPrivateKey(_key, destination);
        }

        protected override void ExportPublicKeyCore(Span<byte> destination)
        {
            Debug.Assert(destination.Length == PublicKeySizeInBytes);
            Interop.Crypto.X25519ExportPublicKey(_key, destination);
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
            SafeEvpPKeyHandle key = Interop.Crypto.X25519GenerateKey();
            Debug.Assert(!key.IsInvalid);
            return new X25519DiffieHellmanImplementation(key, hasPrivate: true);
        }

        internal static X25519DiffieHellmanImplementation ImportPrivateKeyImpl(ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsSupported);
            SafeEvpPKeyHandle key = Interop.Crypto.X25519ImportPrivateKey(source);
            Debug.Assert(!key.IsInvalid);
            return new X25519DiffieHellmanImplementation(key, hasPrivate: true);
        }

        internal static X25519DiffieHellmanImplementation ImportPublicKeyImpl(ReadOnlySpan<byte> source)
        {
            Debug.Assert(IsSupported);
            SafeEvpPKeyHandle key = Interop.Crypto.X25519ImportPublicKey(source);
            Debug.Assert(!key.IsInvalid);
            return new X25519DiffieHellmanImplementation(key, hasPrivate: false);
        }

        private void ThrowIfPrivateNeeded()
        {
            if (!_hasPrivate)
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
        }
    }
}
