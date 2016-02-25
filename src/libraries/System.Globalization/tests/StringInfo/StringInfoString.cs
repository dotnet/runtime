// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoString
    {
        private const int MinStringLength = 8;
        private const int MaxStringLength = 256;
        private static readonly RandomDataGenerator s_randomDataGenerator = new RandomDataGenerator();
        
        [Fact]
        public void String_Set()
        {
            string value = s_randomDataGenerator.GetString(-55, false, MinStringLength, MaxStringLength);
            StringInfo stringInfo = new StringInfo();
            stringInfo.String = value;
            Assert.Equal(value, stringInfo.String);
        }
        
        [Fact]
        public void String_Set_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => new StringInfo().String = null);
        }
    }
}
