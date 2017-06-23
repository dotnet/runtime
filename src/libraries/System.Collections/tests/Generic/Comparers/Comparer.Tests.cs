// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Tests;
using System.Diagnostics;
using Xunit;

namespace System.Collections.Generic.Tests
{
    public class ComparerTests
    {
        [Theory]
        [MemberData(nameof(IComparableComparisonsData))]
        [MemberData(nameof(UInt64EnumComparisonsData))]
        [MemberData(nameof(Int32EnumComparisonsData))]
        [MemberData(nameof(UInt32EnumComparisonsData))]
        [MemberData(nameof(Int64EnumComparisonsData))]
        [MemberData(nameof(PlainObjectComparisonsData))]
        public void MostComparisons<T>(T left, T right, int expected)
        {
            var comparer = Comparer<T>.Default;
            Assert.Equal(expected, Math.Sign(comparer.Compare(left, right)));
            
            // Because of these asserts we don't need to explicitly add tests for
            // 0 being an expected value, it is done automatically for every input
            Assert.Equal(0, comparer.Compare(left, left));
            Assert.Equal(0, comparer.Compare(right, right));

            IComparer nonGenericComparer = comparer;
            // If both sides are Ts then the explicit implementation of IComparer.Compare
            // should also succeed, with the same results
            Assert.Equal(expected, Math.Sign(nonGenericComparer.Compare(left, right)));
            Assert.Equal(0, nonGenericComparer.Compare(left, left));
            Assert.Equal(0, nonGenericComparer.Compare(right, right));

            // All comparers returned by Comparer<T>.Default should be able
            // to handle nulls before dispatching to IComparable<T>.CompareTo()
            if (default(T) == null) // This will be true if T is a reference type or nullable
            {
                T nil = default(T);
                Assert.Equal(0, comparer.Compare(nil, nil));

                // null should be ordered before/equal to everything (never after)
                // We assert that it's -1 or 0 in case left/right is null, as well
                // We assert that it's -1 rather than any negative number since these
                // values are hardcoded in the comparer logic, rather than being left
                // to the object being compared
                Assert.InRange(comparer.Compare(nil, left), -1, 0);
                Assert.InRange(comparer.Compare(nil, right), -1, 0);

                Assert.InRange(comparer.Compare(left, nil), 0, 1);
                Assert.InRange(comparer.Compare(right, nil), 0, 1);

                // Validate behavior for the IComparer.Compare implementation, as well
                Assert.Equal(0, nonGenericComparer.Compare(nil, nil));

                Assert.InRange(nonGenericComparer.Compare(nil, left), -1, 0);
                Assert.InRange(nonGenericComparer.Compare(nil, right), -1, 0);

                Assert.InRange(nonGenericComparer.Compare(left, nil), 0, 1);
                Assert.InRange(nonGenericComparer.Compare(right, nil), 0, 1);
            }
        }

        // NOTE: The test cases from the MemberData don't include 0 as the expected value,
        // since for each case we automatically test that Compare(lhs, lhs) and Compare(rhs, rhs)
        // are both 0.

        public static IEnumerable<object[]> IComparableComparisonsData()
        {
            var testCases = new[]
            {
                Tuple.Create(new GenericComparable(3), new GenericComparable(4), -1),
                Tuple.Create(new GenericComparable(5), new GenericComparable(2), 1),
                // GenericComparable's CompareTo does not handle nulls intentionally, the Comparer should check both
                // inputs for null before dispatching to CompareTo
                Tuple.Create(new GenericComparable(int.MinValue), default(GenericComparable), 1)
            };

            foreach (var testCase in testCases)
            {
                yield return new object[] { testCase.Item1, testCase.Item2, testCase.Item3 };
                yield return new object[] { testCase.Item2, testCase.Item1, -testCase.Item3 }; // Do the comparison in reverse as well
            }
        }

        public static IEnumerable<object[]> UInt64EnumComparisonsData()
        {
            var testCases = new[]
            {
                Tuple.Create(3UL, 5UL, -1),
                // Catch any attempt to cast the enum value to a signed type,
                // which may result in overflow and an incorrect comparison
                Tuple.Create(ulong.MaxValue, (ulong)long.MaxValue, 1),
                Tuple.Create(ulong.MaxValue - 3, ulong.MaxValue, -1)
            };
            
            foreach (var testCase in testCases)
            {
                yield return new object[] { (UInt64Enum)testCase.Item1, (UInt64Enum)testCase.Item2, testCase.Item3 };
                yield return new object[] { (UInt64Enum)testCase.Item2, (UInt64Enum)testCase.Item1, -testCase.Item3 };
            }
        }

        public static IEnumerable<object[]> Int32EnumComparisonsData()
        {
            var testCases = new[]
            {
                Tuple.Create(-1, 4, -1),
                Tuple.Create(-222, -375, 1),
                // The same principle applies for overflow in signed types as above,
                // the implementation should not cast to an unsigned type
                Tuple.Create(int.MaxValue, int.MinValue, 1),
                Tuple.Create(int.MinValue + 1, int.MinValue, 1)
            };

            foreach (var testCase in testCases)
            {
                yield return new object[] { (Int32Enum)testCase.Item1, (Int32Enum)testCase.Item2, testCase.Item3 };
                yield return new object[] { (Int32Enum)testCase.Item2, (Int32Enum)testCase.Item1, -testCase.Item3 };
            }
        }

        public static IEnumerable<object[]> UInt32EnumComparisonsData()
        {
            var testCases = new[]
            {
                Tuple.Create(445u, 123u, 1),
                Tuple.Create(uint.MaxValue, 111u, 1),
                Tuple.Create(uint.MaxValue - 333, uint.MaxValue, -1)
            };

            foreach (var testCase in testCases)
            {
                yield return new object[] { (UInt32Enum)testCase.Item1, (UInt32Enum)testCase.Item2, testCase.Item3 };
                yield return new object[] { (UInt32Enum)testCase.Item2, (UInt32Enum)testCase.Item1, -testCase.Item3 };
            }
        }

        public static IEnumerable<object[]> Int64EnumComparisonsData()
        {
            var testCases = new[]
            {
                Tuple.Create(182912398L, 33L, 1),
                Tuple.Create(long.MinValue, long.MaxValue, -1),
                Tuple.Create(long.MinValue + 9, long.MinValue, 1)
            };
            
            foreach (var testCase in testCases)
            {
                yield return new object[] { (Int64Enum)testCase.Item1, (Int64Enum)testCase.Item2, testCase.Item3 };
                yield return new object[] { (Int64Enum)testCase.Item2, (Int64Enum)testCase.Item1, -testCase.Item3 };
            }
        }

        public static IEnumerable<object[]> PlainObjectComparisonsData()
        {
            var obj = new object(); // this needs to be cached into a local so we can pass the same ref in twice

            var testCases = new[]
            {
                Tuple.Create(obj, obj, 0), // even if it doesn't implement IComparable, if 2 refs are the same then the result should be 0
                Tuple.Create(default(object), obj, -1) // even if it doesn't implement IComparable, if one side is null -1 or 1 should be returned
            };

            foreach (var testCase in testCases)
            {
                yield return new object[] { testCase.Item1, testCase.Item2, testCase.Item3 };
                yield return new object[] { testCase.Item2, testCase.Item1, -testCase.Item3 };
            }
        }

        [Fact]
        public void IComparableComparisonsShouldTryToCallLeftHandCompareToFirst()
        {
            var left = new MutatingComparable(0);
            var right = new MutatingComparable(0);

            var comparer = Comparer<MutatingComparable>.Default;

            // Every CompareTo() call yields the comparable's current
            // state and then increments it
            Assert.Equal(0, comparer.Compare(left, right));
            Assert.Equal(1, comparer.Compare(left, right));
            Assert.Equal(0, comparer.Compare(right, left));
        }

        [Fact]
        public void NonGenericIComparableComparisonsShouldTryToCallLeftHandCompareToFirst()
        {
            var left = new MutatingComparable(0);
            var right = new object();

            var comparer = Comparer<object>.Default;

            Assert.Equal(0, comparer.Compare(left, right));
            Assert.Equal(1, comparer.Compare(left, right));
            // If the lhs does not implement IComparable, the rhs should be checked
            // Additionally the result from rhs.CompareTo should be negated
            Assert.Equal(-2, comparer.Compare(right, left));
        }

        [Fact]
        public void DifferentNonNullObjectsThatDoNotImplementIComparableShouldThrowWhenCompared()
        {
            var left = new object();
            var right = new object();

            var comparer = Comparer<object>.Default;
            AssertExtensions.Throws<ArgumentException>(null, () => comparer.Compare(left, right));
        }

        [Fact]
        public void ComparerDefaultShouldAttemptToUseTheGenericIComparableInterfaceFirst()
        {
            // IComparable<>.CompareTo returns 1 for this type,
            // non-generic overload returns -1
            var left = new BadlyBehavingComparable();
            var right = new BadlyBehavingComparable();

            var comparer = Comparer<BadlyBehavingComparable>.Default;
            // The comparer should pick up on the generic implementation first
            Assert.Equal(1, comparer.Compare(left, right));
            Assert.Equal(1, comparer.Compare(right, left));
        }
        
        // The runtime treats nullables specially when they're boxed,
        // for example `object o = new int?(3); o is int` is true.
        // This messes with the xUnit type inference for generic theories,
        // so we need to write another theory (accepting non-nullable parameters)
        // just for nullables.

        [Theory]
        [MemberData(nameof(NullableOfInt32ComparisonsData))]
        [MemberData(nameof(NullableOfInt32EnumComparisonsData))]
        public void NullableComparisons<T>(T leftValue, bool leftHasValue, T rightValue, bool rightHasValue, int expected) where T : struct
        {
            // Comparer<T> is specialized (for perf reasons) when T : U? where U : IComparable<U>
            T? left = leftHasValue ? new T?(leftValue) : null;
            T? right = rightHasValue ? new T?(rightValue) : null;

            var comparer = Comparer<T?>.Default;
            Assert.Equal(expected, Math.Sign(comparer.Compare(left, right)));
            Assert.Equal(0, comparer.Compare(left, left));
            Assert.Equal(0, comparer.Compare(right, right));

            // Converting the comparer to a non-generic IComparer lets us
            // test the explicit implementation of IComparer.Compare as well,
            // which accepts 2 objects rather than nullables.
            IComparer nonGenericComparer = comparer;

            // The way this works is that, assuming two non-null nullables,
            // T? will get boxed to a object with GetType() == typeof(T),
            // (object is T?) will be true, and then it will get converted
            // back to a T?.
            // If one of the inputs is null, it will get boxed to a null object
            // and then IComparer.Compare() should take care of it itself.
            Assert.Equal(expected, Math.Sign(nonGenericComparer.Compare(left, right)));
            Assert.Equal(0, nonGenericComparer.Compare(left, left));
            Assert.Equal(0, nonGenericComparer.Compare(right, right));

            // As above, the comparer should handle null inputs itself and only
            // return -1, 0, or 1 in such circumstances
            Assert.Equal(0, comparer.Compare(null, null)); // null and null should have the same sorting order
            Assert.InRange(comparer.Compare(null, left), -1, 0); // "null" values should come before anything else
            Assert.InRange(comparer.Compare(null, right), -1, 0);
            Assert.InRange(comparer.Compare(left, null), 0, 1);
            Assert.InRange(comparer.Compare(right, null), 0, 1);

            Assert.Equal(0, nonGenericComparer.Compare(null, null));
            Assert.InRange(nonGenericComparer.Compare(null, left), -1, 0);
            Assert.InRange(nonGenericComparer.Compare(null, right), -1, 0);
            Assert.InRange(nonGenericComparer.Compare(left, null), 0, 1);
            Assert.InRange(nonGenericComparer.Compare(right, null), 0, 1);

            // new T?() < new T?(default(T))
            Assert.Equal(-1, comparer.Compare(null, default(T)));
            Assert.Equal(1, comparer.Compare(default(T), null));
            Assert.Equal(0, comparer.Compare(default(T), default(T)));
            
            Assert.Equal(-1, nonGenericComparer.Compare(null, default(T)));
            Assert.Equal(1, nonGenericComparer.Compare(default(T), null));
            Assert.Equal(0, nonGenericComparer.Compare(default(T), default(T)));
        }

        public static IEnumerable<object[]> NullableOfInt32ComparisonsData()
        {
            var testCases = new[]
            {
                Tuple.Create(default(int), false, int.MinValue, true, -1), // "null" values should come before anything else
                Tuple.Create(int.MaxValue, true, int.MinValue, true, 1) // Comparisons between two non-null nullables should work as normal
            };

            foreach (var testCase in testCases)
            {
                yield return new object[] { testCase.Item1, testCase.Item2, testCase.Item3, testCase.Item4, testCase.Item5 };
                yield return new object[] { testCase.Item3, testCase.Item4, testCase.Item1, testCase.Item2, -testCase.Item5 };
            }
        }

        public static IEnumerable<object[]> NullableOfInt32EnumComparisonsData()
        {
            // Currently the default Comparer/EqualityComparer is optimized for when
            // T : U? where U : IComparable<U> or T : enum, but not T : U? where
            // U : enum (aka T is a nullable enum).
            // So, let's cover that codepath in case that changes/regresses in the future.

            var testCases = new[]
            {
                Tuple.Create(int.MinValue, true, default(int), false, 1), // "null" values should come first
                Tuple.Create(-1, true, 4, true, -1)
            };

            foreach (var testCase in testCases)
            {
                yield return new object[] { testCase.Item1, testCase.Item2, testCase.Item3, testCase.Item4, testCase.Item5 };
                yield return new object[] { testCase.Item3, testCase.Item4, testCase.Item1, testCase.Item2, -testCase.Item5 };
            }
        }

        [Fact]
        public void NullableOfIComparableComparisonsShouldTryToCallLeftHandCompareToFirst()
        {
            // If two non-null nullables are passed in, Default<T?>.Compare
            // should try to call the left-hand side's CompareTo() first

            // Would have liked to reuse MutatingComparable here, but it is
            // a class and can't be nullable, so it's necessary to wrap it
            // in a struct

            var leftValue = new MutatingComparable(0);
            var rightValue = new MutatingComparable(0);

            var left = new ValueComparable<MutatingComparable>?(ValueComparable.Create(leftValue));
            var right = new ValueComparable<MutatingComparable>?(ValueComparable.Create(rightValue));

            var comparer = Comparer<ValueComparable<MutatingComparable>?>.Default;
            Assert.Equal(0, comparer.Compare(left, right));
            Assert.Equal(1, comparer.Compare(left, right));
            Assert.Equal(0, comparer.Compare(right, left));
        }
    }
}
