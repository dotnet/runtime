// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

public class CRC
{
    // Table of CRCs of all 8-bit messages.
    private static ulong[] s_crc_table = new ulong[256];

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
                    c = c >> 1;
            }
            s_crc_table[n] = c;
        }
        s_crc_table_computed = true;
    }

    // Update a running CRC with the bytes buf[0..len-1]--the CRC
    // should be initialized to all 1's, and the transmitted value
    // is the 1's complement of the final running CRC (see the
    // crc() routine below)).
    private static ulong update_crc(ulong crc, byte[] buf, int len)
    {
        ulong c = crc;
        int n;

        if (!s_crc_table_computed)
            make_crc_table();
        for (n = 0; n < len; n++)
        {
            c = s_crc_table[(c ^ buf[n]) & 0xff] ^ (c >> 8);
        }
        return c;
    }

    internal static string CalculateCRC(byte[] buf) => CalculateCRC(buf, buf.Length);

    // Return the CRC of the bytes buf[0..len-1].
    internal static string CalculateCRC(byte[] buf, int len) =>
        (update_crc(0xffffffffL, buf, len) ^ 0xffffffffL).ToString();
}
