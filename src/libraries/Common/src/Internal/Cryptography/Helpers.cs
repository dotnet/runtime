// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static partial class Helpers
    {
        [return: NotNullIfNotNull("src")]
        public static byte[]? CloneByteArray(this byte[]? src)
        {
            if (src == null)
            {
                return null;
            }

            return (byte[])(src.Clone());
        }

        public static int GetPaddingSize(this SymmetricAlgorithm algorithm)
        {
            // CFB8 does not require any padding at all
            // otherwise, it is always required to pad for block size
            if (algorithm.Mode == CipherMode.CFB && algorithm.FeedbackSize == 8)
                return 1;

            return algorithm.BlockSize / 8;
        }

        internal static bool TryCopyToDestination(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
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
