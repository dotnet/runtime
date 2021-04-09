// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;
using Xunit;

namespace System.Security.Cryptography.EcDsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public sealed class ECDiffieHellmanKeyPemTests : ECKeyPemTests<ECDiffieHellman>
    {
        protected override ECDiffieHellman CreateKey() => ECDiffieHellman.Create();
        protected override ECParameters ExportParameters(ECDiffieHellman key, bool includePrivateParameters) =>
            key.ExportParameters(includePrivateParameters);
    }
}
