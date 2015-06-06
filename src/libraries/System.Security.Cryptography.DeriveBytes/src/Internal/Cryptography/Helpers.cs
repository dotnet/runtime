// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static class Helpers
    {
        public static byte[] CloneByteArray(this byte[] src)
        {
            return (byte[])(src.Clone());
        }

        public static byte[] GenerateRandom(int count)
        {
            byte[] buffer = new byte[count];

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }

            return buffer;
        }

        // encodes the integer i into a 4-byte array, in big endian.
        public static byte[] Int(uint i)
        {
            return unchecked(new byte[] { (byte)(i >> 24), (byte)(i >> 16), (byte)(i >> 8), (byte)i });
        }
    }
}

