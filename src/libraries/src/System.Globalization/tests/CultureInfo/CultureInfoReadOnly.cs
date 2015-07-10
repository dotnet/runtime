// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoReadOnly
    {
        [Fact]
        public void PosTest1()
        {
            CultureInfo myCultureInfo = new CultureInfo("en-US");
            Assert.False(myCultureInfo.IsReadOnly);
            myCultureInfo = CultureInfo.ReadOnly(myCultureInfo);
            Assert.True(myCultureInfo.IsReadOnly);
        }

        [Fact]
        public void PosTest2()
        {
            CultureInfo myCultureInfo = new CultureInfo("en");
            Assert.False(myCultureInfo.IsReadOnly);
            myCultureInfo = CultureInfo.ReadOnly(myCultureInfo);
            Assert.True(myCultureInfo.IsReadOnly);
        }

        [Fact]
        public void PosTest3()
        {
            CultureInfo myCultureInfo = CultureInfo.InvariantCulture;
            Assert.True(myCultureInfo.IsReadOnly);
            myCultureInfo = CultureInfo.ReadOnly(myCultureInfo);
            Assert.True(myCultureInfo.IsReadOnly);
        }
    }
}