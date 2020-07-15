// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            if (PlatformDetection.IsBrowser)
            {
                // Browser's ICU returns "French (FR)" for "fr-FR"
                yield return new object[] { "fr-FR", "French (FR)" };
            }
            else
            {
                yield return new object[] { "fr-FR", "French (France)" };
            }
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
