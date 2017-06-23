// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class ReadOnlyCollectionBuilderTests
    {
        [Fact]
        public void ReadOnlyCollectionBuilder_Ctor_Default()
        {
            var rocb = new ReadOnlyCollectionBuilder<int>();

            Assert.Equal(0, rocb.Capacity);

            AssertEmpty(rocb);
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(-1)]
        public void ReadOnlyCollectionBuilder_Ctor_Capacity_ArgumentChecking(int capacity)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new ReadOnlyCollectionBuilder<int>(capacity));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ReadOnlyCollectionBuilder_Ctor_Capacity(int capacity)
        {
            var rocb = new ReadOnlyCollectionBuilder<int>(capacity);

            Assert.Equal(capacity, rocb.Capacity);

            AssertEmpty(rocb);
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_Ctor_Collection_ArgumentChecking()
        {
            AssertExtensions.Throws<ArgumentNullException>("collection", () => new ReadOnlyCollectionBuilder<int>(null));
        }

        [Theory]
        [MemberData(nameof(InitialCollections))]
        public void ReadOnlyCollectionBuilder_Ctor_Collection(IEnumerable<int> collection)
        {
            var rocb = new ReadOnlyCollectionBuilder<int>(collection);

            Assert.Equal(collection.Count(), rocb.Count);
            Assert.True(collection.SequenceEqual(rocb));

            int[] array = rocb.ToArray();

            Assert.Equal(collection.Count(), array.Length);
            Assert.True(collection.SequenceEqual(array));

            ReadOnlyCollection<int> roc = rocb.ToReadOnlyCollection();

            Assert.Equal(collection.Count(), roc.Count);
            Assert.True(collection.SequenceEqual(roc));

            AssertEmpty(rocb); // ToReadOnlyCollection behavior is to empty the builder
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_Capacity1()
        {
            var rocb = new ReadOnlyCollectionBuilder<int>();

            Assert.Equal(0, rocb.Capacity);
            Assert.Equal(0, rocb.Count);

            Assert.Throws<ArgumentOutOfRangeException>(() => rocb.Capacity = -1);

            rocb.Capacity = 0;

            Assert.Equal(0, rocb.Capacity);
            Assert.Equal(0, rocb.Count);

            rocb.Capacity = 1;

            Assert.Equal(1, rocb.Capacity);
            Assert.Equal(0, rocb.Count);

            rocb.Capacity = 2;

            Assert.Equal(2, rocb.Capacity);
            Assert.Equal(0, rocb.Count);
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_Capacity2()
        {
            var rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1 });

            Assert.Equal(1, rocb.Capacity);
            Assert.Equal(1, rocb.Count);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => { rocb.Capacity = 0; });
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => { rocb.Capacity = -1; });

            rocb.Capacity = 1;

            Assert.Equal(1, rocb.Capacity);
            Assert.Equal(1, rocb.Count);

            rocb.Capacity = 2;

            Assert.Equal(2, rocb.Capacity);
            Assert.Equal(1, rocb.Count);
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_Capacity3()
        {
            var rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1 });

            Assert.Equal(1, rocb.Capacity);
            Assert.Equal(1, rocb.Count);

            rocb.RemoveAt(0);

            Assert.Equal(1, rocb.Capacity);
            Assert.Equal(0, rocb.Count);

            rocb.Capacity = 0;

            Assert.Equal(0, rocb.Capacity);
            Assert.Equal(0, rocb.Count);
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_IList_IsReadOnly()
        {
            IList rocb = new ReadOnlyCollectionBuilder<int>();

            Assert.False(rocb.IsReadOnly);
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_ICollectionOfT_IsReadOnly()
        {
            ICollection<int> rocb = new ReadOnlyCollectionBuilder<int>();

            Assert.False(rocb.IsReadOnly);
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_IList_IsFixedSize()
        {
            IList rocb = new ReadOnlyCollectionBuilder<int>();

            Assert.False(rocb.IsFixedSize);
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_IList_IsSynchronized()
        {
            ICollection rocb = new ReadOnlyCollectionBuilder<int>();

            Assert.False(rocb.IsSynchronized);
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_ICollection_SyncRoot()
        {
            ICollection rocb = new ReadOnlyCollectionBuilder<int>();

            object root1 = rocb.SyncRoot;
            Assert.NotNull(root1);

            object root2 = rocb.SyncRoot;
            Assert.Same(root1, root2);
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_IndexOf()
        {
            var rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 2, 3 });

            Assert.Equal(4, rocb.Count);

            Assert.Equal(0, rocb.IndexOf(1));
            Assert.Equal(1, rocb.IndexOf(2));
            Assert.Equal(3, rocb.IndexOf(3));

            Assert.InRange(rocb.IndexOf(0), int.MinValue, -1);
            Assert.InRange(rocb.IndexOf(4), int.MinValue, -1);

            rocb.Capacity = 5;

            Assert.Equal(4, rocb.Count);
            Assert.InRange(rocb.IndexOf(0), int.MinValue, -1); // No default values leak in through underlying array

            Assert.True(rocb.Remove(3));

            Assert.Equal(3, rocb.Count);
            Assert.InRange(rocb.IndexOf(0), int.MinValue, -1); // No default values leak in through underlying array
            Assert.InRange(rocb.IndexOf(3), int.MinValue, -1);
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_IList_IndexOf()
        {
            var rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 2, 3 });
            IList list = rocb;

            Assert.Equal(4, list.Count);

            Assert.Equal(0, list.IndexOf(1));
            Assert.Equal(1, list.IndexOf(2));
            Assert.Equal(3, list.IndexOf(3));

            Assert.InRange(list.IndexOf(0), int.MinValue, -1);
            Assert.InRange(list.IndexOf(4), int.MinValue, -1);

            rocb.Capacity = 5;

            Assert.Equal(4, list.Count);
            Assert.InRange(list.IndexOf(0), int.MinValue, -1); // No default values leak in through underlying array

            list.Remove(3);

            Assert.Equal(3, list.Count);
            Assert.InRange(list.IndexOf(0), int.MinValue, -1); // No default values leak in through underlying array
            Assert.InRange(list.IndexOf(3), int.MinValue, -1);

            Assert.InRange(list.IndexOf("bar"), int.MinValue, -1);
            Assert.InRange(list.IndexOf(null), int.MinValue, -1);
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_Insert()
        {
            var rocb = new ReadOnlyCollectionBuilder<int>();

            Assert.Equal(0, rocb.Count);

            Assert.Throws<ArgumentOutOfRangeException>(() => rocb.Insert(-1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => rocb.Insert(1, 1));

            rocb.Insert(0, 1);

            Assert.True(new[] { 1 }.SequenceEqual(rocb));

            rocb.Insert(0, 2);

            Assert.True(new[] { 2, 1 }.SequenceEqual(rocb));

            rocb.Insert(0, 3);

            Assert.True(new[] { 3, 2, 1 }.SequenceEqual(rocb));

            rocb.Insert(1, 4);

            Assert.True(new[] { 3, 4, 2, 1 }.SequenceEqual(rocb));

            rocb.Insert(4, 5);

            Assert.True(new[] { 3, 4, 2, 1, 5 }.SequenceEqual(rocb));
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_IList_Insert()
        {
            IList rocb = new ReadOnlyCollectionBuilder<int>();

            Assert.Equal(0, rocb.Count);

            Assert.Throws<ArgumentOutOfRangeException>(() => rocb.Insert(-1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => rocb.Insert(1, 1));

            AssertExtensions.Throws<ArgumentException>("value", () => rocb.Insert(1, "bar"));
            AssertExtensions.Throws<ArgumentException>("value", () => rocb.Insert(1, null));

            rocb.Insert(0, 1);

            Assert.True(new[] { 1 }.SequenceEqual(rocb.Cast<int>()));

            rocb.Insert(0, 2);

            Assert.True(new[] { 2, 1 }.SequenceEqual(rocb.Cast<int>()));

            rocb.Insert(0, 3);

            Assert.True(new[] { 3, 2, 1 }.SequenceEqual(rocb.Cast<int>()));

            rocb.Insert(1, 4);

            Assert.True(new[] { 3, 4, 2, 1 }.SequenceEqual(rocb.Cast<int>()));

            rocb.Insert(4, 5);

            Assert.True(new[] { 3, 4, 2, 1, 5 }.SequenceEqual(rocb.Cast<int>()));
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_RemoveAt()
        {
            var rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 3, 4 });

            Assert.True(new[] { 1, 2, 3, 4 }.SequenceEqual(rocb));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => rocb.RemoveAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => rocb.RemoveAt(4));

            rocb.RemoveAt(0);

            Assert.True(new[] { 2, 3, 4 }.SequenceEqual(rocb));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => rocb.RemoveAt(3));

            rocb.RemoveAt(1);

            Assert.True(new[] { 2, 4 }.SequenceEqual(rocb));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => rocb.RemoveAt(2));

            rocb.RemoveAt(1);

            Assert.True(new[] { 2 }.SequenceEqual(rocb));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => rocb.RemoveAt(1));

            rocb.RemoveAt(0);

            Assert.Equal(0, rocb.Count);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => rocb.RemoveAt(0));
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_Remove()
        {
            var rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 2, 4 });

            Assert.True(new[] { 1, 2, 2, 4 }.SequenceEqual(rocb));

            Assert.False(rocb.Remove(0));

            Assert.True(new[] { 1, 2, 2, 4 }.SequenceEqual(rocb));

            Assert.True(rocb.Remove(2));

            Assert.True(new[] { 1, 2, 4 }.SequenceEqual(rocb));

            Assert.True(rocb.Remove(1));

            Assert.True(new[] { 2, 4 }.SequenceEqual(rocb));

            Assert.True(rocb.Remove(4));

            Assert.True(new[] { 2 }.SequenceEqual(rocb));

            Assert.True(rocb.Remove(2));

            Assert.Equal(0, rocb.Count);
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_IList_Remove()
        {
            IList rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 2, 4 });

            Assert.True(new[] { 1, 2, 2, 4 }.SequenceEqual(rocb.Cast<int>()));

            rocb.Remove(0);
            rocb.Remove("bar");
            rocb.Remove(null);

            Assert.True(new[] { 1, 2, 2, 4 }.SequenceEqual(rocb.Cast<int>()));

            rocb.Remove(2);

            Assert.True(new[] { 1, 2, 4 }.SequenceEqual(rocb.Cast<int>()));

            rocb.Remove(1);

            Assert.True(new[] { 2, 4 }.SequenceEqual(rocb.Cast<int>()));

            rocb.Remove(4);

            Assert.True(new[] { 2 }.SequenceEqual(rocb.Cast<int>()));

            rocb.Remove(2);

            Assert.Equal(0, rocb.Count);
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_Indexer_Get()
        {
            var rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 3, 4 });

            // CONSIDER: Throw ArgumentOutOfRangeException instead, see https://github.com/dotnet/corefx/issues/14059
            Assert.Throws<IndexOutOfRangeException>(() => rocb[-1]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => rocb[4]);

            Assert.Equal(1, rocb[0]);
            Assert.Equal(2, rocb[1]);
            Assert.Equal(3, rocb[2]);
            Assert.Equal(4, rocb[3]);
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_Indexer_IList_Get()
        {
            IList rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 3, 4 });

            // CONSIDER: Throw ArgumentOutOfRangeException instead, see https://github.com/dotnet/corefx/issues/14059
            Assert.Throws<IndexOutOfRangeException>(() => rocb[-1]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => rocb[4]);

            Assert.Equal(1, rocb[0]);
            Assert.Equal(2, rocb[1]);
            Assert.Equal(3, rocb[2]);
            Assert.Equal(4, rocb[3]);
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_Indexer_Set()
        {
            var rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 3, 4 });

            // CONSIDER: Throw ArgumentOutOfRangeException instead, see https://github.com/dotnet/corefx/issues/14059
            Assert.Throws<IndexOutOfRangeException>(() => rocb[-1] = -1);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => rocb[4] = -1);

            rocb[0] = -1;
            Assert.Equal(-1, rocb[0]);

            rocb[1] = -2;
            Assert.Equal(-2, rocb[1]);

            rocb[2] = -3;
            Assert.Equal(-3, rocb[2]);

            rocb[3] = -4;
            Assert.Equal(-4, rocb[3]);
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_Indexer_IList_Set()
        {
            IList rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 3, 4 });

            // CONSIDER: Throw ArgumentOutOfRangeException instead, see https://github.com/dotnet/corefx/issues/14059
            Assert.Throws<IndexOutOfRangeException>(() => rocb[-1] = -1);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => rocb[4] = -1);

            AssertExtensions.Throws<ArgumentException>("value", () => rocb[0] = "bar");
            AssertExtensions.Throws<ArgumentException>("value", () => rocb[0] = null);

            rocb[0] = -1;
            Assert.Equal(-1, rocb[0]);

            rocb[1] = -2;
            Assert.Equal(-2, rocb[1]);

            rocb[2] = -3;
            Assert.Equal(-3, rocb[2]);

            rocb[3] = -4;
            Assert.Equal(-4, rocb[3]);
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_Add()
        {
            var rocb = new ReadOnlyCollectionBuilder<int>();

            for (int i = 1; i <= 10; i++)
            {
                rocb.Add(i);

                Assert.True(Enumerable.Range(1, i).SequenceEqual(rocb));
            }
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_IList_Add()
        {
            IList rocb = new ReadOnlyCollectionBuilder<int>();

            for (int i = 1; i <= 10; i++)
            {
                rocb.Add(i);

                Assert.True(Enumerable.Range(1, i).SequenceEqual(rocb.Cast<int>()));
            }

            AssertExtensions.Throws<ArgumentException>("value", () => rocb.Add(null));
            AssertExtensions.Throws<ArgumentException>("value", () => rocb.Add("foo"));
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_Clear1()
        {
            var rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 3 });

            rocb.Clear();

            Assert.Equal(0, rocb.Count);
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_Clear2()
        {
            var rocb = new ReadOnlyCollectionBuilder<int>();

            rocb.Clear();

            Assert.Equal(0, rocb.Count);
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_Contains1()
        {
            var rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 2, 3 });

            Assert.True(rocb.Contains(1));
            Assert.True(rocb.Contains(2));
            Assert.True(rocb.Contains(3));

            Assert.False(rocb.Contains(-1));
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_Contains2()
        {
            var rocb = new ReadOnlyCollectionBuilder<string>(new[] { "bar", "foo", "qux" });

            Assert.True(rocb.Contains("bar"));
            Assert.True(rocb.Contains("foo"));
            Assert.True(rocb.Contains("qux"));

            Assert.False(rocb.Contains(null));
            Assert.False(rocb.Contains("baz"));
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_Contains3()
        {
            var rocb = new ReadOnlyCollectionBuilder<string>(new[] { "bar", "foo", "qux", null });

            Assert.True(rocb.Contains("bar"));
            Assert.True(rocb.Contains("foo"));
            Assert.True(rocb.Contains("qux"));
            Assert.True(rocb.Contains(null));

            Assert.False(rocb.Contains("baz"));
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_IList_Contains1()
        {
            IList rocb = new ReadOnlyCollectionBuilder<string>(new[] { "bar", "foo", "qux", null });

            Assert.True(rocb.Contains("bar"));
            Assert.True(rocb.Contains("foo"));
            Assert.True(rocb.Contains("qux"));
            Assert.True(rocb.Contains(null));

            Assert.False(rocb.Contains("baz"));
            Assert.False(rocb.Contains(42));
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_IList_Contains2()
        {
            IList rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 3 });

            Assert.True(rocb.Contains(1));
            Assert.True(rocb.Contains(2));
            Assert.True(rocb.Contains(3));

            Assert.False(rocb.Contains("baz"));
            Assert.False(rocb.Contains(0));
            Assert.False(rocb.Contains(null));
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_Reverse()
        {
            var rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 3 });

            rocb.Reverse();

            Assert.True(new[] { 3, 2, 1 }.SequenceEqual(rocb));
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_Reverse_Range_ArgumentChecking()
        {
            var rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 3 });

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => rocb.Reverse(-1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => rocb.Reverse(1, -1));

            // CONSIDER: Throw ArgumentException just like List<T> does, see https://github.com/dotnet/corefx/issues/14059
            // AssertExtensions.Throws<ArgumentException>(null, () => rocb.Reverse(3, 1));
            // AssertExtensions.Throws<ArgumentException>(null, () => rocb.Reverse(1, 3));
            // AssertExtensions.Throws<ArgumentException>(null, () => rocb.Reverse(2, 2));
            // AssertExtensions.Throws<ArgumentException>(null, () => rocb.Reverse(3, 1));
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_Reverse_Range()
        {
            var rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 3, 4, 5 });

            rocb.Reverse(1, 3);

            Assert.True(new[] { 1, 4, 3, 2, 5 }.SequenceEqual(rocb));
        }

        [Theory]
        [MemberData(nameof(Lengths))]
        public void ReadOnlyCollectionBuilder_ToArray(int length)
        {
            var rocb = new ReadOnlyCollectionBuilder<int>();

            for (int i = 0; i < length; i++)
            {
                rocb.Add(i);
            }

            int[] array = rocb.ToArray();

            Assert.True(Enumerable.Range(0, length).SequenceEqual(array));
        }

        [Theory]
        [MemberData(nameof(Lengths))]
        public void ReadOnlyCollectionBuilder_ToReadOnlyCollection(int length)
        {
            var rocb = new ReadOnlyCollectionBuilder<int>();

            for (int i = 0; i < length; i++)
            {
                rocb.Add(i);
            }

            ReadOnlyCollection<int> collection = rocb.ToReadOnlyCollection();

            Assert.Equal(length, collection.Count);

            Assert.True(Enumerable.Range(0, length).SequenceEqual(collection));

            AssertEmpty(rocb);
        }

        [Theory]
        [MemberData(nameof(Lengths))]
        public void ReadOnlyCollectionBuilder_GetEnumerator(int length)
        {
            var rocb = new ReadOnlyCollectionBuilder<int>();

            for (int i = 0; i < length; i++)
            {
                rocb.Add(i);
            }

            for (int j = 0; j < 2; j++)
            {
                IEnumerator<int> enumerator = rocb.GetEnumerator();

                // NB: Current property on generic enumerator doesn't throw; this is consistent with List<T>.

                for (int i = 0; i < length; i++)
                {
                    Assert.True(enumerator.MoveNext());
                    Assert.Equal(i, enumerator.Current);
                    Assert.Equal(i, ((IEnumerator)enumerator).Current);

                    enumerator.Dispose(); // NB: Similar to List<T>, calling Dispose does not have an effect here
                }

                Assert.False(enumerator.MoveNext());
                Assert.False(enumerator.MoveNext());

                // NB: Current property on generic enumerator doesn't throw; this is consistent with List<T>.

                enumerator.Reset();
            }
        }

        [Theory]
        [MemberData(nameof(Lengths))]
        public void ReadOnlyCollectionBuilder_IEnumerable_GetEnumerator(int length)
        {
            var rocb = new ReadOnlyCollectionBuilder<int>();

            for (int i = 0; i < length; i++)
            {
                rocb.Add(i);
            }

            for (int j = 0; j < 2; j++)
            {
                IEnumerator enumerator = ((IEnumerable)rocb).GetEnumerator();

                Assert.Throws<InvalidOperationException>(() => enumerator.Current);

                for (int i = 0; i < length; i++)
                {
                    Assert.True(enumerator.MoveNext());
                    Assert.Equal(i, enumerator.Current);
                    Assert.Equal(i, ((IEnumerator)enumerator).Current);

                    ((IDisposable)enumerator).Dispose(); // NB: Similar to List<T>, calling Dispose does not have an effect here
                }

                Assert.False(enumerator.MoveNext());
                Assert.False(enumerator.MoveNext());

                Assert.Throws<InvalidOperationException>(() => enumerator.Current);

                enumerator.Reset();
            }
        }

        [Theory]
        [MemberData(nameof(Versioning))]
        public void ReadOnlyCollectionBuilder_IEnumeratorOfT_Versioning_MoveNext(int index, Action<ReadOnlyCollectionBuilder<int>> edit)
        {
            var rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 3 });

            IEnumerator<int> enumerator = rocb.GetEnumerator();

            Assert.True(enumerator.MoveNext());

            edit(rocb);

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Theory]
        [MemberData(nameof(Versioning))]
        public void ReadOnlyCollectionBuilder_IEnumeratorOfT_Versioning_Reset(int index, Action<ReadOnlyCollectionBuilder<int>> edit)
        {
            var rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 3 });

            IEnumerator<int> enumerator = rocb.GetEnumerator();

            Assert.True(enumerator.MoveNext());

            edit(rocb);

            Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
        }

        [Theory]
        [MemberData(nameof(Versioning))]
        public void ReadOnlyCollectionBuilder_IEnumerator_Versioning_MoveNext(int index, Action<ReadOnlyCollectionBuilder<int>> edit)
        {
            var rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 3 });

            IEnumerator enumerator = ((IEnumerable)rocb).GetEnumerator();

            Assert.True(enumerator.MoveNext());

            edit(rocb);

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Theory]
        [MemberData(nameof(Versioning))]
        public void ReadOnlyCollectionBuilder_IEnumerator_Versioning_Reset(int index, Action<ReadOnlyCollectionBuilder<int>> edit)
        {
            var rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 3 });

            IEnumerator enumerator = ((IEnumerable)rocb).GetEnumerator();

            Assert.True(enumerator.MoveNext());

            edit(rocb);

            Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_CopyTo_ArgumentChecking()
        {
            var rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 3 });

            Assert.Throws<ArgumentNullException>(() => rocb.CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => rocb.CopyTo(new int[3], -1));
            AssertExtensions.Throws<ArgumentException>("destinationArray", () => rocb.CopyTo(new int[3], 3)); // NB: Consistent with List<T> behavior
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_CopyTo1()
        {
            var rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 3 });

            var array = new int[3];

            rocb.CopyTo(array, 0);

            Assert.True(new[] { 1, 2, 3 }.SequenceEqual(array));
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_CopyTo2()
        {
            var rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 3 });

            var array = new int[5] { 1, 2, 3, 4, 5 };

            rocb.CopyTo(array, 1);

            Assert.True(new[] { 1, 1, 2, 3, 5 }.SequenceEqual(array));
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_ICollection_CopyTo_ArgumentChecking()
        {
            ICollection rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 3 });

            Assert.Throws<ArgumentNullException>(() => rocb.CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => rocb.CopyTo(new int[3], -1));
            AssertExtensions.Throws<ArgumentException>("destinationArray", () => rocb.CopyTo(new int[3], 3)); // NB: Consistent with List<T> behavior
            AssertExtensions.Throws<ArgumentException>(null, () => rocb.CopyTo(new int[3, 3], 0));

            // CONSIDER: Throw ArgumentException instead to be consistent with List<T>, see https://github.com/dotnet/corefx/issues/14059
            Assert.Throws<ArrayTypeMismatchException>(() => rocb.CopyTo(new string[3], 0));
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_ICollection_CopyTo1()
        {
            ICollection rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 3 });

            var array = new int[3];

            rocb.CopyTo(array, 0);

            Assert.True(new[] { 1, 2, 3 }.SequenceEqual(array));
        }

        [Fact]
        public void ReadOnlyCollectionBuilder_ICollection_CopyTo2()
        {
            ICollection rocb = new ReadOnlyCollectionBuilder<int>(new[] { 1, 2, 3 });

            var array = new int[5] { 1, 2, 3, 4, 5 };

            rocb.CopyTo(array, 1);

            Assert.True(new[] { 1, 1, 2, 3, 5 }.SequenceEqual(array));
        }

        private static void AssertEmpty<T>(ReadOnlyCollectionBuilder<T> rocb)
        {
            Assert.Equal(0, rocb.Count);

            Assert.False(rocb.Contains(default(T)));
            Assert.False(rocb.Remove(default(T)));
            Assert.InRange(rocb.IndexOf(default(T)), int.MinValue, -1);

            IEnumerator<T> e = rocb.GetEnumerator();
            Assert.False(e.MoveNext());
        }

        private static IEnumerable<object[]> InitialCollections() =>
            new IEnumerable<int>[]
            {
                new int[0],
                new int[] { 1 },
                new int[] { 1, 2 },
                new int[] { 1, 2, 3 },
                new int[] { 1, 2, 3, 4 },
                new int[] { 1, 2, 3, 4, 5 },

                new List<int>(),
                new List<int>() { 1 },
                new List<int>() { 1, 2 },
                new List<int>() { 1, 2, 3 },
                new List<int>() { 1, 2, 3, 4 },
                new List<int>() { 1, 2, 3, 4, 5 },

                Enumerable.Empty<int>(),
                Enumerable.Range(1, 1),
                Enumerable.Range(1, 2),
                Enumerable.Range(1, 3),
                Enumerable.Range(1, 4),
                Enumerable.Range(1, 5),
            }.Select(x => new object[] { x });

        private static IEnumerable<object[]> Lengths() => Enumerable.Range(0, 10).Select(i => new object[] { i });

        private static IEnumerable<object[]> Versioning() =>
            new Action<ReadOnlyCollectionBuilder<int>>[]
            {
                e => e.Add(1),
                e => ((IList)e).Add(1),
                e => e[0] = 1,
                e => ((IList)e)[0] = 1,
                e => e.Insert(0, 1),
                e => ((IList)e).Insert(0, 1),
                e => e.Remove(1),
                e => ((IList)e).Remove(1),
                e => e.RemoveAt(0),
                e => e.Reverse(),
            }.Select((x, i) => new object[] { i, x });
    }
}
