// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    public abstract class MaskGenerationMethod
    {
        public abstract byte[] GenerateMask(byte[] rgbSeed, int cbReturn);
    }
}
