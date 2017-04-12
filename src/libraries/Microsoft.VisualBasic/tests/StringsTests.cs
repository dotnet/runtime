// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class StringsTests
    {
        [Fact]
        public void AscWTest()
        {
            Assert.Equal('3', Strings.AscW('3'));

            Assert.Throws<ArgumentException>(() => Strings.AscW(null));
            Assert.Throws<ArgumentException>(() => Strings.AscW(""));

            Assert.Equal('3', Strings.AscW("3"));
            Assert.Equal('3', Strings.AscW("345"));
        }

        [Theory]
        [InlineData(-32769)]
        [InlineData(65536)]
        public void ChrW_CharCodeOutOfRange(int charCode)
        {
            Assert.Throws<ArgumentException>(() => Strings.ChrW(charCode));
        }

        [Theory]
        [InlineData(97)]
        [InlineData(65)]
        [InlineData(65535)]
        [InlineData(-32768)]
        public void ChrW_CharCodeInRange(int charCode)
        {
            char result = Strings.ChrW(charCode);
            Assert.Equal(Convert.ToChar(charCode & 0XFFFF), result);
        }
    }
}
