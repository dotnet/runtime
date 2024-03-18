// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class ContainsTests : EnumerableTests
    {
        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > int.MinValue
                    select x;

            Assert.Equal(q.Contains(-1), q.Contains(-1));
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", string.Empty }
                    where !string.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.Contains("X"), q.Contains("X"));
        }

        public static IEnumerable<object[]> Int_TestData()
        {
            yield return new object[] { new int[0], 6, false };
            yield return new object[] { new int[] { 8, 10, 3, 0, -8 }, 6, false };
            yield return new object[] { new int[] { 8, 10, 3, 0, -8 }, 8, true };
            yield return new object[] { new int[] { 8, 10, 3, 0, -8 }, -8, true };
            yield return new object[] { new int[] { 8, 0, 10, 3, 0, -8, 0 }, 0, true };

            yield return new object[] { NumberRangeGuaranteedNotCollectionType(0, 0), 0, false };
            yield return new object[] { NumberRangeGuaranteedNotCollectionType(4, 5), 3, false };
            yield return new object[] { NumberRangeGuaranteedNotCollectionType(3, 5), 3, true };
            yield return new object[] { NumberRangeGuaranteedNotCollectionType(3, 5), 7, true };
            yield return new object[] { RepeatedNumberGuaranteedNotCollectionType(10, 3), 10, true };
        }

        [Theory]
        [MemberData(nameof(Int_TestData))]
        public void Int(IEnumerable<int> source, int value, bool expected)
        {
            Assert.Equal(expected, source.Contains(value));
            Assert.Equal(expected, source.Contains(value, null));
        }

        [Theory, MemberData(nameof(Int_TestData))]
        public void IntRunOnce(IEnumerable<int> source, int value, bool expected)
        {
            Assert.Equal(expected, source.RunOnce().Contains(value));
            Assert.Equal(expected, source.RunOnce().Contains(value, null));
        }

        public static IEnumerable<object[]> String_TestData()
        {
            yield return new object[] { new string[] { null }, StringComparer.Ordinal, null, true };
            yield return new object[] { new string[] { "Bob", "Robert", "Tim" }, null, "trboeR", false };
            yield return new object[] { new string[] { "Bob", "Robert", "Tim" }, null, "Tim", true };
            yield return new object[] { new string[] { "Bob", "Robert", "Tim" }, new AnagramEqualityComparer(), "trboeR", true };
            yield return new object[] { new string[] { "Bob", "Robert", "Tim" }, new AnagramEqualityComparer(), "nevar", false };
        }

        [Theory]
        [MemberData(nameof(String_TestData))]
        public void String(IEnumerable<string> source, IEqualityComparer<string> comparer, string value, bool expected)
        {
            if (comparer is null)
            {
                Assert.Equal(expected, source.Contains(value));
            }
            Assert.Equal(expected, source.Contains(value, comparer));
        }

        [Theory, MemberData(nameof(String_TestData))]
        public void StringRunOnce(IEnumerable<string> source, IEqualityComparer<string> comparer, string value, bool expected)
        {
            if (comparer is null)
            {
                Assert.Equal(expected, source.RunOnce().Contains(value));
            }
            Assert.Equal(expected, source.RunOnce().Contains(value, comparer));
        }

        public static IEnumerable<object[]> NullableInt_TestData()
        {
            yield return new object[] { new int?[] { 8, 0, 10, 3, 0, -8, 0 }, null, false };
            yield return new object[] { new int?[] { 8, 0, 10, null, 3, 0, -8, 0 }, null, true };

            yield return new object[] { NullableNumberRangeGuaranteedNotCollectionType(3, 4), null, false };
            yield return new object[] { RepeatedNullableNumberGuaranteedNotCollectionType(null, 5), null, true };
        }

        [Theory]
        [MemberData(nameof(NullableInt_TestData))]
        public void NullableInt(IEnumerable<int?> source, int? value, bool expected)
        {
            Assert.Equal(expected, source.Contains(value));
            Assert.Equal(expected, source.Contains(value, null));
        }

        [Fact]
        public void NullSource_ThrowsArgumentNullException()
        {
            IEnumerable<int> source = null;

            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Contains(42));
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Contains(42, EqualityComparer<int>.Default));
        }

        [Fact]
        public void ExplicitNullComparerDoesNotDeferToCollection()
        {
            IEnumerable<string> source = new HashSet<string>(new AnagramEqualityComparer()) {"ABC"};
            Assert.False(source.Contains("BAC", null));
        }

        [Fact]
        public void ExplicitComparerDoesNotDeferToCollection()
        {
            IEnumerable<string> source = new HashSet<string> {"ABC"};
            Assert.True(source.Contains("abc", StringComparer.OrdinalIgnoreCase));
        }

        [Fact]
        public void ExplicitComparerDoestNotDeferToCollectionWithComparer()
        {
            IEnumerable<string> source = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {"ABC"};
            Assert.True(source.Contains("BAC", new AnagramEqualityComparer()));
        }

        [Fact]
        public void NoComparerDoesDeferToCollection()
        {
            IEnumerable<string> source = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {"ABC"};
            Assert.True(source.Contains("abc"));
        }
    }
}
