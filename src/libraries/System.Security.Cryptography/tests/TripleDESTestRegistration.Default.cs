// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Encryption.TripleDes.Tests
{
    public sealed class TripleDESContractTests_Default : TripleDESContractTests
    {
        protected override TripleDESProvider TripleDESFactory => DefaultTripleDESProvider.Instance;
    }

    public sealed class TripleDESCipherTests_Default : TripleDESCipherTests
    {
        protected override TripleDESProvider TripleDESFactory => DefaultTripleDESProvider.Instance;
    }

    public sealed class TripleDESCipherOneShotTests_Default : TripleDESCipherOneShotTests
    {
        protected override TripleDESProvider TripleDESFactory => DefaultTripleDESProvider.Instance;
    }

    public sealed class TripleDESReusabilityTests_Default : TripleDESReusabilityTests
    {
        protected override TripleDESProvider TripleDESFactory => DefaultTripleDESProvider.Instance;
    }
}
