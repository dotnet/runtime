// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Globalization;

namespace Internal.Cryptography
{
    internal static class Helpers
    {
        public static byte[] CloneByteArray(this byte[] src)
        {
            return (byte[])(src.Clone());
        }

        // Encode a byte array as an array of upper-case hex characters.
        public static char[] ToHexArrayUpper(this byte[] bytes)
        {
            char[] chars = new char[bytes.Length * 2];
            int i = 0;
            foreach (byte b in bytes)
            {
                chars[i++] = NibbleToHex((byte)(b >> 4));
                chars[i++] = NibbleToHex((byte)(b & 0xF));
            }
            return chars;
        }

        // Encode a byte array as an upper case hex string.
        public static string ToHexStringUpper(this byte[] bytes)
        {
            return new string(ToHexArrayUpper(bytes));
        }

        // Decode a hex string-encoded byte array passed to various X509 crypto api. The parsing rules are overly forgiving but for compat reasons,
        // they cannot be tightened.
        public static byte[] DecodeHexString(this string s)
        {
            int whitespaceCount = 0;

            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsWhiteSpace(s[i]))
                    whitespaceCount++;
            }

            uint cbHex = (uint)(s.Length - whitespaceCount) / 2;
            byte[] hex = new byte[cbHex];

            for (int index = 0, i = 0; index < cbHex; index++)
            {
                if (char.IsWhiteSpace(s[i]))
                {
                    i++;
                }
                else
                {
                    hex[index] = (byte)((HexToByte(s[i]) << 4) | HexToByte(s[i + 1]));
                    i += 2;
                }
            }

            return hex;
        }

        private static byte HexToByte(char val)
        {
            if (val <= '9' && val >= '0')
                return (byte)(val - '0');
            else if (val >= 'a' && val <= 'f')
                return (byte)((val - 'a') + 10);
            else if (val >= 'A' && val <= 'F')
                return (byte)((val - 'A') + 10);
            else
                return 0xFF;
        }

        private static char NibbleToHex(byte b)
        {
            Debug.Assert(b >= 0 && b <= 15);
            return (char)(b >= 0 && b <= 9 ? 
                '0' + b : 
                'A' + (b - 10));
        }

        public static bool ContentsEqual(this byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length)
                return false;

            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[0] != a2[0])
                    return false;
            }
            return true;
        }

        //
        // The following group of helpers emulates the non-public Calendar.IsValidDay() method used by X509Certificate.ToString(bool).
        //
        public static bool IsValidDay(this Calendar calendar, int year, int month, int day, int era)
        {
            return (calendar.IsValidMonth(year, month, era) && day >= 1 && day <= calendar.GetDaysInMonth(year, month, era));
        }

        private static bool IsValidMonth(this Calendar calendar, int year, int month, int era)
        {
            return (calendar.IsValidYear(year, era) && month >= 1 && month <= calendar.GetMonthsInYear(year, era));
        }

        private static bool IsValidYear(this Calendar calendar, int year, int era)
        {
            return (year >= calendar.GetYear(calendar.MinSupportedDateTime) && year <= calendar.GetYear(calendar.MaxSupportedDateTime));
        }
    }
}
