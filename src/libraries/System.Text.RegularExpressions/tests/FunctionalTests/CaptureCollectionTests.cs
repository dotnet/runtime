// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public static partial class CaptureCollectionTests
    {
        public static IEnumerable<object[]> BacktrackingEngines()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                if (!RegexHelpers.IsNonBacktracking(engine))
                {
                    yield return [engine];
                }
            }
        }

        public static IEnumerable<object[]> BacktrackingEnginesAndCaptureOptions()
        {
            foreach (object[] engine in BacktrackingEngines())
            {
                yield return [engine[0], RegexOptions.None];
                yield return [engine[0], RegexOptions.ExplicitCapture];
            }
        }

        [Theory]
        [MemberData(nameof(BacktrackingEngines))]
        public static async Task CaptureInEmptyAlternationIsPreservedInBacktrackingEngines(RegexEngine engine)
        {
            Regex withoutEmptyAlternative = await RegexHelpers.GetRegexAsync(engine, @"\w(?n)((?'G')){3}");
            Regex withEmptyAlternative = await RegexHelpers.GetRegexAsync(engine, @"\w(?n)((?'G')|){3}");

            Match expected = withoutEmptyAlternative.Match("1");
            Match actual = withEmptyAlternative.Match("1");

            Assert.True(expected.Success);
            Assert.Equal(expected.Value, actual.Value);
            Assert.Equal(expected.Groups["G"].Captures.Count, actual.Groups["G"].Captures.Count);
            Assert.Equal(3, actual.Groups["G"].Captures.Count);
            for (int i = 0; i < actual.Groups["G"].Captures.Count; i++)
            {
                Assert.Equal(expected.Groups["G"].Captures[i].Index, actual.Groups["G"].Captures[i].Index);
                Assert.Equal(expected.Groups["G"].Captures[i].Length, actual.Groups["G"].Captures[i].Length);
                Assert.Equal(1, actual.Groups["G"].Captures[i].Index);
                Assert.Equal(0, actual.Groups["G"].Captures[i].Length);
            }

            Regex nonEmptyCapture = await RegexHelpers.GetRegexAsync(engine, @"(?n)((?'G'\w)|){3}");
            CaptureCollection captures = nonEmptyCapture.Match("abc").Groups["G"].Captures;

            Assert.Equal(3, captures.Count);
            Assert.Equal(["a", "b", "c"], [captures[0].Value, captures[1].Value, captures[2].Value]);

            // Symmetric "|X" form, with the capture as the second alternative. A trailing $ anchor forces
            // every iteration to take the capturing branch, since taking the empty branch on any iteration
            // would leave the match short of the end of the input.
            Regex nonEmptyCaptureSecondAlternative = await RegexHelpers.GetRegexAsync(engine, @"(?n)(|(?'G'\w)){3}$");
            CaptureCollection secondAlternativeCaptures = nonEmptyCaptureSecondAlternative.Match("abc").Groups["G"].Captures;

            Assert.Equal(3, secondAlternativeCaptures.Count);
            Assert.Equal(["a", "b", "c"], [secondAlternativeCaptures[0].Value, secondAlternativeCaptures[1].Value, secondAlternativeCaptures[2].Value]);
        }

        [Theory]
        [MemberData(nameof(BacktrackingEnginesAndCaptureOptions))]
        public static async Task CaptureInEmptyAlternationPreservesBalancingGroupSemantics(RegexEngine engine, RegexOptions options)
        {
            // A capture-containing "X|" alternation must not be reduced to "X?", as doing so would drop
            // captures needed for balancing groups. The loop below pushes group "G" three times, so the
            // three pops in "(?<-G>){3}" must all succeed for the overall match to succeed.
            Regex regex = await RegexHelpers.GetRegexAsync(engine, @"^(?:(?'G')|){3}(?<-G>){3}$", options);

            Assert.True(regex.IsMatch(""));
        }

        [Theory]
        [MemberData(nameof(BacktrackingEnginesAndCaptureOptions))]
        public static async Task CaptureInEmptyAlternationIsPreservedWithoutExplicitCaptureGroupName(RegexEngine engine, RegexOptions options)
        {
            Regex regex = await RegexHelpers.GetRegexAsync(engine, @"\w(?:(?'G')|){3}", options);
            Match match = regex.Match("1");

            Assert.True(match.Success);
            Assert.Equal(3, match.Groups["G"].Captures.Count);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNetCore))]
        public static void CaptureInEmptyAlternationReducesToLastCaptureInNonBacktrackingEngines()
        {
            Regex regex = new Regex(@"(?n)((?'G'\w)|){3}", RegexHelpers.RegexOptionNonBacktracking);
            CaptureCollection captures = regex.Match("abc").Groups["G"].Captures;

            Assert.Single(captures);
            Assert.Equal("c", captures[0].Value);
            Assert.Equal(2, captures[0].Index);
            Assert.Equal(1, captures[0].Length);
            Assert.DoesNotContain(captures, capture => capture.Value is "a" or "b");
        }

        [Fact]
        public static void GetEnumerator()
        {
            Regex regex = new Regex(@"(?<A1>a*)(?<A2>b*)(?<A3>c*)");
            Match match = regex.Match("aaabbccccccccccaaaabc");

            CaptureCollection captures = match.Captures;
            IEnumerator enumerator = captures.GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                while (enumerator.MoveNext())
                {
                    Assert.Equal(captures[counter], enumerator.Current);
                    counter++;
                }
                Assert.False(enumerator.MoveNext());
                Assert.Equal(captures.Count, counter);
                enumerator.Reset();
            }
        }

        [Fact]
        public static void GetEnumerator_Invalid()
        {
            Regex regex = new Regex(@"(?<A1>a*)(?<A2>b*)(?<A3>c*)");
            Match match = regex.Match("aaabbccccccccccaaaabc");
            IEnumerator enumerator = match.Captures.GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            while (enumerator.MoveNext()) ;
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        }

        [Fact]
        public static void Item_Get()
        {
            CaptureCollection collection = CreateCollection();
            Assert.Equal("This ", collection[0].ToString());
            Assert.Equal("is ", collection[1].ToString());
            Assert.Equal("a ", collection[2].ToString());
            Assert.Equal("sentence", collection[3].ToString());
        }

        [Fact]
        public static void Item_Get_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            Regex regex = new Regex(@"(?<A1>a*)(?<A2>b*)(?<A3>c*)");
            CaptureCollection captures = regex.Match("aaabbccccccccccaaaabc").Captures;

            AssertExtensions.Throws<ArgumentOutOfRangeException>("i", () => captures[-1]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("i", () => captures[captures.Count]);
        }

        [Fact]
        public static void ICollection_Properties()
        {
            Regex regex = new Regex(@"(?<A1>a*)(?<A2>b*)(?<A3>c*)");
            CaptureCollection captures = regex.Match("aaabbccccccccccaaaabc").Captures;
            ICollection collection = captures;

            Assert.False(collection.IsSynchronized);
            Assert.NotNull(collection.SyncRoot);
            Assert.Same(collection.SyncRoot, collection.SyncRoot);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public static void ICollection_CopyTo(int index)
        {
            Regex regex = new Regex(@"(?<A1>a*)(?<A2>b*)(?<A3>c*)");
            CaptureCollection captures = regex.Match("aaabbccccccccccaaaabc").Captures;
            ICollection collection = captures;

            Capture[] copy = new Capture[collection.Count + index];
            collection.CopyTo(copy, index);

            for (int i = 0; i < index; i++)
            {
                Assert.Null(copy[i]);
            }
            for (int i = index; i < copy.Length; i++)
            {
                Assert.Same(captures[i - index], copy[i]);
            }
        }

        [Fact]
        public static void ICollection_CopyTo_Invalid()
        {
            Regex regex = new Regex(@"(?<A1>a*)(?<A2>b*)(?<A3>c*)");
            ICollection collection = regex.Match("aaabbccccccccccaaaabc").Captures;

            // Array is null
            AssertExtensions.Throws<ArgumentNullException>("array", () => collection.CopyTo(null, 0));

            // Array is multidimensional
            AssertExtensions.Throws<ArgumentException>(null, () => collection.CopyTo(new object[10, 10], 0));

            if (PlatformDetection.IsNonZeroLowerBoundArraySupported)
            {
                // Array has a non-zero lower bound
                Array o = Array.CreateInstance(typeof(object), [10], [10]);
                Assert.Throws<IndexOutOfRangeException>(() => collection.CopyTo(o, 0));
            }

            // Index < 0
            Assert.Throws<IndexOutOfRangeException>(() => collection.CopyTo(new object[collection.Count], -1));

            // Invalid index + length
            Assert.Throws<IndexOutOfRangeException>(() => collection.CopyTo(new object[collection.Count], 1));
            Assert.Throws<IndexOutOfRangeException>(() => collection.CopyTo(new object[collection.Count + 1], 2));
        }

        private static CaptureCollection CreateCollection()
        {
            Regex regex = new Regex(@"\b(\w+\s*)+\.");
            Match match = regex.Match("This is a sentence.");
            return match.Groups[1].Captures;
        }
    }
}
