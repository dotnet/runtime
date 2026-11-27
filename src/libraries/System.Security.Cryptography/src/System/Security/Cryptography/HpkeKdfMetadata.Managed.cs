// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal sealed partial class HpkeKdfMetadata
    {
        internal HashAlgorithmName HkdfHashAlgorithm => Kdf switch
        {
            HpkeKdf.HKDF_SHA256 => HashAlgorithmName.SHA256,
            HpkeKdf.HKDF_SHA384 => HashAlgorithmName.SHA384,
            HpkeKdf.HKDF_SHA512 => HashAlgorithmName.SHA512,
            _ => throw new UnreachableException(),
        };

        internal bool IsSupported
        {
            get
            {
                switch (Kdf)
                {
                    case HpkeKdf.HKDF_SHA256:
                        return HashProviderDispenser.MacSupported(HashAlgorithmNames.SHA256);
                    case HpkeKdf.HKDF_SHA384:
                        return HashProviderDispenser.MacSupported(HashAlgorithmNames.SHA384);
                    case HpkeKdf.HKDF_SHA512:
                        return HashProviderDispenser.MacSupported(HashAlgorithmNames.SHA512);
                    case HpkeKdf.SHAKE128:
                        return Shake128.IsSupported;
                    case HpkeKdf.SHAKE256:
                        return Shake256.IsSupported;
                    default:
                        Debug.Fail($"Kdf {Kdf}'s support is unknown.");
                        return false;
                }
            }
        }
    }
}
