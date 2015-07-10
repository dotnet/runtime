// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoCurrencyGroupSeparator
    {
        // PosTest1: Verify default value of property CurrencyGroupSeparator
        [Fact]
        public void TestDefaultValue()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            Assert.Equal(",", nfi.CurrencyGroupSeparator);
        }

        // PosTest2: Verify set value of property CurrencyGroupSeparator
        [Fact]
        public void TestSetValue()
        {
            string testStr = "testStr";
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.CurrencyGroupSeparator = testStr;
            Assert.Equal(testStr, nfi.CurrencyGroupSeparator);
        }

        // NegTest1: ArgumentNullException is not thrown
        [Fact]
        public void TestNull()
        {
            string testStr = null;
            NumberFormatInfo nfi = new NumberFormatInfo();
            Assert.Throws<ArgumentNullException>(() =>
            {
                nfi.CurrencyGroupSeparator = testStr;
            });
        }

        // NegTest2: InvalidOperationException is not thrown
        [Fact]
        public void TestInvalidOperation()
        {
            string testStr = "testStr";
            NumberFormatInfo nfi = new NumberFormatInfo();
            NumberFormatInfo nfiReadOnly = NumberFormatInfo.ReadOnly(nfi);
            Assert.Throws<InvalidOperationException>(() =>
            {
                nfiReadOnly.CurrencyGroupSeparator = testStr;
            });
        }
    }
}
