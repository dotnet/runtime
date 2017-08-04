// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public class ImmutableHashSetTest : ImmutableSetTest
    {
        protected override bool IncludesGetHashCodeDerivative
        {
            get { return true; }
        }

        [Fact]
        public void EmptyTest()
        {
            this.EmptyTestHelper(Empty<int>(), 5, null);
            this.EmptyTestHelper(EmptyTyped<string>().WithComparer(StringComparer.OrdinalIgnoreCase), "a", StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void CustomSort()
        {
            this.CustomSortTestHelper(
                ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal),
                false,
                new[] { "apple", "APPLE" },
                new[] { "apple", "APPLE" });
            this.CustomSortTestHelper(
                ImmutableHashSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase),
                false,
                new[] { "apple", "APPLE" },
                new[] { "apple" });
        }

        [Fact]
        public void ChangeUnorderedEqualityComparer()
        {
            var ordinalSet = ImmutableHashSet<string>.Empty
                .WithComparer(StringComparer.Ordinal)
                .Add("apple")
                .Add("APPLE");
            Assert.Equal(2, ordinalSet.Count); // claimed count
            Assert.False(ordinalSet.Contains("aPpLe"));

            var ignoreCaseSet = ordinalSet.WithComparer(StringComparer.OrdinalIgnoreCase);
            Assert.Equal(1, ignoreCaseSet.Count);
            Assert.True(ignoreCaseSet.Contains("aPpLe"));
        }

        [Fact]
        public void ToSortTest()
        {
            var set = ImmutableHashSet<string>.Empty
                .Add("apple")
                .Add("APPLE");
            var sorted = set.ToImmutableSortedSet();
            CollectionAssertAreEquivalent(set.ToList(), sorted.ToList());
        }

        [Fact]
        public void EnumeratorWithHashCollisionsTest()
        {
            var emptySet = this.EmptyTyped<int>().WithComparer(new BadHasher<int>());
            this.EnumeratorTestHelper(emptySet, null, 3, 1, 5);
        }

        [Fact]
        public void EnumeratorWithHashCollisionsTest_RefType()
        {
            var emptySet = this.EmptyTyped<string>().WithComparer(new BadHasher<string>());
            this.EnumeratorTestHelper(emptySet, null, "c", "a", "e");
        }

        [Fact]
        public void EnumeratorRecyclingMisuse()
        {
            var collection = ImmutableHashSet.Create<int>().Add(5);
            var enumerator = collection.GetEnumerator();
            var enumeratorCopy = enumerator;
            Assert.True(enumerator.MoveNext());
            Assert.False(enumerator.MoveNext());
            enumerator.Dispose();
            Assert.Throws<ObjectDisposedException>(() => enumerator.MoveNext());
            Assert.Throws<ObjectDisposedException>(() => enumerator.Reset());
            Assert.Throws<ObjectDisposedException>(() => enumerator.Current);
            Assert.Throws<ObjectDisposedException>(() => enumeratorCopy.MoveNext());
            Assert.Throws<ObjectDisposedException>(() => enumeratorCopy.Reset());
            Assert.Throws<ObjectDisposedException>(() => enumeratorCopy.Current);
            enumerator.Dispose(); // double-disposal should not throw
            enumeratorCopy.Dispose();

            // We expect that acquiring a new enumerator will use the same underlying Stack<T> object,
            // but that it will not throw exceptions for the new enumerator.
            enumerator = collection.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            enumerator.Dispose();
        }

        [Fact]
        public void Create()
        {
            var comparer = StringComparer.OrdinalIgnoreCase;

            var set = ImmutableHashSet.Create<string>();
            Assert.Equal(0, set.Count);
            Assert.Same(EqualityComparer<string>.Default, set.KeyComparer);

            set = ImmutableHashSet.Create<string>(comparer);
            Assert.Equal(0, set.Count);
            Assert.Same(comparer, set.KeyComparer);

            set = ImmutableHashSet.Create("a");
            Assert.Equal(1, set.Count);
            Assert.Same(EqualityComparer<string>.Default, set.KeyComparer);

            set = ImmutableHashSet.Create(comparer, "a");
            Assert.Equal(1, set.Count);
            Assert.Same(comparer, set.KeyComparer);

            set = ImmutableHashSet.Create("a", "b");
            Assert.Equal(2, set.Count);
            Assert.Same(EqualityComparer<string>.Default, set.KeyComparer);

            set = ImmutableHashSet.Create(comparer, "a", "b");
            Assert.Equal(2, set.Count);
            Assert.Same(comparer, set.KeyComparer);

            set = ImmutableHashSet.CreateRange((IEnumerable<string>)new[] { "a", "b" });
            Assert.Equal(2, set.Count);
            Assert.Same(EqualityComparer<string>.Default, set.KeyComparer);

            set = ImmutableHashSet.CreateRange(comparer, (IEnumerable<string>)new[] { "a", "b" });
            Assert.Equal(2, set.Count);
            Assert.Same(comparer, set.KeyComparer);

            set = ImmutableHashSet.Create(default(string));
            Assert.Equal(1, set.Count);

            set = ImmutableHashSet.CreateRange(new[] { null, "a", null, "b" });
            Assert.Equal(3, set.Count);
        }

        /// <summary>
        /// Verifies the non-removal of an item that does not belong to the set,
        /// but which happens to have a colliding hash code with another value
        /// that *is* in the set.
        /// </summary>
        [Fact]
        public void RemoveValuesFromCollidedHashCode()
        {
            var set = ImmutableHashSet.Create<int>(new BadHasher<int>(), 5, 6);
            Assert.Same(set, set.Remove(2));
            var setAfterRemovingFive = set.Remove(5);
            Assert.Equal(1, setAfterRemovingFive.Count);
            Assert.Equal(new[] { 6 }, setAfterRemovingFive);
        }

        /// <summary>
        /// Verifies the non-removal of an item that does not belong to the set,
        /// but which happens to have a colliding hash code with another value
        /// that *is* in the set.
        /// </summary>
        [Fact]
        public void RemoveValuesFromCollidedHashCode_RefType()
        {
            var set = ImmutableHashSet.Create<string>(new BadHasher<string>(), "a", "b");
            Assert.Same(set, set.Remove("c"));
            var setAfterRemovingA = set.Remove("a");
            Assert.Equal(1, setAfterRemovingA.Count);
            Assert.Equal(new[] { "b" }, setAfterRemovingA);
        }

        [Fact]
        public void TryGetValueTest()
        {
            this.TryGetValueTestHelper(ImmutableHashSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Cannot do DebuggerAttribute testing on UapAot: requires internal Reflection on framework types.")]
        public void DebuggerAttributesValid()
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(ImmutableHashSet.Create<string>());
            ImmutableHashSet<int> set = ImmutableHashSet.Create(1, 2, 3);
            DebuggerAttributeInfo info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(set);
            PropertyInfo itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>().State == DebuggerBrowsableState.RootHidden);
            int[] items = itemProperty.GetValue(info.Instance) as int[];
            Assert.Equal(set, items);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Cannot do DebuggerAttribute testing on UapAot: requires internal Reflection on framework types.")]
        public static void TestDebuggerAttributes_Null()
        {
            Type proxyType = DebuggerAttributes.GetProxyType(ImmutableHashSet.Create<string>());
            TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(proxyType, (object)null));
            Assert.IsType<ArgumentNullException>(tie.InnerException);
        }

        [Fact]
        public void SymmetricExceptWithComparerTests()
        {
            var set = ImmutableHashSet.Create<string>("a").WithComparer(StringComparer.OrdinalIgnoreCase);
            var otherCollection = new[] { "A" };

            var expectedSet = new HashSet<string>(set, set.KeyComparer);
            expectedSet.SymmetricExceptWith(otherCollection);

            var actualSet = set.SymmetricExcept(otherCollection);
            CollectionAssertAreEquivalent(expectedSet.ToList(), actualSet.ToList());
        }

        protected override IImmutableSet<T> Empty<T>()
        {
            return ImmutableHashSet<T>.Empty;
        }

        protected ImmutableHashSet<T> EmptyTyped<T>()
        {
            return ImmutableHashSet<T>.Empty;
        }

        protected override ISet<T> EmptyMutable<T>()
        {
            return new HashSet<T>();
        }

        internal override IBinaryTree GetRootNode<T>(IImmutableSet<T> set)
        {
            return ((ImmutableHashSet<T>)set).Root;
        }

        /// <summary>
        /// Tests various aspects of an unordered set.
        /// </summary>
        /// <typeparam name="T">The type of element stored in the set.</typeparam>
        /// <param name="emptySet">The empty set.</param>
        /// <param name="value">A value that could be placed in the set.</param>
        /// <param name="comparer">The comparer used to obtain the empty set, if any.</param>
        private void EmptyTestHelper<T>(IImmutableSet<T> emptySet, T value, IEqualityComparer<T> comparer)
        {
            Assert.NotNull(emptySet);

            this.EmptyTestHelper(emptySet);
            Assert.Same(emptySet, emptySet.ToImmutableHashSet(comparer));
            Assert.Same(comparer ?? EqualityComparer<T>.Default, ((IHashKeyCollection<T>)emptySet).KeyComparer);

            if (comparer == null)
            {
                Assert.Same(emptySet, ImmutableHashSet<T>.Empty);
            }

            var reemptied = emptySet.Add(value).Clear();
            Assert.Same(reemptied, reemptied.ToImmutableHashSet(comparer)); //, "Getting the empty set from a non-empty instance did not preserve the comparer.");
        }
    }
}
