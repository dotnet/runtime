// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    public sealed class ECDiffieHellmanTests_OpenSsl : ECDiffieHellmanTests
    {
        protected override ECDiffieHellmanProvider ECDiffieHellmanFactory => ECDiffieHellmanOpenSslProvider.Instance;
    }

    public sealed class ECDiffieHellmanFactoryTests_OpenSsl : ECDiffieHellmanFactoryTests
    {
        protected override ECDiffieHellmanProvider ECDiffieHellmanFactory => ECDiffieHellmanOpenSslProvider.Instance;
    }

    public sealed class ECDhKeyFileTests_OpenSsl : ECDhKeyFileTests
    {
        protected override ECDiffieHellmanProvider ECDiffieHellmanFactory => ECDiffieHellmanOpenSslProvider.Instance;
    }

    public sealed class ECDiffieHellmanKeyPemTests_OpenSsl : ECDiffieHellmanKeyPemTests
    {
        protected override ECDiffieHellmanProvider ECDiffieHellmanFactory => ECDiffieHellmanOpenSslProvider.Instance;
    }
}
