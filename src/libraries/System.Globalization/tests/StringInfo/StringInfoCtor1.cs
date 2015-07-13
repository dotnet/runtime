// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
