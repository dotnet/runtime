// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoInvariantCulture
    {
        [Fact]
        public void PosTest1()
        {
            CultureInfo myCultureInfo = CultureInfo.InvariantCulture;
            CultureInfo myExpectedCultureInfo = new CultureInfo("");
            Assert.True(myCultureInfo.Equals(myExpectedCultureInfo));
        }
    }
}
