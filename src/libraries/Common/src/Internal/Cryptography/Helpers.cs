// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Runtime.Versioning;

namespace Internal.Cryptography
{
    internal static partial class Helpers
    {
#if NET5_0_OR_GREATER
        [UnsupportedOSPlatformGuard("ios")]
        [UnsupportedOSPlatformGuard("tvos")]
        public static bool IsDSASupported => !OperatingSystem.IsIOS() && !OperatingSystem.IsTvOS();
#else
        public static bool IsDSASupported => true;
#endif

        [return: NotNullIfNotNull("src")]
        public static byte[]? CloneByteArray(this byte[]? src)
        {
            if (src == null)
            {
                return null;
            }

            return (byte[])(src.Clone());
        }

        public static int GetPaddingSize(this SymmetricAlgorithm algorithm, CipherMode mode, int feedbackSizeInBits)
        {
            return (mode == CipherMode.CFB ? feedbackSizeInBits : algorithm.BlockSize) / 8;
        }

        internal static bool TryCopyToDestination(this ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            if (source.TryCopyTo(destination))
            {
                bytesWritten = source.Length;
                return true;
            }

            bytesWritten = 0;
            return false;
        }
    }
}
