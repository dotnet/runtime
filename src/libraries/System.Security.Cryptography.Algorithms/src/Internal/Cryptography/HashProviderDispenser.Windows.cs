// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

using Internal.NativeCrypto;

namespace Internal.Cryptography
{
    //
    // Provides hash services via the native provider (CNG). 
    //
    internal static partial class HashProviderDispenser
    {
        public static HashProvider CreateHashProvider(String hashAlgorithmId)
        {
            return new HashProviderCng(hashAlgorithmId, null);
        }

        public static HashProvider CreateMacProvider(String hashAlgorithmId, byte[] key)
        {
            return new HashProviderCng(hashAlgorithmId, key);
        }
    }
}


