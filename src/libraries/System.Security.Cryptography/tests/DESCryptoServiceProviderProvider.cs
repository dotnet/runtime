// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Encryption.Des.Tests
{
    public sealed class DESCryptoServiceProviderProvider : DESProvider
    {
        public static readonly DESCryptoServiceProviderProvider Instance = new DESCryptoServiceProviderProvider();

        private DESCryptoServiceProviderProvider() { }

        public override DES Create() => new DESCryptoServiceProvider();

        public override bool OneShotSupported => false;
    }
}
