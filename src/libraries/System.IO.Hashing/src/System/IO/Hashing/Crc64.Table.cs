// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Hashing
{
    public sealed partial class Crc64 : NonCryptographicHashAlgorithm
    {
        // Pre-computed CRC-64 transition table.
        private static readonly ulong[] s_crcLookup = GenerateTable(0x42F0E1EBA9EA3693);

        private static ulong[] GenerateTable(ulong polynomial)
        {
            ulong[] table = new ulong[256];

            for (int i = 0; i < 256; i++)
            {
                ulong val = (ulong)i << 56;

                for (int j = 0; j < 8; j++)
                {
                    if ((val & 0x8000_0000_0000_0000) == 0)
                    {
                        val <<= 1;
                    }
                    else
                    {
                        val = (val << 1) ^ polynomial;
                    }
                }

                table[i] = val;
            }

            return table;
        }
    }
}
