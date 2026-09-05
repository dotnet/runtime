// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal sealed partial class HpkeAeadMetadata
    {
        internal HpkeAead Aead { get; }
        internal int Nk { get; }
        internal int Nn { get; }
        internal int Nt { get; }
        internal string Name { get; }

        private HpkeAeadMetadata(HpkeAead aead, int nk, int nn, int nt, string name)
        {
            Aead = aead;
            Nk = nk;
            Nn = nn;
            Nt = nt;
            Name = name;
        }

        internal static HpkeAeadMetadata? Create(HpkeAead aead)
        {
            // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-hpke-04#section-7.3
            switch (aead)
            {
                case HpkeAead.AES_128_GCM:
                    return new HpkeAeadMetadata(aead, nk: 16, nn: 12, nt: 16, name: "AES-128-GCM");
                case HpkeAead.AES_256_GCM:
                    return new HpkeAeadMetadata(aead, nk: 32, nn: 12, nt: 16, name: "AES-256-GCM");
                case HpkeAead.ChaCha20Poly1305:
                    return new HpkeAeadMetadata(aead, nk: 32, nn: 12, nt: 16, name: "ChaCha20Poly1305");
                default:
                    return null;
            }
        }
    }
}
