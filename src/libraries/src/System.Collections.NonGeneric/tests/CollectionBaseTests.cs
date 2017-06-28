// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Tests
{
    public static class CollectionBaseTests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var collBase = new MyCollection();
            var arrList = new ArrayList();

            Assert.Equal(0, collBase.Capacity);
            Assert.Equal(arrList.Capacity, collBase.Capacity);

            Assert.Equal(0, collBase.InnerListCapacity);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(1024)]
        public static void Ctor_Capacity(int capacity)
        {
            var collBase = new MyCollection(capacity);
            var arrList = new ArrayList(capacity);

            Assert.Equal(capacity, collBase.Capacity);
            Assert.Equal(arrList.Capacity, collBase.Capacity);

            Assert.Equal(capacity, collBase.InnerListCapacity);
        }

        [Fact]
        public static void CtorCapacity_Invalid()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new MyCollection(-1)); // Capacity < 0
            Assert.Throws<OutOfMemoryException>(() => new MyCollection(int.MaxValue)); // Capacity is too large
        }

        private static Foo CreateValue(int i) => new Foo(i, i.ToString());

        private static MyCollection CreateCollection(int count)
        {
            var collBase = new MyCollection();
            for (int i = 0; i < 100; i++)
            {
                collBase.Add(CreateValue(i));
            }
            return collBase;
        }

        [Fact]
        public static void Add()
        {
            MyCollection collBase = new MyCollection();
            for (int i = 0; i < 100; i++)
            {
                Foo value = CreateValue(i);
                collBase.Add(value);
                Assert.True(collBase.Contains(value));
            }
            Assert.Equal(100, collBase.Count);
            for (int i = 0; i < collBase.Count; i++)
            {
                Foo value = CreateValue(i);
                Assert.Equal(value, collBase[i]);               
            }
        }

        [Fact]
        public static void Remove()
        {
            MyCollection collBase = CreateCollection(100);
            for (int i = 0; i < 100; i++)
            {
                Foo value = CreateValue(i);
                collBase.Remove(value);
                Assert.False(collBase.Contains(value));
            }
            Assert.Equal(0, collBase.Count);
        }

        [Fact]
        public static void Remove_Invalid()
        {
            MyCollection collBase = CreateCollection(100);
            AssertExtensions.Throws<ArgumentException>(null, () => collBase.Remove(new Foo())); // Non existent object
            Assert.Equal(100, collBase.Count);
        }

        [Fact]
        public static void Insert()
        {
            var collBase = new MyCollection();
            for (int i = 0; i < 100; i++)
            {
                Foo value = CreateValue(i);
                collBase.Insert(i, value);
                Assert.True(collBase.Contains(value));
            }
            Assert.Equal(100, collBase.Count);
            for (int i = 0; i < collBase.Count; i++)
            {
                var expected = CreateValue(i);
                Assert.Equal(expected, collBase[i]);
            }
        }

        [Fact]
        public static void Insert_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            MyCollection collBase = CreateCollection(100);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collBase.Insert(-1, new Foo())); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collBase.Insert(collBase.Count + 1, new Foo())); // Index > collBase.Count

            Assert.Equal(100, collBase.Count);
        }

        [Fact]
        public static void RemoveAt()
        {
            MyCollection collBase = CreateCollection(100);
            for (int i = 0; i < collBase.Count; i++)
            {
                collBase.RemoveAt(0);
                Assert.False(collBase.Contains(CreateValue(i)));
            }
        }

        [Fact]
        public static void RemoveAt_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            MyCollection collBase = CreateCollection(100);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collBase.RemoveAt(-1)); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collBase.RemoveAt(collBase.Count)); // Index > collBase.Count
            Assert.Equal(100, collBase.Count);
        }

        [Fact]
        public static void Clear()
        {
            MyCollection collBase = CreateCollection(100);
            collBase.Clear();
            Assert.Equal(0, collBase.Count);
        }

        [Fact]
        public static void IndexOf()
        {
            MyCollection collBase = CreateCollection(100);
            for (int i = 0; i < collBase.Count; i++)
            {
                int ndx = collBase.IndexOf(CreateValue(i));
                Assert.Equal(i, ndx);
            }
        }

        [Fact]
        public static void Contains()
        {
            MyCollection collBase = CreateCollection(100);
            for (int i = 0; i < collBase.Count; i++)
            {
                Assert.True(collBase.Contains(CreateValue(i)));
            }
            Assert.False(collBase.Contains(new Foo()));
        }

        [Fact]
        public static void Item_Get()
        {
            MyCollection collBase = CreateCollection(100);
            for (int i = 0; i < collBase.Count; i++)
            {
                Assert.Equal(CreateValue(i), collBase[i]);
            }
        }

        [Fact]
        public static void Item_Get_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            MyCollection collBase = CreateCollection(100);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collBase[-1]); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collBase[collBase.Count]); // Index >= InnerList.Count
        }

        [Fact]
        public static void Item_Set()
        {
            MyCollection collBase = CreateCollection(100);
            for (int i = 0; i < collBase.Count; i++)
            {
                var value = CreateValue(collBase.Count - i);
                collBase[i] = value;
                Assert.Equal(value, collBase[i]);
            }
        }

        [Fact]
        public static void Item_Set_Invalid()
        {
            MyCollection collBase = CreateCollection(100);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collBase[-1] = new Foo()); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collBase[collBase.Count] = new Foo()); // Index >= InnerList.Count

            AssertExtensions.Throws<ArgumentNullException>("value", () => collBase[0] = null); // Object is null
        }

        [Fact]
        public static void CopyTo()
        {
            MyCollection collBase = CreateCollection(100);

            // Basic
            var fooArr = new Foo[100];
            collBase.CopyTo(fooArr, 0);

            Assert.Equal(collBase.Count, fooArr.Length);
            for (int i = 0; i < fooArr.Length; i++)
            {
                Assert.Equal(collBase[i], fooArr.GetValue(i));
            }

            // With index
            fooArr = new Foo[collBase.Count * 2];
            collBase.CopyTo(fooArr, collBase.Count);

            for (int i = collBase.Count; i < fooArr.Length; i++)
            {
                Assert.Equal(collBase[i - collBase.Count], fooArr.GetValue(i));
            }
        }

        [Fact]
        public static void CopyTo_Invalid()
        {
            MyCollection collBase = CreateCollection(100);
            var fooArr = new Foo[100];
            // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("destinationIndex", "dstIndex", () => collBase.CopyTo(fooArr, -1));
            // Index + fooArray.Length > collBase.Count
            AssertExtensions.Throws<ArgumentException>("destinationArray", string.Empty, () => collBase.CopyTo(fooArr, 50));
        }

        [Fact]
        public static void GetEnumerator()
        {
            MyCollection collBase = CreateCollection(100);
            IEnumerator enumerator = collBase.GetEnumerator();
            Assert.NotNull(enumerator);

            int count = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(collBase[count], enumerator.Current);
                count++;
            }

            Assert.Equal(collBase.Count, count);
        }

        [Fact]
        public static void GetEnumerator_Invalid()
        {
            MyCollection collBase = CreateCollection(100);
            IEnumerator enumerator = collBase.GetEnumerator();

            // Index < 0
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            // Index >= dictionary.Count
            while (enumerator.MoveNext()) ;
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.False(enumerator.MoveNext());

            // Current throws after resetting
            enumerator.Reset();
            Assert.True(enumerator.MoveNext());

            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        }

        [Fact]
        public static void SyncRoot()
        {
            // SyncRoot should be the reference to the underlying collection, not to MyCollection
            var collBase = new MyCollection();
            object syncRoot = collBase.SyncRoot;
            Assert.NotEqual(syncRoot, collBase);
            Assert.Equal(collBase.SyncRoot, collBase.SyncRoot);
        }

        [Fact]
        public static void IListProperties()
        {
            MyCollection collBase = CreateCollection(100);
            Assert.False(collBase.IsFixedSize);
            Assert.False(collBase.IsReadOnly);
            Assert.False(collBase.IsSynchronized);
        }

        [Fact]
        public static void CapacityGet()
        {
            var collBase = new MyCollection(new string[10]);
            Assert.True(collBase.Capacity >= collBase.Count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(44)]
        public static void CapacitySet(int capacity)
        {
            var collBase = new MyCollection(0);

            collBase.Capacity = capacity;
            Assert.Equal(capacity, collBase.Capacity);
        }

        [Fact]
        public static void Capacity_Set_Invalid()
        {
            var collBase = new MyCollection(new string[10]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => collBase.Capacity = -1); // Capacity < 0
            Assert.Throws<OutOfMemoryException>(() => collBase.Capacity = int.MaxValue); // Capacity is very large

            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => collBase.Capacity = collBase.Count - 1); // Capacity < list.Count
        }
        [Fact]
        public static void Add_Called()
        {
            var f = new Foo(0, "0");
            var collBase = new OnMethodCalledCollectionBase();

            collBase.Add(f);
            Assert.True(collBase.OnValidateCalled);
            Assert.True(collBase.OnInsertCalled);
            Assert.True(collBase.OnInsertCompleteCalled);

            Assert.True(collBase.Contains(f));
        }

        [Fact]
        public static void Add_Throws_Called()
        {
            var f = new Foo(0, "0");

            // Throw OnValidate
            var collBase = new OnMethodCalledCollectionBase();
            collBase.OnValidateThrow = true;

            Assert.Throws<Exception>(() => collBase.Add(f));
            Assert.Equal(0, collBase.Count);

            // Throw OnInsert
            collBase = new OnMethodCalledCollectionBase();
            collBase.OnInsertThrow = true;

            Assert.Throws<Exception>(() => collBase.Add(f));
            Assert.Equal(0, collBase.Count);

            // Throw OnInsertComplete
            collBase = new OnMethodCalledCollectionBase();
            collBase.OnInsertCompleteThrow = true;

            Assert.Throws<Exception>(() => collBase.Add(f));
            Assert.Equal(0, collBase.Count);
        }

        [Fact]
        public static void Insert_Called()
        {
            var f = new Foo(0, "0");
            var collBase = new OnMethodCalledCollectionBase();

            collBase.Insert(0, f);
            Assert.True(collBase.OnValidateCalled);
            Assert.True(collBase.OnInsertCalled);
            Assert.True(collBase.OnInsertCompleteCalled);

            Assert.True(collBase.Contains(f));
        }

        [Fact]
        public static void Insert_Throws_Called()
        {
            var f = new Foo(0, "0");

            // Throw OnValidate
            var collBase = new OnMethodCalledCollectionBase();
            collBase.OnValidateThrow = true;

            Assert.Throws<Exception>(() => collBase.Insert(0, f));
            Assert.Equal(0, collBase.Count);

            // Throw OnInsert
            collBase = new OnMethodCalledCollectionBase();
            collBase.OnInsertThrow = true;

            Assert.Throws<Exception>(() => collBase.Insert(0, f));
            Assert.Equal(0, collBase.Count);

            // Throw OnInsertComplete
            collBase = new OnMethodCalledCollectionBase();
            collBase.OnInsertCompleteThrow = true;

            Assert.Throws<Exception>(() => collBase.Insert(0, f));
            Assert.Equal(0, collBase.Count);
        }

        [Fact]
        public static void Remove_Called()
        {
            var f = new Foo(0, "0");
            var collBase = new OnMethodCalledCollectionBase();
            collBase.Add(f);
            collBase.OnValidateCalled = false;

            collBase.Remove(f);

            Assert.True(collBase.OnValidateCalled);
            Assert.True(collBase.OnRemoveCalled);
            Assert.True(collBase.OnRemoveCompleteCalled);

            Assert.False(collBase.Contains(f));
        }

        [Fact]
        public static void Remove_Throws_Called()
        {
            var f = new Foo(0, "0");

            // Throw OnValidate
            var collBase = new OnMethodCalledCollectionBase();
            collBase.Add(f);
            collBase.OnValidateThrow = true;

            Assert.Throws<Exception>(() => collBase.Remove(f));
            Assert.Equal(1, collBase.Count);

            // Throw OnRemove
            collBase = new OnMethodCalledCollectionBase();
            collBase.Add(f);
            collBase.OnRemoveThrow = true;

            Assert.Throws<Exception>(() => collBase.Remove(f));
            Assert.Equal(1, collBase.Count);

            // Throw OnRemoveComplete
            collBase = new OnMethodCalledCollectionBase();
            collBase.Add(f);
            collBase.OnRemoveCompleteThrow = true;

            Assert.Throws<Exception>(() => collBase.Remove(f));
            Assert.Equal(1, collBase.Count);
        }

        [Fact]
        public static void RemoveAt_Called()
        {
            var f = new Foo(0, "0");
            var collBase = new OnMethodCalledCollectionBase();
            collBase.Add(f);
            collBase.OnValidateCalled = false;

            collBase.RemoveAt(0);

            Assert.True(collBase.OnValidateCalled);
            Assert.True(collBase.OnRemoveCalled);
            Assert.True(collBase.OnRemoveCompleteCalled);

            Assert.False(collBase.Contains(f));
        }

        [Fact]
        public static void RemoveAt_Throws_Called()
        {
            var f = new Foo(0, "0");

            // Throw OnValidate
            var collBase = new OnMethodCalledCollectionBase();
            collBase.Add(f);
            collBase.OnValidateThrow = true;

            Assert.Throws<Exception>(() => collBase.RemoveAt(0));
            Assert.Equal(1, collBase.Count);

            // Throw OnRemove
            collBase = new OnMethodCalledCollectionBase();
            collBase.Add(f);
            collBase.OnRemoveThrow = true;

            Assert.Throws<Exception>(() => collBase.RemoveAt(0));
            Assert.Equal(1, collBase.Count);

            // Throw OnRemoveComplete
            collBase = new OnMethodCalledCollectionBase();
            collBase.Add(f);
            collBase.OnRemoveCompleteThrow = true;

            Assert.Throws<Exception>(() => collBase.RemoveAt(0));
            Assert.Equal(1, collBase.Count);
        }

        [Fact]
        public static void Clear_Called()
        {
            var f = new Foo(0, "0");
            var collBase = new OnMethodCalledCollectionBase();
            collBase.Add(f);
            collBase.Clear();

            Assert.True(collBase.OnClearCalled);
            Assert.True(collBase.OnClearCompleteCalled);

            Assert.Equal(0, collBase.Count);
        }

        [Fact]
        public static void Clear_Throws_Called()
        {
            var f = new Foo(0, "0");

            // Throw OnValidate
            var collBase = new OnMethodCalledCollectionBase();
            collBase.Add(f);
            collBase.OnValidateThrow = true;

            collBase.Clear();
            Assert.Equal(0, collBase.Count);

            // Throw OnClear
            collBase = new OnMethodCalledCollectionBase();
            collBase.Add(f);
            collBase.OnClearThrow = true;

            Assert.Throws<Exception>(() => collBase.Clear());
            Assert.Equal(1, collBase.Count);

            // Throw OnClearComplete
            collBase = new OnMethodCalledCollectionBase();
            collBase.Add(f);
            collBase.OnClearCompleteThrow = true;

            Assert.Throws<Exception>(() => collBase.Clear());
            Assert.Equal(0, collBase.Count);
        }

        [Fact]
        public static void Set_Called()
        {
            var f = new Foo(1, "1");

            var collBase = new OnMethodCalledCollectionBase();
            collBase.Add(new Foo());
            collBase.OnValidateCalled = false;

            collBase[0] = f;

            Assert.True(collBase.OnValidateCalled);
            Assert.True(collBase.OnSetCalled);
            Assert.True(collBase.OnSetCompleteCalled);

            Assert.Equal(f, collBase[0]);
        }

        [Fact]
        public static void Set_Throws_Called()
        {
            var f1 = new Foo(0, "0");
            var f2 = new Foo(1, "1");

            // Throw OnValidate
            var collBase = new OnMethodCalledCollectionBase();
            collBase.Add(f1);
            collBase.OnValidateThrow = true;

            Assert.Throws<Exception>(() => collBase[0] = f2);
            Assert.Equal(f1, collBase[0]);

            // Throw OnSet
            collBase = new OnMethodCalledCollectionBase();
            collBase.Add(f1);
            collBase.OnSetThrow = true;

            Assert.Throws<Exception>(() => collBase[0] = f2);
            Assert.Equal(f1, collBase[0]);

            // Throw OnSetComplete
            collBase = new OnMethodCalledCollectionBase();
            collBase.Add(f1);
            collBase.OnSetCompleteThrow = true;

            Assert.Throws<Exception>(() => collBase[0] = f2);
            Assert.Equal(f1, collBase[0]);
        }

        // CollectionBase is provided to be used as the base class for strongly typed collections. Lets use one of our own here
        private class MyCollection : CollectionBase
        {
            public MyCollection() : base()
            {
            }

            public MyCollection(int capacity) : base(capacity)
            {
            }

            public MyCollection(ICollection collection) : base()
            {
                foreach (object obj in collection)
                {
                    InnerList.Add(obj);
                }
            }

            public int InnerListCapacity
            {
                get { return InnerList.Capacity; }
            }

            public int Add(Foo f1) => List.Add(f1);

            public Foo this[int indx]
            {
                get { return (Foo)List[indx]; }
                set { List[indx] = value; }
            }

            public void CopyTo(Array array, int index) => List.CopyTo(array, index);

            public int IndexOf(Foo f) => List.IndexOf(f);

            public bool Contains(Foo f) => List.Contains(f);

            public void Insert(int index, Foo f) => List.Insert(index, f);

            public void Remove(Foo f) => List.Remove(f);

            public bool IsSynchronized
            {
                get { return ((ICollection)this).IsSynchronized; }
            }

            public object SyncRoot
            {
                get { return ((ICollection)this).SyncRoot; }
            }

            public bool IsReadOnly
            {
                get { return List.IsReadOnly; }
            }

            public bool IsFixedSize
            {
                get { return List.IsFixedSize; }
            }
        }

        private class OnMethodCalledCollectionBase : CollectionBase
        {
            public bool OnValidateCalled;
            public bool OnSetCalled;
            public bool OnSetCompleteCalled;
            public bool OnInsertCalled;
            public bool OnInsertCompleteCalled;
            public bool OnClearCalled;
            public bool OnClearCompleteCalled;
            public bool OnRemoveCalled;
            public bool OnRemoveCompleteCalled;

            public bool OnValidateThrow;
            public bool OnSetThrow;
            public bool OnSetCompleteThrow;
            public bool OnInsertThrow;
            public bool OnInsertCompleteThrow;
            public bool OnClearThrow;
            public bool OnClearCompleteThrow;
            public bool OnRemoveThrow;
            public bool OnRemoveCompleteThrow;

            public int Add(Foo f1) => List.Add(f1);

            public Foo this[int indx]
            {
                get { return (Foo)List[indx]; }
                set { List[indx] = value; }
            }

            public void CopyTo(Array array, int index) => List.CopyTo(array, index);

            public int IndexOf(Foo f) => List.IndexOf(f);

            public bool Contains(Foo f) => List.Contains(f);

            public void Insert(int index, Foo f) => List.Insert(index, f);

            public void Remove(Foo f) => List.Remove(f);

            protected override void OnSet(int index, object oldValue, object newValue)
            {
                Assert.True(OnValidateCalled);
                Assert.Equal(this[index], oldValue);

                OnSetCalled = true;

                if (OnSetThrow)
                    throw new Exception("OnSet");
            }

            protected override void OnInsert(int index, object value)
            {
                Assert.True(OnValidateCalled);
                Assert.True(index <= Count);
                if (index != Count)
                {
                    Assert.Equal(this[index], value);
                }

                OnInsertCalled = true;

                if (OnInsertThrow)
                    throw new Exception("OnInsert");
            }

            protected override void OnClear()
            {
                OnClearCalled = true;

                if (OnClearThrow)
                    throw new Exception("OnClear");
            }

            protected override void OnRemove(int index, object value)
            {
                Assert.True(OnValidateCalled);
                Assert.Equal(this[index], value);

                OnRemoveCalled = true;

                if (OnRemoveThrow)
                    throw new Exception("OnRemove");
            }

            protected override void OnValidate(object value)
            {
                OnValidateCalled = true;

                if (OnValidateThrow)
                    throw new Exception("OnValidate");
            }

            protected override void OnSetComplete(int index, object oldValue, object newValue)
            {
                Assert.True(OnSetCalled);
                Assert.Equal(this[index], newValue);

                OnSetCompleteCalled = true;

                if (OnSetCompleteThrow)
                    throw new Exception("OnSetComplete");
            }

            protected override void OnInsertComplete(int index, object value)
            {
                Assert.True(OnInsertCalled);
                Assert.Equal(this[index], value);

                OnInsertCompleteCalled = true;

                if (OnInsertCompleteThrow)
                    throw new Exception("OnInsertComplete");
            }

            protected override void OnClearComplete()
            {
                Assert.True(OnClearCalled);
                Assert.Equal(0, Count);
                Assert.Equal(0, InnerList.Count);

                OnClearCompleteCalled = true;

                if (OnClearCompleteThrow)
                    throw new Exception("OnClearComplete");
            }

            protected override void OnRemoveComplete(int index, object value)
            {
                Assert.True(OnRemoveCalled);
                Assert.NotEqual(IndexOf((Foo)value), index);

                OnRemoveCompleteCalled = true;

                if (OnRemoveCompleteThrow)
                    throw new Exception("OnRemoveComplete");
            }
        }

        private class Foo
        {
            public Foo()
            {
            }

            public Foo(int intValue, string stringValue)
            {
                IntValue = intValue;
                StringValue = stringValue;
            }
            
            public int IntValue { get; set; }
            public string StringValue { get; set; }

            public override bool Equals(object obj)
            {
                Foo foo = obj as Foo;
                if (foo == null)
                    return false;
                return foo.IntValue == IntValue && foo.StringValue == StringValue;
            }

            public override int GetHashCode() => IntValue;
        }
    }
}
