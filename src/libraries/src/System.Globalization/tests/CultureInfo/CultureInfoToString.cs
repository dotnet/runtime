// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoToString
    {
        [Fact]
        public void PosTest1()
        {
            string expectName = "en-US";
            CultureInfo myCultureInfo = new CultureInfo(expectName);
            Assert.Equal(expectName, myCultureInfo.ToString());
        }

        [Fact]
        public void PosTest2()
        {
            string expectName = "en";
            CultureInfo myCultureInfo = new CultureInfo(expectName);
            Assert.Equal(expectName, myCultureInfo.ToString());
        }

        [Fact]
        public void PosTest3()
        {
            string expectName = "";
            CultureInfo myCultureInfo = new CultureInfo(expectName);
            Assert.Equal(expectName, myCultureInfo.ToString());
        }
    }
}
