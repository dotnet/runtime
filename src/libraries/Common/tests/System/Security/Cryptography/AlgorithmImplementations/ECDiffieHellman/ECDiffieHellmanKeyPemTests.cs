// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;
using Xunit;

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public abstract class ECDiffieHellmanKeyPemTests : ECKeyPemTests<ECDiffieHellman>
    {
        protected abstract ECDiffieHellmanProvider ECDiffieHellmanFactory { get; }

        protected override ECDiffieHellman CreateKey() => ECDiffieHellmanFactory.Create();
    }
}
