// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace SslStress.Utils
{
    // Adapted from https://github.com/dotnet/corefx/blob/41cd99d051102be4ed83f4f9105ae9e73aa48b7c/src/Common/tests/System/IO/Compression/CRC.cs
    public static class CRC
    {
        // Table of CRCs of all 8-bit messages.
        private static readonly ulong[] s_crc_table = new ulong[256];
        public const ulong InitialCrc = 0xffffffffL;

        // Flag: has the table been computed? Initially false.
        private static bool s_crc_table_computed = false;

        // Make the table for a fast CRC.
        // Derivative work of zlib -- https://github.com/madler/zlib/blob/master/crc32.c (hint: L108)
        private static void make_crc_table()
        {
            ulong c;
            int n, k;

            for (n = 0; n < 256; n++)
            {
                c = (ulong)n;
                for (k = 0; k < 8; k++)
                {
                    if ((c & 1) > 0)
                        c = 0xedb88320L ^ (c >> 1);
                    else
                        c >>= 1;
                }
                s_crc_table[n] = c;
            }
            s_crc_table_computed = true;
        }

        // Update a running CRC with the bytes buf[0..len-1]--the CRC
        // should be initialized to all 1's, and the transmitted value
        // is the 1's complement of the final running CRC (see the
        // crc() routine below)).
        public static ulong update_crc(ulong crc, ReadOnlySpan<byte> buf)
        {
            ulong c = crc;
            int n;

            if (!s_crc_table_computed)
                make_crc_table();
            for (n = 0; n < buf.Length; n++)
            {
                c = s_crc_table[(c ^ buf[n]) & 0xff] ^ (c >> 8);
            }
            return c;
        }

        public static ulong CalculateCRC(ReadOnlySpan<byte> buf) => update_crc(InitialCrc, buf) ^ InitialCrc;
    }
}
