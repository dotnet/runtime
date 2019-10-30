// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Tests;
using Xunit;

using static System.Tests.Utf8TestUtilities;

using ustring = System.Utf8String;

namespace System.Tests
{
    /*
     * Please keep these tests in sync with those in Utf8SpanTests.Searching.cs.
     */

    public unsafe partial class Utf8StringTests
    {
        public static IEnumerable<object[]> TryFindData_Char_Ordinal() => Utf8SpanTests.TryFindData_Char_Ordinal();

        [Theory]
        [MemberData(nameof(TryFindData_Char_Ordinal))]
        public static void TryFind_Char_Ordinal(ustring source, char searchTerm, Range? expectedForwardMatch, Range? expectedBackwardMatch)
        {
            if (source is null)
            {
                return; // don't null ref
            }

            // First, search forward

            bool wasFound = source.TryFind(searchTerm, out Range actualForwardMatch);
            Assert.Equal(expectedForwardMatch.HasValue, wasFound);

            if (wasFound)
            {
                AssertRangesEqual(source.Length, expectedForwardMatch.Value, actualForwardMatch);
            }

            // Also check Contains / StartsWith / SplitOn

            Assert.Equal(wasFound, source.Contains(searchTerm));
            Assert.Equal(wasFound && source[..actualForwardMatch.Start].Length == 0, source.StartsWith(searchTerm));

            (var before, var after) = source.SplitOn(searchTerm);
            if (wasFound)
            {
                Assert.Equal(source[..actualForwardMatch.Start], before);
                Assert.Equal(source[actualForwardMatch.End..], after);
            }
            else
            {
                Assert.Same(source, before); // check for reference equality
                Assert.Null(after);
            }

            // Now search backward

            wasFound = source.TryFindLast(searchTerm, out Range actualBackwardMatch);
            Assert.Equal(expectedBackwardMatch.HasValue, wasFound);

            if (wasFound)
            {
                AssertRangesEqual(source.Length, expectedBackwardMatch.Value, actualBackwardMatch);
            }

            // Also check EndsWith / SplitOnLast

            Assert.Equal(wasFound && source[actualBackwardMatch.End..].Length == 0, source.EndsWith(searchTerm));

            (before, after) = source.SplitOnLast(searchTerm);
            if (wasFound)
            {
                Assert.Equal(source[..actualBackwardMatch.Start], before);
                Assert.Equal(source[actualBackwardMatch.End..], after);
            }
            else
            {
                Assert.Same(source, before); // check for reference equality
                Assert.Null(after);
            }
        }

        public static IEnumerable<object[]> TryFindData_Char_WithComparison() => Utf8SpanTests.TryFindData_Char_WithComparison();

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [MemberData(nameof(TryFindData_Char_WithComparison))]
        public static void TryFind_Char_WithComparison(ustring source, char searchTerm, StringComparison comparison, CultureInfo currentCulture, Range? expectedForwardMatch, Range? expectedBackwardMatch)
        {
            if (source is null)
            {
                return; // don't null ref
            }

            RunOnDedicatedThread(() =>
            {
                if (currentCulture != null)
                {
                    CultureInfo.CurrentCulture = currentCulture;
                }

                // First, search forward

                bool wasFound = source.TryFind(searchTerm, comparison, out Range actualForwardMatch);
                Assert.Equal(expectedForwardMatch.HasValue, wasFound);

                if (wasFound)
                {
                    AssertRangesEqual(source.Length, expectedForwardMatch.Value, actualForwardMatch);
                }

                // Also check Contains / StartsWith / SplitOn

                Assert.Equal(wasFound, source.Contains(searchTerm, comparison));
                Assert.Equal(wasFound && source[..actualForwardMatch.Start].Length == 0, source.StartsWith(searchTerm, comparison));

                (var before, var after) = source.SplitOn(searchTerm, comparison);
                if (wasFound)
                {
                    Assert.Equal(source[..actualForwardMatch.Start], before);
                    Assert.Equal(source[actualForwardMatch.End..], after);
                }
                else
                {
                    Assert.Same(source, before); // check for reference equality
                    Assert.Null(after);
                }

                // Now search backward

                wasFound = source.TryFindLast(searchTerm, comparison, out Range actualBackwardMatch);
                Assert.Equal(expectedBackwardMatch.HasValue, wasFound);

                if (wasFound)
                {
                    AssertRangesEqual(source.Length, expectedBackwardMatch.Value, actualBackwardMatch);
                }

                // Also check EndsWith / SplitOnLast

                Assert.Equal(wasFound && source[actualBackwardMatch.End..].Length == 0, source.EndsWith(searchTerm, comparison));

                (before, after) = source.SplitOnLast(searchTerm, comparison);
                if (wasFound)
                {
                    Assert.Equal(source[..actualBackwardMatch.Start], before);
                    Assert.Equal(source[actualBackwardMatch.End..], after);
                }
                else
                {
                    Assert.Same(source, before); // check for reference equality
                    Assert.Null(after);
                }
            });
        }

        public static IEnumerable<object[]> TryFindData_Rune_Ordinal() => Utf8SpanTests.TryFindData_Rune_Ordinal();

        [Theory]
        [MemberData(nameof(TryFindData_Rune_Ordinal))]
        public static void TryFind_Rune_Ordinal(ustring source, Rune searchTerm, Range? expectedForwardMatch, Range? expectedBackwardMatch)
        {
            if (source is null)
            {
                return; // don't null ref
            }

            // First, search forward

            bool wasFound = source.TryFind(searchTerm, out Range actualForwardMatch);
            Assert.Equal(expectedForwardMatch.HasValue, wasFound);

            if (wasFound)
            {
                AssertRangesEqual(source.Length, expectedForwardMatch.Value, actualForwardMatch);
            }

            // Also check Contains / StartsWith / SplitOn

            Assert.Equal(wasFound, source.Contains(searchTerm));
            Assert.Equal(wasFound && source[..actualForwardMatch.Start].Length == 0, source.StartsWith(searchTerm));

            (var before, var after) = source.SplitOn(searchTerm);
            if (wasFound)
            {
                Assert.Equal(source[..actualForwardMatch.Start], before);
                Assert.Equal(source[actualForwardMatch.End..], after);
            }
            else
            {
                Assert.Same(source, before); // check for reference equality
                Assert.Null(after);
            }

            // Now search backward

            wasFound = source.TryFindLast(searchTerm, out Range actualBackwardMatch);
            Assert.Equal(expectedBackwardMatch.HasValue, wasFound);

            if (wasFound)
            {
                AssertRangesEqual(source.Length, expectedBackwardMatch.Value, actualBackwardMatch);
            }

            // Also check EndsWith / SplitOnLast

            Assert.Equal(wasFound && source[actualBackwardMatch.End..].Length == 0, source.EndsWith(searchTerm));

            (before, after) = source.SplitOnLast(searchTerm);
            if (wasFound)
            {
                Assert.Equal(source[..actualBackwardMatch.Start], before);
                Assert.Equal(source[actualBackwardMatch.End..], after);
            }
            else
            {
                Assert.Same(source, before); // check for reference equality
                Assert.Null(after);
            }
        }

        public static IEnumerable<object[]> TryFindData_Rune_WithComparison() => Utf8SpanTests.TryFindData_Rune_WithComparison();

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [MemberData(nameof(TryFindData_Rune_WithComparison))]
        public static void TryFind_Rune_WithComparison(ustring source, Rune searchTerm, StringComparison comparison, CultureInfo currentCulture, Range? expectedForwardMatch, Range? expectedBackwardMatch)
        {
            if (source is null)
            {
                return; // don't null ref
            }

            RunOnDedicatedThread(() =>
            {
                if (currentCulture != null)
                {
                    CultureInfo.CurrentCulture = currentCulture;
                }

                // First, search forward

                bool wasFound = source.TryFind(searchTerm, comparison, out Range actualForwardMatch);
                Assert.Equal(expectedForwardMatch.HasValue, wasFound);

                if (wasFound)
                {
                    AssertRangesEqual(source.Length, expectedForwardMatch.Value, actualForwardMatch);
                }

                // Also check Contains / StartsWith / SplitOn

                Assert.Equal(wasFound, source.Contains(searchTerm, comparison));
                Assert.Equal(wasFound && source[..actualForwardMatch.Start].Length == 0, source.StartsWith(searchTerm, comparison));

                (var before, var after) = source.SplitOn(searchTerm, comparison);
                if (wasFound)
                {
                    Assert.Equal(source[..actualForwardMatch.Start], before);
                    Assert.Equal(source[actualForwardMatch.End..], after);
                }
                else
                {
                    Assert.Same(source, before); // check for reference equality
                    Assert.Null(after);
                }

                // Now search backward

                wasFound = source.TryFindLast(searchTerm, comparison, out Range actualBackwardMatch);
                Assert.Equal(expectedBackwardMatch.HasValue, wasFound);

                if (wasFound)
                {
                    AssertRangesEqual(source.Length, expectedBackwardMatch.Value, actualBackwardMatch);
                }

                // Also check EndsWith / SplitOnLast

                Assert.Equal(wasFound && source[actualBackwardMatch.End..].Length == 0, source.EndsWith(searchTerm, comparison));

                (before, after) = source.SplitOnLast(searchTerm, comparison);
                if (wasFound)
                {
                    Assert.Equal(source[..actualBackwardMatch.Start], before);
                    Assert.Equal(source[actualBackwardMatch.End..], after);
                }
                else
                {
                    Assert.Same(source, before); // check for reference equality
                    Assert.Null(after);
                }
            });
        }

        public static IEnumerable<object[]> TryFindData_Utf8String_Ordinal() => Utf8SpanTests.TryFindData_Utf8Span_Ordinal();

        [Theory]
        [MemberData(nameof(TryFindData_Utf8String_Ordinal))]
        public static void TryFind_Utf8String_Ordinal(ustring source, ustring searchTerm, Range? expectedForwardMatch, Range? expectedBackwardMatch)
        {
            if (source is null)
            {
                return; // don't null ref
            }

            // First, search forward

            bool wasFound = source.TryFind(searchTerm, out Range actualForwardMatch);
            Assert.Equal(expectedForwardMatch.HasValue, wasFound);

            if (wasFound)
            {
                AssertRangesEqual(source.Length, expectedForwardMatch.Value, actualForwardMatch);
            }

            // Also check Contains / StartsWith / SplitOn

            Assert.Equal(wasFound, source.Contains(searchTerm));
            Assert.Equal(wasFound && source[..actualForwardMatch.Start].Length == 0, source.StartsWith(searchTerm));

            (var before, var after) = source.SplitOn(searchTerm);
            if (wasFound)
            {
                Assert.Equal(source[..actualForwardMatch.Start], before);
                Assert.Equal(source[actualForwardMatch.End..], after);
            }
            else
            {
                Assert.Same(source, before); // check for reference equality
                Assert.Null(after);
            }

            // Now search backward

            wasFound = source.TryFindLast(searchTerm, out Range actualBackwardMatch);
            Assert.Equal(expectedBackwardMatch.HasValue, wasFound);

            if (wasFound)
            {
                AssertRangesEqual(source.Length, expectedBackwardMatch.Value, actualBackwardMatch);
            }

            // Also check EndsWith / SplitOnLast

            Assert.Equal(wasFound && source[actualBackwardMatch.End..].Length == 0, source.EndsWith(searchTerm));

            (before, after) = source.SplitOnLast(searchTerm);
            if (wasFound)
            {
                Assert.Equal(source[..actualBackwardMatch.Start], before);
                Assert.Equal(source[actualBackwardMatch.End..], after);
            }
            else
            {
                Assert.Same(source, before); // check for reference equality
                Assert.Null(after);
            }
        }

        public static IEnumerable<object[]> TryFindData_Utf8String_WithComparison() => Utf8SpanTests.TryFindData_Utf8Span_WithComparison();

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [MemberData(nameof(TryFindData_Utf8String_WithComparison))]
        public static void TryFind_Utf8String_WithComparison(ustring source, ustring searchTerm, StringComparison comparison, CultureInfo currentCulture, Range? expectedForwardMatch, Range? expectedBackwardMatch)
        {
            if (source is null)
            {
                return; // don't null ref
            }

            RunOnDedicatedThread(() =>
            {
                if (currentCulture != null)
                {
                    CultureInfo.CurrentCulture = currentCulture;
                }

                // First, search forward

                bool wasFound = source.TryFind(searchTerm, comparison, out Range actualForwardMatch);
                Assert.Equal(expectedForwardMatch.HasValue, wasFound);

                if (wasFound)
                {
                    AssertRangesEqual(source.Length, expectedForwardMatch.Value, actualForwardMatch);
                }

                // Also check Contains / StartsWith / SplitOn

                Assert.Equal(wasFound, source.Contains(searchTerm, comparison));
                Assert.Equal(wasFound && source[..actualForwardMatch.Start].Length == 0, source.StartsWith(searchTerm, comparison));

                (var before, var after) = source.SplitOn(searchTerm, comparison);
                if (wasFound)
                {
                    Assert.Equal(source[..actualForwardMatch.Start], before);
                    Assert.Equal(source[actualForwardMatch.End..], after);
                }
                else
                {
                    Assert.Same(source, before); // check for reference equality
                    Assert.Null(after);
                }

                // Now search backward

                wasFound = source.TryFindLast(searchTerm, comparison, out Range actualBackwardMatch);
                Assert.Equal(expectedBackwardMatch.HasValue, wasFound);

                if (wasFound)
                {
                    AssertRangesEqual(source.Length, expectedBackwardMatch.Value, actualBackwardMatch);
                }

                // Also check EndsWith / SplitOnLast

                Assert.Equal(wasFound && source[actualBackwardMatch.End..].Length == 0, source.EndsWith(searchTerm, comparison));

                (before, after) = source.SplitOnLast(searchTerm, comparison);
                if (wasFound)
                {
                    Assert.Equal(source[..actualBackwardMatch.Start], before);
                    Assert.Equal(source[actualBackwardMatch.End..], after);
                }
                else
                {
                    Assert.Same(source, before); // check for reference equality
                    Assert.Null(after);
                }
            });
        }

        [Fact]
        public static void TryFind_WithNullUtf8String_Throws()
        {
            static void RunTest(Action action)
            {
                var exception = Assert.Throws<ArgumentNullException>(action);
                Assert.Equal("value", exception.ParamName);
            }

            ustring str = u8("Hello!");
            const ustring value = null;
            const StringComparison comparison = StringComparison.OrdinalIgnoreCase;

            // Run this test for a bunch of methods, not simply TryFind

            RunTest(() => str.Contains(value));
            RunTest(() => str.Contains(value, comparison));
            RunTest(() => str.EndsWith(value));
            RunTest(() => str.EndsWith(value, comparison));
            RunTest(() => str.SplitOn(value));
            RunTest(() => str.SplitOn(value, comparison));
            RunTest(() => str.SplitOnLast(value));
            RunTest(() => str.SplitOnLast(value, comparison));
            RunTest(() => str.StartsWith(value));
            RunTest(() => str.StartsWith(value, comparison));
            RunTest(() => str.TryFind(value, out _));
            RunTest(() => str.TryFind(value, comparison, out _));
            RunTest(() => str.TryFindLast(value, out _));
            RunTest(() => str.TryFindLast(value, comparison, out _));
        }
    }
}
