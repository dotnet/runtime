// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class CultureInfoNativeName
    {
        public static IEnumerable<object[]> NativeName_TestData()
        {
            yield return new object[] { CultureInfo.CurrentCulture.Name, CultureInfo.CurrentCulture.NativeName };

            // Android has its own ICU, which doesn't 100% map to UsingLimitedCultures
            if (PlatformDetection.IsNotUsingLimitedCultures || PlatformDetection.IsAndroid || PlatformDetection.IsHybridGlobalizationOnApplePlatform)
            {
                yield return new object[] { "en-US", "English (United States)" };
                yield return new object[] { "en-CA", "English (Canada)" };
                yield return new object[] { "en-GB", "English (United Kingdom)" };
            }
            else
            {
                // Mobile / Browser ICU doesn't contain CultureInfo.NativeName
                yield return new object[] { "en-US", "en (US)" };
                yield return new object[] { "en-CA", "en (CA)" };
            }
        }

        [Theory]
        [MemberData(nameof(NativeName_TestData))]
        public void NativeName(string name, string expected)
        {
            CultureInfo myTestCulture = new CultureInfo(name);
            Assert.Equal(expected, myTestCulture.NativeName);
        }
    }
}
