// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoEquals
    {
        [Fact]
        public void TestClone()
        {
            CultureInfo myCultureInfo = new CultureInfo("fr-FR");
            CultureInfo myClone = myCultureInfo.Clone() as CultureInfo;
            Assert.True(myClone.Equals(myCultureInfo));
        }

        [Fact]
        public void TestEqualCultureName()
        {
            CultureInfo myCultureInfo = new CultureInfo("en");
            CultureInfo myCultureInfo1 = new CultureInfo("en");
            Assert.True(myCultureInfo1.Equals(myCultureInfo));
        }

        [Fact]
        public void TestInvariantCulture()
        {
            CultureInfo myCultureInfo = CultureInfo.InvariantCulture;
            CultureInfo myCultureInfo1 = CultureInfo.InvariantCulture;
            Assert.True(myCultureInfo.Equals(myCultureInfo1));
        }

        [Fact]
        public void TestEqualNameAndCultureIdentifier()
        {
            CultureInfo myCultureInfo = new CultureInfo("en");
            CultureInfo myCultureInfo1 = new CultureInfo("en-US");
            Assert.False(myCultureInfo1.Equals(myCultureInfo));
        }

        [Fact]
        public void TestSameCultureInfo()
        {
            CultureInfo myCultureInfo = new CultureInfo("en-US");
            Assert.True(myCultureInfo.Equals(myCultureInfo));
        }

        [Fact]
        public void TestNull()
        {
            CultureInfo myCultureInfo = new CultureInfo("en-US");
            Assert.False(myCultureInfo.Equals(null));
        }

        [Fact]
        public void TestEqualCultureIdentifier()
        {
            CultureInfo myCultureInfo = new CultureInfo("en-US");
            CultureInfo myCultureInfo1 = new CultureInfo("en-US");
            Assert.True(myCultureInfo1.Equals(myCultureInfo));
        }

        [Fact]
        public void TestUnequal()
        {
            CultureInfo myCultureInfo = new CultureInfo("en-US");
            CultureInfo myCultureInfo1 = new CultureInfo("fr-FR");
            Assert.False(myCultureInfo1.Equals(myCultureInfo));
        }
    }
}