// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Tests
{
    public class ReadOnlyCollectionBaseTests
    {
        private static MyReadOnlyCollectionBase CreateCollection()
        {
            var fooArray = new Foo[100];
            for (int i = 0; i < 100; i++)
            {
                fooArray[i] = new Foo(i, i.ToString());
            }

            return new MyReadOnlyCollectionBase(fooArray);
        }

        [Fact]
        public static void TestSyncRoot()
        {
            MyReadOnlyCollectionBase collection = CreateCollection();
            Assert.False(collection.SyncRoot is ArrayList);
            Assert.Same(collection.SyncRoot, collection.SyncRoot);
        }

        [Fact]
        public static void TestAddRange_Count()
        {
            MyReadOnlyCollectionBase collection = CreateCollection();
            Assert.Equal(100, collection.Count);
        }
        
        [Fact]
        public static void TestCopyTo_ZeroIndex()
        {
            MyReadOnlyCollectionBase collection = CreateCollection();

            var copyArray = new Foo[100];
            collection.CopyTo(copyArray, 0);

            Assert.Equal(100, copyArray.Length);
            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(i, copyArray[i].IntValue);
                Assert.Equal(i.ToString(), copyArray[i].StringValue);
            }
        }

        [Fact]
        public static void TestCopyTo_NonZeroIndex()
        {
            MyReadOnlyCollectionBase collection = CreateCollection();

            var copyArray = new Foo[200];
            collection.CopyTo(copyArray, 100);

            Assert.Equal(200, copyArray.Length);
            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(i, copyArray[100 + i].IntValue);
                Assert.Equal(i.ToString(), copyArray[100 + i].StringValue);
            }
        }

        [Fact]
        public static void TestCopyTo_Invalid()
        {
            MyReadOnlyCollectionBase collection = CreateCollection();

            Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null, 0)); // Array is null

            Assert.Throws<ArgumentException>(() => collection.CopyTo(new Foo[100], 50)); // Index + collection.Count > array.Length
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(new Foo[100], -1)); // Index < 0
        }

        [Fact]
        public static void TestGetEnumerator()
        {
            MyReadOnlyCollectionBase collection = CreateCollection();

            IEnumerator enumerator = collection.GetEnumerator();
            // Calling current should throw when the enumerator has not started enumerating
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            int counter = 0;
            while (enumerator.MoveNext())
            {
                Foo current = (Foo)enumerator.Current;

                Assert.Equal(counter, current.IntValue);
                Assert.Equal(counter.ToString(), current.StringValue);
                counter++;
            }

            Assert.Equal(collection.Count, counter);

            // Calling current should throw when the enumerator has finished enumerating
            Assert.Throws<InvalidOperationException>(() => (Foo)enumerator.Current);

            // Calling current should throw when the enumerator is reset
            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => (Foo)enumerator.Current);
        }

        [Fact]
        public static void TestIsSynchronized()
        {
            MyReadOnlyCollectionBase collection = CreateCollection();

            Assert.False(((ICollection)collection).IsSynchronized);
        }

        [Fact]
        public static void TestIListMethods()
        {
            MyReadOnlyCollectionBase collection = CreateCollection();

            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(i, collection[i].IntValue);
                Assert.Equal(i.ToString(), collection[i].StringValue);

                Assert.Equal(i, collection.IndexOf(new Foo(i, i.ToString())));
                Assert.True(collection.Contains(new Foo(i, i.ToString())));
            }
        }

        [Fact]
        public static void TestIListProperties()
        {
            MyReadOnlyCollectionBase collection = CreateCollection();

            Assert.True(collection.IsFixedSize);
            Assert.True(collection.IsReadOnly);
        }

        [Fact]
        public static void TestVirtualMethods()
        {
            VirtualTestReadOnlyCollection collectionBase = new VirtualTestReadOnlyCollection();
            Assert.Equal(collectionBase.Count, int.MinValue);

            Assert.Null(collectionBase.GetEnumerator());
        }

        // ReadOnlyCollectionBase is provided to be used as the base class for strongly typed collections. Lets use one of our own here.
        // This collection only allows the type Foo
        private class MyReadOnlyCollectionBase : ReadOnlyCollectionBase
        {
            //we need a way of initializing this collection
            public MyReadOnlyCollectionBase(Foo[] values)
            {
                InnerList.AddRange(values);
            }

            public Foo this[int indx]
            {
                get { return (Foo)InnerList[indx]; }
            }

            public void CopyTo(Array array, int index)
            {
                ((ICollection)this).CopyTo(array, index);// Use the base class explicit implemenation of ICollection.CopyTo
            }

            public virtual object SyncRoot
            {
                get
                {
                    return ((ICollection)this).SyncRoot;// Use the base class explicit implemenation of ICollection.SyncRoot
                }
            }

            public int IndexOf(Foo f)
            {
                return ((IList)InnerList).IndexOf(f);
            }

            public bool Contains(Foo f)
            {
                return ((IList)InnerList).Contains(f);
            }

            public bool IsFixedSize
            {
                get { return true; }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }
        }

        private class VirtualTestReadOnlyCollection : ReadOnlyCollectionBase
        {
            public override int Count
            {
                get
                {
                    return int.MinValue;
                }
            }

            public override IEnumerator GetEnumerator()
            {
                return null;
            }
        }

        private class Foo
        {
            public Foo()
            {
            }

            public Foo(int intValue, string stringValue)
            {
                _intValue = intValue;
                _stringValue = stringValue;
            }

            private int _intValue;
            public int IntValue
            {
                get { return _intValue; }
                set { _intValue = value; }
            }

            private string _stringValue;
            public string StringValue
            {
                get { return _stringValue; }
                set { _stringValue = value; }
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;
                if (!(obj is Foo))
                    return false;
                if ((((Foo)obj).IntValue == _intValue) && (((Foo)obj).StringValue == _stringValue))
                    return true;
                return false;
            }

            public override int GetHashCode()
            {
                return _intValue;
            }
        }
    }
}
