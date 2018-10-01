// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public partial class ImmutableSortedSetBuilderTest : ImmutablesTestBase
    {
        [Fact]
        public void CreateBuilder()
        {
            ImmutableSortedSet<string>.Builder builder = ImmutableSortedSet.CreateBuilder<string>();
            Assert.NotNull(builder);

            builder = ImmutableSortedSet.CreateBuilder<string>(StringComparer.OrdinalIgnoreCase);
            Assert.Same(StringComparer.OrdinalIgnoreCase, builder.KeyComparer);
        }

        [Fact]
        public void ToBuilder()
        {
            var builder = ImmutableSortedSet<int>.Empty.ToBuilder();
            Assert.True(builder.Add(3));
            Assert.True(builder.Add(5));
            Assert.False(builder.Add(5));
            Assert.Equal(2, builder.Count);
            Assert.True(builder.Contains(3));
            Assert.True(builder.Contains(5));
            Assert.False(builder.Contains(7));

            var set = builder.ToImmutable();
            Assert.Equal(builder.Count, set.Count);
            Assert.True(builder.Add(8));
            Assert.Equal(3, builder.Count);
            Assert.Equal(2, set.Count);
            Assert.True(builder.Contains(8));
            Assert.False(set.Contains(8));
        }

        [Fact]
        public void BuilderFromSet()
        {
            var set = ImmutableSortedSet<int>.Empty.Add(1);
            var builder = set.ToBuilder();
            Assert.True(builder.Contains(1));
            Assert.True(builder.Add(3));
            Assert.True(builder.Add(5));
            Assert.False(builder.Add(5));
            Assert.Equal(3, builder.Count);
            Assert.True(builder.Contains(3));
            Assert.True(builder.Contains(5));
            Assert.False(builder.Contains(7));

            var set2 = builder.ToImmutable();
            Assert.Equal(builder.Count, set2.Count);
            Assert.True(set2.Contains(1));
            Assert.True(builder.Add(8));
            Assert.Equal(4, builder.Count);
            Assert.Equal(3, set2.Count);
            Assert.True(builder.Contains(8));

            Assert.False(set.Contains(8));
            Assert.False(set2.Contains(8));
        }

        [Fact]
        public void EnumerateBuilderWhileMutating()
        {
            var builder = ImmutableSortedSet<int>.Empty.Union(Enumerable.Range(1, 10)).ToBuilder();
            Assert.Equal(Enumerable.Range(1, 10), builder);

            var enumerator = builder.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            builder.Add(11);

            // Verify that a new enumerator will succeed.
            Assert.Equal(Enumerable.Range(1, 11), builder);

            // Try enumerating further with the previous enumerable now that we've changed the collection.
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            enumerator.Reset();
            enumerator.MoveNext(); // resetting should fix the problem.

            // Verify that by obtaining a new enumerator, we can enumerate all the contents.
            Assert.Equal(Enumerable.Range(1, 11), builder);
        }

        [Fact]
        public void BuilderReusesUnchangedImmutableInstances()
        {
            var collection = ImmutableSortedSet<int>.Empty.Add(1);
            var builder = collection.ToBuilder();
            Assert.Same(collection, builder.ToImmutable()); // no changes at all.
            builder.Add(2);

            var newImmutable = builder.ToImmutable();
            Assert.NotSame(collection, newImmutable); // first ToImmutable with changes should be a new instance.
            Assert.Same(newImmutable, builder.ToImmutable()); // second ToImmutable without changes should be the same instance.
        }

        [Fact]
        public void GetEnumeratorTest()
        {
            var builder = ImmutableSortedSet.Create("a", "B").WithComparer(StringComparer.Ordinal).ToBuilder();
            IEnumerable<string> enumerable = builder;
            using (var enumerator = enumerable.GetEnumerator())
            {
                Assert.True(enumerator.MoveNext());
                Assert.Equal("B", enumerator.Current);
                Assert.True(enumerator.MoveNext());
                Assert.Equal("a", enumerator.Current);
                Assert.False(enumerator.MoveNext());
            }
        }

        [Fact]
        public void MaxMin()
        {
            var builder = ImmutableSortedSet.Create(1, 2, 3).ToBuilder();
            Assert.Equal(1, builder.Min);
            Assert.Equal(3, builder.Max);
        }

        [Fact]
        public void Clear()
        {
            var set = ImmutableSortedSet<int>.Empty.Add(1);
            var builder = set.ToBuilder();
            builder.Clear();
            Assert.Equal(0, builder.Count);
        }

        [Fact]
        public void KeyComparer()
        {
            var builder = ImmutableSortedSet.Create("a", "B").ToBuilder();
            Assert.Same(Comparer<string>.Default, builder.KeyComparer);
            Assert.True(builder.Contains("a"));
            Assert.False(builder.Contains("A"));

            builder.KeyComparer = StringComparer.OrdinalIgnoreCase;
            Assert.Same(StringComparer.OrdinalIgnoreCase, builder.KeyComparer);
            Assert.Equal(2, builder.Count);
            Assert.True(builder.Contains("a"));
            Assert.True(builder.Contains("A"));

            var set = builder.ToImmutable();
            Assert.Same(StringComparer.OrdinalIgnoreCase, set.KeyComparer);
        }

        [Fact]
        public void KeyComparerCollisions()
        {
            var builder = ImmutableSortedSet.Create("a", "A").ToBuilder();
            builder.KeyComparer = StringComparer.OrdinalIgnoreCase;
            Assert.Equal(1, builder.Count);
            Assert.True(builder.Contains("a"));

            var set = builder.ToImmutable();
            Assert.Same(StringComparer.OrdinalIgnoreCase, set.KeyComparer);
            Assert.Equal(1, set.Count);
            Assert.True(set.Contains("a"));
        }

        [Fact]
        public void KeyComparerEmptyCollection()
        {
            var builder = ImmutableSortedSet.Create<string>().ToBuilder();
            Assert.Same(Comparer<string>.Default, builder.KeyComparer);
            builder.KeyComparer = StringComparer.OrdinalIgnoreCase;
            Assert.Same(StringComparer.OrdinalIgnoreCase, builder.KeyComparer);
            var set = builder.ToImmutable();
            Assert.Same(StringComparer.OrdinalIgnoreCase, set.KeyComparer);
        }

        [Fact]
        public void UnionWith()
        {
            var builder = ImmutableSortedSet.Create(1, 2, 3).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.UnionWith(null));
            builder.UnionWith(new[] { 2, 3, 4 });
            Assert.Equal(new[] { 1, 2, 3, 4 }, builder);
        }

        [Fact]
        public void ExceptWith()
        {
            var builder = ImmutableSortedSet.Create(1, 2, 3).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.ExceptWith(null));
            builder.ExceptWith(new[] { 2, 3, 4 });
            Assert.Equal(new[] { 1 }, builder);
        }

        [Fact]
        public void SymmetricExceptWith()
        {
            var builder = ImmutableSortedSet.Create(1, 2, 3).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.SymmetricExceptWith(null));
            builder.SymmetricExceptWith(new[] { 2, 3, 4 });
            Assert.Equal(new[] { 1, 4 }, builder);
        }

        [Fact]
        public void IntersectWith()
        {
            var builder = ImmutableSortedSet.Create(1, 2, 3).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.IntersectWith(null));
            builder.IntersectWith(new[] { 2, 3, 4 });
            Assert.Equal(new[] { 2, 3 }, builder);
        }

        [Fact]
        public void IsProperSubsetOf()
        {
            var builder = ImmutableSortedSet.CreateRange(Enumerable.Range(1, 3)).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.IsProperSubsetOf(null));
            Assert.False(builder.IsProperSubsetOf(Enumerable.Range(1, 3)));
            Assert.True(builder.IsProperSubsetOf(Enumerable.Range(1, 5)));
        }

        [Fact]
        public void IsProperSupersetOf()
        {
            var builder = ImmutableSortedSet.CreateRange(Enumerable.Range(1, 3)).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.IsProperSupersetOf(null));
            Assert.False(builder.IsProperSupersetOf(Enumerable.Range(1, 3)));
            Assert.True(builder.IsProperSupersetOf(Enumerable.Range(1, 2)));
        }

        [Fact]
        public void IsSubsetOf()
        {
            var builder = ImmutableSortedSet.CreateRange(Enumerable.Range(1, 3)).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.IsSubsetOf(null));
            Assert.False(builder.IsSubsetOf(Enumerable.Range(1, 2)));
            Assert.True(builder.IsSubsetOf(Enumerable.Range(1, 3)));
            Assert.True(builder.IsSubsetOf(Enumerable.Range(1, 5)));
        }

        [Fact]
        public void IsSupersetOf()
        {
            var builder = ImmutableSortedSet.CreateRange(Enumerable.Range(1, 3)).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.IsSupersetOf(null));
            Assert.False(builder.IsSupersetOf(Enumerable.Range(1, 4)));
            Assert.True(builder.IsSupersetOf(Enumerable.Range(1, 3)));
            Assert.True(builder.IsSupersetOf(Enumerable.Range(1, 2)));
        }

        [Fact]
        public void Overlaps()
        {
            var builder = ImmutableSortedSet.CreateRange(Enumerable.Range(1, 3)).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.Overlaps(null));
            Assert.True(builder.Overlaps(Enumerable.Range(3, 2)));
            Assert.False(builder.Overlaps(Enumerable.Range(4, 3)));
        }

        [Fact]
        public void Remove()
        {
            var builder = ImmutableSortedSet.Create("a").ToBuilder();
            Assert.False(builder.Remove("b"));
            Assert.True(builder.Remove("a"));
        }

        [Fact]
        public void Reverse()
        {
            var builder = ImmutableSortedSet.Create("a", "b").ToBuilder();
            Assert.Equal(new[] { "b", "a" }, builder.Reverse());
        }

        [Fact]
        public void SetEquals()
        {
            var builder = ImmutableSortedSet.Create("a").ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.SetEquals(null));
            Assert.False(builder.SetEquals(new[] { "b" }));
            Assert.True(builder.SetEquals(new[] { "a" }));
            Assert.True(builder.SetEquals(builder));
        }

        [Fact]
        public void ICollectionOfTMethods()
        {
            ICollection<string> builder = ImmutableSortedSet.Create("a").ToBuilder();
            builder.Add("b");
            Assert.True(builder.Contains("b"));

            var array = new string[3];
            builder.CopyTo(array, 1);
            Assert.Equal(new[] { null, "a", "b" }, array);

            Assert.False(builder.IsReadOnly);

            Assert.Equal(new[] { "a", "b" }, builder.ToArray()); // tests enumerator
        }

        [Fact]
        public void ICollectionMethods()
        {
            ICollection builder = ImmutableSortedSet.Create("a").ToBuilder();

            var array = new string[builder.Count + 1];
            builder.CopyTo(array, 1);
            Assert.Equal(new[] { null, "a" }, array);

            Assert.False(builder.IsSynchronized);
            Assert.NotNull(builder.SyncRoot);
            Assert.Same(builder.SyncRoot, builder.SyncRoot);
        }

        [Fact]
        public void Indexer()
        {
            var builder = ImmutableSortedSet.Create(1, 3, 2).ToBuilder();
            Assert.Equal(1, builder[0]);
            Assert.Equal(2, builder[1]);
            Assert.Equal(3, builder[2]);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder[-1]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder[3]);
        }

        [Fact]
        public void NullHandling()
        {
            var builder = ImmutableSortedSet<string>.Empty.ToBuilder();
            Assert.True(builder.Add(null));
            Assert.False(builder.Add(null));
            Assert.True(builder.Contains(null));
            Assert.True(builder.Remove(null));

            builder.UnionWith(new[] { null, "a" });
            Assert.True(builder.IsSupersetOf(new[] { null, "a" }));
            Assert.True(builder.IsSubsetOf(new[] { null, "a" }));
            Assert.True(builder.IsProperSupersetOf(new[] { default(string) }));
            Assert.True(builder.IsProperSubsetOf(new[] { null, "a", "b" }));

            builder.IntersectWith(new[] { default(string) });
            Assert.Equal(1, builder.Count);

            builder.ExceptWith(new[] { default(string) });
            Assert.False(builder.Remove(null));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Cannot do DebuggerAttribute testing on UapAot: requires internal Reflection on framework types.")]
        public void DebuggerAttributesValid()
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(ImmutableSortedSet.CreateBuilder<string>());
            DebuggerAttributes.ValidateDebuggerTypeProxyProperties(ImmutableSortedSet.CreateBuilder<int>());
            ImmutableSortedSet<int>.Builder builder = ImmutableSortedSet.CreateBuilder<int>();
            builder.Add(1);
            builder.Add(2);
            builder.Add(3);
            DebuggerAttributeInfo info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(builder);
            PropertyInfo itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>().State == DebuggerBrowsableState.RootHidden);
            int[] items = itemProperty.GetValue(info.Instance) as int[];
            Assert.Equal(builder, items);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Cannot do DebuggerAttribute testing on UapAot: requires internal Reflection on framework types.")]
        public static void TestDebuggerAttributes_Null()
        {
            Type proxyType = DebuggerAttributes.GetProxyType(ImmutableSortedSet.CreateBuilder<int>());
            TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(proxyType, (object)null));
            Assert.IsType<ArgumentNullException>(tie.InnerException);
        }

        [Fact]
        public void ToImmutableSortedSet()
        {
            ImmutableSortedSet<int>.Builder builder = ImmutableSortedSet.CreateBuilder<int>();
            builder.Add(1);
            builder.Add(5);
            builder.Add(10);

            var set = builder.ToImmutableSortedSet();
            Assert.Equal(1, builder[0]);
            Assert.Equal(5, builder[1]);
            Assert.Equal(10, builder[2]);

            builder.Remove(10);
            Assert.False(builder.Contains(10));
            Assert.True(set.Contains(10));

            builder.Clear();
            Assert.True(builder.ToImmutableSortedSet().IsEmpty);
            Assert.False(set.IsEmpty);

            ImmutableSortedSet<int>.Builder nullBuilder = null;
            AssertExtensions.Throws<ArgumentNullException>("builder", () => nullBuilder.ToImmutableSortedSet());
        }
    }
}
