// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Collections.ObjectModel.Tests
{
    public class ReadOnlySetTests
    {
        [Fact]
        public void Ctor_NullSet_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("set", () => new ReadOnlySet<int>(null));
        }

        [Fact]
        public void Ctor_SetProperty_Roundtrips()
        {
            var set = new HashSet<int>();
            Assert.Same(set, new DerivedReadOnlySet<int>(set).Set);
        }

        [Fact]
        public void Empty_EmptyAndIdempotent()
        {
            Assert.Same(ReadOnlySet<int>.Empty, ReadOnlySet<int>.Empty);
            Assert.Empty(ReadOnlySet<int>.Empty);
            Assert.Same(ReadOnlySet<int>.Empty.GetEnumerator(), ReadOnlySet<int>.Empty.GetEnumerator());
        }

        [Fact]
        public void MembersDelegateToWrappedSet()
        {
            var set = new ReadOnlySet<int>(new HashSet<int>() { 1, 2, 3 });

            Assert.True(set.Contains(2));
            Assert.False(set.Contains(4));

            Assert.Equal(3, set.Count);

            Assert.True(set.IsProperSubsetOf([1, 2, 3, 4]));
            Assert.False(set.IsProperSubsetOf([1, 2, 5]));

            Assert.True(set.IsProperSupersetOf([1, 2]));
            Assert.False(set.IsProperSupersetOf([1, 4]));

            Assert.True(set.IsSubsetOf([1, 2, 3, 4]));
            Assert.False(set.IsSubsetOf([1, 2, 5]));

            Assert.True(set.IsSupersetOf([1, 2]));
            Assert.False(set.IsSupersetOf([1, 4]));

            Assert.True(set.Overlaps([-1, 0, 1]));
            Assert.False(set.Overlaps([-1, 0]));

            Assert.True(set.SetEquals([1, 2, 3]));
            Assert.False(set.SetEquals([1, 2, 4]));

            int[] result = new int[3];
            ((ICollection<int>)set).CopyTo(result, 0);
            Assert.Equal(result, new int[] { 1, 2, 3 });

            Array.Clear(result);
            ((ICollection)set).CopyTo(result, 0);
            Assert.Equal(result, new int[] { 1, 2, 3 });

            Assert.NotNull(set.GetEnumerator());
        }

        [Fact]
        public void ChangesToUnderlyingSetReflected()
        {
            var set = new HashSet<int> { 1, 2, 3 };
            var readOnlySet = new ReadOnlySet<int>(set);

            set.Add(4);
            Assert.Equal(4, readOnlySet.Count);
            Assert.True(readOnlySet.Contains(4));

            set.Remove(2);
            Assert.Equal(3, readOnlySet.Count);
            Assert.False(readOnlySet.Contains(2));
        }

        [Fact]
        public void IsReadOnly_True()
        {
            var set = new ReadOnlySet<int>(new HashSet<int> { 1, 2, 3 });
            Assert.True(((ICollection<int>)set).IsReadOnly);
        }

        [Fact]
        public void MutationThrows_CollectionUnmodified()
        {
            var set = new HashSet<int> { 1, 2, 3 };
            var readOnlySet = new ReadOnlySet<int>(set);

            Assert.Throws<NotSupportedException>(() => ((ICollection<int>)readOnlySet).Add(4));
            Assert.Throws<NotSupportedException>(() => ((ICollection<int>)readOnlySet).Remove(1));
            Assert.Throws<NotSupportedException>(() => ((ICollection<int>)readOnlySet).Clear());

            Assert.Throws<NotSupportedException>(() => ((ISet<int>)readOnlySet).Add(4));
            Assert.Throws<NotSupportedException>(() => ((ISet<int>)readOnlySet).ExceptWith([1, 2, 3]));
            Assert.Throws<NotSupportedException>(() => ((ISet<int>)readOnlySet).IntersectWith([1, 2, 3]));
            Assert.Throws<NotSupportedException>(() => ((ISet<int>)readOnlySet).SymmetricExceptWith([1, 2, 3]));
            Assert.Throws<NotSupportedException>(() => ((ISet<int>)readOnlySet).UnionWith([1, 2, 3]));

            Assert.Equal(3, set.Count);
        }

        [Fact]
        public void ICollection_Synchronization()
        {
            var set = new ReadOnlySet<int>(new HashSet<int> { 1, 2, 3 });

            Assert.False(((ICollection)set).IsSynchronized);
            Assert.Same(set, ((ICollection)set).SyncRoot);
        }

        private class DerivedReadOnlySet<T> : ReadOnlySet<T>
        {
            public DerivedReadOnlySet(HashSet<T> set) : base(set) { }

            public new ISet<T> Set => base.Set;
        }
    }
}
