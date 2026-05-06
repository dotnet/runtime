// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "We are providing the implementation for RC2, not consuming it.")]
    internal sealed partial class RC2Implementation : RC2
    {
#pragma warning disable IDE0060
        private static UniversalCryptoTransform CreateTransformCore(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            byte[] key,
            byte[]? iv,
            int blockSize,
            int feedbackSizeInBytes,
            int paddingSize,
            bool encrypting)
        {
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(RC2)));
        }

        private static ILiteSymmetricCipher CreateLiteCipher(
            CipherMode cipherMode,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> iv,
            int blockSize,
            int paddingSize,
            bool encrypting)
        {
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(RC2)));
        }
#pragma warning restore IDE0060
    }
}
