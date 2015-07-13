// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoToString
    {
        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)] 
        public void PosTest1()
        {
            string expectName = "en-US";
            CultureInfo myCultureInfo = new CultureInfo(expectName);
            Assert.Equal(expectName, myCultureInfo.ToString());
        }

        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)] 
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