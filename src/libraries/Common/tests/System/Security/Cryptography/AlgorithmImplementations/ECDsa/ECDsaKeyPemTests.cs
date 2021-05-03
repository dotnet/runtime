// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;
using Xunit;

namespace System.Security.Cryptography.EcDsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public sealed class ECDsaKeyPemTests : ECKeyPemTests<ECDsa>
    {
        protected override ECDsa CreateKey() => ECDsa.Create();
        protected override ECParameters ExportParameters(ECDsa key, bool includePrivateParameters) =>
            key.ExportParameters(includePrivateParameters);
    }
}
