// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    public sealed class ECDiffieHellmanTests_Cng : ECDiffieHellmanTests
    {
        protected override ECDiffieHellmanProvider ECDiffieHellmanFactory { get; } = ECDiffieHellmanCngProvider.Instance;
    }

    public sealed class ECDiffieHellmanFactoryTests_Cng : ECDiffieHellmanFactoryTests
    {
        protected override ECDiffieHellmanProvider ECDiffieHellmanFactory { get; } = ECDiffieHellmanCngProvider.Instance;
    }

    public sealed class ECDhKeyFileTests_Cng : ECDhKeyFileTests
    {
        protected override ECDiffieHellmanProvider ECDiffieHellmanFactory { get; } = ECDiffieHellmanCngProvider.Instance;
    }

    public sealed class ECDiffieHellmanKeyPemTests_Cng : ECDiffieHellmanKeyPemTests
    {
        protected override ECDiffieHellmanProvider ECDiffieHellmanFactory { get; } = ECDiffieHellmanCngProvider.Instance;
    }
}
