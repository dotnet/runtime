// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public static partial class EncodingHelpers
    {
        private static void GetByteCount_NetCoreApp(Encoding encoding, string chars, int index, int count, int expected)
        {
            // Use GetByteCount(string, int, int)
            Assert.Equal(expected, encoding.GetByteCount(chars, index, count));
        }

        private static void GetBytes_NetCoreApp(Encoding encoding, string chars, int index, int count, byte[] expected)
        {
            // Use GetBytes(string, int, int)
            byte[] stringResultAdvanced = encoding.GetBytes(chars, index, count);
            VerifyGetBytes(stringResultAdvanced, 0, stringResultAdvanced.Length, new byte[expected.Length], expected);
        }
    }
}
