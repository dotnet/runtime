// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Apple;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal static partial class DSAImplementation
    {
        public sealed partial class DSASecurityTransforms : DSA
        {
            public override DSAParameters ExportParameters(bool includePrivateParameters)
                => throw new PlatformNotSupportedException();

            public override void ImportParameters(DSAParameters parameters)
                => throw new PlatformNotSupportedException();
        }
    }
}
