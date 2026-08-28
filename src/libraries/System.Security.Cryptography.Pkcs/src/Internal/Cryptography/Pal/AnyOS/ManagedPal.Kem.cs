// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.Pkcs.Asn1;

namespace Internal.Cryptography.Pal.AnyOS
{
    internal sealed partial class ManagedPkcsPal
    {
        private sealed class ManagedKemRecipientInfoPal : KemRecipientInfoPal
        {
            private readonly KemRecipientInfoAsn _asn;

            internal ManagedKemRecipientInfoPal(KemRecipientInfoAsn asn)
            {
                _asn = asn;
            }

            public override byte[] EncryptedKey => _asn.EncryptedKey.ToArray();

            public override AlgorithmIdentifier KeyDerivationAlgorithm => ToAlgorithmIdentifier(_asn.Kdf);

            public override AlgorithmIdentifier KeyEncapsulationAlgorithm => ToAlgorithmIdentifier(_asn.Kem);

            public override ReadOnlyMemory<byte> KeyEncapsulationCiphertext => _asn.Kemct;

            public override AlgorithmIdentifier KeyEncryptionAlgorithm => ToAlgorithmIdentifier(_asn.Wrap);

            public override int KeyEncryptionKeyLengthInBytes => _asn.KekLength;

            public override SubjectIdentifier RecipientIdentifier =>
                new SubjectIdentifier(_asn.Rid.IssuerAndSerialNumber, _asn.Rid.SubjectKeyIdentifier);

            public override ReadOnlyMemory<byte>? UserKeyingMaterial => _asn.Ukm;

            public override int Version => _asn.Version;

            private static AlgorithmIdentifier ToAlgorithmIdentifier(AlgorithmIdentifierAsn algorithmIdentifier)
            {
                return new AlgorithmIdentifier(new Oid(algorithmIdentifier.Algorithm))
                {
                    Parameters = algorithmIdentifier.Parameters?.ToArray() ?? Array.Empty<byte>(),
                };
            }
        }
    }
}
