// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoIsNeutralCulture
    {
        [Fact]
        public void PosTest1()
        {
            CultureInfo myCultureInfo = CultureInfo.InvariantCulture;
            Assert.False(myCultureInfo.IsNeutralCulture);
        }

        [Fact]
        public void PosTest2()
        {
            CultureInfo myCultureInfo = new CultureInfo("fr");
            Assert.True(myCultureInfo.IsNeutralCulture);
        }

        [Fact]
        public void PosTest3()
        {
            CultureInfo myCultureInfo = new CultureInfo("fr-FR");
            Assert.False(myCultureInfo.IsNeutralCulture);
        }
    }
}
