// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Encryption.RC2.Tests
{
    using RC2 = System.Security.Cryptography.RC2;

    public sealed class RC2CryptoServiceProviderProvider : RC2Provider
    {
        public static readonly RC2CryptoServiceProviderProvider Instance = new RC2CryptoServiceProviderProvider();

        private RC2CryptoServiceProviderProvider() { }

        public override RC2 Create() => new RC2CryptoServiceProvider();

        public override bool OneShotSupported => false;
    }
}
