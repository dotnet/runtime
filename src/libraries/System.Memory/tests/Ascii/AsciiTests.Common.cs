// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers.Text.Tests
{
    public partial class AsciiUnitTests
    {
        private const int SizeOfVector128 = 128 / 8;

        private static byte[] CharsToAsciiBytesChecked(ReadOnlySpan<char> chars)
        {
            byte[] retVal = new byte[chars.Length];
            for (int i = 0; i < chars.Length; i++)
            {
                retVal[i] = checked((byte)chars[i]);
            }
            return retVal;
        }
    }
}
