// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoParent
    {
        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)] 
        public void PosTest1()
        {
            CultureInfo myExpectParentCulture = new CultureInfo("en");
            CultureInfo myTestCulture = new CultureInfo("en-us");
            Assert.Equal(myTestCulture.Parent, myExpectParentCulture);
        }

        [Fact]
        public void PosTest2()
        {
            CultureInfo myTestCulture = new CultureInfo("en");
            CultureInfo myExpectParent = CultureInfo.InvariantCulture;
            Assert.Equal(myTestCulture.Parent, myExpectParent);
        }

        [Fact]
        public void PosTest3()
        {
            CultureInfo myExpectParent1 = new CultureInfo("");
            CultureInfo myTestCulture = CultureInfo.InvariantCulture;
            Assert.Equal(myTestCulture.Parent, myExpectParent1);
        }
    }
}