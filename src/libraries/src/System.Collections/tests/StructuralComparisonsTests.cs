// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Tests
{
    public class StructuralComparisonsTests
    {
        [Fact]
        public void StructuralComparer_ReturnsSameInstance()
        {
            Assert.Same(StructuralComparisons.StructuralComparer, StructuralComparisons.StructuralComparer);
        }

        public static IEnumerable<object[]> StructuralComparer_Compare_TestData()
        {
            yield return new object[] { null, null, 0 };
            yield return new object[] { null, "abc", -1 };
            yield return new object[] { "abc", null, 1 };
            yield return new object[] { "abc", "abc", 0 };
            yield return new object[] { "abc", "def", -1 };
            yield return new object[] { "def", "abc", 1 };
            yield return new object[] { new StructuralObject(), "abc", 5 };
            yield return new object[] { new StructuralObject(), 123, -5 };
        }

        [Theory]
        [MemberData(nameof(StructuralComparer_Compare_TestData))]
        public void StructuralComparer_Compare(object x, object y, int expected)
        {
            Assert.Equal(expected, StructuralComparisons.StructuralComparer.Compare(x, y));
        }

        [Fact]
        public void StructuralEqualityComparer_ReturnsSameInstance()
        {
            Assert.Same(StructuralComparisons.StructuralEqualityComparer, StructuralComparisons.StructuralEqualityComparer);
        }

        public static IEnumerable<object[]> StructuralEqualityComparer_Equals_TestData()
        {
            yield return new object[] { null, null, true };
            yield return new object[] { null, "abc", false };
            yield return new object[] { "abc", null, false };
            yield return new object[] { "abc", "abc", true };
            yield return new object[] { "def", "abc", false };
            yield return new object[] { "abc", "def", false };
            yield return new object[] { new StructuralObject(), "abc", true };
            yield return new object[] { new StructuralObject(), 123, false };
        }

        [Theory]
        [MemberData(nameof(StructuralEqualityComparer_Equals_TestData))]
        public void StructuralEqualityComparer_Equals(object x, object y, bool expected)
        {
            Assert.Equal(expected, StructuralComparisons.StructuralEqualityComparer.Equals(x, y));
        }

        public static IEnumerable<object[]> StructuralEqualityComparer_GetHashCode_TestData()
        {
            yield return new object[] { null, 0 };
            yield return new object[] { "abc", "abc".GetHashCode() };
            yield return new object[] { new StructuralObject(), 5 };
        }

        [Theory]
        [MemberData(nameof(StructuralEqualityComparer_GetHashCode_TestData))]
        public void StructuralEqualityComparer_GetHashCode(object obj, int expected)
        {
            Assert.Equal(expected, StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj));
        }

        private class StructuralObject : IStructuralEquatable, IStructuralComparable
        {
            public int CompareTo(object other, IComparer comparer) => other is string ? 5 : -5;

            public bool Equals(object other, IEqualityComparer comparer) => other is string;
            public int GetHashCode(IEqualityComparer comparer) => 5;
        }
    }
}
