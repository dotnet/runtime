// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Diagnostics;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using System.Tests;

namespace System.Collections.Tests
{
    public class CaseInsensitiveComparerTests
    {
        [Theory]
        [InlineData("hello", "HELLO", 0)]
        [InlineData("hello", "hello", 0)]
        [InlineData("HELLO", "HELLO", 0)]
        [InlineData("hello", "goodbye", 1)]
        [InlineData("hello", null, 1)]
        [InlineData(null, "hello", -1)]
        [InlineData(5, 5, 0)]
        [InlineData(10, 5, 1)]
        [InlineData(5, 10, -1)]
        [InlineData(5, null, 1)]
        [InlineData(null, 5, -1)]
        [InlineData(null, null, 0)]
        public void Ctor_Empty_Compare(object a, object b, int expected)
        {
            CaseInsensitiveComparer comparer = new CaseInsensitiveComparer();
            Assert.Equal(expected, Math.Sign(comparer.Compare(a, b)));
        }

        [Theory]
        [InlineData("hello", "HELLO", 0)]
        [InlineData("hello", "hello", 0)]
        [InlineData("HELLO", "HELLO", 0)]
        [InlineData("hello", "goodbye", 1)]
        [InlineData("hello", null, 1)]
        [InlineData(null, "hello", -1)]
        [InlineData(5, 5, 0)]
        [InlineData(10, 5, 1)]
        [InlineData(5, 10, -1)]
        [InlineData(5, null, 1)]
        [InlineData(null, 5, -1)]
        [InlineData(null, null, 0)]
        public void Ctor_CultureInfo_Compare(object a, object b, int expected)
        {
            var cultureNames = Helpers.TestCultureNames;

            foreach (string cultureName in cultureNames)
            {
                CultureInfo culture;
                try
                {
                    culture = new CultureInfo(cultureName);
                }
                catch (CultureNotFoundException)
                {
                    continue;
                }

                var comparer = new CaseInsensitiveComparer(culture);
                Assert.Equal(expected, Math.Sign(comparer.Compare(a, b)));
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37069", TestPlatforms.Android | TestPlatforms.LinuxBionic)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/95338", typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnApplePlatform))]
        public void Ctor_CultureInfo_Compare_TurkishI()
        {
            var cultureNames = Helpers.TestCultureNames;

            foreach (string cultureName in cultureNames)
            {
                CultureInfo culture;
                try
                {
                    culture = new CultureInfo(cultureName);
                }
                catch (CultureNotFoundException)
                {
                    continue;
                }

                var comparer = new CaseInsensitiveComparer(culture);

                // Turkish has lower-case and upper-case version of the dotted "i", so the upper case of "i" (U+0069) isn't "I" (U+0049)
                // but rather U+0130.
                if (culture.Name == "tr-TR")
                {
                    Assert.Equal(1, comparer.Compare("file", "FILE"));
                }
                else
                {
                    Assert.Equal(0, comparer.Compare("file", "FILE"));
                }
            }
        }

        [Fact]
        public void Ctor_CultureInfo_NullCulture_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("culture", () => new CaseInsensitiveComparer(null)); // Culture is null
        }

        [Theory]
        [InlineData("hello", "HELLO", 0)]
        [InlineData("hello", "hello", 0)]
        [InlineData("HELLO", "HELLO", 0)]
        [InlineData("hello", "goodbye", 1)]
        [InlineData("hello", null, 1)]
        [InlineData(null, "hello", -1)]
        [InlineData("file", "FILE", 0)] // Turkey's comparing system is ignored as this is invariant
        [InlineData(5, 5, 0)]
        [InlineData(10, 5, 1)]
        [InlineData(5, 10, -1)]
        [InlineData(5, null, 1)]
        [InlineData(null, 5, -1)]
        [InlineData(null, null, 0)]
        public void DefaultInvariant_Compare(object a, object b, int expected)
        {
            var cultureNames = Helpers.TestCultureNames;

            foreach (string cultureName in cultureNames)
            {
                CultureInfo culture;
                try
                {
                    culture = new CultureInfo(cultureName);
                }
                catch (CultureNotFoundException)
                {
                    continue;
                }

                // Set current culture
                using (new ThreadCultureChange(culture, culture))
                {
                    // All cultures should sort the same way, irrespective of the thread's culture
                    CaseInsensitiveComparer defaultInvComparer = CaseInsensitiveComparer.DefaultInvariant;
                    Assert.Equal(expected, Math.Sign(defaultInvComparer.Compare(a, b)));
                }
            }
        }

        [Theory]
        [InlineData("hello", "HELLO", 0)]
        [InlineData("hello", "hello", 0)]
        [InlineData("HELLO", "HELLO", 0)]
        [InlineData("hello", "goodbye", 1)]
        [InlineData("hello", null, 1)]
        [InlineData(null, "hello", -1)]
        [InlineData(5, 5, 0)]
        [InlineData(10, 5, 1)]
        [InlineData(5, 10, -1)]
        [InlineData(5, null, 1)]
        [InlineData(null, 5, -1)]
        [InlineData(null, null, 0)]
        public void Default_Compare(object a, object b, int expected)
        {
            Assert.Equal(expected, Math.Sign(CaseInsensitiveComparer.Default.Compare(a, b)));
        }

        [Fact]
        public void Default_Compare_TurkishI()
        {
            // Turkish has lower-case and upper-case version of the dotted "i", so the upper case of "i" (U+0069) isn't "I" (U+0049)
            // but rather U+0130.
            CultureInfo culture = CultureInfo.CurrentCulture;
            CaseInsensitiveComparer comparer = CaseInsensitiveComparer.Default;
            if (culture.Name == "tr-TR")
            {
                Assert.Equal(1, comparer.Compare("file", "FILE"));
            }
            else
            {
                Assert.Equal(0, comparer.Compare("file", "FILE"));
            }
        }
    }
}
