// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal sealed class HpkeX25519DiffieHellmanKemAdapter : HpkeManagedKemAdapter
    {
        private X25519DiffieHellman? _x25519;

        internal HpkeX25519DiffieHellmanKemAdapter(HpkeSuite suite) : base(suite)
        {
        }

        internal override void ImportEncapsulationKey(ReadOnlySpan<byte> encapsulationKey)
        {
            Debug.Assert(_x25519 is null);
            _x25519 = X25519DiffieHellman.ImportPublicKey(encapsulationKey);
        }

        internal override void DeriveKeyPair(ReadOnlySpan<byte> ikm)
        {
            Debug.Assert(_x25519 is null);

            Span<byte> privateKey = stackalloc byte[X25519DiffieHellman.PrivateKeySizeInBytes];

            using (CryptoPoolLease prk = CryptoPoolLease.RentConditionally(
                KeyDerivationKdf.Nh, stackalloc byte[PrkStackBufferSize]))
            {
                try
                {
                    LabeledExtract(ReadOnlySpan<byte>.Empty, "dkp_prk"u8, ikm, prk.Span);
                    LabeledExpand(prk.Span, "sk"u8, ReadOnlySpan<byte>.Empty, privateKey);
                    _x25519 = X25519DiffieHellman.ImportPrivateKey(privateKey);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(privateKey);
                }
            }
        }

        internal override void ExportDecapsulationKey(Span<byte> destination)
        {
            Debug.Assert(_x25519 is not null);
            Debug.Assert(destination.Length == Suite.DecapsulationKeySizeInBytes);

            _x25519.ExportPrivateKey(destination);
        }

        public override void Dispose() => _x25519?.Dispose();
    }
}
