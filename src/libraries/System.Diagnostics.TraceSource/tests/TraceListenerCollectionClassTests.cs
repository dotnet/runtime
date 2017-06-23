// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using Xunit;
using System.Reflection;

namespace System.Diagnostics.TraceSourceTests
{
    public sealed class TraceListenerCollectionClassTests
        : ListBaseTests<TraceListenerCollection>
    {

        public override TraceListenerCollection Create(int count = 0)
        {
            // TraceListenerCollection has an internal constructor
            // so we use a TraceSource to create one for us.
            var list = new TraceSource("Test").Listeners;
            list.Clear();
            for (int i = 0; i < count; i++)
            {
                list.Add(CreateListener());
            }
            return list;
        }

        public TraceListener CreateListener()
        {
            return new TestTraceListener();
        }

        public override object CreateItem()
        {
            return CreateListener();
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override bool IsFixedSize
        {
            get { return false; }
        }

        public override bool IsSynchronized
        {
            get { return true; }
        }

        [Fact]
        public void TraceListenerIndexerTest()
        {
            var list = Create();
            var item = CreateListener();
            list.Add(item);
            Assert.Equal(item, list[0]);
            item = CreateListener();
            list[0] = item;
            Assert.Equal(item, list[0]);
        }

        [Fact]
        public void TraceListenerNameIndexerTest()
        {
            var list = Create();
            var item = CreateListener();
            item.Name = "TestListener";
            list.Add(item);
            Assert.Equal(item, list["TestListener"]);
            Assert.Equal(null, list["NO_EXIST"]);
        }

        [Fact]
        public void AddListenerTest()
        {
            var list = Create();
            var item = CreateListener();
            list.Add(item);
            Assert.Equal(item, list[0]);
            Assert.Throws<ArgumentNullException>(() => list.Add(null));
            Assert.Equal(1, list.Count);
        }

        [Fact]
        public void AddRangeArrayTest()
        {
            var list = Create();
            Assert.Throws<ArgumentNullException>(() => list.AddRange((TraceListener[])null));
            var items =
                new TraceListener[] {
                    CreateListener(),
                    CreateListener(),
                };
            list.AddRange(items);
            Assert.Equal(items[0], list[0]);
            Assert.Equal(items[1], list[1]);
        }

        [Fact]
        public void AddRangeCollectionTest()
        {
            var list = Create();
            Assert.Throws<ArgumentNullException>(() => list.AddRange((TraceListenerCollection)null));
            var items = Create();
            var item0 = CreateListener();
            var item1 = CreateListener();
            items.Add(item0);
            items.Add(item1);
            list.AddRange(items);
            Assert.Equal(item0, list[0]);
            Assert.Equal(item1, list[1]);
        }

        [Fact]
        public void ContainsTest()
        {
            var list = Create();
            var item = CreateListener();
            list.Add(item);
            Assert.True(list.Contains(item));
            item = CreateListener();
            Assert.False(list.Contains(item));
            Assert.False(list.Contains(null));
        }

        [Fact]
        public void CopyToListenerTest()
        {
            var list = Create(2);
            var arr = new TraceListener[4];
            list.CopyTo(arr, 1);
            Assert.Null(arr[0]);
            Assert.Equal(arr[1], list[0]);
            Assert.Equal(arr[2], list[1]);
            Assert.Null(arr[3]);
        }

        [Fact]
        public void IndexOfListenerTest()
        {
            var list = Create(2);
            var item = CreateListener();
            list.Insert(1, item);
            var idx = list.IndexOf(item);
            Assert.Equal(1, idx);

            idx = list.IndexOf(null);
            Assert.Equal(-1, idx);

            item = CreateListener();
            idx = list.IndexOf(item);
            Assert.Equal(-1, idx);
        }

        [Fact]
        public void InsertListenerTest()
        {
            var list = Create(2);
            var item = CreateListener();
            list.Insert(1, item);
            Assert.Equal(3, list.Count);
            Assert.Equal(item, list[1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(5, item));
        }

        [Fact]
        public void RemoveListenerTest()
        {
            var list = Create();
            var item = new TestTraceListener("Test1");
            list.Add(item);
            Assert.Equal(1, list.Count);
            list.Remove(item);
            Assert.Equal(0, list.Count);
        }

        [Fact]
        public void RemoveAtTest()
        {
            var list = Create();
            var item = new TestTraceListener("Test1");
            list.Add(item);
            list.RemoveAt(0);
            Assert.False(list.Contains(item));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
        }

        [Fact]
        public void RemoveByNameTest()
        {
            var list = Create();
            var item = new TestTraceListener("Test1");
            list.Add(item);
            Assert.Equal(1, list.Count);
            list.Remove("NO_EXIST");
            Assert.Equal(1, list.Count);
            list.Remove("Test1");
            Assert.Equal(0, list.Count);
        }
    }

    public abstract class ListBaseTests<T> : CollectionBaseTests<T>
        where T : IList
    {
        public abstract bool IsReadOnly { get; }

        public abstract bool IsFixedSize { get; }

        public virtual Object CreateNonItem()
        {
            return new Object();
        }

        [Fact]
        public virtual void AddTest()
        {
            var list = Create();
            var item = CreateItem();

            if (IsReadOnly || IsFixedSize)
            {
                Assert.Throws<NotSupportedException>(() => list.Add(item));
            }
            else
            {
                list.Add(item);
                Assert.Equal(item, list[0]);
            }
        }

        [Fact]
        public virtual void AddExceptionTest()
        {
            var list = Create();
            var item = CreateNonItem();

            if (IsReadOnly || IsFixedSize)
            {
                Assert.Throws<NotSupportedException>(() => list.Add(item));
            }
            else
            {
                AssertExtensions.Throws<ArgumentException>("value", () => list.Add(item));
            }
        }

        [Fact]
        public virtual void IndexerGetTest()
        {
            var list = Create();
            var item = CreateItem();
            list.Add(item);
            Assert.Equal(item, list[0]);

            var item2 = CreateItem();
            list[0] = item2;
            Assert.Equal(item2, list[0]);
        }

        [Fact]
        public virtual void IndexerSetTest()
        {
            var list = Create(3);
            var item = CreateItem();
            if (IsReadOnly)
            {
                Assert.Throws<NotSupportedException>(() => list[1] = item);
            }
            else
            {
                list[1] = item;
                Assert.Equal(item, list[1]);

                var nonItem = CreateNonItem();
                AssertExtensions.Throws<ArgumentException>("value", () => list[1] = nonItem);
            }
        }

        [Fact]
        public virtual void IndexOfTest()
        {
            var list = Create();
            var item0 = CreateItem();
            list.Add(item0);
            var item1 = CreateItem();
            list.Add(item1);
            Assert.Equal(0, list.IndexOf(item0));
            Assert.Equal(1, list.IndexOf(item1));
            var itemN = CreateItem();
            Assert.Equal(-1, list.IndexOf(itemN));
        }

        [Fact]
        public virtual void InsertTest()
        {
            var list = Create(2);
            var item = CreateItem();
            list.Insert(1, item);
            Assert.Equal(3, list.Count);
            Assert.Equal(item, list[1]);
        }

        [Fact]
        public virtual void InsertExceptionTest()
        {
            var list = Create(2);
            AssertExtensions.Throws<ArgumentException>("value", () => list.Insert(1, null));
        }

        [Fact]
        public virtual void InsertExceptionTest2()
        {
            var list = Create(2);
            var item = CreateItem();
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, item));
            Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(4, item));
        }

        [Fact]
        public virtual void RemoveTest()
        {
            var list = Create();
            var item = CreateItem();
            list.Add(item);
            Assert.True(list.Contains(item));
            list.Remove(item);
            Assert.False(list.Contains(item));
        }

        [Fact]
        public virtual void IsReadOnlyTest()
        {
            var list = Create();
            Assert.Equal(IsReadOnly, list.IsReadOnly);
        }

        [Fact]
        public virtual void IsFixedSizeTest()
        {
            var list = Create();
            Assert.Equal(IsFixedSize, list.IsFixedSize);
        }
    }

    public abstract class CollectionBaseTests<T>
        : EnumerableBaseTests<T>
        where T : ICollection
    {
        public abstract bool IsSynchronized { get; }

        [Fact]
        public virtual void CountTest()
        {
            var list = Create();
            Assert.Equal(0, list.Count);
        }

        [Fact]
        public virtual void SyncRootTest()
        {
            var list = Create();
            var sync1 = list.SyncRoot;
            Assert.NotNull(sync1);
            var sync2 = list.SyncRoot;
            Assert.NotNull(sync2);
            Assert.Equal(sync1, sync2);
        }

        [Fact]
        public virtual void IsSynchronizedTest()
        {
            var list = Create();
            var value = list.IsSynchronized;
            var expected = IsSynchronized;
            Assert.Equal(expected, value);
        }

        [Fact]
        public virtual void CopyToTest()
        {
            var list = Create(4);
            var arr = new Object[4];
            list.CopyTo(arr, 0);
        }

        [Fact]
        public virtual void CopyToTest2()
        {
            var list = Create(4);
            var arr = new Object[6];
            list.CopyTo(arr, 2);
            Assert.Null(arr[0]);
            Assert.Null(arr[1]);
        }

        [Fact]
        public virtual void CopyToExceptionTest()
        {
            var list = Create(4);
            var arr = new Object[2];
            AssertExtensions.Throws<ArgumentException>("destinationArray", "", () => list.CopyTo(arr, 0));
        }
    }


    public abstract class EnumerableBaseTests<T>
        where T : IEnumerable
    {
        public abstract T Create(int count = 0);

        public abstract Object CreateItem();

        [Fact]
        public virtual void GetEnumeratorEmptyTest()
        {
            var list = Create();
            var enumerator = list.GetEnumerator();
            Assert.NotNull(enumerator);
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            var more = enumerator.MoveNext();
            Assert.False(more);
            more = enumerator.MoveNext();
            Assert.False(more);
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        }

        [Fact]
        public virtual void GetEnumeratorTest()
        {
            var list = Create(2);
            var enumerator = list.GetEnumerator();
            Assert.NotNull(enumerator);
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            var more = enumerator.MoveNext();
            Assert.True(more);
            var item = enumerator.Current;
            more = enumerator.MoveNext();
            Assert.True(more);
            more = enumerator.MoveNext();
            Assert.False(more);
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        }
    }
}
