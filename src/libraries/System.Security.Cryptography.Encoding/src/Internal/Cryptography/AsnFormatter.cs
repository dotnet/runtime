// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal abstract partial class AsnFormatter
    {
        private static readonly char[] s_hexValues =
            { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        internal static AsnFormatter Instance { get { return s_instance; } }

        public string Format(Oid oid, byte[] rawData, bool multiLine)
        {
            return FormatNative(oid, rawData, multiLine) ?? EncodeHexString(rawData);
        }

        protected abstract string FormatNative(Oid oid, byte[] rawData, bool multiLine);

        protected static string EncodeHexString(byte[] sArray, bool spaceSeparated = false)
        {
            return EncodeHexString(sArray, 0, (uint)sArray.Length, spaceSeparated);
        }

        private static string EncodeHexString(byte[] sArray, uint start, uint end, bool spaceSeparated)
        {
            string result = null;

            if (sArray != null)
            {
                uint len = (end - start) * 2;

                if (spaceSeparated)
                {
                    // There will be n-1 spaces between n bytes.
                    len += (end - start - 1);
                }

                char[] hexOrder = new char[len];

                for (uint i = start, j = 0; i < end; i++)
                {
                    if (spaceSeparated && i > start)
                    {
                        hexOrder[j++] = ' ';
                    }

                    uint digit = (uint)((sArray[i] & 0xf0) >> 4);
                    hexOrder[j++] = s_hexValues[digit];

                    digit = (uint)(sArray[i] & 0x0f);
                    hexOrder[j++] = s_hexValues[digit];
                }

                result = new string(hexOrder);
            }

            return result;
        }
    }
}
