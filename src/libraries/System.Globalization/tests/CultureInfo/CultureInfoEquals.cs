// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoEquals
    {
        public static IEnumerable<object[]> Equals_TestData()
        {
            CultureInfo frFRCulture = new CultureInfo("fr-FR");
            yield return new object[] { frFRCulture, frFRCulture.Clone(), true };
            yield return new object[] { frFRCulture, frFRCulture, true };
            yield return new object[] { new CultureInfo("en"), new CultureInfo("en"), true };
            yield return new object[] { new CultureInfo("en-US"), new CultureInfo("en-US"), true };
            yield return new object[] { CultureInfo.InvariantCulture, CultureInfo.InvariantCulture, true };
            yield return new object[] { CultureInfo.InvariantCulture, new CultureInfo(""), true };

            yield return new object[] { new CultureInfo("en"), new CultureInfo("en-US"), false };
            yield return new object[] { new CultureInfo("en-US"), new CultureInfo("fr-FR"), false };
            yield return new object[] { new CultureInfo("en-US"), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals(CultureInfo culture, object value, bool expected)
        {
            Assert.Equal(expected, culture.Equals(value));
        }
    }
}
