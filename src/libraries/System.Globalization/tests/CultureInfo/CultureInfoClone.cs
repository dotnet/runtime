// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoClone
    {
        [Fact]
        public void TestClone1()
        {
            CultureInfo myCultureInfo = new CultureInfo("fr-FR");
            CultureInfo myClone = myCultureInfo.Clone() as CultureInfo;
            Assert.True(myClone.Equals(myCultureInfo));
            Assert.NotSame(myClone, myCultureInfo);
        }

        [Fact]
        public void TestClone2()
        {
            CultureInfo myCultureInfo = new CultureInfo("en");
            CultureInfo myClone = myCultureInfo.Clone() as CultureInfo;
            Assert.True(myClone.Equals(myCultureInfo));
            Assert.NotSame(myClone, myCultureInfo);
        }

        [Fact]
        public void TestClone3()
        {
            CultureInfo myTestCulture = CultureInfo.InvariantCulture;
            CultureInfo myClone = myTestCulture.Clone() as CultureInfo;
            Assert.True(myClone.Equals(myTestCulture));
            Assert.NotSame(myClone, myTestCulture);
        }
    }
}