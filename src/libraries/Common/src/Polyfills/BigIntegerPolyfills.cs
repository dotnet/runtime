// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics;

/// <summary>Provides downlevel polyfills for instance methods on <see cref="BigInteger"/>.</summary>
internal static class BigIntegerPolyfills
{
    extension(BigInteger self)
    {
        public byte[] ToByteArray(bool isUnsigned, bool isBigEndian)
        {
            if (isUnsigned && self.Sign < 0)
                throw new OverflowException();

            byte[] littleEndianBytes = self.ToByteArray();

            if (!isUnsigned && !isBigEndian)
                return littleEndianBytes;

            int length = littleEndianBytes.Length;

            // For unsigned, trim a single most-significant 0x00 sign-extension byte
            // from the end of the little-endian array.
            if (isUnsigned && length > 1 && littleEndianBytes[length - 1] == 0x00)
                length--;

            if (isBigEndian)
            {
                byte[] result = new byte[length];

                for (int i = 0; i < length; i++)
                {
                    result[i] = littleEndianBytes[length - 1 - i];
                }

                return result;
            }

            if (length == littleEndianBytes.Length)
                return littleEndianBytes;

            byte[] trimmed = new byte[length];
            Array.Copy(littleEndianBytes, trimmed, length);

            return trimmed;
        }
    }
}
