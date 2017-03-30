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
        public static void SyncRoot()
        {
            MyReadOnlyCollectionBase collection = CreateCollection();
            Assert.False(collection.SyncRoot is ArrayList);
            Assert.Same(collection.SyncRoot, collection.SyncRoot);
        }

        [Fact]
        public static void AddRange_Count()
        {
            MyReadOnlyCollectionBase collection = CreateCollection();
            Assert.Equal(100, collection.Count);
        }
        
        [Fact]
        public static void CopyTo_ZeroIndex()
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
        public static void CopyTo_NonZeroIndex()
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
        public static void CopyTo_Invalid()
        {
            MyReadOnlyCollectionBase collection = CreateCollection();

            AssertExtensions.Throws<ArgumentNullException>("destinationArray", "dest", () => collection.CopyTo(null, 0)); // Array is null

            AssertExtensions.Throws<ArgumentException>("destinationArray", string.Empty, () => collection.CopyTo(new Foo[100], 50)); // Index + collection.Count > array.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("destinationIndex", "dstIndex", () => collection.CopyTo(new Foo[100], -1)); // Index < 0
        }

        [Fact]
        public static void GetEnumerator()
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
        public static void IsSynchronized()
        {
            MyReadOnlyCollectionBase collection = CreateCollection();
            Assert.False(((ICollection)collection).IsSynchronized);
        }

        [Fact]
        public static void IListMethods()
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
        public static void IListProperties()
        {
            MyReadOnlyCollectionBase collection = CreateCollection();
            Assert.True(collection.IsFixedSize);
            Assert.True(collection.IsReadOnly);
        }

        [Fact]
        public static void VirtualMethods()
        {
            VirtualTestReadOnlyCollection collectionBase = new VirtualTestReadOnlyCollection();
            Assert.Equal(collectionBase.Count, int.MinValue);
            Assert.Null(collectionBase.GetEnumerator());
        }

        // ReadOnlyCollectionBase is provided to be used as the base class for strongly typed collections.
        // Let's use one of our own here for the type Foo.
        private class MyReadOnlyCollectionBase : ReadOnlyCollectionBase
        {
            public MyReadOnlyCollectionBase(Foo[] values)
            {
                InnerList.AddRange(values);
            }

            public Foo this[int indx]
            {
                get { return (Foo)InnerList[indx]; }
            }

            public void CopyTo(Array array, int index) => ((ICollection)this).CopyTo(array, index);

            public virtual object SyncRoot
            {
                get { return ((ICollection)this).SyncRoot; }
            }

            public int IndexOf(Foo f) => ((IList)InnerList).IndexOf(f);

            public bool Contains(Foo f) => ((IList)InnerList).Contains(f);

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
                get { return int.MinValue; }
            }

            public override IEnumerator GetEnumerator() => null;
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
                if (obj == null)
                    return false;
                return foo.IntValue == IntValue && foo.StringValue == StringValue;
            }

            public override int GetHashCode() => IntValue;
        }
    }
}
