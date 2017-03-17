// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    internal static class KeyBlobHelpers
    {
        internal static byte[] TrimPaddingByte(byte[] data)
        {
            byte[] dataLocal = data;
            TrimPaddingByte(ref dataLocal);
            return dataLocal;
        }

        internal static void TrimPaddingByte(ref byte[] data)
        {
            if (data[0] != 0)
                return;

            byte[] newData = new byte[data.Length - 1];
            Buffer.BlockCopy(data, 1, newData, 0, newData.Length);
            data = newData;
        }

        internal static byte[] PadOrTrim(byte[] data, int length)
        {
            byte[] dataLocal = data;
            PadOrTrim(ref dataLocal, length);
            return dataLocal;
        }

        internal static void PadOrTrim(ref byte[] data, int length)
        {
            if (data.Length == length)
                return;

            // Need to skip the sign-padding byte.
            if (data.Length == length + 1 && data[0] == 0)
            {
                TrimPaddingByte(ref data);
                return;
            }

            int offset = length - data.Length;

            byte[] newData = new byte[length];
            Buffer.BlockCopy(data, 0, newData, offset, data.Length);
            data = newData;
        }
    }
}
