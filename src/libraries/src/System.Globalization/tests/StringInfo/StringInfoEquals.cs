// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoEquals
    {
        private const int c_MINI_STRING_LENGTH = 8;
        private const int c_MAX_STRING_LENGTH = 256;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        // PosTest1: Compare two equal StringInfo
        [Fact]
        public void TestEqualStringInfoWithArg()
        {
            string str = _generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            StringInfo stringInfo1 = new StringInfo(str);
            StringInfo stringInfo2 = new StringInfo(str);
            Assert.True(stringInfo1.Equals(stringInfo2));
        }

        // PosTest2: The two stringinfos reference to one object
        [Fact]
        public void TestSameReference()
        {
            string str = _generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            StringInfo stringInfo1 = new StringInfo(str);
            StringInfo stringInfo2 = stringInfo1;
            Assert.True(stringInfo1.Equals(stringInfo2));
        }

        // PosTest3: Using default constructor to create two equal instance
        [Fact]
        public void TestEqualStringInfoWithNoArg()
        {
            StringInfo stringInfo1 = new StringInfo();
            StringInfo stringInfo2 = new StringInfo();
            Assert.True(stringInfo1.Equals(stringInfo2));
        }

        // PosTest4: Compare two instance with different string value
        [Fact]
        public void TestDiffStringInfo()
        {
            StringInfo stringInfo1 = new StringInfo("stringinfo1");
            StringInfo stringInfo2 = new StringInfo("stringinfo2");
            Assert.False(stringInfo1.Equals(stringInfo2));
        }

        // PosTest5: Compare with a different kind of type
        [Fact]
        public void TestDiffType()
        {
            StringInfo stringInfo1 = new StringInfo("stringinfo1");
            string str = "stringinfo1";
            Assert.False(stringInfo1.Equals(str));
        }

        // PosTest6: The argument is a null reference
        [Fact]
        public void TestNullReference()
        {
            StringInfo stringInfo1 = new StringInfo("stringinfo1");
            object ob = null;
            Assert.False(stringInfo1.Equals(ob));
        }

        // PosTest7: The argument is value type
        [Fact]
        public void TestValueType()
        {
            StringInfo stringInfo1 = new StringInfo("123");
            int i = 123;
            Assert.False(stringInfo1.Equals(i));
        }
    }
}
