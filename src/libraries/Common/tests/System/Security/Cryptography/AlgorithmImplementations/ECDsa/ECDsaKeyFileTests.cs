// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;
using Xunit;

namespace System.Security.Cryptography.EcDsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/64389", TestPlatforms.Windows)]
    public class ECDsaKeyFileTests : ECKeyFileTests<ECDsa>
    {
        protected override ECDsa CreateKey() => ECDsaFactory.Create();
        protected override void Exercise(ECDsa key) => key.Exercise();
    }
}
