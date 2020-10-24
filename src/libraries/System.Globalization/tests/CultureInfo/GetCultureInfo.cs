// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class GetCultureInfoTests
    {
        public static bool PlatformSupportsFakeCulture => !PlatformDetection.IsWindows || (PlatformDetection.WindowsVersion >= 10 && !PlatformDetection.IsNetFramework);

        [ConditionalTheory(nameof(PlatformSupportsFakeCulture))]
        [InlineData("en")]
        [InlineData("en-US")]
        [InlineData("ja-JP")]
        [InlineData("ar-SA")]
        [InlineData("xx-XX")]
        public void GetCultureInfo(string name)
        {
            Assert.Equal(name, CultureInfo.GetCultureInfo(name).Name);
            Assert.Equal(name, CultureInfo.GetCultureInfo(name, predefinedOnly: false).Name);
        }

        [ConditionalTheory(nameof(PlatformSupportsFakeCulture))]
        [InlineData("en@US")]
        [InlineData("\uFFFF")]
        public void TestInvalidCultureNames(string name)
        {
            Assert.Throws<CultureNotFoundException>(() => CultureInfo.GetCultureInfo(name));
            Assert.Throws<CultureNotFoundException>(() => CultureInfo.GetCultureInfo(name, predefinedOnly: false));
            Assert.Throws<CultureNotFoundException>(() => CultureInfo.GetCultureInfo(name, predefinedOnly: true));
        }

        [ConditionalTheory(nameof(PlatformSupportsFakeCulture))]
        [InlineData("en")]
        [InlineData("en-US")]
        [InlineData("ja-JP")]
        [InlineData("ar-SA")]
        public void TestGetCultureInfoWithNoneConstructedCultures(string name)
        {
            Assert.Equal(name, CultureInfo.GetCultureInfo(name).Name);
            Assert.Equal(name, CultureInfo.GetCultureInfo(name, predefinedOnly: false).Name);
            Assert.Equal(name, CultureInfo.GetCultureInfo(name, predefinedOnly: true).Name);
        }

        [ConditionalTheory(nameof(PlatformSupportsFakeCulture))]
        [InlineData("xx")]
        [InlineData("xx-XX")]
        [InlineData("xx-YY")]
        public void TestFakeCultureNames(string name)
        {
            Assert.Equal(name, CultureInfo.GetCultureInfo(name).Name);
            Assert.Equal(name, CultureInfo.GetCultureInfo(name, predefinedOnly: false).Name);
            Assert.Throws<CultureNotFoundException>(() => CultureInfo.GetCultureInfo(name, predefinedOnly: true));
        }
    }
}
