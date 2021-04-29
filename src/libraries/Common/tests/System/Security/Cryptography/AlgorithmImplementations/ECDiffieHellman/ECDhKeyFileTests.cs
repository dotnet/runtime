// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;
using Xunit;

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public class ECDhKeyFileTests : ECKeyFileTests<ECDiffieHellman>
    {
        protected override ECDiffieHellman CreateKey()
        {
            return ECDiffieHellmanFactory.Create();
        }

        protected override byte[] ExportECPrivateKey(ECDiffieHellman key)
        {
            return key.ExportECPrivateKey();
        }

        protected override bool TryExportECPrivateKey(ECDiffieHellman key, Span<byte> destination, out int bytesWritten)
        {
            return key.TryExportECPrivateKey(destination, out bytesWritten);
        }

        protected override void ImportECPrivateKey(ECDiffieHellman key, ReadOnlySpan<byte> source, out int bytesRead)
        {
            key.ImportECPrivateKey(source, out bytesRead);
        }

        protected override void ImportParameters(ECDiffieHellman key, ECParameters ecParameters)
        {
            key.ImportParameters(ecParameters);
        }

        protected override ECParameters ExportParameters(ECDiffieHellman key, bool includePrivate)
        {
            return key.ExportParameters(includePrivate);
        }

        protected override void Exercise(ECDiffieHellman key) => key.Exercise();

        protected override Func<ECDiffieHellman, byte[]> PublicKeyWriteArrayFunc { get; } =
            key => key.PublicKey.ExportSubjectPublicKeyInfo();

        protected override WriteKeyToSpanFunc PublicKeyWriteSpanFunc { get; } =
            (ECDiffieHellman key, Span<byte> destination, out int bytesWritten) =>
                key.PublicKey.TryExportSubjectPublicKeyInfo(destination, out bytesWritten);
    }
}
