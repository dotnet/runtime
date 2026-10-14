// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal sealed partial class HpkeKdfMetadata
    {
        internal HpkeKdf Kdf { get; }
        internal int Nh { get; }
        internal bool IsTwoStage { get; }
        internal string Name { get; }
        internal int MaximumExporterContextLength { get; }
        internal int MaximumInfoLength { get; }
        internal int MaximumPskLength { get; }
        internal int MaximumPskIdLength { get; }

        // HKDF is limited to 255 hash blocks; HPKE encodes SHAKE output lengths in two bytes.
        // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-hpke-04#section-4.4
        internal int MaximumExportLength => IsTwoStage ? 255 * Nh : ushort.MaxValue;

        private HpkeKdfMetadata(HpkeKdf kdf, int nh, bool isTwoStage, string name)
        {
            Debug.Assert(nh <= 64, "Nh value is larger than 64.");

            Kdf = kdf;
            Nh = nh;
            IsTwoStage = isTwoStage;
            Name = name;
            MaximumExporterContextLength = Hpke.MaximumInputSizeInBytes;

            if (IsTwoStage)
            {
                MaximumInfoLength = Hpke.MaximumInputSizeInBytes;
                MaximumPskLength = Hpke.MaximumInputSizeInBytes;
                MaximumPskIdLength = Hpke.MaximumInputSizeInBytes;
            }
            else
            {
                // One-stage KDFs length-prefix each of these inputs with a 16-bit length.
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
                // HKDF's limits exceed the maximum input size supported by the HPKE API.
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
