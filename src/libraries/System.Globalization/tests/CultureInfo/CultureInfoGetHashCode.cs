// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoGetHashCode
    {
        [Fact]
        public void PosTest1()
        {
            CultureInfo myCultureInfo = new CultureInfo("en-US");

            // the only guarantee that can be made about HashCodes is that they will be the same across calls
            int actualValue = myCultureInfo.GetHashCode();
            int expectedValue = myCultureInfo.GetHashCode();
            Assert.Equal(actualValue, expectedValue);
        }

        [Fact]
        public void PosTest2()
        {
            CultureInfo myCultureInfo = new CultureInfo("en");

            // the only guarantee that can be made about HashCodes is that they will be the same across calls
            int actualValue = myCultureInfo.GetHashCode();
            int expectedValue = myCultureInfo.GetHashCode();
            Assert.Equal(actualValue, expectedValue);
        }

        [Fact]
        public void PosTest3()
        {
            CultureInfo myCultureInfo = CultureInfo.InvariantCulture;

            // the only guarantee that can be made about HashCodes is that they will be the same across calls
            int actualValue = myCultureInfo.GetHashCode();
            int expectedValue = myCultureInfo.GetHashCode();
            Assert.Equal(actualValue, expectedValue);
        }
    }
}