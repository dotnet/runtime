// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Xunit;
using System.Collections.Generic;

namespace System.Globalization.Tests
{
    public class StringNormalizationTests
    {
        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34577", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [InlineData("\u00C4\u00C7", NormalizationForm.FormC, true)]
        [InlineData("\u00C4\u00C7", NormalizationForm.FormD, false)]
        [InlineData("A\u0308C\u0327", NormalizationForm.FormC, false)]
        [InlineData("A\u0308C\u0327", NormalizationForm.FormD, true)]
        public void IsNormalized(string value, NormalizationForm normalizationForm, bool expected)
        {
            if (normalizationForm == NormalizationForm.FormC)
            {
                Assert.Equal(expected, value.IsNormalized());
            }
            Assert.Equal(expected, value.IsNormalized(normalizationForm));
        }

        [Fact]
        public void IsNormalized_Invalid()
        {
            Assert.Throws<ArgumentException>(() => "\uFB01".IsNormalized((NormalizationForm)10));
            AssertExtensions.Throws<ArgumentException>("strInput", () => "\uFFFE".IsNormalized()); // Invalid codepoint
            AssertExtensions.Throws<ArgumentException>("strInput", () => "\uD800\uD800".IsNormalized()); // Invalid surrogate pair
        }

        [Fact]
        public void IsNormalized_Null()
        {
            AssertExtensions.Throws<ArgumentNullException>("strInput", () => StringNormalizationExtensions.IsNormalized(null));
        }

        public static IEnumerable<object[]> NormalizeTestData()
        {
            yield return new object[] { "", NormalizationForm.FormC, "" };
            yield return new object[] { "\u00C4\u00C7", NormalizationForm.FormD, "A\u0308C\u0327" };
            yield return new object[] { "A\u0308C\u0327", NormalizationForm.FormC, "\u00C4\u00C7" };
            yield return new object[] { "\uFB01", NormalizationForm.FormC, "\uFB01" };
            yield return new object[] { "\uFB01", NormalizationForm.FormD, "\uFB01" };
            yield return new object[] { "\u1E9b\u0323", NormalizationForm.FormC, "\u1E9b\u0323" };
            yield return new object[] { "\u1E9b\u0323", NormalizationForm.FormD, "\u017f\u0323\u0307" };

            if (PlatformDetection.IsNotUsingLimitedCultures || PlatformDetection.IsHybridGlobalizationOnApplePlatform)
            {
                // Mobile / Browser ICU doesn't support FormKC and FormKD
                yield return new object[] { "\uFB01", NormalizationForm.FormKC, "fi" };
                yield return new object[] { "\uFB01", NormalizationForm.FormKD, "fi" };
                yield return new object[] { "\u1E9b\u0323", NormalizationForm.FormKC, "\u1E69" };
                yield return new object[] { "\u1E9b\u0323", NormalizationForm.FormKD, "\u0073\u0323\u0307" };
            }
        }

        [Theory]
        [MemberData(nameof(NormalizeTestData))]
        public void Normalize(string value, NormalizationForm normalizationForm, string expected)
        {
            if (normalizationForm == NormalizationForm.FormC)
            {
                Assert.Equal(expected, value.Normalize());
            }
            Assert.Equal(expected, value.Normalize(normalizationForm));
        }

        [Fact]
        public void Normalize_Invalid()
        {
            Assert.Throws<ArgumentException>(() => "\uFB01".Normalize((NormalizationForm)7));

            AssertExtensions.Throws<ArgumentException>("strInput", () => "\uFFFE".Normalize()); // Invalid codepoint
            AssertExtensions.Throws<ArgumentException>("strInput", () => "\uD800\uD800".Normalize()); // Invalid surrogate pair
        }

        [Fact]
        public void Normalize_Null()
        {
            AssertExtensions.Throws<ArgumentNullException>("strInput", () => StringNormalizationExtensions.Normalize(null));
        }
    }
}
