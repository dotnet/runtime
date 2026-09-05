// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal sealed partial class HpkeKdfMetadata
    {
        internal HpkeKdf Kdf { get; }
        internal int Nh { get; }
        internal bool IsTwoStage { get; }
        internal string Name { get; }

        private HpkeKdfMetadata(HpkeKdf kdf, int nh, bool isTwoStage, string name)
        {
            Kdf = kdf;
            Nh = nh;
            IsTwoStage = isTwoStage;
            Name = name;
        }

        internal static HpkeKdfMetadata? Create(HpkeKdf kdf)
        {
            switch (kdf)
            {
                // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-hpke-04#section-7.2
                case HpkeKdf.HKDF_SHA256:
                    return new HpkeKdfMetadata(kdf, nh: 32, isTwoStage: true, name: "HKDF-SHA256");
                case HpkeKdf.HKDF_SHA384:
                    return new HpkeKdfMetadata(kdf, nh: 48, isTwoStage: true, name: "HKDF-SHA384");
                case HpkeKdf.HKDF_SHA512:
                    return new HpkeKdfMetadata(kdf, nh: 64, isTwoStage: true, name: "HKDF-SHA512");

                // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-pq-05#section-5
                case HpkeKdf.SHAKE128:
                    return new HpkeKdfMetadata(kdf, nh: 32, isTwoStage: false, name: "SHAKE128");
                case HpkeKdf.SHAKE256:
                    return new HpkeKdfMetadata(kdf, nh: 64, isTwoStage: false, name: "SHAKE256");

                default:
                    return null;
            }
        }
    }
}
