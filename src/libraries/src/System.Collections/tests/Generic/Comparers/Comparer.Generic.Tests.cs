// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Generic.Tests
{
    public abstract partial class ComparersGenericTests<T>
    {
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Missing https://github.com/dotnet/coreclr/pull/4340")]
        [Fact]
        public void Comparer_ComparerDefault()
        {
            var firstResult = Comparer<T>.Default;
            Assert.NotNull(firstResult);
            Assert.Same(firstResult, Comparer<T>.Default);
        }

        [Fact]
        public void Comparer_EqualsShouldBeOverriddenAndWorkForDifferentInstances()
        {
            var comparer = Comparer<T>.Default;

            // Whether the comparer has overridden Object.Equals or not, all of these
            // comparisons should be false
            Assert.False(comparer.Equals(null));
            Assert.False(comparer.Equals(3));
            Assert.False(comparer.Equals("foo"));
            Assert.False(comparer.Equals(Comparer<Task<T>>.Default));
        }

        [Fact]
        public void Comparer_EqualsShouldBeOverriddenAndWorkForDifferentInstances_cloned()
        {
            var comparer = Comparer<T>.Default;
            var cloned = ObjectCloner.MemberwiseClone(comparer); // calls MemberwiseClone() on the comparer via reflection, which returns a different instance

            // Whatever the type of the comparer, it should have overridden Equals(object) so
            // it can return true as long as the other object is the same type (not nec. the same instance)
            Assert.True(cloned.Equals(comparer));
            Assert.True(comparer.Equals(cloned));

            // Equals() should not return true for null
            // Prevent a faulty implementation like Equals(obj) => obj is FooComparer<T>, which will be true for null
            Assert.False(cloned.Equals(null));
        }

        [Fact]
        public void Comparer_GetHashCodeShouldBeOverriddenAndBeTheSameAsLongAsTheTypeIsTheSame()
        {
            var comparer = Comparer<T>.Default;

            // Multiple invocations should return the same result,
            // whether GetHashCode() was overridden or not
            Assert.Equal(comparer.GetHashCode(), comparer.GetHashCode());
        }

        [Fact]
        public void Comparer_GetHashCodeShouldBeOverriddenAndBeTheSameAsLongAsTheTypeIsTheSame_cloned()
        {
            var comparer = Comparer<T>.Default;
            var cloned = ObjectCloner.MemberwiseClone(comparer);
            Assert.Equal(cloned.GetHashCode(), cloned.GetHashCode());

            // Since comparer and cloned should have the same type, they should have the same hash
            Assert.Equal(comparer.GetHashCode(), cloned.GetHashCode());
        }

        [Fact]
        public void Comparer_IComparerCompareWithObjectsNotOfMatchingTypeShouldThrow()
        {
            // Comparer<T> implements IComparer for back-compat reasons.
            // The explicit implementation of IComparer.Compare(object, object) should
            // throw if both inputs are non-null and one of them is not of type T
            IComparer comparer = Comparer<T>.Default;
            Task<T> notOfTypeT = Task.FromResult(default(T));
            if (default(T) != null) // if default(T) is null these asserts will fail as IComparer.Compare returns early if either side is null
            {
                AssertExtensions.Throws<ArgumentException>(null, () => comparer.Compare(notOfTypeT, default(T))); // lhs is the problem
                AssertExtensions.Throws<ArgumentException>(null, () => comparer.Compare(default(T), notOfTypeT)); // rhs is the problem
            }
            if (!(notOfTypeT is T)) // catch cases where Task<T> actually is a T, like object or non-generic Task
            {
                AssertExtensions.Throws<ArgumentException>(null, () => comparer.Compare(notOfTypeT, notOfTypeT)); // The implementation should not attempt to short-circuit if both sides have reference equality
                AssertExtensions.Throws<ArgumentException>(null, () => comparer.Compare(notOfTypeT, Task.FromResult(default(T)))); // And it should also work when they don't
            }
        }

        [Fact]
        public void Comparer_ComparerCreate()
        {
            const int ExpectedValue = 0x77777777;

            bool comparisonCalled = false;
            Comparison<T> comparison = (left, right) =>
            {
                comparisonCalled = true;
                return ExpectedValue;
            };

            var comparer = Comparer<T>.Create(comparison);
            var comparer2 = Comparer<T>.Create(comparison);

            Assert.NotNull(comparer);
            Assert.NotNull(comparer2);
            Assert.NotSame(comparer, comparer2);

            // Test the functionality of the Comparer's Compare()
            int result = comparer.Compare(default(T), default(T));
            Assert.True(comparisonCalled);
            Assert.Equal(ExpectedValue, result);

            // Unlike the Default comparers, comparers created with Create
            // should not override Equals() or GetHashCode()
            Assert.False(comparer.Equals(comparer2));
            Assert.False(comparer2.Equals(comparer));
            // The default GetHashCode implementation is just a call to RuntimeHelpers.GetHashCode
            Assert.Equal(RuntimeHelpers.GetHashCode(comparer), comparer.GetHashCode());
            Assert.Equal(RuntimeHelpers.GetHashCode(comparer2), comparer2.GetHashCode());
        }

        [Fact]
        public void Comparer_ComparerCreateWithNullComparisonThrows()
        {
            AssertExtensions.Throws<ArgumentNullException>("comparison", () => Comparer<T>.Create(comparison: null));
        }
    }
}
