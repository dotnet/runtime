// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoCtor2
    {
        private const int c_MINI_STRING_LENGTH = 8;
        private const int c_MAX_STRING_LENGTH = 256;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        // PosTest1: Call constructor to create an instance with a random string argument
        [Fact]
        public void TestCtorWithRandomString()
        {
            string str = _generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            StringInfo stringInfo = new StringInfo(str);
            Assert.Equal(str, stringInfo.String);
        }

        // PosTest2: Call constructor to create an instance with an empty string argument
        [Fact]
        public void TestCtorWithEmptyString()
        {
            string str = string.Empty;
            StringInfo stringInfo = new StringInfo(str);
            Assert.Equal(string.Empty, stringInfo.String);
        }

        // PosTest3: Call constructor to create an instance with a string of white space
        [Fact]
        public void TestCtorWithWhitespace()
        {
            string str = " ";
            StringInfo stringInfo = new StringInfo(str);
            Assert.Equal(" ", stringInfo.String);
        }

        // NegTest1: The string is a null reference
        [Fact]
        public void TestNullReference()
        {
            string str = null;
            Assert.Throws<ArgumentNullException>(() =>
            {
                StringInfo stringInfo = new StringInfo(str);
            });
        }
    }
}
