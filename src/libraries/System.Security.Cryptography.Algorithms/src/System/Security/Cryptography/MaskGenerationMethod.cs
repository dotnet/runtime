// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    [UnsupportedOSPlatform("browser")]
    public abstract class MaskGenerationMethod
    {
        public abstract byte[] GenerateMask(byte[] rgbSeed, int cbReturn);
    }
}
