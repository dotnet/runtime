// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    internal sealed partial class HpkeKemMetadata
    {
        internal HpkeKdfMetadata KemKdf { get; private set; }
        internal byte[] SuiteId { get; private set; }

        [MemberNotNull(nameof(KemKdf))]
        [MemberNotNull(nameof(SuiteId))]
        partial void Setup()
        {
            switch (Kem)
            {
                case HpkeKem.DHKEM_P256_HKDF_SHA256:
                    (KemKdf, SuiteId) = CreateMetadata(Kem, HpkeKdf.HKDF_SHA256);
                    break;
                case HpkeKem.DHKEM_P384_HKDF_SHA384:
                    (KemKdf, SuiteId) = CreateMetadata(Kem, HpkeKdf.HKDF_SHA384);
                    break;
                case HpkeKem.DHKEM_P521_HKDF_SHA512:
                    (KemKdf, SuiteId) = CreateMetadata(Kem, HpkeKdf.HKDF_SHA512);
                    break;
                case HpkeKem.DHKEM_X25519_HKDF_SHA256:
                    (KemKdf, SuiteId) = CreateMetadata(Kem, HpkeKdf.HKDF_SHA256);
                    break;
                case HpkeKem.MLKEM_512:
                    (KemKdf, SuiteId) = CreateMetadata(Kem, HpkeKdf.SHAKE256);
                    break;
                case HpkeKem.MLKEM_768:
                    (KemKdf, SuiteId) = CreateMetadata(Kem, HpkeKdf.SHAKE256);
                    break;
                case HpkeKem.MLKEM_1024:
                    (KemKdf, SuiteId) = CreateMetadata(Kem, HpkeKdf.SHAKE256);
                    break;
                case HpkeKem.MLKEM768_P256:
                    (KemKdf, SuiteId) = CreateMetadata(Kem, HpkeKdf.SHAKE256);
                    break;
                case HpkeKem.MLKEM1024_P384:
                    (KemKdf, SuiteId) = CreateMetadata(Kem, HpkeKdf.SHAKE256);
                    break;
                default:
                    Debug.Fail($"Missing KEM KDF mapping for {Kem}.");
                    throw new CryptographicException();
            }

            static (HpkeKdfMetadata Metadata, byte[] SuiteId) CreateMetadata(HpkeKem kem, HpkeKdf kdf)
            {
                HpkeKdfMetadata? metadata = HpkeKdfMetadata.Create(kdf);

                if (metadata is null)
                {
                    Debug.Fail("KEM depends on unmapped KDF.");
                    throw new CryptographicException();
                }

                byte[] suiteId = [.."KEM"u8, 0x00, 0x00];
                BinaryPrimitives.WriteUInt16BigEndian(suiteId.AsSpan(^2), checked((ushort)kem));
                return (metadata, suiteId);
            }
        }

        internal bool IsSupported
        {
            get
            {
                switch (Kem)
                {
                    case HpkeKem.DHKEM_P256_HKDF_SHA256:
                    case HpkeKem.DHKEM_P384_HKDF_SHA384:
                    case HpkeKem.DHKEM_P521_HKDF_SHA512:
                        return !OperatingSystem.IsBrowser() && !OperatingSystem.IsWasi();
                    case HpkeKem.DHKEM_X25519_HKDF_SHA256:
                        return X25519DiffieHellman.IsSupported;
                    case HpkeKem.MLKEM_512:
                    case HpkeKem.MLKEM_768:
                    case HpkeKem.MLKEM_1024:
                    case HpkeKem.MLKEM768_P256:
                    case HpkeKem.MLKEM1024_P384:
                        return false;
                    default:
                        Debug.Fail($"Kem ${Kem}'s support is unknown.");
                        return false;
                }
            }
        }
    }
}
