// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;

using Internal.Cryptography;

namespace System.Security.Cryptography
{
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    internal static partial class ECDsaImplementation
    {
#endif
    public sealed partial class ECDsaCng : ECDsa
    {
        protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) =>
            CngCommon.HashData(data, offset, count, hashAlgorithm);

        protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) =>
            CngCommon.HashData(data, hashAlgorithm);

        protected override bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten) =>
            CngCommon.TryHashData(source, destination, hashAlgorithm, out bytesWritten);
    }
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    }
#endif
}
