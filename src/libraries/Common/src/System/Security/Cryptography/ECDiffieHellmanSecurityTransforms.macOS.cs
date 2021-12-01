// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography.Apple;

namespace System.Security.Cryptography
{
    internal static partial class ECDiffieHellmanImplementation
    {
        public sealed partial class ECDiffieHellmanSecurityTransforms : ECDiffieHellman
        {
            public override void ImportSubjectPublicKeyInfo(
                ReadOnlySpan<byte> source,
                out int bytesRead)
            {
                KeySizeValue = _ecc.ImportSubjectPublicKeyInfo(source, out bytesRead);
            }
        }
    }
}
