// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Encryption.RC2.Tests
{
    public sealed class RC2ContractTests_Default : RC2ContractTests
    {
        protected override RC2Provider RC2Factory => DefaultRC2Provider.Instance;
    }

    public sealed class RC2CipherTests_Default : RC2CipherTests
    {
        protected override RC2Provider RC2Factory => DefaultRC2Provider.Instance;
    }

    public sealed class RC2CipherOneShotTests_Default : RC2CipherOneShotTests
    {
        protected override RC2Provider RC2Factory => DefaultRC2Provider.Instance;
    }

    public sealed class RC2Tests_Default : RC2Tests
    {
        protected override RC2Provider RC2Factory => DefaultRC2Provider.Instance;
    }
}
