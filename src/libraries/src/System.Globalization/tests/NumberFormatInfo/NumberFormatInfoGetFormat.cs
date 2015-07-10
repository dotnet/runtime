// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoGetFormat
    {
        // PosTest1: Verify method GetFormat when arg is a type of NumberFormatInfo
        [Fact]
        public void TestArgNumberFormatInfo()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            Type formatType = typeof(NumberFormatInfo);
            object obj = nfi.GetFormat(formatType);
            bool testVerify = obj is NumberFormatInfo;
            Assert.True(testVerify);
        }

        // PosTest2: Verify method GetFormat when arg is not a type of NumberFormatInfo
        [Fact]
        public void TestArgNotNumberFormatInfo()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            Type formatType = typeof(object);
            Assert.Null(nfi.GetFormat(formatType));
        }
    }
}
