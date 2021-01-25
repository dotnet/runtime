// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class NameObjectCollectionBaseKeysTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public void Keys_PreservesInstance(int count)
        {
            MyNameObjectCollection nameObjectCollection = Helpers.CreateNameObjectCollection(count);
            Assert.Same(nameObjectCollection.Keys, nameObjectCollection.Keys);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public void Keys_GetEnumerator(int count)
        {
            MyNameObjectCollection nameObjectCollection = Helpers.CreateNameObjectCollection(count);

            NameObjectCollectionBase.KeysCollection keys = nameObjectCollection.Keys;
            Assert.NotSame(keys.GetEnumerator(), keys.GetEnumerator());

            IEnumerator enumerator = keys.GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                while (enumerator.MoveNext())
                {
                    Assert.Equal(keys[counter], enumerator.Current);
                    counter++;
                }
                Assert.Equal(count, keys.Count);
                enumerator.Reset();
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public void Keys_GetEnumerator_Invalid(int count)
        {
            MyNameObjectCollection nameObjectCollection = Helpers.CreateNameObjectCollection(count);
            NameObjectCollectionBase.KeysCollection keys = nameObjectCollection.Keys;
            IEnumerator enumerator = keys.GetEnumerator();

            // Has not started enumerating
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            // Has finished enumerating
            while (enumerator.MoveNext()) ;
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            // Has reset enumerating
            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            // Modify collection
            enumerator.MoveNext();
            nameObjectCollection.Add("new-name", new Foo("new-value"));
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
            if (count > 0)
            {
                Assert.NotNull(enumerator.Current);
            }

            // Modified read only collection still throws
            nameObjectCollection.IsReadOnly = true;
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Reset());

            // Clear collection
            nameObjectCollection.IsReadOnly = false;
            enumerator = keys.GetEnumerator();
            enumerator.MoveNext();
            nameObjectCollection.Clear();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public void Keys_Properties(int count)
        {
            MyNameObjectCollection nameObjectCollection = Helpers.CreateNameObjectCollection(count);
            ICollection keysCollection = nameObjectCollection.Keys;

            Assert.Same(((ICollection)nameObjectCollection).SyncRoot, keysCollection.SyncRoot);
            Assert.False(keysCollection.IsSynchronized);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 5)]
        [InlineData(10, 0)]
        [InlineData(10, 5)]
        public void Keys_CopyTo(int count, int index)
        {
            MyNameObjectCollection nameObjectCollection = Helpers.CreateNameObjectCollection(count);
            Keys_CopyTo_Helper(nameObjectCollection, index);
        }

        [Fact]
        public void Keys_CopyTo_NullKeys()
        {
            MyNameObjectCollection nameObjectCollection = new MyNameObjectCollection();
            nameObjectCollection.Add(null, new Foo("1"));
            nameObjectCollection.Add(null, new Foo("2"));
            nameObjectCollection.Add(null, null);
            nameObjectCollection.Add(null, new Foo("3"));

            Keys_CopyTo_Helper(nameObjectCollection, 0);
        }

        private static void Keys_CopyTo_Helper(MyNameObjectCollection nameObjectCollection, int index)
        {
            ICollection keys = nameObjectCollection.Keys;
            string[] keysArray = new string[index + keys.Count + index];
            keys.CopyTo(keysArray, index);

            for (int i = 0; i < index; i++)
            {
                Assert.Null(keysArray[i]);
            }
            for (int i = 0; i < keys.Count; i++)
            {
                Assert.Equal(nameObjectCollection.GetKey(i), keysArray[i + index]);
            }
            for (int i = index + keys.Count; i < keysArray.Length; i++)
            {
                Assert.Null(keysArray[i]);
            }

            // Clearing the nameObjectCollection should not affect the keys copy
            int previousCount = keysArray.Length;
            nameObjectCollection.Clear();
            Assert.Equal(previousCount, keysArray.Length);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public void Keys_CopyTo_Invalid(int count)
        {
            MyNameObjectCollection nameObjectCollection = Helpers.CreateNameObjectCollection(count);
            NameObjectCollectionBase.KeysCollection keys = nameObjectCollection.Keys;
            ICollection keysCollection = keys;

            AssertExtensions.Throws<ArgumentNullException>("array", () => keysCollection.CopyTo(null, 0));
            AssertExtensions.Throws<ArgumentException>("array", null, () => keysCollection.CopyTo(new string[count, count], 0));

            if (count > 0)
            {
                AssertExtensions.Throws<ArgumentException>(null, () => keysCollection.CopyTo(new string[0], 0));
                AssertExtensions.Throws<ArgumentException>(null, () => keysCollection.CopyTo(new string[count - 1], 0));

                Assert.Throws<InvalidCastException>(() => keysCollection.CopyTo(new Foo[count], 0));
            }

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => keysCollection.CopyTo(new string[count], -1));
            AssertExtensions.Throws<ArgumentException>(null, () => keysCollection.CopyTo(new string[count], 1));
            AssertExtensions.Throws<ArgumentException>(null, () => keysCollection.CopyTo(new string[count], count + 1));
        }
    }
}
