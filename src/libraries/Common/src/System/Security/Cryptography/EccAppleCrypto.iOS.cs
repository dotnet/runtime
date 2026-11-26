// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal sealed partial class EccAppleCrypto
    {
#pragma warning disable IDE0060
        private static ECParameters ExportParametersFromLegacyKey(SecKeyPair keys, bool includePrivateParameters)
            => throw new CryptographicException();
#pragma warning restore IDE0060
    }
}
