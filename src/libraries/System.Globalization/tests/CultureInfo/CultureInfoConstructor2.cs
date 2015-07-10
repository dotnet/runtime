// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoConstructor2
    {
        [Fact]
        public void TestDiffCulture()
        {
            CultureInfo myCulture = new CultureInfo("");
            CultureInfo myCultureEn = new CultureInfo("en");
            CultureInfo myCultureFr = new CultureInfo("fr-FR");
            CultureInfo myCultureDe = new CultureInfo("de-DE");
        }

        [Fact]
        public void TestNullCulture()
        {
            Assert.Throws<ArgumentNullException>(() => { CultureInfo myCulture = new CultureInfo(null); });
        }

        [Fact]
        public void TestInvalidCulture()
        {
            Assert.Throws<CultureNotFoundException>(() => { CultureInfo myCulture = new CultureInfo("NotAValidCulture"); });
        }
    }
}