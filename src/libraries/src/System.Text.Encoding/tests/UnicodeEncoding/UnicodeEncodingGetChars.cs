// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UnicodeEncodingGetChars
    {
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        public static IEnumerable<object[]> GetChars_TestData()
        {
            string randomString = EncodingHelpers.GetRandomString(10);
            byte[] randomBytesBasic = new UnicodeEncoding().GetBytes(randomString);

            yield return new object[] { randomBytesBasic, 0, randomBytesBasic.Length, new char[randomString.Length], 0, randomString.ToCharArray() };

            int randomByteCount = s_randomDataGenerator.GetInt16(-55) % randomString.Length + 1;
            byte[] randomBytesAdvanced = new UnicodeEncoding().GetBytes(randomString.ToCharArray());
            yield return new object[] { randomBytesAdvanced, 0, randomByteCount * 2, new char[randomString.Length], 0, randomString.ToCharArray(0, randomByteCount) };
            
            yield return new object[] { randomBytesBasic, 0, 0, new char[randomString.Length], randomString.Length, new char[0] };

            yield return new object[] { randomBytesBasic, 20, 0, new char[randomString.Length], randomString.Length, new char[0] };
        }

        [Theory]
        [MemberData(nameof(GetChars_TestData))]
        public void GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, char[] expectedChars)
        {
            EncodingHelpers.GetChars(new UnicodeEncoding(), bytes, byteIndex, byteCount, chars, charIndex, expectedChars);
        }
    }
}
