// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoCtorTests
    {
        private const int MinStringLength = 8;
        private const int MaxStringLength = 256;
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();

        [Fact]
        public void Ctor_Empty()
        {
            StringInfo stringInfo = new StringInfo();
            Assert.Equal(string.Empty, stringInfo.String);
        }

        public static IEnumerable<object[]> Ctor_String_TestData()
        {
            yield return new object[] { s_randomDataGenerator.GetString(-55, false, MinStringLength, MaxStringLength) };
            yield return new object[] { "" };
            yield return new object[] { " " };
        }

        [Theory]
        [MemberData(nameof(Ctor_String_TestData))]
        public void Ctor_String(string value)
        {
            var stringInfo = new StringInfo(value);
            Assert.Equal(value, stringInfo.String);
        }
        
        [Fact]
        public void Ctor_String_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => new StringInfo(null));
        }
    }
}
