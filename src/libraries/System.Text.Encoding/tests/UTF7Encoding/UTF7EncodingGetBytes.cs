// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UTF7EncodingGetBytes
    {
        public static IEnumerable<object[]> GetBytes_TestData()
        {
            string chars1 = "\u0023\u0025\u03a0\u03a3";
            int charsLength = new UTF7Encoding().GetByteCount(chars1.ToCharArray(), 1, 2);
            yield return new object[] { chars1, 1, 2, new byte[charsLength], 0, new byte[] { 43, 65, 67, 85, 68, 111, 65, 45 } };

            yield return new object[] { string.Empty, 0, 0, new byte[0], 0, new byte[0] };

            string chars2 = "UTF7 Encoding Example";
            yield return new object[] { chars2, 1, 2, new byte[chars2.Length], 0, new byte[] { 84, 70 } };
        }

        [Theory]
        [MemberData(nameof(GetBytes_TestData))]
        public void GetBytes(string source, int index, int count, byte[] bytes, int byteIndex, byte[] expectedBytes)
        {
            EncodingHelpers.GetBytes(new UTF7Encoding(), source, index, count, bytes, byteIndex, expectedBytes);
        }
    }
}
