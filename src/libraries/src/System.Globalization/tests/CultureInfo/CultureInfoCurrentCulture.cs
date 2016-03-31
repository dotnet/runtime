// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoCurrentCultureTests
    {
        [Fact]
        public void CurrentCulture()
        {
            // Run all tests in one method to avoid multi-threading issues
            CultureInfo defaultCulture = CultureInfo.CurrentCulture;
            Assert.NotEqual(CultureInfo.InvariantCulture, defaultCulture);

            CultureInfo newCulture = new CultureInfo(defaultCulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "ar-SA" : "ja-JP");
            CultureInfo.CurrentCulture = newCulture;
            try
            {
                Assert.Equal(CultureInfo.CurrentCulture, newCulture);

                newCulture = new CultureInfo("de-DE_phoneb");
                CultureInfo.CurrentCulture = newCulture;
                Assert.Equal(CultureInfo.CurrentCulture, newCulture);
                Assert.Equal("de-DE_phoneb", newCulture.CompareInfo.Name);
            }
            finally
            {
                CultureInfo.CurrentCulture = defaultCulture;
            }
            Assert.Equal(CultureInfo.CurrentCulture, defaultCulture);
        }

        [Fact]
        public void CurrentUICulture()
        {
            // Run all tests in one method to avoid multi-threading issues
            CultureInfo defaultUICulture = CultureInfo.CurrentUICulture;
            Assert.NotEqual(CultureInfo.InvariantCulture, defaultUICulture);

            CultureInfo newUICulture = new CultureInfo(defaultUICulture.Name.Equals("ja-JP", StringComparison.OrdinalIgnoreCase) ? "ar-SA" : "ja-JP");
            CultureInfo.CurrentUICulture = newUICulture;
            try
            {
                Assert.Equal(CultureInfo.CurrentUICulture, newUICulture);

                newUICulture = new CultureInfo("de-DE_phoneb");
                CultureInfo.CurrentUICulture = newUICulture;
                Assert.Equal(CultureInfo.CurrentUICulture, newUICulture);
                Assert.Equal("de-DE_phoneb", newUICulture.CompareInfo.Name);
            }
            finally
            {
                CultureInfo.CurrentUICulture = defaultUICulture;
            }
            Assert.Equal(CultureInfo.CurrentUICulture, defaultUICulture);
        }
    }
}
