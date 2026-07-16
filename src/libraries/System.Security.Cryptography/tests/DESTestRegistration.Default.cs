// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Encryption.Des.Tests
{
    public sealed class DesContractTests_Default : DesContractTests
    {
        protected override DESProvider DESFactory => DefaultDESProvider.Instance;
    }

    public sealed class DesCipherTests_Default : DesCipherTests
    {
        protected override DESProvider DESFactory => DefaultDESProvider.Instance;
    }

    public sealed class DesCipherOneShotTests_Default : DesCipherOneShotTests
    {
        protected override DESProvider DESFactory => DefaultDESProvider.Instance;
    }

    public sealed class DesTests_Default : DesTests
    {
        protected override DESProvider DESFactory => DefaultDESProvider.Instance;
    }
}
