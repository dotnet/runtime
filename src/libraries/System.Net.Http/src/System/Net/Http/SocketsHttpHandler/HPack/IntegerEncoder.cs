// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http.HPack
{
    internal static class IntegerEncoder
    {
        public static bool Encode(int value, int numBits, Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length == 0)
            {
                bytesWritten = 0;
                return false;
            }

            destination[0] &= MaskHigh(8 - numBits);

            if (value < (1 << numBits) - 1)
            {
                destination[0] |= (byte)value;

                bytesWritten = 1;
                return true;
            }
            else
            {
                destination[0] |= (byte)((1 << numBits) - 1);

                if (1 == destination.Length)
                {
                    bytesWritten = 0;
                    return false;
                }

                value = value - ((1 << numBits) - 1);
                int i = 1;

                while (value >= 128)
                {
                    destination[i++] = (byte)(value % 128 + 128);

                    if (i > destination.Length)
                    {
                        bytesWritten = 0;
                        return false;
                    }

                    value = value / 128;
                }
                destination[i++] = (byte)value;

                bytesWritten = i;
                return true;
            }
        }

        private static byte MaskHigh(int n) => (byte)(sbyte.MinValue >> (n - 1));
    }
}
