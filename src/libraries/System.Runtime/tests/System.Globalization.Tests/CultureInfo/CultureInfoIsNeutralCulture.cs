// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoIsNeutralCulture
    {
        public static IEnumerable<object[]> IsNeutralCulture_TestData()
        {
            yield return new object[] { new CultureInfo(CultureInfo.InvariantCulture.Name), false };
            yield return new object[] { CultureInfo.InvariantCulture, false };
            yield return new object[] { new CultureInfo("fr-FR"), false };
            yield return new object[] { new CultureInfo("fr"), true };
        }

        [Theory]
        [MemberData(nameof(IsNeutralCulture_TestData))]
        public void IsNeutralCulture(CultureInfo culture, bool expected)
        {
            Assert.Equal(expected, culture.IsNeutralCulture);
        }
    }
}
