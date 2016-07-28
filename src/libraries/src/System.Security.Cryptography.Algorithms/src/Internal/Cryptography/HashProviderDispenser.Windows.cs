// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

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


