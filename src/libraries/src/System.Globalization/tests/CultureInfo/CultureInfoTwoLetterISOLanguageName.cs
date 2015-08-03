// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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