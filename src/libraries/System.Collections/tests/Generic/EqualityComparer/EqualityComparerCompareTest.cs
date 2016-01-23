// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.Tests
{
    public class EqualityComparerCompareTest
    {
        public static IEnumerable<object[]> EqualData
        {
            get
            {
                return new[] 
                {
                    new object[] {EqualityComparer<Double>.Default , Double.MaxValue , Double.MaxValue },
                    new object[] {EqualityComparer<Single>.Default , Single.MaxValue , Single.MaxValue }
                };
            }
        }

        public static IEnumerable<object[]> UnequalData
        {
            get
            {
                return new[] 
                {
                    new object[] {EqualityComparer<Double>.Default , Double.MaxValue, Double.MinValue },
                    new object[] {EqualityComparer<Single>.Default , Single.MaxValue, Single.MinValue }
                };
            }
        }

        public static IEnumerable<object[]> FirstParamNaNData
        {
            get
            {
                return new[] 
                {
                    new object[] {EqualityComparer<Double>.Default , Double.NaN, (Double)2 },
                    new object[] {EqualityComparer<Single>.Default , Single.NaN, (Single)2 }
                };
            }
        }

        public static IEnumerable<object[]> SecondParamNaNData
        {
            get
            {
                return new[] 
                {
                    new object[] {EqualityComparer<Double>.Default , (Double)1, Double.NaN },
                    new object[] {EqualityComparer<Single>.Default , (Single)1, Single.NaN }
                };
            }
        }

        public static IEnumerable<object[]> BothParamNaNData
        {
            get
            {
                return new[] 
                {
                    new object[] {EqualityComparer<Double>.Default , Double.NaN, Double.NaN },
                    new object[] {EqualityComparer<Single>.Default , Single.NaN, Single.NaN }
                };
            }
        }

        [Theory]
        [MemberData("EqualData")]
        public static void CompareEqualDataTest<T>(EqualityComparer<T> comparer, T x, T y)
        {
            Assert.True(comparer.Equals(x, y));
        }

        [Theory]
        [MemberData("UnequalData")]
        public static void CompareUnequalDataTest<T>(EqualityComparer<T> comparer, T x, T y)
        {
            Assert.False(comparer.Equals(x, y));
        }

        [Theory]
        [MemberData("FirstParamNaNData")]
        public static void CompareWhenFirstValuesIsNaNTest<T>(EqualityComparer<T> comparer, T x, T y)
        {
            Assert.False(comparer.Equals(x, y));
        }

        [Theory]
        [MemberData("SecondParamNaNData")]
        public static void CompareWhenSecondValuesIsNaNTest<T>(EqualityComparer<T> comparer, T x, T y)
        {
            Assert.False(comparer.Equals(x, y));
        }

        [Theory]
        [MemberData("BothParamNaNData")]
        public static void CompareWhenBothValuesIsNaNTest<T>(EqualityComparer<T> comparer, T x, T y)
        {
            Assert.True(comparer.Equals(x, y));
        }
    }
}
