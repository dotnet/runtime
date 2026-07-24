// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Encryption.Des.Tests
{
    public sealed class DesCipherTests_Csp : DesCipherTests
    {
        protected override DESProvider DESFactory => DESCryptoServiceProviderProvider.Instance;
    }

    public sealed class DesTests_Csp : DesTests
    {
        protected override DESProvider DESFactory => DESCryptoServiceProviderProvider.Instance;
    }
}
