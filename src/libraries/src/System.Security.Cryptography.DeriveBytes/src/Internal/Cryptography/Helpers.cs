// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
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
        public static void WriteInt(uint i, byte[] arr, int offset)
        {
            unchecked
            {
                Debug.Assert(arr != null);
                Debug.Assert(arr.Length >= offset + sizeof(uint));

                arr[offset] = (byte)(i >> 24);
                arr[offset + 1] = (byte)(i >> 16);
                arr[offset + 2] = (byte)(i >> 8);
                arr[offset + 3] = (byte)i;
            }
        }
    }
}

