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
            byte[] randomBytes = new UnicodeEncoding().GetBytes(randomString);

            yield return new object[] { randomBytes, 0, randomBytes.Length, randomString.ToCharArray() };

            int randomByteCount = s_randomDataGenerator.GetInt16(-55) % randomString.Length + 1;
            yield return new object[] { randomBytes, 0, randomByteCount * 2, randomString.ToCharArray(0, randomByteCount) };

            yield return new object[] { randomBytes, 0, 2, randomString.ToCharArray(0, 1) };
            yield return new object[] { randomBytes, 2, 2, randomString.ToCharArray(1, 1) };
            
            yield return new object[] { randomBytes, 0, 0, new char[0] };
            yield return new object[] { randomBytes, 20, 0, new char[0] };
        }

        [Theory]
        [MemberData(nameof(GetChars_TestData))]
        public void GetChars(byte[] bytes, int index, int count, char[] expected)
        {
            EncodingHelpers.Decode(new UnicodeEncoding(), bytes, index, count, expected);
        }
    }
}
