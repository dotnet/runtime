// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoEnglishName
    {
        public static IEnumerable<object[]> EnglishName_TestData()
        {
            yield return new object[] { CultureInfo.CurrentCulture.Name, CultureInfo.CurrentCulture.EnglishName };
            yield return new object[] { "en-US", "English (United States)" };
            yield return new object[] { "fr-FR", "French (France)" };
        }

        [Theory]
        [MemberData(nameof(EnglishName_TestData))]
        public void EnglishName(string name, string expected)
        {
            CultureInfo myTestCulture = new CultureInfo(name);
            Assert.Equal(expected, myTestCulture.EnglishName);
        }
    }
}
