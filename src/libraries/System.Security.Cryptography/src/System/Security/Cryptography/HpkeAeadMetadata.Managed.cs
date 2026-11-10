// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal sealed partial class HpkeAeadMetadata
    {
        internal bool IsSupported
        {
            get
            {
                switch (Aead)
                {
#pragma warning disable CA1416
                    case HpkeAead.AES_128_GCM:
                    case HpkeAead.AES_256_GCM:
                        return AesGcm.IsSupported;
                    case HpkeAead.ChaCha20Poly1305:
                        return ChaCha20Poly1305.IsSupported;
#pragma warning restore CA1416
                    default:
                        Debug.Fail($"Aead {Aead}'s support is unknown.");
                        return false;
                }
            }
        }
    }
}
