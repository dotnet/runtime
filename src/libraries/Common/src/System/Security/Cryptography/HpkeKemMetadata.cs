// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal sealed partial class HpkeKemMetadata
    {
        internal HpkeKem Kem { get; }
        internal int Nsk { get; }
        internal int Npk { get; }
        internal int Nenc { get; }
        internal int Nsecret { get; }
        internal string Name { get; }

        private HpkeKemMetadata(HpkeKem kem, int nsecret, int nenc, int npk, int nsk, string name)
        {
            Kem = kem;
            Nsk = nsk;
            Npk = npk;
            Nenc = nenc;
            Nsecret = nsecret;
            Name = name;
            Setup();
        }

        partial void Setup();

        internal static HpkeKemMetadata? Create(HpkeKem kem)
        {
            switch (kem)
            {
                // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-hpke-04#section-7.1
                case HpkeKem.DHKEM_P256_HKDF_SHA256:
                    return new HpkeKemMetadata(kem, nsecret: 32, nenc: 65, npk: 65, nsk: 32, name: "DHKEM(P-256, HKDF-SHA256)");
                case HpkeKem.DHKEM_P384_HKDF_SHA384:
                    return new HpkeKemMetadata(kem, nsecret: 48, nenc: 97, npk: 97, nsk: 48, name: "DHKEM(P-384, HKDF-SHA384)");
                case HpkeKem.DHKEM_P521_HKDF_SHA512:
                    return new HpkeKemMetadata(kem, nsecret: 64, nenc: 133, npk: 133, nsk: 66, name: "DHKEM(P-521, HKDF-SHA512)");
                case HpkeKem.DHKEM_X25519_HKDF_SHA256:
                    return new HpkeKemMetadata(kem, nsecret: 32, nenc: 32, npk: 32, nsk: 32, name: "DHKEM(X25519, HKDF-SHA256)");

                // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-pq-05#section-8.1
                // Nsk is the 64-byte seed, not the expanded ML-KEM decapsulation key.
                case HpkeKem.MLKEM_512:
                    return new HpkeKemMetadata(kem, nsecret: 32, nenc: 768, npk: 800, nsk: 64, name: "ML-KEM-512");
                case HpkeKem.MLKEM_768:
                    return new HpkeKemMetadata(kem, nsecret: 32, nenc: 1088, npk: 1184, nsk: 64, name: "ML-KEM-768");
                case HpkeKem.MLKEM_1024:
                    return new HpkeKemMetadata(kem, nsecret: 32, nenc: 1568, npk: 1568, nsk: 64, name: "ML-KEM-1024");

                // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-pq-05#section-8.2
                // Nsk is the 32-byte seed used to derive both component key pairs.
                case HpkeKem.MLKEM768_P256:
                    return new HpkeKemMetadata(kem, nsecret: 32, nenc: 1153, npk: 1249, nsk: 32, name: "MLKEM768-P256");
                case HpkeKem.MLKEM1024_P384:
                    return new HpkeKemMetadata(kem, nsecret: 32, nenc: 1665, npk: 1665, nsk: 32, name: "MLKEM1024-P384");

                default:
                    return null;
            }
        }
    }
}
