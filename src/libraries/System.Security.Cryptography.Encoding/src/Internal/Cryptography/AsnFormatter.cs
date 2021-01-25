// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal abstract partial class AsnFormatter
    {
        internal static AsnFormatter Instance { get { return s_instance; } }

        public string Format(Oid? oid, byte[] rawData, bool multiLine)
        {
            return FormatNative(oid, rawData, multiLine) ?? Convert.ToHexString(rawData);
        }

        protected abstract string? FormatNative(Oid? oid, byte[] rawData, bool multiLine);

        protected static string EncodeSpaceSeparatedHexString(byte[] sArray)
        {
            Debug.Assert(sArray != null && sArray.Length != 0);

            int length = (sArray.Length * 3) - 1; // two chars per byte, plus 1 space between each

            return string.Create(length, sArray, (hexOrder, sArray) =>
            {
                int j = 0;

                for (int i = 0; i < sArray.Length; i++)
                {
                    if (i != 0)
                    {
                        hexOrder[j++] = ' ';
                    }

                    int digit = sArray[i];
                    hexOrder[j++] = HexConverter.ToCharUpper(digit >> 4);
                    hexOrder[j++] = HexConverter.ToCharUpper(digit);
                }

                Debug.Assert(j == hexOrder.Length);
            });
        }
    }
}
