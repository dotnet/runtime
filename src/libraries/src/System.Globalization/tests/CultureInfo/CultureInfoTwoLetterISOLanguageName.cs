// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoTwoLetterISOLanguageName
    {
        [Fact]
        public void PosTest1()
        {
            CultureInfo myCultureInfo = new CultureInfo("de-DE");
            string expectedstring = "de";
            Assert.Equal(myCultureInfo.TwoLetterISOLanguageName, expectedstring);
        }

        [Fact]
        public void PosTest2()
        {
            CultureInfo myCultureInfo = new CultureInfo("en");
            string expectedstring = "en";
            Assert.Equal(myCultureInfo.TwoLetterISOLanguageName, expectedstring);
        }

        [Fact]
        public void PosTest3()
        {
            CultureInfo myTestCulture = CultureInfo.InvariantCulture;
            string expectedstring = "iv";
            Assert.Equal(myTestCulture.TwoLetterISOLanguageName, expectedstring);
        }
    }
}
