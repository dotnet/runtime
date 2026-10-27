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

        internal override void ImportDecapsulationKey(ReadOnlySpan<byte> decapsulationKey)
        {
            Debug.Assert(_x25519 is null);
            _x25519 = X25519DiffieHellman.ImportPrivateKey(decapsulationKey);
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
            Span<byte> prkBuffer = stackalloc byte[PrkStackBufferSize];

            try
            {
                Span<byte> prk = prkBuffer.Slice(0, KeyDerivationKdf.Nh);
                LabeledExtract(ReadOnlySpan<byte>.Empty, "dkp_prk"u8, ikm, prk);
                LabeledExpand(prk, "sk"u8, ReadOnlySpan<byte>.Empty, privateKey);
                _x25519 = X25519DiffieHellman.ImportPrivateKey(privateKey);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(privateKey);
                CryptographicOperations.ZeroMemory(prkBuffer);
            }
        }

        internal override void Encapsulate(Span<byte> encapsulatedSecret, Span<byte> sharedSecret)
        {
            Debug.Assert(_x25519 is not null);

            using (HpkeX25519DiffieHellmanKemAdapter ephemeral = new HpkeX25519DiffieHellmanKemAdapter(Suite))
            {
                ephemeral.Generate();
                Debug.Assert(ephemeral._x25519 is not null);

                Span<byte> dh = stackalloc byte[X25519DiffieHellman.SecretAgreementSizeInBytes];

                try
                {
                    ephemeral._x25519.DeriveRawSecretAgreement(_x25519, dh);
                    ephemeral.ExportEncapsulationKey(encapsulatedSecret);
                    ExtractAndExpand(dh, encapsulatedSecret, sharedSecret);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(dh);
                }
            }
        }

        // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-hpke-04#section-4.5
        internal override void Decapsulate(ReadOnlySpan<byte> encapsulatedSecret, Span<byte> sharedSecret)
        {
            Debug.Assert(_x25519 is not null);
            Span<byte> secretAgreement = stackalloc byte[X25519DiffieHellman.SecretAgreementSizeInBytes];

            try
            {
                _x25519.DeriveRawSecretAgreement(encapsulatedSecret, secretAgreement);
                ExtractAndExpand(secretAgreement, encapsulatedSecret, sharedSecret);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(secretAgreement);
            }
        }

        internal override void ExportDecapsulationKey(Span<byte> destination)
        {
            Debug.Assert(_x25519 is not null);
            Debug.Assert(destination.Length == Suite.DecapsulationKeySizeInBytes);

            _x25519.ExportPrivateKey(destination);
        }

        internal override void ExportEncapsulationKey(Span<byte> destination)
        {
            Debug.Assert(_x25519 is not null);
            Debug.Assert(destination.Length == Suite.EncapsulationKeySizeInBytes);

            _x25519.ExportPublicKey(destination);
        }

        public override void Dispose() => _x25519?.Dispose();
    }
}
