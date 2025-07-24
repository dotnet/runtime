// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace System.Security.Cryptography
{
    internal static partial class KeyBlobHelpers
    {
        internal static byte[] ToUnsignedIntegerBytes(this ReadOnlyMemory<byte> memory, int length)
        {
            if (memory.Length == length)
            {
                return memory.ToArray();
            }

            ReadOnlySpan<byte> span = memory.Span;

            if (memory.Length == length + 1)
            {
                if (span[0] == 0)
                {
                    return span.Slice(1).ToArray();
                }
            }

            if (span.Length > length)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            byte[] target = new byte[length];
            span.CopyTo(target.AsSpan(length - span.Length));
            return target;
        }

        internal static byte[] ExportKeyParameter(this BigInteger value, int length)
        {
            byte[] target = new byte[length];

            if (value.TryWriteBytes(target, out int bytesWritten, isUnsigned: true, isBigEndian: true))
            {
                if (bytesWritten < length)
                {
                    Buffer.BlockCopy(target, 0, target, length - bytesWritten, bytesWritten);
                    target.AsSpan(0, length - bytesWritten).Clear();
                }

                return target;
            }

            throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
        }
    }
}
