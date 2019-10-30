// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Tests;

namespace System.Security.Cryptography.EcDsa.Tests
{
    public class ECDsaKeyFileTests : ECKeyFileTests<ECDsa>
    {
        protected override ECDsa CreateKey()
        {
            return ECDsaFactory.Create();
        }

        protected override byte[] ExportECPrivateKey(ECDsa key)
        {
            return key.ExportECPrivateKey();
        }

        protected override bool TryExportECPrivateKey(ECDsa key, Span<byte> destination, out int bytesWritten)
        {
            return key.TryExportECPrivateKey(destination, out bytesWritten);
        }

        protected override void ImportECPrivateKey(ECDsa key, ReadOnlySpan<byte> source, out int bytesRead)
        {
            key.ImportECPrivateKey(source, out bytesRead);
        }

        protected override void ImportParameters(ECDsa key, ECParameters ecParameters)
        {
            key.ImportParameters(ecParameters);
        }

        protected override ECParameters ExportParameters(ECDsa key, bool includePrivate)
        {
            return key.ExportParameters(includePrivate);
        }
    }
}
