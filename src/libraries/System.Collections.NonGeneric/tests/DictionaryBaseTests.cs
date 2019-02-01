// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Tests
{
    public static class DictionaryBaseTests
    {
        private static FooKey CreateKey(int i) => new FooKey(i, i.ToString());

        private static FooValue CreateValue(int i) => new FooValue(i, i.ToString());

        private static MyDictionary CreateDictionary(int count)
        {
            var dictionary = new MyDictionary();
            for (int i = 0; i < count; i++)
            {
                dictionary.Add(CreateKey(i), CreateValue(i));
            }
            return dictionary;
        }

        [Fact]
        public static void Add()
        {
            var dictBase = new MyDictionary();
            for (int i = 0; i < 100; i++)
            {
                FooKey key = CreateKey(i);
                dictBase.Add(key, CreateValue(i));
                Assert.True(dictBase.Contains(key));
            }

            Assert.Equal(100, dictBase.Count);
            for (int i = 0; i < dictBase.Count; i++)
            {
                Assert.Equal(CreateValue(i), dictBase[CreateKey(i)]);
            }

            FooKey nullKey = CreateKey(101);
            dictBase.Add(nullKey, null);
            Assert.Equal(null, dictBase[nullKey]);
        }

        [Fact]
        public static void Remove()
        {
            MyDictionary dictBase = CreateDictionary(100);
            for (int i = 0; i < 100; i++)
            {
                FooKey key = CreateKey(i);
                dictBase.Remove(key);
                Assert.False(dictBase.Contains(key));
            }
            Assert.Equal(0, dictBase.Count);
            dictBase.Remove(new FooKey()); // Doesn't exist, but doesn't throw
        }

        [Fact]
        public static void Contains()
        {
            MyDictionary dictBase = CreateDictionary(100);
            for (int i = 0; i < dictBase.Count; i++)
            {
                Assert.True(dictBase.Contains(CreateKey(i)));
            }
            Assert.False(dictBase.Contains(new FooKey()));
        }

        [Fact]
        public static void Keys()
        {
            MyDictionary dictBase = CreateDictionary(100);
            ICollection keys = dictBase.Keys;

            Assert.Equal(dictBase.Count, keys.Count);
            foreach (FooKey key in keys)
            {
                Assert.True(dictBase.Contains(key));
            }
        }

        [Fact]
        public static void Values()
        {
            MyDictionary dictBase = CreateDictionary(100);
            ICollection values = dictBase.Values;

            Assert.Equal(dictBase.Count, values.Count);
            foreach (FooValue value in values)
            {
                FooKey key = CreateKey(value.IntValue);
                Assert.Equal(value, dictBase[key]);
            }
        }

        [Fact]
        public static void Item_Get()
        {
            MyDictionary dictBase = CreateDictionary(100);
            for (int i = 0; i < dictBase.Count; i++)
            {
                Assert.Equal(CreateValue(i), dictBase[CreateKey(i)]);
            }
            Assert.Equal(null, dictBase[new FooKey()]);
        }

        [Fact]
        public static void Item_Get_NullKey_ThrowsArgumentNullException()
        {
            var dictBase = new MyDictionary();
            AssertExtensions.Throws<ArgumentNullException>("key", () => dictBase[null]);
        }

        [Fact]
        public static void Item_Set()
        {
            MyDictionary dictBase = CreateDictionary(100);

            for (int i = 0; i < dictBase.Count; i++)
            {
                FooKey key = CreateKey(i);
                FooValue value = CreateValue(dictBase.Count - i - 1);
                dictBase[key] = value;
                Assert.Equal(value, dictBase[key]);
            }

            FooKey nonExistentKey = CreateKey(101);

            dictBase[nonExistentKey] = null;
            Assert.Equal(101, dictBase.Count); // Should add a key/value pair if the key 
            Assert.Equal(null, dictBase[nonExistentKey]);
        }

        [Fact]
        public static void Indexer_Set_NullKey_ThrowsArgumentNullException()
        {
            var dictBase = new MyDictionary();
            AssertExtensions.Throws<ArgumentNullException>("key", () => dictBase[null] = new FooValue());
        }

        [Fact]
        public static void Clear()
        {
            MyDictionary dictBase = CreateDictionary(100);
            dictBase.Clear();
            Assert.Equal(0, dictBase.Count);
        }

        [Fact]
        public static void CopyTo()
        {
            MyDictionary dictBase = CreateDictionary(100);
            // Basic
            var entries = new DictionaryEntry[dictBase.Count];
            dictBase.CopyTo(entries, 0);

            Assert.Equal(dictBase.Count, entries.Length);
            for (int i = 0; i < entries.Length; i++)
            {
                DictionaryEntry entry = entries[i];
                Assert.Equal(CreateKey(entries.Length - i - 1), entry.Key);
                Assert.Equal(CreateValue(entries.Length - i - 1), entry.Value);
            }

            // With index
            entries = new DictionaryEntry[dictBase.Count * 2];
            dictBase.CopyTo(entries, dictBase.Count);

            Assert.Equal(dictBase.Count * 2, entries.Length);
            for (int i = dictBase.Count; i < entries.Length; i++)
            {
                DictionaryEntry entry = entries[i];
                Assert.Equal(CreateKey(entries.Length - i - 1), entry.Key);
                Assert.Equal(CreateValue(entries.Length - i - 1), entry.Value);
            }
        }

        [Fact]
        public static void CopyTo_Invalid()
        {
            MyDictionary dictBase = CreateDictionary(100);
            AssertExtensions.Throws<ArgumentNullException>("array", () => dictBase.CopyTo(null, 0)); // Array is null

            AssertExtensions.Throws<ArgumentException>("array", null, () => dictBase.CopyTo(new object[100, 100], 0)); // Array is multidimensional

            AssertExtensions.Throws<ArgumentOutOfRangeException>("arrayIndex", () => dictBase.CopyTo(new DictionaryEntry[100], -1)); // Index < 0

            AssertExtensions.Throws<ArgumentException>(null, () => dictBase.CopyTo(new DictionaryEntry[100], 100)); // Index >= count
            AssertExtensions.Throws<ArgumentException>(null, () => dictBase.CopyTo(new DictionaryEntry[100], 50)); // Index + array.Count >= count
        }

        [Fact]
        public static void GetEnumerator_IDictionaryEnumerator()
        {
            MyDictionary dictBase = CreateDictionary(100);
            IDictionaryEnumerator enumerator = dictBase.GetEnumerator();
            Assert.NotNull(enumerator);

            int count = 0;
            while (enumerator.MoveNext())
            {
                DictionaryEntry entry1 = (DictionaryEntry)enumerator.Current;
                DictionaryEntry entry2 = enumerator.Entry;

                Assert.Equal(entry1.Key, entry2.Key);
                Assert.Equal(entry1.Value, entry2.Value);

                Assert.Equal(enumerator.Key, entry1.Key);
                Assert.Equal(enumerator.Value, entry1.Value);

                Assert.Equal(enumerator.Value, dictBase[(FooKey)enumerator.Key]);
                count++;
            }

            Assert.Equal(dictBase.Count, count);
        }

        [Fact]
        public static void GetEnumerator_IDictionaryEnumerator_Invalid()
        {
            MyDictionary dictBase = CreateDictionary(100);
            IDictionaryEnumerator enumerator = dictBase.GetEnumerator();

            // Index < 0
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
            Assert.Throws<InvalidOperationException>(() => enumerator.Key);
            Assert.Throws<InvalidOperationException>(() => enumerator.Value);

            // Index > dictionary.Count
            while (enumerator.MoveNext()) ;
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
            Assert.Throws<InvalidOperationException>(() => enumerator.Key);
            Assert.Throws<InvalidOperationException>(() => enumerator.Value);

            // Current throws after resetting
            enumerator.Reset();
            Assert.True(enumerator.MoveNext());

            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
            Assert.Throws<InvalidOperationException>(() => enumerator.Key);
            Assert.Throws<InvalidOperationException>(() => enumerator.Value);
        }

        [Fact]
        public static void GetEnumerator_IEnumerator()
        {
            MyDictionary dictBase = CreateDictionary(100);
            IEnumerator enumerator = ((IEnumerable)dictBase).GetEnumerator();
            Assert.NotNull(enumerator);

            int count = 0;
            while (enumerator.MoveNext())
            {
                DictionaryEntry entry = (DictionaryEntry)enumerator.Current;
                Assert.Equal((FooValue)entry.Value, dictBase[(FooKey)entry.Key]);
                count++;
            }

            Assert.Equal(dictBase.Count, count);
        }

        [Fact]
        public static void GetEnumerator_IEnumerator_Invalid()
        {
            MyDictionary dictBase = CreateDictionary(100);
            IEnumerator enumerator = ((IEnumerable)dictBase).GetEnumerator();

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
            // SyncRoot should be the reference to the underlying dictionary, not to MyDictionary
            var dictBase = new MyDictionary();
            object syncRoot = dictBase.SyncRoot;
            Assert.NotSame(syncRoot, dictBase);
            Assert.Same(dictBase.SyncRoot, dictBase.SyncRoot);
        }

        [Fact]
        public static void IDictionaryProperties()
        {
            var dictBase = new MyDictionary();
            Assert.False(dictBase.IsFixedSize);
            Assert.False(dictBase.IsReadOnly);
            Assert.False(dictBase.IsSynchronized);
        }

        [Fact]
        public static void Add_Called()
        {
            var f = new FooKey(0, "0");
            var dictBase = new OnMethodCalledDictionary();
            dictBase.Add(f, "hello");
            Assert.True(dictBase.OnValidateCalled);
            Assert.True(dictBase.OnInsertCalled);
            Assert.True(dictBase.OnInsertCompleteCalled);

            Assert.True(dictBase.Contains(f));
        }

        [Fact]
        public static void Add_Throws_Called()
        {
            var f = new FooKey(0, "0");

            // Throw OnValidate
            var dictBase = new OnMethodCalledDictionary();
            dictBase.OnValidateThrow = true;

            Assert.Throws<Exception>(() => dictBase.Add(f, ""));
            Assert.Equal(0, dictBase.Count);

            // Throw OnInsert
            dictBase = new OnMethodCalledDictionary();
            dictBase.OnInsertThrow = true;

            Assert.Throws<Exception>(() => dictBase.Add(f, ""));
            Assert.Equal(0, dictBase.Count);

            // Throw OnInsertComplete
            dictBase = new OnMethodCalledDictionary();
            dictBase.OnInsertCompleteThrow = true;

            Assert.Throws<Exception>(() => dictBase.Add(f, ""));
            Assert.Equal(0, dictBase.Count);
        }

        [Fact]
        public static void Remove_Called()
        {
            var f = new FooKey(0, "0");
            var dictBase = new OnMethodCalledDictionary();
            dictBase.Add(f, "");
            dictBase.OnValidateCalled = false;

            dictBase.Remove(f);

            Assert.True(dictBase.OnValidateCalled);
            Assert.True(dictBase.OnRemoveCalled);
            Assert.True(dictBase.OnRemoveCompleteCalled);

            Assert.False(dictBase.Contains(f));
        }

        [Fact]
        public static void Remove_Throws_Called()
        {
            var f = new FooKey(0, "0");

            // Throw OnValidate
            var dictBase = new OnMethodCalledDictionary();
            dictBase.Add(f, "");
            dictBase.OnValidateThrow = true;

            Assert.Throws<Exception>(() => dictBase.Remove(f));
            Assert.Equal(1, dictBase.Count);

            // Throw OnRemove
            dictBase = new OnMethodCalledDictionary();
            dictBase.Add(f, "");
            dictBase.OnRemoveThrow = true;

            Assert.Throws<Exception>(() => dictBase.Remove(f));
            Assert.Equal(1, dictBase.Count);

            // Throw OnRemoveComplete
            dictBase = new OnMethodCalledDictionary();
            dictBase.Add(f, "");
            dictBase.OnRemoveCompleteThrow = true;

            Assert.Throws<Exception>(() => dictBase.Remove(f));
            Assert.Equal(1, dictBase.Count);
        }

        [Fact]
        public static void Clear_Called()
        {
            var f = new FooKey(0, "0");
            var dictBase = new OnMethodCalledDictionary();
            dictBase.Add(f, "");
            dictBase.Clear();

            Assert.True(dictBase.OnClearCalled);
            Assert.True(dictBase.OnClearCompleteCalled);

            Assert.Equal(0, dictBase.Count);
        }

        [Fact]
        public static void Clear_Throws_Called()
        {
            var f = new FooKey(0, "0");

            // Throw OnValidate
            var dictBase = new OnMethodCalledDictionary();
            dictBase.Add(f, "");
            dictBase.OnValidateThrow = true;

            dictBase.Clear();
            Assert.Equal(0, dictBase.Count);

            // Throw OnClear
            dictBase = new OnMethodCalledDictionary();
            dictBase.Add(f, "");
            dictBase.OnClearThrow = true;

            Assert.Throws<Exception>(() => dictBase.Clear());
            Assert.Equal(1, dictBase.Count);

            // Throw OnClearComplete
            dictBase = new OnMethodCalledDictionary();
            dictBase.Add(f, "");
            dictBase.OnClearCompleteThrow = true;

            Assert.Throws<Exception>(() => dictBase.Clear());
            Assert.Equal(0, dictBase.Count);
        }

        [Fact]
        public static void Set_New_Called()
        {
            var f = new FooKey(1, "1");

            var dictBase = new OnMethodCalledDictionary();
            dictBase.OnValidateCalled = false;

            dictBase[f] = "hello";

            Assert.True(dictBase.OnValidateCalled);
            Assert.True(dictBase.OnSetCalled);
            Assert.True(dictBase.OnSetCompleteCalled);

            Assert.Equal(1, dictBase.Count);
            Assert.Equal("hello", dictBase[f]);
        }

        [Fact]
        public static void Set_New_Throws_Called()
        {
            var f = new FooKey(0, "0");

            // Throw OnValidate
            var dictBase = new OnMethodCalledDictionary();
            dictBase.OnValidateThrow = true;

            Assert.Throws<Exception>(() => dictBase[f] = "hello");
            Assert.Equal(0, dictBase.Count);

            // Throw OnSet
            dictBase = new OnMethodCalledDictionary();
            dictBase.OnSetThrow = true;

            Assert.Throws<Exception>(() => dictBase[f] = "hello");
            Assert.Equal(0, dictBase.Count);

            // Throw OnSetComplete
            dictBase = new OnMethodCalledDictionary();
            dictBase.OnSetCompleteThrow = true;

            Assert.Throws<Exception>(() => dictBase[f] = "hello");
            Assert.Equal(0, dictBase.Count);
        }

        [Fact]
        public static void Set_Existing_Called()
        {
            var f = new FooKey(1, "1");

            var dictBase = new OnMethodCalledDictionary();
            dictBase.Add(new FooKey(), "");
            dictBase.OnValidateCalled = false;

            dictBase[f] = "hello";

            Assert.True(dictBase.OnValidateCalled);
            Assert.True(dictBase.OnSetCalled);
            Assert.True(dictBase.OnSetCompleteCalled);

            Assert.Equal("hello", dictBase[f]);
        }

        [Fact]
        public static void Set_Existing_Throws_Called()
        {
            var f = new FooKey(0, "0");

            // Throw OnValidate
            var dictBase = new OnMethodCalledDictionary();
            dictBase.Add(f, "");
            dictBase.OnValidateThrow = true;

            Assert.Throws<Exception>(() => dictBase[f] = "hello");
            Assert.Equal("", dictBase[f]);

            // Throw OnSet
            dictBase = new OnMethodCalledDictionary();
            dictBase.Add(f, "");
            dictBase.OnSetThrow = true;

            Assert.Throws<Exception>(() => dictBase[f] = "hello");
            Assert.Equal("", dictBase[f]);

            // Throw OnSetComplete
            dictBase = new OnMethodCalledDictionary();
            dictBase.Add(f, "");
            dictBase.OnSetCompleteThrow = true;

            Assert.Throws<Exception>(() => dictBase[f] = "hello");
            Assert.Equal("", dictBase[f]);
        }

        // DictionaryBase is provided to be used as the base class for strongly typed collections. Lets use one of our own here
        private class MyDictionary : DictionaryBase
        {
            public void Add(FooKey key, FooValue value) => Dictionary.Add(key, value);

            public FooValue this[FooKey key]
            {
                get { return (FooValue)Dictionary[key]; }
                set { Dictionary[key] = value; }
            }

            public bool IsSynchronized
            {
                get { return Dictionary.IsSynchronized; }
            }

            public object SyncRoot
            {
                get { return Dictionary.SyncRoot; }
            }

            public bool Contains(FooKey key) => Dictionary.Contains(key);

            public void Remove(FooKey key) => Dictionary.Remove(key);

            public bool IsFixedSize
            {
                get { return Dictionary.IsFixedSize; }
            }

            public bool IsReadOnly
            {
                get { return Dictionary.IsReadOnly; }
            }

            public ICollection Keys
            {
                get { return Dictionary.Keys; }
            }

            public ICollection Values
            {
                get { return Dictionary.Values; }
            }
        }

        private class FooKey : IComparable
        {
            public FooKey()
            {
            }

            public FooKey(int i, string str)
            {
                IntValue = i;
                StringValue = str;
            }

            public int IntValue { get; set; }            
            public string StringValue { get; set; }

            public override bool Equals(object obj)
            {
                FooKey foo = obj as FooKey;
                if (foo == null)
                    return false;
                return foo.IntValue == IntValue && foo.StringValue == StringValue;
            }

            public override int GetHashCode() =>IntValue;

            public int CompareTo(object obj)
            {
                FooKey temp = (FooKey)obj;
                return IntValue.CompareTo(temp.IntValue);
            }
        }

        private class FooValue : IComparable
        {
            public FooValue()
            {
            }

            public FooValue(int intValue, string stringValue)
            {
                IntValue = intValue;
                StringValue = stringValue;
            }
            
            public int IntValue { get; set; }
            public string StringValue { get; set; }

            public override bool Equals(object obj)
            {
                FooValue foo = obj as FooValue;
                if (foo == null)
                    return false;
                return foo.IntValue == IntValue && foo.StringValue == StringValue;
            }

            public override int GetHashCode() => IntValue;

            public int CompareTo(object obj)
            {
                FooValue temp = (FooValue)obj;
                return IntValue.CompareTo(temp.IntValue);
            }
        }

        // DictionaryBase is provided to be used as the base class for strongly typed collections. Lets use one of our own here
        private class OnMethodCalledDictionary : DictionaryBase
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

            public void Add(FooKey key, string value) => Dictionary.Add(key, value);

            public string this[FooKey key]
            {
                get { return (string)Dictionary[key]; }
                set { Dictionary[key] = value; }
            }

            public bool Contains(FooKey key) => Dictionary.Contains(key);

            public void Remove(FooKey key) => Dictionary.Remove(key);

            protected override void OnSet(object key, object oldValue, object newValue)
            {
                Assert.True(OnValidateCalled);
                Assert.Equal(oldValue, this[(FooKey)key]);

                OnSetCalled = true;

                if (OnSetThrow)
                    throw new Exception("OnSet");
            }

            protected override void OnInsert(object key, object value)
            {
                Assert.True(OnValidateCalled);
                Assert.NotEqual(value, this[(FooKey)key]);

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

            protected override void OnRemove(object key, object value)
            {
                Assert.True(OnValidateCalled);
                Assert.Equal(value, this[(FooKey)key]);

                OnRemoveCalled = true;

                if (OnRemoveThrow)
                    throw new Exception("OnRemove");
            }

            protected override void OnValidate(object key, object value)
            {
                OnValidateCalled = true;

                if (OnValidateThrow)
                    throw new Exception("OnValidate");
            }

            protected override void OnSetComplete(object key, object oldValue, object newValue)
            {
                Assert.True(OnSetCalled);
                Assert.Equal(newValue, this[(FooKey)key]);

                OnSetCompleteCalled = true;

                if (OnSetCompleteThrow)
                    throw new Exception("OnSetComplete");
            }

            protected override void OnInsertComplete(object key, object value)
            {
                Assert.True(OnInsertCalled);
                Assert.Equal(value, this[(FooKey)key]);

                OnInsertCompleteCalled = true;

                if (OnInsertCompleteThrow)
                    throw new Exception("OnInsertComplete");
            }

            protected override void OnClearComplete()
            {
                Assert.True(OnClearCalled);

                OnClearCompleteCalled = true;

                if (OnClearCompleteThrow)
                    throw new Exception("OnClearComplete");
            }

            protected override void OnRemoveComplete(object key, object value)
            {
                Assert.True(OnRemoveCalled);
                Assert.False(Contains((FooKey)key));

                OnRemoveCompleteCalled = true;

                if (OnRemoveCompleteThrow)
                    throw new Exception("OnRemoveComplete");
            }
        }
    }
}
