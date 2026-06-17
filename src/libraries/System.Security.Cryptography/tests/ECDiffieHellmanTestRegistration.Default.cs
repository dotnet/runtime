// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    public sealed class ECDiffieHellmanTests_Default : ECDiffieHellmanTests
    {
        protected override ECDiffieHellmanProvider ECDiffieHellmanFactory { get; } = DefaultECDiffieHellmanProvider.Instance;
    }

    public sealed class ECDiffieHellmanFactoryTests_Default : ECDiffieHellmanFactoryTests
    {
        protected override ECDiffieHellmanProvider ECDiffieHellmanFactory { get; } = DefaultECDiffieHellmanProvider.Instance;
    }

    public sealed class ECDhKeyFileTests_Default : ECDhKeyFileTests
    {
        protected override ECDiffieHellmanProvider ECDiffieHellmanFactory { get; } = DefaultECDiffieHellmanProvider.Instance;
    }

    public sealed class ECDiffieHellmanKeyPemTests_Default : ECDiffieHellmanKeyPemTests
    {
        protected override ECDiffieHellmanProvider ECDiffieHellmanFactory { get; } = DefaultECDiffieHellmanProvider.Instance;
    }
}
