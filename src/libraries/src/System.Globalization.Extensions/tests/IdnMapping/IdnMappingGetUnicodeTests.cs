// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class IdnMappingGetUnicodeTests
    {
        public static IEnumerable<object[]> GetUnicode_Invalid_TestData()
        {
            // Ascii is null
            yield return new object[] { null, 0, 0, typeof(ArgumentNullException) };
            yield return new object[] { null, -5, -10, typeof(ArgumentNullException) };

            // Index or count are invalid
            yield return new object[] { "abc", -1, 0, typeof(ArgumentOutOfRangeException) };
            yield return new object[] { "abc", 0, -1, typeof(ArgumentOutOfRangeException) };
            yield return new object[] { "abc", -5, -10, typeof(ArgumentOutOfRangeException) };
            yield return new object[] { "abc", 2, 2, typeof(ArgumentOutOfRangeException) };
            yield return new object[] { "abc", 4, 99, typeof(ArgumentOutOfRangeException) };
            yield return new object[] { "abc", 3, 0, typeof(ArgumentException) };

            // Null containing strings
            yield return new object[] { "abc\u0000", 0, 4, typeof(ArgumentException) };
            yield return new object[] { "ab\u0000c", 0, 4, typeof(ArgumentException) };

            // Invalid unicode strings
            for (int i = 0; i <= 0x1F; i++)
            {
                yield return new object[] { "abc" + (char)i + "def", 0, 7, typeof(ArgumentException) };
            }
            
            yield return new object[] { "abc" + (char)0x7F + "def", 0, 7, typeof(ArgumentException) };
        }

        [Theory]
        [MemberData(nameof(GetUnicode_Invalid_TestData))]
        public void GetUnicode_Invalid(string ascii, int index, int count, Type exceptionType)
        {
            GetUnicode_Invalid(new IdnMapping() { UseStd3AsciiRules = false }, ascii, index, count, exceptionType);
            GetUnicode_Invalid(new IdnMapping() { UseStd3AsciiRules = true }, ascii, index, count, exceptionType);
        }

        public static void GetUnicode_Invalid(IdnMapping idnMapping, string ascii, int index, int count, Type exceptionType)
        {
            if (ascii == null || index + count == ascii.Length)
            {
                if (ascii == null || index == 0)
                {
                    Assert.Throws(exceptionType, () => idnMapping.GetUnicode(ascii));
                }
                Assert.Throws(exceptionType, () => idnMapping.GetUnicode(ascii, index));
            }
            Assert.Throws(exceptionType, () => idnMapping.GetAscii(ascii, index, count));
        }
    }
}
