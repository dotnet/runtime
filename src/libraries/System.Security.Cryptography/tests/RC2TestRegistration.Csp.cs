// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Encryption.RC2.Tests
{
    public sealed class RC2CipherTests_Csp : RC2CipherTests
    {
        protected override RC2Provider RC2Factory => RC2CryptoServiceProviderProvider.Instance;
    }

    public sealed class RC2Tests_Csp : RC2Tests
    {
        protected override RC2Provider RC2Factory => RC2CryptoServiceProviderProvider.Instance;
    }
}
