// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;
using Xunit;

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/64389", TestPlatforms.Windows)]
    public class ECDhKeyFileTests : ECKeyFileTests<ECDiffieHellman>
    {
        protected override ECDiffieHellman CreateKey() => ECDiffieHellmanFactory.Create();
        protected override void Exercise(ECDiffieHellman key) => key.Exercise();

        protected override Func<ECDiffieHellman, byte[]> PublicKeyWriteArrayFunc { get; } =
            key =>
            {
                using (ECDiffieHellmanPublicKey publicKey = key.PublicKey)
                {
                    return publicKey.ExportSubjectPublicKeyInfo();
                }
            };

        protected override WriteKeyToSpanFunc PublicKeyWriteSpanFunc { get; } =
            (ECDiffieHellman key, Span<byte> destination, out int bytesWritten) =>
            {
                using (ECDiffieHellmanPublicKey publicKey = key.PublicKey)
                {
                    return publicKey.TryExportSubjectPublicKeyInfo(destination, out bytesWritten);
                }
            };
    }
}
