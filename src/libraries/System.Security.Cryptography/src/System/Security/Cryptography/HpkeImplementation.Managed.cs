// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    internal sealed class HpkeImplementation : Hpke
    {
        private HpkeImplementation(HpkeSuite suite) : base(suite)
        {
        }

        internal static bool IsSupportedImpl(HpkeSuite suite) =>
            suite.KemMetadata.IsSupported &&
            suite.KdfMetadata.IsSupported &&
            suite.AeadMetadata.IsSupported;
    }

    internal abstract class ManagedHpkeKemAdapter : IDisposable
    {
        protected const int PrkStackBufferSize = SHA512.HashSizeInBytes;

        private static ReadOnlySpan<byte> VersionLabel => "HPKE-v1"u8;

        protected HpkeSuite Suite { get; }
        protected HpkeKdfMetadata KeyDerivationKdf => Suite.KemMetadata.KemKdf;

        protected ManagedHpkeKemAdapter(HpkeSuite suite)
        {
            Suite = suite;
        }

        internal static ManagedHpkeKemAdapter Create(HpkeSuite suite)
        {
            switch (suite.KemAlgorithm)
            {
                case HpkeKem.DHKEM_P256_HKDF_SHA256:
                case HpkeKem.DHKEM_P384_HKDF_SHA384:
                    return new ECDiffieHellmanHpkeKemAdapter(suite);
                case HpkeKem.DHKEM_X25519_HKDF_SHA256:
                    return new X25519DiffieHellmanHpkeKemAdapter(suite);
                default:
                    throw new PlatformNotSupportedException();
            }
        }

        internal void Generate()
        {
            const int MaxStackIkmSize = 64;

            using (CryptoPoolLease ikm = CryptoPoolLease.RentConditionally(
                Suite.KemMetadata.Nsk, stackalloc byte[MaxStackIkmSize]))
            {
                RandomNumberGenerator.Fill(ikm.Span);
                DeriveKeyPair(ikm.Span);
            }
        }

        // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-hpke-04#section-4.4
        protected void LabeledExtract(
            ReadOnlySpan<byte> salt,
            ReadOnlySpan<byte> label,
            ReadOnlySpan<byte> ikm,
            Span<byte> prk)
        {
            Debug.Assert(KeyDerivationKdf.IsTwoStage);
            Debug.Assert(prk.Length == KeyDerivationKdf.Nh);

            using (IncrementalHash hmac = IncrementalHash.CreateHMAC(KeyDerivationKdf.HkdfHashAlgorithm, salt))
            {
                hmac.AppendData(VersionLabel);
                hmac.AppendData(Suite.KemMetadata.SuiteId);
                hmac.AppendData(label);
                hmac.AppendData(ikm);
                int written = hmac.GetHashAndReset(prk);
                Debug.Assert(written == prk.Length);
            }
        }

        protected void LabeledExpand(
            ReadOnlySpan<byte> prk,
            ReadOnlySpan<byte> label,
            ReadOnlySpan<byte> info,
            Span<byte> output)
        {
            Debug.Assert(KeyDerivationKdf.IsTwoStage);
            Debug.Assert(prk.Length == KeyDerivationKdf.Nh);
            Debug.Assert(output.Length <= ushort.MaxValue);

            ReadOnlySpan<byte> suiteId = Suite.KemMetadata.SuiteId;
            int labeledInfoLength = checked(sizeof(ushort) + VersionLabel.Length + suiteId.Length + label.Length + info.Length);
            const int MaxStackLabeledInfoLength = 64;

            using (CryptoPoolLease labeledInfo = CryptoPoolLease.RentConditionally(
                labeledInfoLength, stackalloc byte[MaxStackLabeledInfoLength]))
            {
                Span<byte> destination = labeledInfo.Span;
                BinaryPrimitives.WriteUInt16BigEndian(destination, checked((ushort)output.Length));
                int offset = sizeof(ushort);
                VersionLabel.CopyTo(destination.Slice(offset));
                offset += VersionLabel.Length;
                suiteId.CopyTo(destination.Slice(offset));
                offset += suiteId.Length;
                label.CopyTo(destination.Slice(offset));
                offset += label.Length;
                info.CopyTo(destination.Slice(offset));

                HKDF.Expand(KeyDerivationKdf.HkdfHashAlgorithm, prk, output, labeledInfo.Span);
            }
        }

        internal abstract void DeriveKeyPair(ReadOnlySpan<byte> ikm);
        internal abstract void ImportEncapsulationKey(ReadOnlySpan<byte> encapsulationKey);
        public abstract void Dispose();
    }

    internal sealed class ECDiffieHellmanHpkeKemAdapter : ManagedHpkeKemAdapter
    {
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

        internal ECDiffieHellmanHpkeKemAdapter(HpkeSuite suite) : base(suite)
        {
            _curve = suite.KemAlgorithm switch
            {
                HpkeKem.DHKEM_P256_HKDF_SHA256 => ECCurve.NamedCurves.nistP256,
                HpkeKem.DHKEM_P384_HKDF_SHA384 => ECCurve.NamedCurves.nistP384,
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
                encapsulationKey, hasPrivateKey: false, out ECParameters parameters);
            parameters.Curve = _curve;
#pragma warning disable CA1416 // Not supported on browser
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

                // The P-256 and P-384 masks are 0xFF, so no candidate bits need to be cleared.
                for (int counter = 0; counter <= byte.MaxValue; counter++)
                {
                    counterBytes[0] = (byte)counter;
                    LabeledExpand(prk.Span, "candidate"u8, counterBytes, privateKey);

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

        public override void Dispose() => _ecdh?.Dispose();

        private static bool IsValidScalar(ReadOnlySpan<byte> scalar, ReadOnlySpan<byte> order)
        {
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

    internal sealed class X25519DiffieHellmanHpkeKemAdapter : ManagedHpkeKemAdapter
    {
        private X25519DiffieHellman? _x25519;

        internal X25519DiffieHellmanHpkeKemAdapter(HpkeSuite suite) : base(suite)
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

        public override void Dispose() => _x25519?.Dispose();
    }
}
