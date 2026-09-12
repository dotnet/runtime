// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.Pkcs.Asn1;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal.AnyOS
{
    internal sealed partial class ManagedPkcsPal
    {
        private static readonly AlgorithmIdentifierAsn s_hkdfSha384Identifier = new() { Algorithm = Oids.HkdfWithSha384 };
        private static readonly AlgorithmIdentifierAsn s_aes256KwIdentifier = new() { Algorithm = Oids.Aes256Wrap };

        private static RecipientInfoAsn MakeKemRecipientInfo(byte[] cek, CmsRecipient recipient)
        {
            KemRecipientInfoAsn kemRecipientInfo = MakeKeri(cek, recipient);
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            kemRecipientInfo.Encode(writer);

            return new RecipientInfoAsn
            {
                Ori = new OtherRecipientInfoAsn
                {
                    OriType = Oids.IdSmimeOriKem,
                    OriValue = writer.Encode(),
                },
            };
        }

        private static KemRecipientInfoAsn MakeKeri(byte[] cek, CmsRecipient recipient)
        {
            if (cek.Length < ManagedKemRecipientInfoPal.Aes128KeySizeInBytes ||
                cek.Length % 8 != 0 ||
                cek.Length > ManagedKemRecipientInfoPal.Aes256KeySizeInBytes)
            {
                throw new CryptographicException(SR.Cryptography_Cms_InvalidSymmetricKey);
            }

            KemRecipientInfoAsn keri = default;
            keri.Rid = PkcsHelpers.MakeRecipientIdentifier(recipient);

            // KDF and AES-KW algorithm is not user selectable currently. Always use AES-256-KW with SHA-2-384 since it
            // meets all requirements.
            keri.Kdf = s_hkdfSha384Identifier;
            keri.Wrap = s_aes256KwIdentifier;
            keri.KekLength = ManagedKemRecipientInfoPal.Aes256KeySizeInBytes;
            keri.Ukm = recipient.KeyEncapsulationUserKeyingMaterial;

            const int SharedSecretSize = 32;
            Span<byte> sharedSecret = stackalloc byte[SharedSecretSize];
            byte[]? algorithmParameters = recipient.Certificate.GetKeyAlgorithmParameters();

            try
            {
                string keyAlgorithm = recipient.Certificate.GetKeyAlgorithm();

                if (PkcsHelpers.IsCompositeMLKemAlgorithm(keyAlgorithm))
                {
                    throw new PlatformNotSupportedException(
                        SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(CompositeMLKem)));
                }

                switch (keyAlgorithm)
                {
                    case Oids.MlKem512 or Oids.MlKem768 or Oids.MlKem1024 when algorithmParameters is null:
                        using (MLKem? key = recipient.Certificate.GetMLKemPublicKey())
                        {
                            Debug.Assert(key is not null);
                            byte[] ciphertext = new byte[key.Algorithm.CiphertextSizeInBytes];
                            Debug.Assert(key.Algorithm.SharedSecretSizeInBytes == SharedSecretSize);

                            key.Encapsulate(ciphertext, sharedSecret);
                            keri.Kemct = ciphertext;
                            keri.Kem.Algorithm = keyAlgorithm;
                        }
                        break;
                    default:
                        throw new CryptographicException(SR.Cryptography_Cms_UnknownAlgorithm, keyAlgorithm);
                }

                State3<ReadOnlySpan<byte>, ReadOnlySpan<byte>, int> encodeState = new(cek, sharedSecret, 0);
                AsnWriter hkdfInfoWriter = ManagedKemRecipientInfoPal.EncodeKdfInfo(
                    keri.Wrap,
                    keri.KekLength,
                    keri.Ukm);

                keri.EncryptedKey = hkdfInfoWriter.Encode(encodeState, static (state, info) =>
                {
                    Span<byte> derivedKey = stackalloc byte[ManagedKemRecipientInfoPal.Aes256KeySizeInBytes];

                    try
                    {
                        HKDF.DeriveKey(HashAlgorithmName.SHA384, state.Item2, derivedKey, salt: [], info);

                        using (Aes aes = Aes.Create())
                        {
                            aes.SetKey(derivedKey);
                            return aes.EncryptKeyWrap(state.Item1);
                        }
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(derivedKey);
                    }
                });
            }
            finally
            {
                CryptographicOperations.ZeroMemory(sharedSecret);
            }

            return keri;
        }

        private sealed class ManagedKemRecipientInfoPal : KemRecipientInfoPal
        {
            internal const int Aes128KeySizeInBytes = 128 / 8;
            internal const int Aes192KeySizeInBytes = 192 / 8;
            internal const int Aes256KeySizeInBytes = 256 / 8;
            internal const int SharedSecretSizeInBytes = 32;
            internal const int MinimumKeySizeInBytes = 24;

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

            internal byte[]? DecryptCek(X509Certificate2 cert, out Exception? exception)
            {
                string kemAlgorithm = _asn.Kem.Algorithm;

                if (PkcsHelpers.IsCompositeMLKemAlgorithm(kemAlgorithm))
                {
                    exception = new PlatformNotSupportedException(
                        SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(CompositeMLKem)));

                    return null;
                }

                if (PkcsHelpers.IsMLKemAlgorithm(kemAlgorithm))
                {
                    using (MLKem? certificatePrivateKey = cert.GetMLKemPrivateKey())
                    {
                        if (certificatePrivateKey is null)
                        {
                            exception = new CryptographicException(SR.Cryptography_Cms_Signing_RequiresPrivateKey);
                            return null;
                        }

                        return DecryptCek(certificatePrivateKey, out exception);
                    }
                }

                exception = new CryptographicException(SR.Cryptography_Cms_UnknownAlgorithm, kemAlgorithm);
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
                    State3<ManagedKemRecipientInfoPal, HashAlgorithmName, ReadOnlySpan<byte>> encodeState =
                        new(this, hkdfAlgorithm.Value, sharedSecret);

                    return EncodeKdfInfo(_asn.Wrap, _asn.KekLength, _asn.Ukm).Encode(encodeState,
                        static (state, info) =>
                        {
                            // AES-256-KW is the largest supported key size.
                            const int MaxKeyEncryptionKeySize = 32;
                            Span<byte> derivedKey = stackalloc byte[MaxKeyEncryptionKeySize]
                                .Slice(0, state.Item1.KeyEncryptionKeyLengthInBytes);

                            try
                            {
                                HKDF.DeriveKey(state.Item2, state.Item3, derivedKey, salt: [], info);

                                using (Aes aes = Aes.Create())
                                {
                                    aes.SetKey(derivedKey);
                                    return aes.DecryptKeyWrap(state.Item1._asn.EncryptedKey.Span);
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

            internal static AsnWriter EncodeKdfInfo(in AlgorithmIdentifierAsn wrap, int kekLength, ReadOnlyMemory<byte>? ukm)
            {
                CmsOriForKemOtherInfoAsn kdfInfo = new()
                {
                    Wrap = wrap,
                    KekLength = kekLength,
                    Ukm = ukm,
                };

                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                kdfInfo.Encode(writer);
                return writer;
            }
        }
    }

    file readonly ref struct State3<T1, T2, T3>
        where T1 : allows ref struct
        where T2 : allows ref struct
        where T3 : allows ref struct
    {
        internal T1 Item1 { get; }
        internal T2 Item2 { get; }
        internal T3 Item3 { get; }

        internal State3(T1 item1, T2 item2, T3 item3)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
        }
    }
}
