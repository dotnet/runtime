// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal sealed class HpkeMLKemAdapter : HpkeManagedKemAdapter
    {
        private readonly MLKemAlgorithm _algorithm;
        private MLKem? _mlKem;

        internal HpkeMLKemAdapter(HpkeSuite suite) : base(suite)
        {
            _algorithm = GetAlgorithm(suite.KemAlgorithm);
        }

        internal override void Generate()
        {
            Debug.Assert(_mlKem is null);
            _mlKem = MLKem.GenerateKey(_algorithm);
        }

        internal override void DeriveKeyPair(ReadOnlySpan<byte> ikm)
        {
            Debug.Assert(_mlKem is null);
            Span<byte> privateSeed = stackalloc byte[64];

            try
            {
                DerivePrivateSeed(ikm, privateSeed);
                _mlKem = MLKem.ImportPrivateSeed(_algorithm, privateSeed);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(privateSeed);
            }
        }

        internal override void Encapsulate(Span<byte> encapsulatedSecret, Span<byte> sharedSecret)
        {
            Debug.Assert(_mlKem is not null);
            _mlKem.Encapsulate(encapsulatedSecret, sharedSecret);
        }

        internal override void Decapsulate(ReadOnlySpan<byte> encapsulatedSecret, Span<byte> sharedSecret)
        {
            Debug.Assert(_mlKem is not null);
            _mlKem.Decapsulate(encapsulatedSecret, sharedSecret);
        }

        internal override void ImportDecapsulationKey(ReadOnlySpan<byte> decapsulationKey)
        {
            Debug.Assert(_mlKem is null);
            _mlKem = MLKem.ImportPrivateSeed(_algorithm, decapsulationKey);
        }

        internal override void ImportEncapsulationKey(ReadOnlySpan<byte> encapsulationKey)
        {
            Debug.Assert(_mlKem is null);
            _mlKem = MLKem.ImportEncapsulationKey(_algorithm, encapsulationKey);
        }

        internal override void ExportDecapsulationKey(Span<byte> destination)
        {
            Debug.Assert(_mlKem is not null);
            _mlKem.ExportPrivateSeed(destination);
        }

        internal override void ExportEncapsulationKey(Span<byte> destination)
        {
            Debug.Assert(_mlKem is not null);
            _mlKem.ExportEncapsulationKey(destination);
        }

        public override void Dispose() => _mlKem?.Dispose();

        private void DerivePrivateSeed(ReadOnlySpan<byte> ikm, Span<byte> privateSeed)
        {
            Debug.Assert(privateSeed.Length == _algorithm.PrivateSeedSizeInBytes);
            LabeledDeriveWithShake256(ikm, "DeriveKeyPair"u8, ReadOnlySpan<byte>.Empty, privateSeed);
        }

        private static MLKemAlgorithm GetAlgorithm(HpkeKem kem) => kem switch
        {
            HpkeKem.MLKEM_512 => MLKemAlgorithm.MLKem512,
            HpkeKem.MLKEM_768 => MLKemAlgorithm.MLKem768,
            HpkeKem.MLKEM_1024 => MLKemAlgorithm.MLKem1024,
            _ => throw new UnreachableException(),
        };
    }
}
