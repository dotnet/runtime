// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Tests;
using Xunit;

using static System.Tests.Utf8TestUtilities;

using ustring = System.Utf8String;

namespace System.Text.Tests
{
    /*
     * Please keep these tests in sync with those in Utf8StringTests.Searching.cs.
     */

    public unsafe partial class Utf8SpanTests
    {
        [Theory]
        [MemberData(nameof(TryFindData_Char_Ordinal))]
        public static void TryFind_Char_Ordinal(ustring source, char searchTerm, Range? expectedForwardMatch, Range? expectedBackwardMatch)
        {
            using BoundedUtf8Span boundedSpan = new BoundedUtf8Span(source.AsBytes());
            Utf8Span searchSpan = boundedSpan.Span;
            source = null; // to avoid accidentally using this for the remainder of the test

            // First, search forward

            bool wasFound = searchSpan.TryFind(searchTerm, out Range actualForwardMatch);
            Assert.Equal(expectedForwardMatch.HasValue, wasFound);

            if (wasFound)
            {
                AssertRangesEqual(searchSpan.Length, expectedForwardMatch.Value, actualForwardMatch);
            }

            // Also check Contains / StartsWith / SplitOn

            Assert.Equal(wasFound, searchSpan.Contains(searchTerm));
            Assert.Equal(wasFound && searchSpan.Bytes[..actualForwardMatch.Start].IsEmpty, searchSpan.StartsWith(searchTerm));

            (var before, var after) = searchSpan.SplitOn(searchTerm);
            if (wasFound)
            {
                Assert.True(searchSpan.Bytes[..actualForwardMatch.Start] == before.Bytes); // check for referential equality
                Assert.True(searchSpan.Bytes[actualForwardMatch.End..] == after.Bytes); // check for referential equality
            }
            else
            {
                Assert.True(searchSpan.Bytes == before.Bytes); // check for reference equality
                Assert.True(after.IsNull());
            }

            // Now search backward

            wasFound = searchSpan.TryFindLast(searchTerm, out Range actualBackwardMatch);
            Assert.Equal(expectedBackwardMatch.HasValue, wasFound);

            if (wasFound)
            {
                AssertRangesEqual(searchSpan.Length, expectedBackwardMatch.Value, actualBackwardMatch);
            }

            // Also check EndsWith / SplitOnLast

            Assert.Equal(wasFound && searchSpan.Bytes[actualBackwardMatch.End..].IsEmpty, searchSpan.EndsWith(searchTerm));

            (before, after) = searchSpan.SplitOnLast(searchTerm);
            if (wasFound)
            {
                Assert.True(searchSpan.Bytes[..actualBackwardMatch.Start] == before.Bytes); // check for referential equality
                Assert.True(searchSpan.Bytes[actualBackwardMatch.End..] == after.Bytes); // check for referential equality
            }
            else
            {
                Assert.True(searchSpan.Bytes == before.Bytes); // check for reference equality
                Assert.True(after.IsNull());
            }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [MemberData(nameof(TryFindData_Char_WithComparison))]
        public static void TryFind_Char_WithComparison(ustring source, char searchTerm, StringComparison comparison, CultureInfo currentCulture, Range? expectedForwardMatch, Range? expectedBackwardMatch)
        {
            RunOnDedicatedThread(() =>
            {
                using BoundedUtf8Span boundedSpan = new BoundedUtf8Span(source.AsBytes());
                Utf8Span searchSpan = boundedSpan.Span;
                source = null; // to avoid accidentally using this for the remainder of the test

                if (currentCulture != null)
                {
                    CultureInfo.CurrentCulture = currentCulture;
                }

                // First, search forward

                bool wasFound = searchSpan.TryFind(searchTerm, comparison, out Range actualForwardMatch);
                Assert.Equal(expectedForwardMatch.HasValue, wasFound);

                if (wasFound)
                {
                    AssertRangesEqual(searchSpan.Length, expectedForwardMatch.Value, actualForwardMatch);
                }

                // Also check Contains / StartsWith / SplitOn

                Assert.Equal(wasFound, searchSpan.Contains(searchTerm, comparison));
                Assert.Equal(wasFound && searchSpan.Bytes[..actualForwardMatch.Start].IsEmpty, searchSpan.StartsWith(searchTerm, comparison));

                (var before, var after) = searchSpan.SplitOn(searchTerm, comparison);
                if (wasFound)
                {
                    Assert.True(searchSpan.Bytes[..actualForwardMatch.Start] == before.Bytes); // check for referential equality
                    Assert.True(searchSpan.Bytes[actualForwardMatch.End..] == after.Bytes); // check for referential equality
                }
                else
                {
                    Assert.True(searchSpan.Bytes == before.Bytes); // check for reference equality
                    Assert.True(after.IsNull());
                }

                // Now search backward

                wasFound = searchSpan.TryFindLast(searchTerm, comparison, out Range actualBackwardMatch);
                Assert.Equal(expectedBackwardMatch.HasValue, wasFound);

                if (wasFound)
                {
                    AssertRangesEqual(searchSpan.Length, expectedBackwardMatch.Value, actualBackwardMatch);
                }

                // Also check EndsWith / SplitOnLast

                Assert.Equal(wasFound && searchSpan.Bytes[actualBackwardMatch.End..].IsEmpty, searchSpan.EndsWith(searchTerm, comparison));

                (before, after) = searchSpan.SplitOnLast(searchTerm, comparison);
                if (wasFound)
                {
                    Assert.True(searchSpan.Bytes[..actualBackwardMatch.Start] == before.Bytes); // check for referential equality
                    Assert.True(searchSpan.Bytes[actualBackwardMatch.End..] == after.Bytes); // check for referential equality
                }
                else
                {
                    Assert.True(searchSpan.Bytes == before.Bytes); // check for reference equality
                    Assert.True(after.IsNull());
                }
            });
        }

        [Theory]
        [MemberData(nameof(TryFindData_Rune_Ordinal))]
        public static void TryFind_Rune_Ordinal(ustring source, Rune searchTerm, Range? expectedForwardMatch, Range? expectedBackwardMatch)
        {
            using BoundedUtf8Span boundedSpan = new BoundedUtf8Span(source.AsBytes());
            Utf8Span searchSpan = boundedSpan.Span;
            source = null; // to avoid accidentally using this for the remainder of the test

            // First, search forward

            bool wasFound = searchSpan.TryFind(searchTerm, out Range actualForwardMatch);
            Assert.Equal(expectedForwardMatch.HasValue, wasFound);

            if (wasFound)
            {
                AssertRangesEqual(searchSpan.Length, expectedForwardMatch.Value, actualForwardMatch);
            }

            // Also check Contains / StartsWith / SplitOn

            Assert.Equal(wasFound, searchSpan.Contains(searchTerm));
            Assert.Equal(wasFound && searchSpan.Bytes[..actualForwardMatch.Start].IsEmpty, searchSpan.StartsWith(searchTerm));

            (var before, var after) = searchSpan.SplitOn(searchTerm);
            if (wasFound)
            {
                Assert.True(searchSpan.Bytes[..actualForwardMatch.Start] == before.Bytes); // check for referential equality
                Assert.True(searchSpan.Bytes[actualForwardMatch.End..] == after.Bytes); // check for referential equality
            }
            else
            {
                Assert.True(searchSpan.Bytes == before.Bytes); // check for reference equality
                Assert.True(after.IsNull());
            }

            // Now search backward

            wasFound = searchSpan.TryFindLast(searchTerm, out Range actualBackwardMatch);
            Assert.Equal(expectedBackwardMatch.HasValue, wasFound);

            if (wasFound)
            {
                AssertRangesEqual(searchSpan.Length, expectedBackwardMatch.Value, actualBackwardMatch);
            }

            // Also check EndsWith / SplitOnLast

            Assert.Equal(wasFound && searchSpan.Bytes[actualBackwardMatch.End..].IsEmpty, searchSpan.EndsWith(searchTerm));

            (before, after) = searchSpan.SplitOnLast(searchTerm);
            if (wasFound)
            {
                Assert.True(searchSpan.Bytes[..actualBackwardMatch.Start] == before.Bytes); // check for referential equality
                Assert.True(searchSpan.Bytes[actualBackwardMatch.End..] == after.Bytes); // check for referential equality
            }
            else
            {
                Assert.True(searchSpan.Bytes == before.Bytes); // check for reference equality
                Assert.True(after.IsNull());
            }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [MemberData(nameof(TryFindData_Rune_WithComparison))]
        public static void TryFind_Rune_WithComparison(ustring source, Rune searchTerm, StringComparison comparison, CultureInfo currentCulture, Range? expectedForwardMatch, Range? expectedBackwardMatch)
        {
            RunOnDedicatedThread(() =>
            {
                using BoundedUtf8Span boundedSpan = new BoundedUtf8Span(source.AsBytes());
                Utf8Span searchSpan = boundedSpan.Span;
                source = null; // to avoid accidentally using this for the remainder of the test

                if (currentCulture != null)
                {
                    CultureInfo.CurrentCulture = currentCulture;
                }

                // First, search forward

                bool wasFound = searchSpan.TryFind(searchTerm, comparison, out Range actualForwardMatch);
                Assert.Equal(expectedForwardMatch.HasValue, wasFound);

                if (wasFound)
                {
                    AssertRangesEqual(searchSpan.Length, expectedForwardMatch.Value, actualForwardMatch);
                }

                // Also check Contains / StartsWith / SplitOn

                Assert.Equal(wasFound, searchSpan.Contains(searchTerm, comparison));
                Assert.Equal(wasFound && searchSpan.Bytes[..actualForwardMatch.Start].IsEmpty, searchSpan.StartsWith(searchTerm, comparison));

                (var before, var after) = searchSpan.SplitOn(searchTerm, comparison);
                if (wasFound)
                {
                    Assert.True(searchSpan.Bytes[..actualForwardMatch.Start] == before.Bytes); // check for referential equality
                    Assert.True(searchSpan.Bytes[actualForwardMatch.End..] == after.Bytes); // check for referential equality
                }
                else
                {
                    Assert.True(searchSpan.Bytes == before.Bytes); // check for reference equality
                    Assert.True(after.IsNull());
                }

                // Now search backward

                wasFound = searchSpan.TryFindLast(searchTerm, comparison, out Range actualBackwardMatch);
                Assert.Equal(expectedBackwardMatch.HasValue, wasFound);

                if (wasFound)
                {
                    AssertRangesEqual(searchSpan.Length, expectedBackwardMatch.Value, actualBackwardMatch);
                }

                // Also check EndsWith / SplitOnLast

                Assert.Equal(wasFound && searchSpan.Bytes[actualBackwardMatch.End..].IsEmpty, searchSpan.EndsWith(searchTerm, comparison));

                (before, after) = searchSpan.SplitOnLast(searchTerm, comparison);
                if (wasFound)
                {
                    Assert.True(searchSpan.Bytes[..actualBackwardMatch.Start] == before.Bytes); // check for referential equality
                    Assert.True(searchSpan.Bytes[actualBackwardMatch.End..] == after.Bytes); // check for referential equality
                }
                else
                {
                    Assert.True(searchSpan.Bytes == before.Bytes); // check for reference equality
                    Assert.True(after.IsNull());
                }
            });
        }

        [Theory]
        [MemberData(nameof(TryFindData_Utf8Span_Ordinal))]
        public static void TryFind_Utf8Span_Ordinal(ustring source, ustring searchTerm, Range? expectedForwardMatch, Range? expectedBackwardMatch)
        {
            using BoundedUtf8Span boundedSpan = new BoundedUtf8Span(source.AsBytes());
            Utf8Span searchSpan = boundedSpan.Span;
            source = null; // to avoid accidentally using this for the remainder of the test

            // First, search forward

            bool wasFound = searchSpan.TryFind(searchTerm, out Range actualForwardMatch);
            Assert.Equal(expectedForwardMatch.HasValue, wasFound);

            if (wasFound)
            {
                AssertRangesEqual(searchSpan.Length, expectedForwardMatch.Value, actualForwardMatch);
            }

            // Also check Contains / StartsWith / SplitOn

            Assert.Equal(wasFound, searchSpan.Contains(searchTerm));
            Assert.Equal(wasFound && searchSpan.Bytes[..actualForwardMatch.Start].IsEmpty, searchSpan.StartsWith(searchTerm));

            (var before, var after) = searchSpan.SplitOn(searchTerm);
            if (wasFound)
            {
                Assert.True(searchSpan.Bytes[..actualForwardMatch.Start] == before.Bytes); // check for referential equality
                Assert.True(searchSpan.Bytes[actualForwardMatch.End..] == after.Bytes); // check for referential equality
            }
            else
            {
                Assert.True(searchSpan.Bytes == before.Bytes); // check for reference equality
                Assert.True(after.IsNull());
            }

            // Now search backward

            wasFound = searchSpan.TryFindLast(searchTerm, out Range actualBackwardMatch);
            Assert.Equal(expectedBackwardMatch.HasValue, wasFound);

            if (wasFound)
            {
                AssertRangesEqual(searchSpan.Length, expectedBackwardMatch.Value, actualBackwardMatch);
            }

            // Also check EndsWith / SplitOnLast

            Assert.Equal(wasFound && searchSpan.Bytes[actualBackwardMatch.End..].IsEmpty, searchSpan.EndsWith(searchTerm));

            (before, after) = searchSpan.SplitOnLast(searchTerm);
            if (wasFound)
            {
                Assert.True(searchSpan.Bytes[..actualBackwardMatch.Start] == before.Bytes); // check for referential equality
                Assert.True(searchSpan.Bytes[actualBackwardMatch.End..] == after.Bytes); // check for referential equality
            }
            else
            {
                Assert.True(searchSpan.Bytes == before.Bytes); // check for reference equality
                Assert.True(after.IsNull());
            }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [MemberData(nameof(TryFindData_Utf8Span_WithComparison))]
        public static void TryFind_Utf8Span_WithComparison(ustring source, ustring searchTerm, StringComparison comparison, CultureInfo currentCulture, Range? expectedForwardMatch, Range? expectedBackwardMatch)
        {
            RunOnDedicatedThread(() =>
            {
                using BoundedUtf8Span boundedSpan = new BoundedUtf8Span(source.AsBytes());
                Utf8Span searchSpan = boundedSpan.Span;
                source = null; // to avoid accidentally using this for the remainder of the test

                if (currentCulture != null)
                {
                    CultureInfo.CurrentCulture = currentCulture;
                }

                // First, search forward

                bool wasFound = searchSpan.TryFind(searchTerm, comparison, out Range actualForwardMatch);
                Assert.Equal(expectedForwardMatch.HasValue, wasFound);

                if (wasFound)
                {
                    AssertRangesEqual(searchSpan.Length, expectedForwardMatch.Value, actualForwardMatch);
                }

                // Also check Contains / StartsWith / SplitOn

                Assert.Equal(wasFound, searchSpan.Contains(searchTerm, comparison));
                Assert.Equal(wasFound && searchSpan.Bytes[..actualForwardMatch.Start].IsEmpty, searchSpan.StartsWith(searchTerm, comparison));

                (var before, var after) = searchSpan.SplitOn(searchTerm, comparison);
                if (wasFound)
                {
                    Assert.True(searchSpan.Bytes[..actualForwardMatch.Start] == before.Bytes); // check for referential equality
                    Assert.True(searchSpan.Bytes[actualForwardMatch.End..] == after.Bytes); // check for referential equality
                }
                else
                {
                    Assert.True(searchSpan.Bytes == before.Bytes); // check for reference equality
                    Assert.True(after.IsNull());
                }

                // Now search backward

                wasFound = searchSpan.TryFindLast(searchTerm, comparison, out Range actualBackwardMatch);
                Assert.Equal(expectedBackwardMatch.HasValue, wasFound);

                if (wasFound)
                {
                    AssertRangesEqual(searchSpan.Length, expectedBackwardMatch.Value, actualBackwardMatch);
                }

                // Also check EndsWith / SplitOnLast

                Assert.Equal(wasFound && searchSpan.Bytes[actualBackwardMatch.End..].IsEmpty, searchSpan.EndsWith(searchTerm, comparison));

                (before, after) = searchSpan.SplitOnLast(searchTerm, comparison);
                if (wasFound)
                {
                    Assert.True(searchSpan.Bytes[..actualBackwardMatch.Start] == before.Bytes); // check for referential equality
                    Assert.True(searchSpan.Bytes[actualBackwardMatch.End..] == after.Bytes); // check for referential equality
                }
                else
                {
                    Assert.True(searchSpan.Bytes == before.Bytes); // check for reference equality
                    Assert.True(after.IsNull());
                }
            });
        }
    }
}
