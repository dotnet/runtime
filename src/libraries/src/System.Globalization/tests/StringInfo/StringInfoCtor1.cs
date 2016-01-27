// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class StringInfoCtor1
    {
        // PosTest1: Call constructor to create an instance
        [Fact]
        public void TestCtor()
        {
            StringInfo stringInfo = new StringInfo();
            Assert.Equal(string.Empty, stringInfo.String);
        }
    }
}
