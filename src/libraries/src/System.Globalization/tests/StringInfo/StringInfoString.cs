// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoString
    {
        private const int c_MINI_STRING_LENGTH = 8;
        private const int c_MAX_STRING_LENGTH = 256;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        // PosTest1: The string property in stringinfo object is a random string argument
        [Fact]
        public void TestRandomStringValue()
        {
            string str = _generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            StringInfo stringInfo = new StringInfo(str);
            Assert.Equal(str, stringInfo.String);
        }

        // PosTest2: The string property in stringinfo object is an empty string argument
        [Fact]
        public void TestEmptyString()
        {
            string str = string.Empty;
            StringInfo stringInfo = new StringInfo(str);
            Assert.Equal(string.Empty, stringInfo.String);
        }

        // PosTest3:  Check an instance with a string of white space
        [Fact]
        public void TestWhitespaceString()
        {
            string str = " ";
            StringInfo stringInfo = new StringInfo(str);
            Assert.Equal(" ", stringInfo.String);
        }

        // PosTest4:  Set the property with a random string value
        [Fact]
        public void TestSetProperty()
        {
            string str = _generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            StringInfo stringInfo = new StringInfo();
            stringInfo.String = str;
            Assert.Equal(str, stringInfo.String);
        }

        // NegTest1: The property was set with a value of null
        [Fact]
        public void TestNullReference()
        {
            string str = null;
            StringInfo stringInfo = new StringInfo();
            Assert.Throws<ArgumentNullException>(() =>
            {
                stringInfo.String = str;
            });
        }
    }
}
