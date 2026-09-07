// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Security.Cryptography
{
    internal sealed class HpkeECDiffieHellmanKemAdapter : HpkeManagedKemAdapter
    {
        private readonly byte _candidateBitmask;
        private readonly ECCurve _curve;
        private ECDiffieHellman? _ecdh;

        // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-hpke-04#section-7.1.3
        private static ReadOnlySpan<byte> P256Order =>
        [
            0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xBC, 0xE6, 0xFA, 0xAD, 0xA7, 0x17, 0x9E, 0x84,
            0xF3, 0xB9, 0xCA, 0xC2, 0xFC, 0x63, 0x25, 0x51,
        ];

        private static ReadOnlySpan<byte> P384Order =>
        [
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xC7, 0x63, 0x4D, 0x81, 0xF4, 0x37, 0x2D, 0xDF,
            0x58, 0x1A, 0x0D, 0xB2, 0x48, 0xB0, 0xA7, 0x7A,
            0xEC, 0xEC, 0x19, 0x6A, 0xCC, 0xC5, 0x29, 0x73,
        ];

        private ReadOnlySpan<byte> Order => Suite.KemAlgorithm switch
        {
            HpkeKem.DHKEM_P256_HKDF_SHA256 => P256Order,
            HpkeKem.DHKEM_P384_HKDF_SHA384 => P384Order,
            _ => throw new UnreachableException(),
        };

        internal HpkeECDiffieHellmanKemAdapter(HpkeSuite suite) : base(suite)
        {
            (_curve, _candidateBitmask) = suite.KemAlgorithm switch
            {
                HpkeKem.DHKEM_P256_HKDF_SHA256 => (ECCurve.NamedCurves.nistP256, byte.MaxValue),
                HpkeKem.DHKEM_P384_HKDF_SHA384 => (ECCurve.NamedCurves.nistP384, byte.MaxValue),
                _ => throw new UnreachableException(),
            };
        }

        internal override void ImportEncapsulationKey(ReadOnlySpan<byte> encapsulationKey)
        {
            Debug.Assert(_ecdh is null);

            if (encapsulationKey.Length != Suite.EncapsulationKeySizeInBytes)
            {
                throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
            }

            // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-hpke-04#section-7.1.1
            AsymmetricAlgorithmHelpers.DecodeFromUncompressedAnsiX963Key(
                encapsulationKey,
                hasPrivateKey: false,
                out ECParameters parameters);

#pragma warning disable CA1416 // Not supported on browser
            parameters.Curve = _curve;
            _ecdh = ECDiffieHellman.Create(parameters);
#pragma warning restore CA1416 // Not supported on browser
        }

        internal override void DeriveKeyPair(ReadOnlySpan<byte> ikm)
        {
            Debug.Assert(_ecdh is null);

            ReadOnlySpan<byte> order = Order;
            Debug.Assert(order.Length == Suite.KemMetadata.Nsk);
            byte[] privateKey = new byte[Suite.KemMetadata.Nsk];

            using (PinAndClear.Track(privateKey))
            using (CryptoPoolLease prk = CryptoPoolLease.RentConditionally(
                KeyDerivationKdf.Nh, stackalloc byte[PrkStackBufferSize]))
            {
                LabeledExtract(ReadOnlySpan<byte>.Empty, "dkp_prk"u8, ikm, prk.Span);
                Span<byte> counterBytes = stackalloc byte[1];

                for (int counter = 0; counter <= byte.MaxValue; counter++)
                {
                    counterBytes[0] = (byte)counter;
                    LabeledExpand(prk.Span, "candidate"u8, counterBytes, privateKey);
                    // P-521 uses 0x01 here because Nsk is 66 bytes; P-256 and P-384 use 0xFF.
                    privateKey[0] &= _candidateBitmask;

                    if (IsValidScalar(privateKey, order))
                    {
#pragma warning disable CA1416 // Not supported on browser
                        _ecdh = ECDiffieHellman.Create(new ECParameters
                        {
                            Curve = _curve,
                            D = privateKey,
                        });
#pragma warning restore CA1416 // Not supported on browser
                        return;
                    }
                }

                throw new CryptographicException(SR.Cryptography_HpkeKeyDerivationFailed);
            }
        }

        internal override void ExportDecapsulationKey(Span<byte> destination)
        {
            Debug.Assert(_ecdh is not null);
            Debug.Assert(destination.Length == Suite.DecapsulationKeySizeInBytes);

            ECParameters parameters = _ecdh.ExportParameters(includePrivateParameters: true);
            Debug.Assert(parameters.D is not null);

            using (PinAndClear.Track(parameters.D))
            {
                if (parameters.D.Length != destination.Length)
                {
                    throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
                }

                parameters.D.AsSpan().CopyTo(destination);
            }
        }

        internal override void ExportEncapsulationKey(Span<byte> destination)
        {
            Debug.Assert(_ecdh is not null);
            Debug.Assert(destination.Length == Suite.EncapsulationKeySizeInBytes);

            ECParameters parameters = _ecdh.ExportParameters(includePrivateParameters: false);
            byte[]? x = parameters.Q.X;
            byte[]? y = parameters.Q.Y;

            Debug.Assert(x is not null);
            Debug.Assert(y is not null);

            if (x is null ||
                y is null ||
                x.Length != destination.Length / 2 ||
                y.Length != destination.Length / 2)
            {
                throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
            }

            AsymmetricAlgorithmHelpers.EncodeToUncompressedAnsiX963Key(x, y, ReadOnlySpan<byte>.Empty, destination);
        }

        public override void Dispose() => _ecdh?.Dispose();

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static bool IsValidScalar(ReadOnlySpan<byte> scalar, ReadOnlySpan<byte> order)
        {
            // NoOptimization because the comparison must remain non-short-circuiting.
            //
            // NoInlining because the NoOptimization would get lost if the method got inlined.

            Debug.Assert(scalar.Length == order.Length);

            uint borrow = 0;
            uint nonZero = 0;

            // Subtract the public order without branching on individual secret bytes.
            for (int i = scalar.Length - 1; i >= 0; i--)
            {
                uint value = scalar[i];
                nonZero |= value;
                borrow = unchecked(value - order[i] - borrow) >> 31;
            }

            return (borrow != 0) & (nonZero != 0);
        }
    }
}
