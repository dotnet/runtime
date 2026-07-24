// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    public sealed class AesContractTests_Default : AesContractTests
    {
        protected override AesProvider AesFactory => DefaultAesProvider.Instance;
    }

    public sealed class AesCipherTests_Default : AesCipherTests
    {
        protected override AesProvider AesFactory => DefaultAesProvider.Instance;
    }

    public sealed class AesModeTests_Default : AesModeTests
    {
        protected override AesProvider AesFactory => DefaultAesProvider.Instance;
    }

    public sealed class AesCipherOneShotTests_Default : AesCipherOneShotTests
    {
        protected override AesProvider AesFactory => DefaultAesProvider.Instance;
    }

    public sealed class DecryptorReusability_Default : DecryptorReusability
    {
        protected override AesProvider AesFactory => DefaultAesProvider.Instance;
    }
}
