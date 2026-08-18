// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Encryption.TripleDes.Tests
{
    public sealed class TripleDESContractTests_Cng : TripleDESContractTests
    {
        protected override TripleDESProvider TripleDESFactory => TripleDESCngProvider.Instance;
    }

    public sealed class TripleDESCipherTests_Cng : TripleDESCipherTests
    {
        protected override TripleDESProvider TripleDESFactory => TripleDESCngProvider.Instance;
    }

    public sealed class TripleDESCipherOneShotTests_Cng : TripleDESCipherOneShotTests
    {
        protected override TripleDESProvider TripleDESFactory => TripleDESCngProvider.Instance;
    }

    public sealed class TripleDESReusabilityTests_Cng : TripleDESReusabilityTests
    {
        protected override TripleDESProvider TripleDESFactory => TripleDESCngProvider.Instance;
    }
}
