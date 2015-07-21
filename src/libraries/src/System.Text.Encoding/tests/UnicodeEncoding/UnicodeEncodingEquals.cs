// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class UnicodeEncodingEquals
    {
        [Fact]
        public void PosTest1()
        {
            UnicodeEncoding uEncoding1 = new UnicodeEncoding();
            UnicodeEncoding uEncoding2 = new UnicodeEncoding(false, true);
            bool actualValue;
            actualValue = uEncoding1.Equals(uEncoding2);
            Assert.True(actualValue);
        }

        [Fact]
        public void PosTest2()
        {
            UnicodeEncoding uEncoding1 = new UnicodeEncoding();
            UnicodeEncoding uEncoding2 = new UnicodeEncoding(false, false);
            bool actualValue;
            actualValue = uEncoding1.Equals(uEncoding2);
            Assert.False(actualValue);
        }

        [Fact]
        public void PosTest3()
        {
            UnicodeEncoding uEncoding = new UnicodeEncoding();
            bool actualValue;
            actualValue = uEncoding.Equals(new TimeSpan());
            Assert.False(actualValue);
        }
    }
}
