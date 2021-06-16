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

            if (PlatformDetection.IsNotUsingLimitedCultures)
            {
                yield return new object[] { "en-US", "English (United States)" };
                yield return new object[] { "fr-FR", "French (France)" };
            }
            else
            {
                // Mobile / Browser ICU doesn't contain CultureInfo.EnglishName
                yield return new object[] { "en-US", "en (US)" };
                yield return new object[] { "fr-FR", "fr (FR)" };
            }
        }

        [Theory]
        [MemberData(nameof(EnglishName_TestData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36672", TestPlatforms.Android)]
        public void EnglishName(string name, string expected)
        {
            CultureInfo myTestCulture = new CultureInfo(name);
            Assert.Equal(expected, myTestCulture.EnglishName);
        }
    }
}
