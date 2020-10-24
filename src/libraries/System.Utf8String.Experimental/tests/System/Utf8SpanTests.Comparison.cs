// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

using static System.Tests.Utf8TestUtilities;

namespace System.Text.Tests
{
    public partial class Utf8SpanTests
    {
        [Fact]
        public static void Equals_Object_ThrowsNotSupported()
        {
            Utf8Span span = Utf8Span.Empty;

            Assert.Throws<NotSupportedException>(() => Utf8Span.Empty.Equals((object)null));
            Assert.Throws<NotSupportedException>(() => Utf8Span.Empty.Equals(new object()));
        }

        [Fact]
        public static void Equals_Ordinal()
        {
            // First make sure referential equality passes

            Utf8Span span1 = u8("Hello!");
            Utf8Span span2 = span1;
            AssertEqualOrdinal(span1, span2);

            // Now make sure deep equality passes

            span2 = Utf8Span.UnsafeCreateWithoutValidation(Encoding.UTF8.GetBytes("Hello!"));
            AssertEqualOrdinal(span1, span2);

            // Now mutate one of the inputs and make sure they're inequal

            span2 = u8("Bello!");
            AssertNotEqualOrdinal(span1, span2);

            // Finally, make sure null / null and null / empty are treated as the same

            AssertEqualOrdinal(Utf8Span.Empty, Utf8Span.Empty);
            AssertEqualOrdinal(Utf8Span.Empty, u8(""));
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNlsGlobalization))]
        [InlineData(null, null, StringComparison.OrdinalIgnoreCase, null, true)]
        [InlineData("encyclopaedia", "encyclopædia", StringComparison.OrdinalIgnoreCase, null, false)]
        [InlineData("encyclopaedia", "encyclopædia", StringComparison.InvariantCulture, null, true)]
        [InlineData("encyclopaedia", "ENCYCLOPÆDIA", StringComparison.InvariantCulture, null, false)]
        [InlineData("encyclopaedia", "encyclopædia", StringComparison.InvariantCultureIgnoreCase, null, true)]
        [InlineData("encyclopaedia", "ENCYCLOPÆDIA", StringComparison.InvariantCultureIgnoreCase, null, true)]
        [InlineData("Weiß", "WEISS", StringComparison.OrdinalIgnoreCase, null, false)]
        [InlineData("Weiß", "WEISS", StringComparison.InvariantCulture, null, false)]
        [InlineData("Weiß", "WEISS", StringComparison.InvariantCultureIgnoreCase, null, true)]
        [InlineData("Weiß", "WEISS", StringComparison.CurrentCulture, "de-DE", false)]
        [InlineData("Weiß", "WEISS", StringComparison.CurrentCultureIgnoreCase, "de-DE", true)]
        [InlineData("γένεσις", "ΓΈΝΕΣΙΣ", StringComparison.InvariantCultureIgnoreCase, null, true)]
        [InlineData("ıI", "iI", StringComparison.CurrentCulture, "tr-TR", false)]
        [InlineData("ıI", "iI", StringComparison.CurrentCultureIgnoreCase, "tr-TR", false)]
        [InlineData("İI", "iI", StringComparison.CurrentCultureIgnoreCase, "tr-TR", true)]
        public static void Equals_NonOrdinal(string str1, string str2, StringComparison comparison, string culture, bool shouldCompareAsEqual)
        {
            using (new ThreadCultureChange(culture))
            {
                using BoundedUtf8Span boundedSpan1 = new BoundedUtf8Span(str1);
                using BoundedUtf8Span boundedSpan2 = new BoundedUtf8Span(str2);

                Utf8Span span1 = boundedSpan1.Span;
                Utf8Span span2 = boundedSpan2.Span;

                Assert.Equal(shouldCompareAsEqual, span1.Equals(span2, comparison));
                Assert.Equal(shouldCompareAsEqual, span2.Equals(span1, comparison));
                Assert.Equal(shouldCompareAsEqual, Utf8Span.Equals(span1, span2, comparison));
                Assert.Equal(shouldCompareAsEqual, Utf8Span.Equals(span2, span1, comparison));
            }
        }

        private static void AssertEqualOrdinal(Utf8Span span1, Utf8Span span2)
        {
            Assert.True(span1.Equals(span2));
            Assert.True(span2.Equals(span1));

            Assert.True(span1.Equals(span2, StringComparison.Ordinal));
            Assert.True(span2.Equals(span1, StringComparison.Ordinal));

            Assert.True(Utf8Span.Equals(span1, span2));
            Assert.True(Utf8Span.Equals(span2, span1));

            Assert.True(Utf8Span.Equals(span1, span2, StringComparison.Ordinal));
            Assert.True(Utf8Span.Equals(span2, span1, StringComparison.Ordinal));

            Assert.True(span1 == span2);
            Assert.True(span2 == span1);

            Assert.False(span1 != span2);
            Assert.False(span2 != span1);
        }

        private static void AssertNotEqualOrdinal(Utf8Span span1, Utf8Span span2)
        {
            Assert.False(span1.Equals(span2));
            Assert.False(span2.Equals(span1));

            Assert.False(span1.Equals(span2, StringComparison.Ordinal));
            Assert.False(span2.Equals(span1, StringComparison.Ordinal));

            Assert.False(Utf8Span.Equals(span1, span2));
            Assert.False(Utf8Span.Equals(span2, span1));

            Assert.False(Utf8Span.Equals(span1, span2, StringComparison.Ordinal));
            Assert.False(Utf8Span.Equals(span2, span1, StringComparison.Ordinal));

            Assert.False(span1 == span2);
            Assert.False(span2 == span1);

            Assert.True(span1 != span2);
            Assert.True(span2 != span1);
        }
    }
}
