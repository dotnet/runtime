// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;
using Xunit;

namespace System.Security.Cryptography.EcDsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public sealed class ECDiffieHellmanKeyPemTests : ECKeyPemTests<ECDiffieHellman>
    {
        protected override ECDiffieHellman CreateKey() => ECDiffieHellman.Create();

        protected override ECParameters ExportParameters(ECDiffieHellman key, bool includePrivateParameters) =>
            key.ExportParameters(includePrivateParameters);

        protected override void ImportParameters(ECDiffieHellman key, ECParameters ecParameters) =>
            key.ImportParameters(ecParameters);

        protected override string ExportECPrivateKeyPem(ECDiffieHellman key) => key.ExportECPrivateKeyPem();

        protected override bool TryExportECPrivateKeyPem(ECDiffieHellman key, Span<char> destination, out int charsWritten) =>
            key.TryExportECPrivateKeyPem(destination, out charsWritten);
    }
}
