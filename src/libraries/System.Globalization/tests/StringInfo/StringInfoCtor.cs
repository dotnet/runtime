// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoCtorTests
    {
        [Fact]
        public void Ctor_Empty()
        {
            StringInfo stringInfo = new StringInfo();
            Assert.Equal(string.Empty, stringInfo.String);
            Assert.Equal(0, stringInfo.LengthInTextElements);
        }

        public static IEnumerable<object[]> Ctor_String_TestData()
        {
            yield return new object[] { new string('a', 256), 256 };
            yield return new object[] { "\u4f00\u302a\ud800\udc00\u4f01", 3 };
            yield return new object[] { "abcdefgh", 8 };
            yield return new object[] { "zj\uDBFF\uDFFFlk", 5 };
            yield return new object[] { "!@#$%^&", 7 };
            yield return new object[] { "!\u20D1bo\uFE22\u20D1\u20EB|", 4 };
            yield return new object[] { "1\uDBFF\uDFFF@\uFE22\u20D1\u20EB9", 4 };
            yield return new object[] { "a\u0300", 1 };
            yield return new object[] { "   ", 3 };
            yield return new object[] { "", 0 };
        }

        [Theory]
        [MemberData(nameof(Ctor_String_TestData))]
        public void Ctor_String(string value, int lengthInTextElements)
        {
            var stringInfo = new StringInfo(value);
            Assert.Same(value, stringInfo.String);
            Assert.Equal(lengthInTextElements, stringInfo.LengthInTextElements);
        }
        
        [Fact]
        public void Ctor_String_Null_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("String", () => new StringInfo(null));
        }
    }
}
