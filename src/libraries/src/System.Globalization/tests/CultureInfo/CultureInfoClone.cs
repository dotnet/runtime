// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
