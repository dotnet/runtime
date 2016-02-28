// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UTF7EncodingGetByteCount
    {
        public static IEnumerable<object[]> GetByteCount_TestData()
        {
            const string DirectChars = "\t\n\rXYZabc123";
            const int DirectCharsLength = 12;
            yield return new object[] { new UTF7Encoding(false), DirectChars, 0, DirectChars.Length, DirectCharsLength };
            yield return new object[] { new UTF7Encoding(true), DirectChars, 0, DirectChars.Length, DirectCharsLength };

            const string OptionalChars = "!\"#$%&*;<=>@[]^_`{|}";
            yield return new object[] { new UTF7Encoding(false), OptionalChars, 0, OptionalChars.Length, 56 };
            yield return new object[] { new UTF7Encoding(true), OptionalChars, 0, OptionalChars.Length, 20 };

            const string SpecialChars = "\u03a0\u03a3";
            const int SpecialCharsLength = 8;
            yield return new object[] { new UTF7Encoding(false), SpecialChars, 0, SpecialChars.Length, SpecialCharsLength };
            yield return new object[] { new UTF7Encoding(true), SpecialChars, 0, SpecialChars.Length, SpecialCharsLength };

            yield return new object[] { new UTF7Encoding(false), string.Empty, 0, 0, 0 };
            yield return new object[] { new UTF7Encoding(true), string.Empty, 0, 0, 0 };
        }

        [Theory]
        [MemberData(nameof(GetByteCount_TestData))]
        public void GetByteCount(UTF7Encoding encoding, string chars, int index, int count, int expected)
        {
            EncodingHelpers.GetByteCount(encoding, chars, index, count, expected);
        }
    }
}
