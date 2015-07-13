// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class Test
    {
        [Fact]
        public void TestCurrentCultures()
        {
            CultureInfo defaultCulture = CultureInfo.CurrentCulture;
            CultureInfo newCulture = new CultureInfo(defaultCulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "ar-SA" : "ja-JP");
            CultureInfo defaultUICulture = CultureInfo.CurrentUICulture;
            CultureInfo newUICulture = new CultureInfo(defaultCulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "ar-SA" : "ja-JP");

            CultureInfo.CurrentCulture = newCulture;
            Assert.Equal(CultureInfo.CurrentCulture, newCulture);

            CultureInfo.CurrentUICulture = newUICulture;
            Assert.Equal(CultureInfo.CurrentUICulture, newUICulture);

            CultureInfo.CurrentCulture = defaultCulture;
            Assert.Equal(CultureInfo.CurrentCulture, defaultCulture);

            CultureInfo.CurrentUICulture = defaultUICulture;
            Assert.Equal(CultureInfo.CurrentUICulture, defaultUICulture);
        }
    }
}