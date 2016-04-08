// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class ASCIIEncodingGetChars
    {
        private const int MinStringLength = 2;
        private const int MaxStringLength = 260;

        private const char MinASCIIChar = (char)0x0;
        private const char MaxASCIIChar = (char)0x7f;

        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> GetChars_TestData()
        {
            yield return new object[] { new byte[0], 0, 0 };
            yield return new object[] { new byte[10], 5, 0 };
            yield return new object[] { new byte[10], 10, 0 };

            string randomString = s_randomDataGenerator.GetString(-55, false, MinStringLength, MaxStringLength);
            byte[] randomBytes = new ASCIIEncoding().GetBytes(randomString);
            int randomByteIndex = s_randomDataGenerator.GetInt32(-55) % randomBytes.Length;
            int randomByteCount = s_randomDataGenerator.GetInt32(-55) % (randomBytes.Length - randomByteIndex) + 1;
            yield return new object[] { randomBytes, 0, randomBytes.Length };
            yield return new object[] { randomBytes, randomByteIndex, randomByteCount };
        }

        [Theory]
        [MemberData(nameof(GetChars_TestData))]
        public void GetChars(byte[] bytes, int index, int count)
        {
            char[] expectedChars = new char[count];
            for (int i = 0; i < count; i++)
            {
                expectedChars[i] = (char)bytes[i + index]; 
            }
            EncodingHelpers.Decode(new ASCIIEncoding(), bytes, index, count, expectedChars);
        }
    }
}
