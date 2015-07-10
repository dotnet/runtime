// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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