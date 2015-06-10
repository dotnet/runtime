// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    internal static class ByteUtils
    {
        internal static byte[] HexToByteArray(this string hexString)
        {
            byte[] ba = new byte[hexString.Length / 2];

            for (int i = 0; i < hexString.Length; i += 2)
            {
                string s = hexString.Substring(i, 2);
                ba[i / 2] = byte.Parse(s, NumberStyles.HexNumber, null);
            }

            return ba;
        }
    }
}
