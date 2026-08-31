// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Formats.Asn1;
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
            private const int Aes128KeySizeInBytes = 128 / 8;
            private const int Aes192KeySizeInBytes = 192 / 8;
            private const int Aes256KeySizeInBytes = 256 / 8;
            private const int SharedSecretSizeInBytes = 32;

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
                new(_asn.Rid.IssuerAndSerialNumber, _asn.Rid.SubjectKeyIdentifier);

            public override ReadOnlyMemory<byte>? UserKeyingMaterial => _asn.Ukm;

            public override int Version => _asn.Version;

#pragma warning disable CA1822 // Instance member can be made static
            internal byte[]? DecryptCek(CompositeMLKem privateKey, out Exception? exception)
#pragma warning restore CA1822
            {
                _ = privateKey;
                exception = new PlatformNotSupportedException();
                return null;
            }

            internal byte[]? DecryptCek(MLKem privateKey, out Exception? exception)
            {
                exception = null;

                MLKemAlgorithm? encodedAlgorithm = KeyEncapsulationAlgorithm.Oid.Value switch
                {
                    Oids.MlKem512 => MLKemAlgorithm.MLKem512,
                    Oids.MlKem768 => MLKemAlgorithm.MLKem768,
                    Oids.MlKem1024 => MLKemAlgorithm.MLKem1024,
                    _ => null,
                };

                // RFC 9936: Appendix A's KEM-ALGORITHMs are all `PARAMS ARE absent`.
                if (encodedAlgorithm is null ||
                    encodedAlgorithm != privateKey.Algorithm ||
                    KeyEncapsulationAlgorithm.Parameters is not [])
                {
                    exception = new CryptographicException(SR.Cryptography_Cms_UnknownAlgorithm);
                    return null;
                }

                if (KeyEncapsulationCiphertext.Length != encodedAlgorithm.CiphertextSizeInBytes)
                {
                    exception = new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    return null;
                }

                // All ML-KEM and Composite-ML-KEM instances have a 256-bit shared secret.
                // Since the decapulation implementations use precisely sized buffers an assert is enough here.
                Debug.Assert(encodedAlgorithm.SharedSecretSizeInBytes == SharedSecretSizeInBytes);

                return DecryptCek(
                    privateKey,
                    static (privateKey, ciphertext, destination) => privateKey.Decapsulate(ciphertext, destination),
                    out exception);
            }

            private byte[]? DecryptCek<TState>(
                TState state,
                Action<TState, ReadOnlySpan<byte>, Span<byte>> decapsulator,
                out Exception? exception)
            {
                 // RFC 9629 section 3 "MUST be 0"
                if (Version != 0)
                {
                    exception = new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    return null;
                }

                HashAlgorithmName? hkdfAlgorithm = KeyDerivationAlgorithm.Oid.Value switch
                {
                    // There is no IETF-specified OID for HKDF with SHA-1 or MD5.
                    Oids.HkdfWithSha256 => HashAlgorithmName.SHA256,
                    Oids.HkdfWithSha384 => HashAlgorithmName.SHA384,
                    Oids.HkdfWithSha512 => HashAlgorithmName.SHA512,
                    Oids.HkdfWithSha3_256 => HashAlgorithmName.SHA3_256,
                    Oids.HkdfWithSha3_384 => HashAlgorithmName.SHA3_384,
                    Oids.HkdfWithSha3_512 => HashAlgorithmName.SHA3_512,
                    _ => null,
                };

                if (hkdfAlgorithm is null || KeyDerivationAlgorithm.Parameters is not [])
                {
                    exception = new CryptographicException(SR.Cryptography_Cms_UnknownAlgorithm);
                    return null;
                }

                // Validate the that OID of the AES-KW algorithm matches the key size.
                int? aesKeySizeInBytes = KeyEncryptionAlgorithm.Oid.Value switch
                {
                    Oids.Aes128Wrap => Aes128KeySizeInBytes,
                    Oids.Aes192Wrap => Aes192KeySizeInBytes,
                    Oids.Aes256Wrap => Aes256KeySizeInBytes,
                    _ => null,
                };

                //  RFC 3565 2.3.2 explicitly requires params ARE absent for the key encryption algorithm.
                if (aesKeySizeInBytes != KeyEncryptionKeyLengthInBytes || KeyEncryptionAlgorithm.Parameters is not [])
                {
                    exception = new CryptographicException(SR.Cryptography_Cms_UnknownAlgorithm);
                    return null;
                }

                const int MinimumKeySizeInBytes = 24;

                if (_asn.EncryptedKey.Length % 8 != 0 || _asn.EncryptedKey.Length < MinimumKeySizeInBytes)
                {
                    exception = new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    return null;
                }

                Span<byte> sharedSecret = stackalloc byte[SharedSecretSizeInBytes];

                try
                {
                    decapsulator(state, KeyEncapsulationCiphertext.Span, sharedSecret);

                    exception = null;
                    return EncodeKdfInfo().Encode(
                        new HkdfCallback(this, hkdfAlgorithm.Value, sharedSecret),
                        static (state, info) =>
                        {
                            // AES-256-KW is the largest supported key size.
                            const int MaxKeyEncryptionKeySize = 32;
                            Span<byte> derivedKey = stackalloc byte[MaxKeyEncryptionKeySize]
                                .Slice(0, state.Instance.KeyEncryptionKeyLengthInBytes);

                            try
                            {
                                HKDF.DeriveKey(
                                    state.HkdfAlgorithm,
                                    state.Key,
                                    derivedKey,
                                    salt: [],
                                    info);

                                using (Aes aes = Aes.Create())
                                {
                                    aes.SetKey(derivedKey);
                                    return aes.DecryptKeyWrap(state.Instance._asn.EncryptedKey.Span);
                                }
                            }
                            finally
                            {
                                CryptographicOperations.ZeroMemory(derivedKey);
                            }
                        });
                }
                catch (CryptographicException e)
                {
                    exception = e;
                    return null;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(sharedSecret);
                }
            }

            private static AlgorithmIdentifier ToAlgorithmIdentifier(AlgorithmIdentifierAsn algorithmIdentifier)
            {
                return new AlgorithmIdentifier(new Oid(algorithmIdentifier.Algorithm))
                {
                    Parameters = algorithmIdentifier.Parameters?.ToArray() ?? Array.Empty<byte>(),
                };
            }


            private AsnWriter EncodeKdfInfo()
            {
                CmsOriForKemOtherInfoAsn kdfInfo = new()
                {
                    Wrap = _asn.Wrap,
                    KekLength = _asn.KekLength,
                    Ukm = _asn.Ukm,
                };

                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                kdfInfo.Encode(writer);
                return writer;
            }

            private readonly ref struct HkdfCallback
            {
                internal HkdfCallback(
                    ManagedKemRecipientInfoPal instance,
                    HashAlgorithmName hkdfAlgorithm,
                    ReadOnlySpan<byte> key)
                {
                    Instance = instance;
                    HkdfAlgorithm = hkdfAlgorithm;
                    Key = key;
                }

                internal ManagedKemRecipientInfoPal Instance { get; }
                internal HashAlgorithmName HkdfAlgorithm { get; }
                internal ReadOnlySpan<byte> Key { get; }
            }
        }
    }
}
