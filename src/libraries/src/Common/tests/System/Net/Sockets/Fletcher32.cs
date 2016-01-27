// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Sockets.Tests
{
    // Utility type for calculating a running Fletcher-32 checksum.
    internal struct Fletcher32
    {
        private ushort _sum1, _sum2;
        private byte? _leftover;

        public uint Sum 
        {
            get
            {
                ushort s1 = _sum1, s2 = _sum2;
                if (_leftover != null)
                {
                    Add((byte)_leftover, 0, ref s1, ref s2);
                }

                return (uint)s1 << 16 | (uint)s2;
            }
        }

        private static void Add(byte lo, byte hi, ref ushort sum1, ref ushort sum2)
        {
            ushort word = (ushort)((ushort)lo << 8 | (ushort)hi);
            sum1 = (ushort)((sum1 + word) % 65535);
            sum2 = (ushort)((sum2 + sum1) % 65535);
        }

        public void Add(byte[] bytes, int offset, int count)
        {
            int numBytes = count;
            if (numBytes == 0)
            {
                return;
            }

            int i = offset;
            if (_leftover != null)
            {
                Add((byte)_leftover, bytes[i], ref _sum1, ref _sum2);
                i++;
                numBytes--;
            }

            int words = numBytes / 2;
            for (int w = 0; w < words; w++, i += 2)
            {
                Add(bytes[i], bytes[i + 1], ref _sum1, ref _sum2);
            }

            _leftover = numBytes % 2 != 0 ? ((byte?)bytes[i]) : null;
        }

        public static uint Checksum(byte[] bytes, int offset, int count)
        {
            var fletcher32 = new Fletcher32();
            fletcher32.Add(bytes, offset, count);
            return fletcher32.Sum;
        }
    }
}
