// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            ImmutableSortedSet<int>.Builder builder = ImmutableSortedSet<int>.Empty.ToBuilder();
            Assert.True(builder.Add(3));
            Assert.True(builder.Add(5));
            Assert.False(builder.Add(5));
            Assert.Equal(2, builder.Count);
            Assert.True(builder.Contains(3));
            Assert.True(builder.Contains(5));
            Assert.False(builder.Contains(7));

            ImmutableSortedSet<int> set = builder.ToImmutable();
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
            ImmutableSortedSet<int> set = ImmutableSortedSet<int>.Empty.Add(1);
            ImmutableSortedSet<int>.Builder builder = set.ToBuilder();
            Assert.True(builder.Contains(1));
            Assert.True(builder.Add(3));
            Assert.True(builder.Add(5));
            Assert.False(builder.Add(5));
            Assert.Equal(3, builder.Count);
            Assert.True(builder.Contains(3));
            Assert.True(builder.Contains(5));
            Assert.False(builder.Contains(7));

            ImmutableSortedSet<int> set2 = builder.ToImmutable();
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
        public void IndexOf()
        {
            ImmutableSortedSet<int>.Builder builder = ImmutableSortedSet<int>.Empty.ToBuilder();
            Assert.Equal(~0, builder.IndexOf(5));

            builder = ImmutableSortedSet<int>.Empty.Union(Enumerable.Range(1, 10).Select(n => n * 10)).ToBuilder();
            Assert.Equal(0, builder.IndexOf(10));
            Assert.Equal(1, builder.IndexOf(20));
            Assert.Equal(4, builder.IndexOf(50));
            Assert.Equal(8, builder.IndexOf(90));
            Assert.Equal(9, builder.IndexOf(100));

            Assert.Equal(~0, builder.IndexOf(5));
            Assert.Equal(~1, builder.IndexOf(15));
            Assert.Equal(~2, builder.IndexOf(25));
            Assert.Equal(~5, builder.IndexOf(55));
            Assert.Equal(~9, builder.IndexOf(95));
            Assert.Equal(~10, builder.IndexOf(105));

            ImmutableSortedSet<int?>.Builder nullableSet = ImmutableSortedSet<int?>.Empty.ToBuilder();
            Assert.Equal(~0, nullableSet.IndexOf(null));
            nullableSet.Add(null);
            nullableSet.Add(0);
            Assert.Equal(0, nullableSet.IndexOf(null));
        }

        [Fact]
        public void EnumerateBuilderWhileMutating()
        {
            ImmutableSortedSet<int>.Builder builder = ImmutableSortedSet<int>.Empty.Union(Enumerable.Range(1, 10)).ToBuilder();
            Assert.Equal(Enumerable.Range(1, 10), builder);

            ImmutableSortedSet<int>.Enumerator enumerator = builder.GetEnumerator();
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
            ImmutableSortedSet<int> collection = ImmutableSortedSet<int>.Empty.Add(1);
            ImmutableSortedSet<int>.Builder builder = collection.ToBuilder();
            Assert.Same(collection, builder.ToImmutable()); // no changes at all.
            builder.Add(2);

            ImmutableSortedSet<int> newImmutable = builder.ToImmutable();
            Assert.NotSame(collection, newImmutable); // first ToImmutable with changes should be a new instance.
            Assert.Same(newImmutable, builder.ToImmutable()); // second ToImmutable without changes should be the same instance.
        }

        [Fact]
        public void GetEnumeratorTest()
        {
            ImmutableSortedSet<string>.Builder builder = ImmutableSortedSet.Create("a", "B").WithComparer(StringComparer.Ordinal).ToBuilder();
            IEnumerable<string> enumerable = builder;
            using (IEnumerator<string> enumerator = enumerable.GetEnumerator())
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
            ImmutableSortedSet<int>.Builder builder = ImmutableSortedSet.Create(1, 2, 3).ToBuilder();
            Assert.Equal(1, builder.Min);
            Assert.Equal(3, builder.Max);
        }

        [Fact]
        public void Clear()
        {
            ImmutableSortedSet<int> set = ImmutableSortedSet<int>.Empty.Add(1);
            ImmutableSortedSet<int>.Builder builder = set.ToBuilder();
            builder.Clear();
            Assert.Equal(0, builder.Count);
        }

        [Fact]
        public void KeyComparer()
        {
            ImmutableSortedSet<string>.Builder builder = ImmutableSortedSet.Create("a", "B").ToBuilder();
            Assert.Same(Comparer<string>.Default, builder.KeyComparer);
            Assert.True(builder.Contains("a"));
            Assert.False(builder.Contains("A"));

            builder.KeyComparer = StringComparer.OrdinalIgnoreCase;
            Assert.Same(StringComparer.OrdinalIgnoreCase, builder.KeyComparer);
            Assert.Equal(2, builder.Count);
            Assert.True(builder.Contains("a"));
            Assert.True(builder.Contains("A"));

            ImmutableSortedSet<string> set = builder.ToImmutable();
            Assert.Same(StringComparer.OrdinalIgnoreCase, set.KeyComparer);
        }

        [Fact]
        public void KeyComparerCollisions()
        {
            ImmutableSortedSet<string>.Builder builder = ImmutableSortedSet.Create("a", "A").ToBuilder();
            builder.KeyComparer = StringComparer.OrdinalIgnoreCase;
            Assert.Equal(1, builder.Count);
            Assert.True(builder.Contains("a"));

            ImmutableSortedSet<string> set = builder.ToImmutable();
            Assert.Same(StringComparer.OrdinalIgnoreCase, set.KeyComparer);
            Assert.Equal(1, set.Count);
            Assert.True(set.Contains("a"));
        }

        [Fact]
        public void KeyComparerEmptyCollection()
        {
            ImmutableSortedSet<string>.Builder builder = ImmutableSortedSet.Create<string>().ToBuilder();
            Assert.Same(Comparer<string>.Default, builder.KeyComparer);
            builder.KeyComparer = StringComparer.OrdinalIgnoreCase;
            Assert.Same(StringComparer.OrdinalIgnoreCase, builder.KeyComparer);
            ImmutableSortedSet<string> set = builder.ToImmutable();
            Assert.Same(StringComparer.OrdinalIgnoreCase, set.KeyComparer);
        }

        [Fact]
        public void UnionWith()
        {
            ImmutableSortedSet<int>.Builder builder = ImmutableSortedSet.Create(1, 2, 3).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.UnionWith(null));
            builder.UnionWith(new[] { 2, 3, 4 });
            Assert.Equal(new[] { 1, 2, 3, 4 }, builder);
        }

        [Fact]
        public void ExceptWith()
        {
            ImmutableSortedSet<int>.Builder builder = ImmutableSortedSet.Create(1, 2, 3).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.ExceptWith(null));
            builder.ExceptWith(new[] { 2, 3, 4 });
            Assert.Equal(new[] { 1 }, builder);
        }

        [Fact]
        public void SymmetricExceptWith()
        {
            ImmutableSortedSet<int>.Builder builder = ImmutableSortedSet.Create(1, 2, 3).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.SymmetricExceptWith(null));
            builder.SymmetricExceptWith(new[] { 2, 3, 4 });
            Assert.Equal(new[] { 1, 4 }, builder);
        }

        [Fact]
        public void IntersectWith()
        {
            ImmutableSortedSet<int>.Builder builder = ImmutableSortedSet.Create(1, 2, 3).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.IntersectWith(null));
            builder.IntersectWith(new[] { 2, 3, 4 });
            Assert.Equal(new[] { 2, 3 }, builder);
        }

        [Fact]
        public void IsProperSubsetOf()
        {
            ImmutableSortedSet<int>.Builder builder = ImmutableSortedSet.CreateRange(Enumerable.Range(1, 3)).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.IsProperSubsetOf(null));
            Assert.False(builder.IsProperSubsetOf(Enumerable.Range(1, 3)));
            Assert.True(builder.IsProperSubsetOf(Enumerable.Range(1, 5)));
        }

        [Fact]
        public void IsProperSupersetOf()
        {
            ImmutableSortedSet<int>.Builder builder = ImmutableSortedSet.CreateRange(Enumerable.Range(1, 3)).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.IsProperSupersetOf(null));
            Assert.False(builder.IsProperSupersetOf(Enumerable.Range(1, 3)));
            Assert.True(builder.IsProperSupersetOf(Enumerable.Range(1, 2)));
        }

        [Fact]
        public void IsSubsetOf()
        {
            ImmutableSortedSet<int>.Builder builder = ImmutableSortedSet.CreateRange(Enumerable.Range(1, 3)).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.IsSubsetOf(null));
            Assert.False(builder.IsSubsetOf(Enumerable.Range(1, 2)));
            Assert.True(builder.IsSubsetOf(Enumerable.Range(1, 3)));
            Assert.True(builder.IsSubsetOf(Enumerable.Range(1, 5)));
        }

        [Fact]
        public void IsSupersetOf()
        {
            ImmutableSortedSet<int>.Builder builder = ImmutableSortedSet.CreateRange(Enumerable.Range(1, 3)).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.IsSupersetOf(null));
            Assert.False(builder.IsSupersetOf(Enumerable.Range(1, 4)));
            Assert.True(builder.IsSupersetOf(Enumerable.Range(1, 3)));
            Assert.True(builder.IsSupersetOf(Enumerable.Range(1, 2)));
        }

        [Fact]
        public void Overlaps()
        {
            ImmutableSortedSet<int>.Builder builder = ImmutableSortedSet.CreateRange(Enumerable.Range(1, 3)).ToBuilder();
            AssertExtensions.Throws<ArgumentNullException>("other", () => builder.Overlaps(null));
            Assert.True(builder.Overlaps(Enumerable.Range(3, 2)));
            Assert.False(builder.Overlaps(Enumerable.Range(4, 3)));
        }

        [Fact]
        public void Remove()
        {
            ImmutableSortedSet<string>.Builder builder = ImmutableSortedSet.Create("a").ToBuilder();
            Assert.False(builder.Remove("b"));
            Assert.True(builder.Remove("a"));
        }

        [Fact]
        public void Reverse()
        {
            ImmutableSortedSet<string>.Builder builder = ImmutableSortedSet.Create("a", "b").ToBuilder();
            Assert.Equal(new[] { "b", "a" }, builder.Reverse());
        }

        [Fact]
        public void SetEquals()
        {
            ImmutableSortedSet<string>.Builder builder = ImmutableSortedSet.Create("a").ToBuilder();
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

            string[] array = new string[3];
            builder.CopyTo(array, 1);
            Assert.Equal(new[] { null, "a", "b" }, array);

            Assert.False(builder.IsReadOnly);

            Assert.Equal(new[] { "a", "b" }, builder.ToArray()); // tests enumerator
        }

        [Fact]
        public void ICollectionMethods()
        {
            ICollection builder = ImmutableSortedSet.Create("a").ToBuilder();

            string[] array = new string[builder.Count + 1];
            builder.CopyTo(array, 1);
            Assert.Equal(new[] { null, "a" }, array);

            Assert.False(builder.IsSynchronized);
            Assert.NotNull(builder.SyncRoot);
            Assert.Same(builder.SyncRoot, builder.SyncRoot);
        }

        [Fact]
        public void Indexer()
        {
            ImmutableSortedSet<int>.Builder builder = ImmutableSortedSet.Create(1, 3, 2).ToBuilder();
            Assert.Equal(1, builder[0]);
            Assert.Equal(2, builder[1]);
            Assert.Equal(3, builder[2]);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder[-1]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder[3]);
        }

        [Fact]
        public void NullHandling()
        {
            ImmutableSortedSet<string>.Builder builder = ImmutableSortedSet<string>.Empty.ToBuilder();
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsDebuggerTypeProxyAttributeSupported))]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsDebuggerTypeProxyAttributeSupported))]
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

            ImmutableSortedSet<int> set = builder.ToImmutableSortedSet();
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

        [Fact]
        public void TryGetValue()
        {
            ImmutableSortedSet<int>.Builder builder = ImmutableSortedSet.Create(1, 2, 3).ToBuilder();
            Assert.True(builder.TryGetValue(2, out _));

            builder = ImmutableSortedSet.Create(CustomComparer.Instance, 1, 2, 3, 4).ToBuilder();
            int existing;
            Assert.True(builder.TryGetValue(5, out existing));
            Assert.Equal(4, existing);
        }

        private class CustomComparer : IComparer<int>
        {
            private CustomComparer()
            {
            }

            public static CustomComparer Instance { get; } = new CustomComparer();

            public int Compare(int x, int y) =>
                x >> 1 == y >> 1 ? 0 :
                x < y ? -1 :
                1;
        }
    }
}
