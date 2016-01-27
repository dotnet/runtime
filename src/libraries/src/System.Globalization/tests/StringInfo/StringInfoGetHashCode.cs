// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoGetHashCode
    {
        private const int c_MINI_STRING_LENGTH = 8;
        private const int c_MAX_STRING_LENGTH = 256;
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        // PosTest1: The two instances created by the same argument return the same hashcode
        [Fact]
        public void TestInstancesWithSameArg()
        {
            string str = _generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            StringInfo stringInfo1 = new StringInfo(str);
            StringInfo stringInfo2 = new StringInfo(str);
            Assert.Equal(stringInfo2.GetHashCode(), stringInfo1.GetHashCode());
        }

        // PosTest2: Check two different instance
        [Fact]
        public void TestDiffInstances()
        {
            string str = _generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            StringInfo stringInfo1 = new StringInfo(str);
            StringInfo stringInfo2 = new StringInfo("");
            Assert.NotEqual(stringInfo2.GetHashCode(), stringInfo1.GetHashCode());
        }
    }
}
