// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
