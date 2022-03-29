// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

namespace Internal.Runtime
{
    /// <summary>
    /// Extension methods that provide low level ToString() equivalents for some of the core types.
    /// Calling regular ToString() on these types goes through a lot of the CultureInfo machinery
    /// which is not low level enough to be used everywhere.
    /// </summary>
    internal static class LowLevelStringConverter
    {
        private const string HexDigits = "0123456789ABCDEF";

        // TODO: Rename to ToHexString()
        public static string LowLevelToString(this int arg)
        {
            return ((uint)arg).LowLevelToString();
        }

        // TODO: Rename to ToHexString()
        public static string LowLevelToString(this uint arg)
        {
            StringBuilder sb = new StringBuilder(8);
            int shift = 4 * 8;
            while (shift > 0)
            {
                shift -= 4;
                int digit = (int)((arg >> shift) & 0xF);
                sb.Append(HexDigits[digit]);
            }

            return sb.ToString();
        }

        public static string LowLevelToString(this IntPtr arg)
        {
            StringBuilder sb = new StringBuilder(IntPtr.Size * 4);
            ulong num = (ulong)arg;

            int shift = IntPtr.Size * 8;
            while (shift > 0)
            {
                shift -= 4;
                int digit = (int)((num >> shift) & 0xF);
                sb.Append(HexDigits[digit]);
            }

            return sb.ToString();
        }
    }
}
