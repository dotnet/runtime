// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoCurrencyDecimalSeparator
    {
        // TestDefaultValue: Verify default value of property CurrencyDecimalSeparator
        [Fact]
        public void TestDefaultValue()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            Assert.Equal(".", nfi.CurrencyDecimalSeparator);
        }

        // TestSetValue: Verify set value of property CurrencyDecimalSeparator
        [Fact]
        public void TestSetValue()
        {
            string testStr = "testStr";
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.CurrencyDecimalSeparator = testStr;
            Assert.Equal(testStr, nfi.CurrencyDecimalSeparator);
        }

        // TestNull: ArgumentNullException is thrown
        [Fact]
        public void TestNull()
        {
            string testStr = null;
            NumberFormatInfo nfi = new NumberFormatInfo();
            Assert.Throws<ArgumentNullException>(() =>
            {
                nfi.CurrencyDecimalSeparator = testStr;
            });
        }

        // TestInvalidOperation: InvalidOperationException is thrown
        [Fact]
        public void TestInvalidOperation()
        {
            string testStr = "testStr";
            NumberFormatInfo nfi = new NumberFormatInfo();
            NumberFormatInfo nfiReadOnly = NumberFormatInfo.ReadOnly(nfi);
            Assert.Throws<InvalidOperationException>(() =>
            {
                nfiReadOnly.CurrencyDecimalSeparator = testStr;
            });
        }

        // TestEmptyString: ArgumentException is thrown
        [Fact]
        public void TestEmptyString()
        {
            string testStr = "";
            NumberFormatInfo nfi = new NumberFormatInfo();
            NumberFormatInfo nfiReadOnly = NumberFormatInfo.ReadOnly(nfi);
            Assert.Throws<ArgumentException>(() =>
            {
                nfi.CurrencyDecimalSeparator = testStr;
            });
        }
    }
}
