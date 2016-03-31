// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class ASCIIEncodingGetBytes
    {
        private const int MinStringLength = 2;
        private const int MaxStringLength = 260;

        private const char MinASCIIChar = (char)0x0;
        private const char MaxASCIIChar = (char)0x7f;

        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> GetBytes_TestData()
        {
            yield return new object[] { string.Empty, 0, 0, new byte[0], 0 };

            string randomString = s_randomDataGenerator.GetString(-55, false, MinStringLength, MaxStringLength);
            int randomIndex = s_randomDataGenerator.GetInt32(-55) % randomString.Length;
            int randomCount = s_randomDataGenerator.GetInt32(-55) % (randomString.Length - randomIndex) + 1;
            int minLength = new ASCIIEncoding().GetByteCount(randomString.Substring(randomIndex, randomCount));
            int randomBytesLength = minLength + s_randomDataGenerator.GetInt32(-55) % (short.MaxValue - minLength);
            byte[] bytes = new byte[randomBytesLength];
            int randomByteIndex = s_randomDataGenerator.GetInt32(-55) % (bytes.Length - minLength + 1);
            yield return new object[] { randomString, randomIndex, randomCount, bytes, randomByteIndex };
        }
        
        [Theory]
        [MemberData(nameof(GetBytes_TestData))]
        public void GetBytes(string source, int index, int count, byte[] bytes, int byteIndex)
        {
            byte[] expectedBytes = new byte[count];
            for (int i = 0; i < expectedBytes.Length; i++)
            {
                if (source[i] >= 0x0 && source[i] <= 0x7f)
                {
                    expectedBytes[i] = (byte)source[i + index];
                }
                else
                {
                    // Verify the fallback character for non-ASCII chars
                    expectedBytes[i] = 63;
                }
            }
            EncodingHelpers.GetBytes(new ASCIIEncoding(), source, index, count, bytes, byteIndex, expectedBytes);
        }
    }
}
