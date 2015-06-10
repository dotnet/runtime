// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static class Helpers
    {
        public static byte[] CloneByteArray(this byte[] src)
        {
            return (byte[])(src.Clone());
        }

        // Encode a byte array as an upper case hex string.
        public static String ToHexStringUpper(this byte[] bytes)
        {
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                sb.AppendFormat("{0:X2}", b);
            }
            return sb.ToString();
        }

        // Decode a hex string-encoded byte array passed to various X509 crypto api. The parsing rules are overly forgiving but for compat reasons,
        // they cannot be tightened.
        public static byte[] DecodeHexString(this String s)
        {
            String hexString = DiscardWhiteSpaces(s);
            uint cbHex = (uint)hexString.Length / 2;
            byte[] hex = new byte[cbHex];
            int i = 0;
            for (int index = 0; index < cbHex; index++)
            {
                hex[index] = (byte)((HexToByte(hexString[i]) << 4) | HexToByte(hexString[i + 1]));
                i += 2;
            }
            return hex;
        }

        private static String DiscardWhiteSpaces(String inputBuffer)
        {
            return DiscardWhiteSpaces(inputBuffer, 0, inputBuffer.Length);
        }

        private static String DiscardWhiteSpaces(String inputBuffer, int inputOffset, int inputCount)
        {
            int i, iCount = 0;
            for (i = 0; i < inputCount; i++)
            {
                if (Char.IsWhiteSpace(inputBuffer[inputOffset + i]))
                {
                    iCount++;
                }
            }

            char[] rgbOut = new char[inputCount - iCount];
            iCount = 0;
            for (i = 0; i < inputCount; i++)
            {
                if (!Char.IsWhiteSpace(inputBuffer[inputOffset + i]))
                {
                    rgbOut[iCount++] = inputBuffer[inputOffset + i];
                }
            }
            return new String(rgbOut);
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


