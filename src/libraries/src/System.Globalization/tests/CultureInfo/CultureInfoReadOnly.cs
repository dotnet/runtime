// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
