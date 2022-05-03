// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Key derivation functions used to transform the raw secret agreement into key material
    /// </summary>
    public enum ECDiffieHellmanKeyDerivationFunction
    {
        Hash,
        Hmac,
        Tls
    }
}
