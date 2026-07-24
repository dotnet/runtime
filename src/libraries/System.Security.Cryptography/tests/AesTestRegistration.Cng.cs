// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    public sealed class AesContractTests_Cng : AesContractTests
    {
        protected override AesProvider AesFactory => AesCngProvider.Instance;
    }

    public sealed class AesCipherTests_Cng : AesCipherTests
    {
        protected override AesProvider AesFactory => AesCngProvider.Instance;
    }

    public sealed class AesModeTests_Cng : AesModeTests
    {
        protected override AesProvider AesFactory => AesCngProvider.Instance;
    }

    public sealed class AesCipherOneShotTests_Cng : AesCipherOneShotTests
    {
        protected override AesProvider AesFactory => AesCngProvider.Instance;
    }

    public sealed class DecryptorReusability_Cng : DecryptorReusability
    {
        protected override AesProvider AesFactory => AesCngProvider.Instance;
    }
}
