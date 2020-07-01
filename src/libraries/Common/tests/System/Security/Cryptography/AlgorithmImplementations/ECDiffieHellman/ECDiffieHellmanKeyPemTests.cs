// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Tests;

namespace System.Security.Cryptography.EcDsa.Tests
{
    public sealed class ECDiffieHellmanKeyPemTests : ECKeyPemTests<ECDiffieHellman>
    {
        protected override ECDiffieHellman CreateKey() => ECDiffieHellman.Create();
        protected override ECParameters ExportParameters(ECDiffieHellman key, bool includePrivateParameters) =>
            key.ExportParameters(includePrivateParameters);
    }
}
