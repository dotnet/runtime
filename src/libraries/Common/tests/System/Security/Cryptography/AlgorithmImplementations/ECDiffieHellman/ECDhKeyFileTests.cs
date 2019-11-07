// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Tests;

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
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
    }
}
