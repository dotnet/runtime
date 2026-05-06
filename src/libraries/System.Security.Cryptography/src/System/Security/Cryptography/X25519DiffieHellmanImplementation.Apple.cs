// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography.Apple;

namespace System.Security.Cryptography
{
    internal sealed class X25519DiffieHellmanImplementation : X25519DiffieHellman
    {
        private readonly SafeX25519KeyHandle _key;
        private readonly bool _hasPrivate;

        internal static new bool IsSupported => true;

        private X25519DiffieHellmanImplementation(SafeX25519KeyHandle key, bool hasPrivate)
        {
            _key = key;
            _hasPrivate = hasPrivate;
        }

        protected override void DeriveRawSecretAgreementCore(X25519DiffieHellman otherParty, Span<byte> destination)
        {
            ThrowIfPrivateNeeded();

            if (otherParty is X25519DiffieHellmanImplementation x25519impl)
            {
                Interop.AppleCrypto.X25519DeriveRawSecretAgreement(_key, x25519impl._key, destination);
            }
            else
            {
                Span<byte> publicKeyBuffer = stackalloc byte[PublicKeySizeInBytes];
                otherParty.ExportPublicKey(publicKeyBuffer);

                using (SafeX25519KeyHandle publicKey = Interop.AppleCrypto.X25519ImportPublicKey(publicKeyBuffer))
                {
                    Interop.AppleCrypto.X25519DeriveRawSecretAgreement(_key, publicKey, destination);
                }
            }
        }

        protected override void ExportPrivateKeyCore(Span<byte> destination)
        {
            ThrowIfPrivateNeeded();
            Debug.Assert(destination.Length == PrivateKeySizeInBytes);
            Interop.AppleCrypto.X25519ExportPrivateKey(_key, destination);
        }

        protected override void ExportPublicKeyCore(Span<byte> destination)
        {
            Debug.Assert(destination.Length == PublicKeySizeInBytes);
            Interop.AppleCrypto.X25519ExportPublicKey(_key, destination);
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
            return new X25519DiffieHellmanImplementation(Interop.AppleCrypto.X25519GenerateKey(), hasPrivate: true);
        }

        internal static X25519DiffieHellmanImplementation ImportPrivateKeyImpl(ReadOnlySpan<byte> source)
        {
            return new X25519DiffieHellmanImplementation(
                Interop.AppleCrypto.X25519ImportPrivateKey(source),
                hasPrivate: true);
        }

        internal static X25519DiffieHellmanImplementation ImportPublicKeyImpl(ReadOnlySpan<byte> source)
        {
            return new X25519DiffieHellmanImplementation(
                Interop.AppleCrypto.X25519ImportPublicKey(source),
                hasPrivate: false);
        }

        private void ThrowIfPrivateNeeded()
        {
            if (!_hasPrivate)
                throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
        }
    }
}
