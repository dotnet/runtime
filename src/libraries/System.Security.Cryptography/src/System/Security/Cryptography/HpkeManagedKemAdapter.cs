// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal abstract class HpkeManagedKemAdapter : IDisposable
    {
        protected const int PrkStackBufferSize = SHA512.HashSizeInBytes;

        private static ReadOnlySpan<byte> VersionLabel => "HPKE-v1"u8;

        internal HpkeSuite Suite { get; }
        protected HpkeKdfMetadata KeyDerivationKdf => Suite.KemMetadata.KemKdf;

        protected HpkeManagedKemAdapter(HpkeSuite suite)
        {
            Suite = suite;
        }

        internal static HpkeManagedKemAdapter Create(HpkeSuite suite)
        {
            switch (suite.KemAlgorithm)
            {
                case HpkeKem.DHKEM_P256_HKDF_SHA256:
                case HpkeKem.DHKEM_P384_HKDF_SHA384:
                    return new HpkeECDiffieHellmanKemAdapter(suite);
                case HpkeKem.DHKEM_X25519_HKDF_SHA256:
                    return new HpkeX25519DiffieHellmanKemAdapter(suite);
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
            int labeledInfoLength =
                checked(sizeof(ushort) + VersionLabel.Length + suiteId.Length + label.Length + info.Length);
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
}
