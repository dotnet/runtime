// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "We are providing the implementation for RC2, not consuming it.")]
    internal sealed partial class RC2Implementation : RC2
    {
        private static UniversalCryptoTransform CreateTransformCore(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            byte[] key,
            int effectiveKeyLength,
            byte[]? iv,
            int blockSize,
            int feedbackSizeInBytes,
            int paddingSize,
            bool encrypting)
        {
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(RC2)));
        }
    }
}
