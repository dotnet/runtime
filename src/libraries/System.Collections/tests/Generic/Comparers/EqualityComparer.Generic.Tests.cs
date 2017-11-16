// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Generic.Tests
{
    public abstract partial class ComparersGenericTests<T>
    {
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Missing https://github.com/dotnet/coreclr/pull/4340")]
        [Fact]
        public void EqualityComparer_EqualityComparerDefault()
        {
            var firstResult = EqualityComparer<T>.Default;
            Assert.NotNull(firstResult);
            Assert.Same(firstResult, EqualityComparer<T>.Default);
        }

        [Fact]
        public void EqualityComparer_EqualsShouldBeOverriddenAndWorkForDifferentInstances()
        {
            var comparer = EqualityComparer<T>.Default;

            // Whether the comparer has overridden Object.Equals or not, all of these
            // comparisons should be false
            Assert.False(comparer.Equals(null));
            Assert.False(comparer.Equals(3));
            Assert.False(comparer.Equals("foo"));
            Assert.False(comparer.Equals(EqualityComparer<Task<T>>.Default));
        }

        [Fact]
        public void EqualityComparer_EqualsShouldBeOverriddenAndWorkForDifferentInstances_cloned()
        {
            var comparer = EqualityComparer<T>.Default;
            var cloned = ObjectCloner.MemberwiseClone(comparer); // calls MemberwiseClone() on the comparer via reflection, which returns a different instance

            // Whatever the type of the comparer, it should have overridden Equals(object) so
            // it can return true as long as the other object is the same type (not nec. the same instance)
            Assert.True(cloned.Equals(comparer));
            Assert.True(comparer.Equals(cloned));

            // Equals() should not return true for null
            // Prevent a faulty implementation like Equals(obj) => obj is FooEqualityComparer<T>, which will be true for null
            Assert.False(cloned.Equals(null));
        }

        [Fact]
        public void EqualityComparer_GetHashCodeShouldBeOverriddenAndBeTheSameAsLongAsTheTypeIsTheSame()
        {
            var comparer = EqualityComparer<T>.Default;

            // Multiple invocations should return the same result,
            // whether GetHashCode() was overridden or not
            Assert.Equal(comparer.GetHashCode(), comparer.GetHashCode());
        }

        [Fact]
        public void EqualityComparer_GetHashCodeShouldBeOverriddenAndBeTheSameAsLongAsTheTypeIsTheSame_cloned()
        {
            var comparer = EqualityComparer<T>.Default;
            var cloned = ObjectCloner.MemberwiseClone(comparer);
            Assert.Equal(cloned.GetHashCode(), cloned.GetHashCode());

            // Since comparer and cloned should have the same type, they should have the same hash
            Assert.Equal(comparer.GetHashCode(), cloned.GetHashCode());
        }

        [Fact]
        public void EqualityComparer_IEqualityComparerEqualsWithObjectsNotOfMatchingType()
        {
            // EqualityComparer<T> implements IEqualityComparer for back-compat reasons.
            // The explicit implementation of IEqualityComparer.Equals(object, object) should
            // throw if the inputs are not reference-equal, both non-null and either of them
            // is not of type T.
            IEqualityComparer comparer = EqualityComparer<T>.Default;
            Task<T> notOfTypeT = Task.FromResult(default(T));

            Assert.True(comparer.Equals(default(T), default(T))); // This should not throw since both inputs will either be null or Ts.
            Assert.True(comparer.Equals(notOfTypeT, notOfTypeT)); // This should not throw since the inputs are reference-equal.

            // Null inputs should never raise an exception.
            Assert.Equal(default(T) == null, comparer.Equals(default(T), null));
            Assert.Equal(default(T) == null, comparer.Equals(null, default(T)));
            Assert.True(comparer.Equals(null, null));

            if (default(T) != null) // if default(T) is null this assert will fail as IEqualityComparer.Equals returns early if either input is null
            {
                AssertExtensions.Throws<ArgumentException>(null, () => comparer.Equals(notOfTypeT, default(T))); // lhs is the problem
                AssertExtensions.Throws<ArgumentException>(null, () => comparer.Equals(default(T), notOfTypeT)); // rhs is the problem
            }

            if (!(notOfTypeT is T)) // catch cases where Task<T> actually is a T, like object or non-generic Task
            {
                AssertExtensions.Throws<ArgumentException>(null, () => comparer.Equals(notOfTypeT, Task.FromResult(default(T))));
            }
        }

        [Fact]
        public void EqualityComparer_IEqualityComparerGetHashCodeWithObjectNotOfMatchingType()
        {
            // IEqualityComparer.GetHashCode explicit implementation should
            // first return 0 if null, then check for T, then throw an ArgumentException.

            IEqualityComparer comparer = EqualityComparer<T>.Default;
            Task<T> notOfTypeT = Task.FromResult(default(T));

            Assert.Equal(0, comparer.GetHashCode(null));
            Assert.Equal(comparer.GetHashCode(default(T)), comparer.GetHashCode(default(T)));

            if (!(notOfTypeT is T))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => comparer.GetHashCode(notOfTypeT));
            }
        }
    }
}
