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
        internal int? MaximumInfoLength { get; }
        internal int? MaximumPskLength { get; }
        internal int? MaximumPskIdLength { get; }

        // HKDF is limited to 255 hash blocks; HPKE encodes SHAKE output lengths in two bytes.
        // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-hpke-04#section-4.4
        internal int MaximumExportLength => IsTwoStage ? 255 * Nh : ushort.MaxValue;

        private HpkeKdfMetadata(HpkeKdf kdf, int nh, bool isTwoStage, string name)
        {
            Kdf = kdf;
            Nh = nh;
            IsTwoStage = isTwoStage;
            Name = name;

            if (!IsTwoStage)
            {
                // One-stage KDFs length-prefix each of these inputs with a 16-bit length.
                // HKDF input limits exceed the length representable by a span.
                // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-hpke-04#section-5.1
                MaximumInfoLength = ushort.MaxValue;
                MaximumPskLength = ushort.MaxValue;
                MaximumPskIdLength = ushort.MaxValue;
            }
        }

        internal static HpkeKdfMetadata? Create(HpkeKdf kdf)
        {
            switch (kdf)
            {
                // HKDF SHAs have limits on their info size, 2^61 - 91 and 2^125 - 155. Since this is well above
                // A Span's possible length we'll treat it as unlimited.
                // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-hpke-04#section-7.2
                case HpkeKdf.HKDF_SHA256:
                    return new HpkeKdfMetadata(kdf, nh: 32, isTwoStage: true, name: "HKDF-SHA256");
                case HpkeKdf.HKDF_SHA384:
                    return new HpkeKdfMetadata(kdf, nh: 48, isTwoStage: true, name: "HKDF-SHA384");
                case HpkeKdf.HKDF_SHA512:
                    return new HpkeKdfMetadata(kdf, nh: 64, isTwoStage: true, name: "HKDF-SHA512");
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
