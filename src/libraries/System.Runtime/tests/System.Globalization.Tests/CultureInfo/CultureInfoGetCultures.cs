// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoGetCultures
    {
        [Fact]
        public void GetSpecificCultures()
        {
            var specificCultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
            Assert.True(specificCultures.Length > 0);
            Assert.All(specificCultures, c => Assert.True(c.IsNeutralCulture == false));
        }

        [Fact]
        public void GetAllCultures()
        {
            var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            Assert.True(allCultures.Length > 0);
        }
    }
}
