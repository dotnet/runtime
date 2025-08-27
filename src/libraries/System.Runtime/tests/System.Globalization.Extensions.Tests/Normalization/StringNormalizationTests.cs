// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
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
                Assert.Equal(expected, value.AsSpan().IsNormalized());
            }
            Assert.Equal(expected, value.IsNormalized(normalizationForm));
            Assert.Equal(expected, value.AsSpan().IsNormalized(normalizationForm));
        }

        [Fact]
        public void IsNormalized_Invalid()
        {
            Assert.Throws<ArgumentException>(() => "\uFB01".IsNormalized((NormalizationForm)10));
            Assert.Throws<ArgumentException>(() => "\uFB01".AsSpan().IsNormalized((NormalizationForm)10));

            AssertExtensions.Throws<ArgumentException>("source", () => "\uFFFE".IsNormalized()); // Invalid codepoint
            AssertExtensions.Throws<ArgumentException>("source", () => "\uFFFE".AsSpan().IsNormalized()); // Invalid codepoint

            AssertExtensions.Throws<ArgumentException>("source", () => "\uD800\uD800".IsNormalized()); // Invalid surrogate pair
            AssertExtensions.Throws<ArgumentException>("source", () => "\uD800\uD800".AsSpan().IsNormalized()); // Invalid surrogate pair
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
            Span<char> destination = new char[expected.Length + 1]; // NLS sometimes need extra character in the buffer mostly if need to insert the null terminator
            int charsWritten;

            if (normalizationForm == NormalizationForm.FormC)
            {
                Assert.Equal(expected, value.Normalize());

                Assert.True(value.AsSpan().TryNormalize(destination, out charsWritten));
                Assert.Equal(expected, destination.Slice(0, charsWritten).ToString());

                if (PlatformDetection.IsNlsGlobalization)
                {
                    // NLS return estimated normalized length that is enough to hold the result but doesn't return the exact length
                    Assert.True(expected.Length <= value.GetNormalizedLength(), $"Expected: {expected.Length}, Actual: {value.GetNormalizedLength()}");
                }
                else
                {
                    // ICU returns the exact normalized length
                    Assert.Equal(expected.Length, value.AsSpan().GetNormalizedLength());
                }
            }

            Assert.Equal(expected, value.Normalize(normalizationForm));

            if (expected.Length > 0)
            {
                Assert.False(value.AsSpan().TryNormalize(destination.Slice(0, expected.Length - 1), out charsWritten, normalizationForm), $"Trying to normalize '{value}' to a buffer of length {expected.Length - 1} succeeded!");
            }

            Assert.True(value.AsSpan().TryNormalize(destination, out charsWritten, normalizationForm), $"Failed to normalize '{value}' to a buffer of length {destination.Length}");
            Assert.Equal(expected, destination.Slice(0, charsWritten).ToString());
            if (PlatformDetection.IsNlsGlobalization)
            {
                // NLS return estimated normalized length that is enough to hold the result but doesn't return the exact length
                Assert.True(expected.Length <= value.AsSpan().GetNormalizedLength(normalizationForm), $"Expected: {expected.Length}, Actual: {value.AsSpan().GetNormalizedLength(normalizationForm)}");
            }
            else
            {
                // ICU returns the exact normalized length
                Assert.Equal(expected.Length, value.AsSpan().GetNormalizedLength(normalizationForm));
            }
        }

        [Fact]
        public void Normalize_Invalid()
        {
            char[] destination = new char[100];
            Assert.Throws<ArgumentException>(() => "\uFB01".Normalize((NormalizationForm)7));
            Assert.Throws<ArgumentException>(() => "\uFB01".AsSpan().TryNormalize(destination.AsSpan(), out int charsWritten, (NormalizationForm)7));

            AssertExtensions.Throws<ArgumentException>("strInput", () => "\uFFFE".Normalize()); // Invalid codepoint
            AssertExtensions.Throws<ArgumentException>("source", () => "\uFFFE".AsSpan().TryNormalize(destination.AsSpan(), out int charsWritten)); // Invalid codepoint

            AssertExtensions.Throws<ArgumentException>("strInput", () => "\uD800\uD800".Normalize()); // Invalid surrogate pair
            AssertExtensions.Throws<ArgumentException>("source", () => "\uD800\uD800".AsSpan().TryNormalize(destination, out int charsWritten)); // Invalid surrogate pair

            char[] overlappingDestination = new char[5] { 'a', 'b', 'c', 'd', 'e' };
            Assert.Throws<ArgumentException>(() => overlappingDestination.AsSpan().TryNormalize(overlappingDestination.AsSpan(), out int charsWritten, NormalizationForm.FormC));
            Assert.Throws<ArgumentException>(() => overlappingDestination.AsSpan(0, 3).TryNormalize(overlappingDestination.AsSpan(), out int charsWritten, NormalizationForm.FormC));
            Assert.Throws<ArgumentException>(() => overlappingDestination.AsSpan(4).TryNormalize(overlappingDestination.AsSpan(), out int charsWritten, NormalizationForm.FormC));
        }

        [Fact]
        public void Normalize_Null()
        {
            AssertExtensions.Throws<ArgumentNullException>("strInput", () => StringNormalizationExtensions.Normalize(null));
        }
    }
}
