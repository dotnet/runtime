// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                    SuiteId = [.."KEM"u8, 0x00, 0x10];
                    KemKdf = CreateKemKdf(HpkeKdf.HKDF_SHA256);
                    break;
                case HpkeKem.DHKEM_P384_HKDF_SHA384:
                    SuiteId = [.."KEM"u8, 0x00, 0x11];
                    KemKdf = CreateKemKdf(HpkeKdf.HKDF_SHA384);
                    break;
                case HpkeKem.DHKEM_X25519_HKDF_SHA256:
                    SuiteId = [.."KEM"u8, 0x00, 0x20];
                    KemKdf = CreateKemKdf(HpkeKdf.HKDF_SHA256);
                    break;
                case HpkeKem.MLKEM_512:
                    SuiteId = [.."KEM"u8, 0x00, 0x40];
                    KemKdf = CreateKemKdf(HpkeKdf.SHAKE256);
                    break;
                case HpkeKem.MLKEM_768:
                    SuiteId = [.."KEM"u8, 0x00, 0x41];
                    KemKdf = CreateKemKdf(HpkeKdf.SHAKE256);
                    break;
                case HpkeKem.MLKEM_1024:
                    SuiteId = [.."KEM"u8, 0x00, 0x42];
                    KemKdf = CreateKemKdf(HpkeKdf.SHAKE256);
                    break;
                case HpkeKem.MLKEM768_P256:
                    SuiteId = [.."KEM"u8, 0x00, 0x50];
                    KemKdf = CreateKemKdf(HpkeKdf.SHAKE256);
                    break;
                case HpkeKem.MLKEM1024_P384:
                    SuiteId = [.."KEM"u8, 0x00, 0x51];
                    KemKdf = CreateKemKdf(HpkeKdf.SHAKE256);
                    break;
                default:
                    Debug.Fail($"Missing KEM KDF mapping for {Kem}.");
                    throw new CryptographicException();
            }

            static HpkeKdfMetadata CreateKemKdf(HpkeKdf kdf)
            {
                HpkeKdfMetadata? metadata = HpkeKdfMetadata.Create(kdf);

                if (metadata is null)
                {
                    Debug.Fail("KEM depends on unmapped KDF.");
                    throw new CryptographicException();
                }

                return metadata;
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
