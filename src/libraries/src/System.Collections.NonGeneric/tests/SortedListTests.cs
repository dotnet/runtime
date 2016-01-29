// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Tests
{
    public static class SortedListTests
    {
        [Fact]
        public static void TestCtor_Empty()
        {
            var sortList = new SortedList();
            Assert.Equal(0, sortList.Count);
            Assert.Equal(0, sortList.Capacity);

            Assert.False(sortList.IsFixedSize);
            Assert.False(sortList.IsReadOnly);
            Assert.False(sortList.IsSynchronized);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public static void TestCtor_Capacity(int capacity)
        {
            var sortList = new SortedList(capacity);
            Assert.Equal(0, sortList.Count);
            Assert.Equal(capacity, sortList.Capacity);

            Assert.False(sortList.IsFixedSize);
            Assert.False(sortList.IsReadOnly);
            Assert.False(sortList.IsSynchronized);
        }

        [Fact]
        public static void TestCtor_Capcaity_Invalid()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SortedList(-1)); // Capacity < 0
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public static void TestCtor_IComparer_Capacity(int capacity)
        {
            var sortList = new SortedList(new CustomComparer(), capacity);
            Assert.Equal(0, sortList.Count);
            Assert.Equal(capacity, sortList.Capacity);

            Assert.False(sortList.IsFixedSize);
            Assert.False(sortList.IsReadOnly);
            Assert.False(sortList.IsSynchronized);
        }

        [Fact]
        public static void TestCtor_IComparer_Capacity_Invalid()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SortedList(new CustomComparer(), -1)); // Capacity < 0
        }

        [Fact]
        public static void TestCtor_IComparer()
        {
            var sortList = new SortedList(new CustomComparer());
            Assert.Equal(0, sortList.Count);

            Assert.False(sortList.IsFixedSize);
            Assert.False(sortList.IsReadOnly);
            Assert.False(sortList.IsSynchronized);
        }

        [Fact]
        public static void Test_Ctor_IComparer_Null()
        {
            var sortList = new SortedList((IComparer)null);
            Assert.Equal(0, sortList.Count);

            Assert.False(sortList.IsFixedSize);
            Assert.False(sortList.IsReadOnly);
            Assert.False(sortList.IsSynchronized);
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(10, true)]
        [InlineData(100, true)]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(10, false)]
        [InlineData(100, false)]
        public static void TestCtor_IDictionary(int count, bool sorted)
        {
            var hashtable = new Hashtable();
            if (sorted)
            {
                // Create a hashtable in the correctly sorted order
                for (int i = 0; i < count; i++)
                {
                    hashtable.Add("Key_" + i.ToString("D2"), "Value_" + i.ToString("D2"));
                }
            }
            else
            {
                // Create a hashtable in the wrong order and make sure it is sorted
                for (int i = count - 1; i >= 0; i--)
                {
                    hashtable.Add("Key_" + i.ToString("D2"), "Value_" + i.ToString("D2"));
                }
            }

            var sortList = new SortedList(hashtable);

            Assert.Equal(count, sortList.Count);
            Assert.True(sortList.Capacity >= sortList.Count);

            for (int i = 0; i < count; i++)
            {
                string key = "Key_" + i.ToString("D2");
                string value = "Value_" + i.ToString("D2");

                Assert.Equal(sortList.GetByIndex(i), value);
                Assert.Equal(hashtable[key], sortList[key]);
            }

            Assert.False(sortList.IsFixedSize);
            Assert.False(sortList.IsReadOnly);
            Assert.False(sortList.IsSynchronized);
        }

        [Fact]
        public static void TestCtor_IDictionary_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => new SortedList((IDictionary)null)); // Dictionary is null
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(10, true)]
        [InlineData(100, true)]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(10, false)]
        [InlineData(100, false)]
        public static void TestCtor_IDictionary_IComparer(int count, bool sorted)
        {
            var hashtable = new Hashtable();
            if (sorted)
            {
                // Create a hashtable in the correctly sorted order
                for (int i = count - 1; i >= 0; i--)
                {
                    hashtable.Add("Key_" + i.ToString("D2"), "Value_" + i.ToString("D2"));
                }
            }
            else
            {
                // Create a hashtable in the wrong order and make sure it is sorted
                for (int i = 0; i < count; i++)
                {
                    hashtable.Add("Key_" + i.ToString("D2"), "Value_" + i.ToString("D2"));
                }
            }

            var sortList = new SortedList(hashtable, new CustomComparer());

            Assert.Equal(count, sortList.Count);
            Assert.True(sortList.Capacity >= sortList.Count);

            for (int i = 0; i < count; i++)
            {
                string key = "Key_" + i.ToString("D2");
                string value = "Value_" + i.ToString("D2");
                string expectedValue = "Value_" + (count - i - 1).ToString("D2");

                Assert.Equal(sortList.GetByIndex(i), expectedValue);
                Assert.Equal(hashtable[key], sortList[key]);
            }

            Assert.False(sortList.IsFixedSize);
            Assert.False(sortList.IsReadOnly);
            Assert.False(sortList.IsSynchronized);
        }

        [Fact]
        public static void TestCtor_IDictionary_IComparer_Null()
        {
            var sortList = new SortedList(new Hashtable(), null);
            Assert.Equal(0, sortList.Count);

            Assert.False(sortList.IsFixedSize);
            Assert.False(sortList.IsReadOnly);
            Assert.False(sortList.IsSynchronized);
        }

        [Fact]
        public static void TestCtor_IDictionary_IComparer_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => new SortedList(null, new CustomComparer())); // Dictionary is null
        }

        [Fact]
        public static void TestDebuggerAttribute()
        {
            var list = new SortedList() { { "a", 1 }, { "b", 2 } };

            DebuggerAttributes.ValidateDebuggerDisplayReferences(new SortedList());

            DebuggerAttributes.ValidateDebuggerTypeProxyProperties(list);
            DebuggerAttributes.ValidateDebuggerTypeProxyProperties(typeof(SortedList), SortedList.Synchronized(list));

            bool threwNull = false;
            try
            {
                DebuggerAttributes.ValidateDebuggerTypeProxyProperties(typeof(SortedList), null);
            }
            catch (TargetInvocationException ex)
            {
                ArgumentNullException nullException = ex.InnerException as ArgumentNullException;
                threwNull = nullException != null;
            }

            Assert.True(threwNull);
        }

        [Fact]
        public static void TestAdd()
        {
            var sortList1 = new SortedList();
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                for (int i = 0; i < 100; i++)
                {
                    string key = "Key_" + i.ToString("D2");
                    string value = "Value_" + i;
                    sortList2.Add(key, value);

                    Assert.True(sortList2.ContainsKey(key));
                    Assert.True(sortList2.ContainsValue(value));

                    Assert.Equal(i, sortList2.IndexOfKey(key));
                    Assert.Equal(i, sortList2.IndexOfValue(value));

                    Assert.Equal(i + 1, sortList2.Count);
                }
            });
        }

        [Fact]
        public static void TestAdd_Invalid()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                Assert.Throws<ArgumentNullException>(() => sortList2.Add(null, 101)); // Key is null

                Assert.Throws<ArgumentException>(() => sortList2.Add(1, 101)); // Key already exists
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public static void TestClear(int count)
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(count);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                sortList2.Clear();
                Assert.Equal(0, sortList2.Count);

                sortList2.Clear();
                Assert.Equal(0, sortList2.Count);
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public static void TestClone(int count)
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(count);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                SortedList sortListClone = (SortedList)sortList2.Clone();

                Assert.Equal(sortList2.Count, sortListClone.Count);
                Assert.False(sortListClone.IsSynchronized); // IsSynchronized is not copied
                Assert.Equal(sortList2.IsFixedSize, sortListClone.IsFixedSize);
                Assert.Equal(sortList2.IsReadOnly, sortListClone.IsReadOnly);
                for (int i = 0; i < sortListClone.Count; i++)
                {
                    Assert.Equal(sortList2[i], sortListClone[i]);
                }
            });
        }

        [Fact]
        public static void TestClone_IsShallowCopy()
        {
            var sortList = new SortedList();
            for (int i = 0; i < 10; i++)
            {
                sortList.Add(i, new Foo());
            }

            SortedList sortListClone = (SortedList)sortList.Clone();

            string stringValue = "Hello World";
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(stringValue, ((Foo)sortListClone[i]).StringValue);
            }

            // Now we remove an object from the original list, but this should still be present in the clone
            sortList.RemoveAt(9);
            Assert.Equal(stringValue, ((Foo)sortListClone[9]).StringValue);

            stringValue = "Good Bye";
            ((Foo)sortList[0]).StringValue = stringValue;
            Assert.Equal(stringValue, ((Foo)sortList[0]).StringValue);
            Assert.Equal(stringValue, ((Foo)sortListClone[0]).StringValue);

            // If we change the object, of course, the previous should not happen
            sortListClone[0] = new Foo();

            Assert.Equal(stringValue, ((Foo)sortList[0]).StringValue);

            stringValue = "Hello World";
            Assert.Equal(stringValue, ((Foo)sortListClone[0]).StringValue);
        }

        [Fact]
        public static void TestContainsKey()
        {
            var sortList1 = new SortedList();
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                for (int i = 0; i < 100; i++)
                {
                    string key = "Key_" + i;
                    sortList2.Add(key, i);
                    Assert.True(sortList2.Contains(key));
                    Assert.True(sortList2.ContainsKey(key));
                }

                Assert.False(sortList2.ContainsKey("Non_Existent_Key"));

                for (int i = 0; i < sortList2.Count; i++)
                {
                    string removedKey = "Key_" + i;
                    sortList2.Remove(removedKey);
                    Assert.False(sortList2.Contains(removedKey));
                    Assert.False(sortList2.ContainsKey(removedKey));
                }
            });
        }

        [Fact]
        public static void TestContainsKey_Invalid()
        {
            var sortList1 = new SortedList();
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                Assert.Throws<ArgumentNullException>(() => sortList2.Contains(null)); // Key is null
                Assert.Throws<ArgumentNullException>(() => sortList2.ContainsKey(null)); // Key is null
            });
        }

        [Fact]
        public static void TestContainsValue()
        {
            var sortList1 = new SortedList();
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                for (int i = 0; i < 100; i++)
                {
                    sortList2.Add(i, "Value_" + i);
                    Assert.True(sortList2.ContainsValue("Value_" + i));
                }

                Assert.False(sortList2.ContainsValue("Non_Existent_Value"));
                for (int i = 0; i < sortList2.Count; i++)
                {
                    sortList2.Remove(i);
                    Assert.False(sortList2.ContainsValue("Value_" + i));
                }
            });
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(10, 0)]
        [InlineData(100, 0)]
        [InlineData(0, 50)]
        [InlineData(1, 50)]
        [InlineData(10, 50)]
        [InlineData(100, 50)]
        public static void TestCopyTo(int count, int index)
        {
            SortedList sortList1 = Helpers.CreateStringSortedList(count);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                var array = new object[index + count];
                sortList2.CopyTo(array, index);

                Assert.Equal(index + count, array.Length);
                for (int i = index; i < index + count; i++)
                {
                    int actualIndex = i - index;
                    string key = "Key_" + actualIndex.ToString("D2");
                    string value = "Value_" + actualIndex;
                    DictionaryEntry entry = (DictionaryEntry)array[i];

                    Assert.Equal(key, entry.Key);
                    Assert.Equal(value, entry.Value);
                }
            });
        }

        [Fact]
        public static void TestCopyTo_Invalid()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                Assert.Throws<ArgumentNullException>(() => sortList2.CopyTo(null, 0)); // Array is null
                Assert.Throws<ArgumentException>(() => sortList2.CopyTo(new object[10, 10], 0)); // Array is multidimensional

                Assert.Throws<ArgumentOutOfRangeException>(() => sortList2.CopyTo(new object[100], -1)); // Index < 0
                Assert.Throws<ArgumentException>(() => sortList2.CopyTo(new object[150], 51)); // Index + list.Count > array.Count
            });
        }

        [Fact]
        public static void TestGetByIndex()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                for (int i = 0; i < sortList2.Count; i++)
                {
                    Assert.Equal(i, sortList2.GetByIndex(i));
                    int i2 = sortList2.IndexOfKey(i);
                    Assert.Equal(i, i2);

                    i2 = sortList2.IndexOfValue(i);
                    Assert.Equal(i, i2);
                }
            });
        }

        [Fact]
        public static void TestGetByIndex_Invalid()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => sortList2.GetByIndex(-1)); // Index < 0
                Assert.Throws<ArgumentOutOfRangeException>(() => sortList2.GetByIndex(sortList2.Count)); // Index >= list.Count
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public static void TestGetEnumerator_IDictionaryEnumerator(int count)
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(count);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                IDictionaryEnumerator enumerator1 = sortList2.GetEnumerator();
                IDictionaryEnumerator enumerator2 = sortList2.GetEnumerator();

                IDictionaryEnumerator[] enumerators = { enumerator1, enumerator2 };
                foreach (IDictionaryEnumerator enumerator in enumerators)
                {
                    Assert.NotNull(enumerator);

                    for (int i = 0; i < 2; i++)
                    {
                        int counter = 0;
                        while (enumerator.MoveNext())
                        {
                            Assert.Equal(enumerator.Current, enumerator.Entry);
                            Assert.Equal(enumerator.Entry.Key, enumerator.Key);
                            Assert.Equal(enumerator.Entry.Value, enumerator.Value);

                            Assert.Equal(sortList2.GetKey(counter), enumerator.Entry.Key);
                            Assert.Equal(sortList2.GetByIndex(counter), enumerator.Entry.Value);

                            counter++;
                        }
                        Assert.Equal(count, counter);
                        enumerator.Reset();
                    }
                }
            });
        }

        [Fact]
        public static void TestGetEnumerator_IDictionaryEnumerator_Invalid()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                // If the underlying collection is modified, MoveNext, Reset, Entry, Key and Value throw, but Current etc. doesn't
                IDictionaryEnumerator enumerator = sortList2.GetEnumerator();
                enumerator.MoveNext();
                sortList2.Add(101, 101);

                Assert.NotNull(enumerator.Current);
                Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
                Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
                Assert.Throws<InvalidOperationException>(() => enumerator.Key);
                Assert.Throws<InvalidOperationException>(() => enumerator.Value);

                // Current etc. throw if index < 0
                enumerator = sortList2.GetEnumerator();
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
                Assert.Throws<InvalidOperationException>(() => enumerator.Key);
                Assert.Throws<InvalidOperationException>(() => enumerator.Value);

                // Current etc. throw after resetting
                enumerator = sortList2.GetEnumerator();
                enumerator.MoveNext();

                enumerator.Reset();
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
                Assert.Throws<InvalidOperationException>(() => enumerator.Key);
                Assert.Throws<InvalidOperationException>(() => enumerator.Value);

                // Current etc. throw if the current index is >= count
                enumerator = sortList2.GetEnumerator();
                while (enumerator.MoveNext()) ;
                Assert.False(enumerator.MoveNext());
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
                Assert.Throws<InvalidOperationException>(() => enumerator.Key);
                Assert.Throws<InvalidOperationException>(() => enumerator.Value);
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public static void TestGetEnumerator_IEnumerator(int count)
        {
            SortedList sortList = Helpers.CreateIntSortedList(count);
            IEnumerator enumerator1 = ((IEnumerable)sortList).GetEnumerator();
            IEnumerator enumerator2 = ((IEnumerable)sortList).GetEnumerator();

            IEnumerator[] enumerators = { enumerator1, enumerator2 };
            foreach (IEnumerator enumerator in enumerators)
            {
                Assert.NotNull(enumerator);
                for (int i = 0; i < 2; i++)
                {
                    int counter = 0;
                    while (enumerator.MoveNext())
                    {
                        DictionaryEntry dictEntry = (DictionaryEntry)enumerator.Current;

                        Assert.Equal(sortList.GetKey(counter), dictEntry.Key);
                        Assert.Equal(sortList.GetByIndex(counter), dictEntry.Value);

                        counter++;
                    }
                    Assert.Equal(count, counter);
                    enumerator.Reset();
                }
            }
        }

        [Fact]
        public static void TestGetEnumerator_IEnumerator_Invalid()
        {
            SortedList sortList = Helpers.CreateIntSortedList(100);

            // If the underlying collection is modified, MoveNext, Reset, Entry, Key and Value throw, but Current doesn't
            IEnumerator enumerator = ((IEnumerable)sortList).GetEnumerator();
            enumerator.MoveNext();
            sortList.Add(101, 101);

            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Reset());

            // Current throws if index < 0
            enumerator = sortList.GetEnumerator();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            // Current throw after resetting
            enumerator = sortList.GetEnumerator();
            enumerator.MoveNext();

            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            // Current throw if the current index is >= count
            enumerator = sortList.GetEnumerator();
            while (enumerator.MoveNext()) ;
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public static void TestGetKeyList(int count)
        {
            SortedList sortList1 = Helpers.CreateStringSortedList(count);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                IList keys1 = sortList2.GetKeyList();
                IList keys2 = sortList2.GetKeyList();
                
                // Test we have copied the correct keys
                Assert.Equal(count, keys1.Count);
                Assert.Equal(count, keys2.Count);

                for (int i = 0; i < keys1.Count; i++)
                {
                    string key = "Key_" + i.ToString("D2");
                    Assert.Equal(key, keys1[i]);
                    Assert.Equal(key, keys2[i]);

                    Assert.True(sortList2.ContainsKey(keys1[i]));
                }
            });
        }

        [Fact]
        public static void TestGetKeyList_IsSameAsKeysProperty()
        {
            var sortList = Helpers.CreateIntSortedList(10);
            Assert.Same(sortList.GetKeyList(), sortList.Keys);
        }

        [Fact]
        public static void TestGetKeyList_IListProperties()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                IList keys = sortList2.GetKeyList();

                Assert.True(keys.IsReadOnly);
                Assert.True(keys.IsFixedSize);
                Assert.False(keys.IsSynchronized);
                Assert.Equal(sortList2.SyncRoot, keys.SyncRoot);
            });
        }

        [Fact]
        public static void TestGetKeyList_Contains()
        {
            SortedList sortList1 = Helpers.CreateStringSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                IList keys = sortList2.GetKeyList();

                for (int i = 0; i < keys.Count; i++)
                {
                    string key = "Key_" + i.ToString("D2");
                    Assert.True(keys.Contains(key));
                }

                Assert.False(keys.Contains("Key_101")); // No such key
            });
        }

        [Fact]
        public static void TestGetKeyList_Contains_Invalid()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                IList keys = sortList2.GetKeyList();
                Assert.Throws<InvalidOperationException>(() => keys.Contains("hello")); // Value is a different object type
            });

        }

        [Fact]
        public static void TestGetKeyList_IndexOf()
        {
            SortedList sortList1 = Helpers.CreateStringSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                IList keys = sortList2.GetKeyList();

                for (int i = 0; i < keys.Count; i++)
                {
                    string key = "Key_" + i.ToString("D2");
                    Assert.Equal(i, keys.IndexOf(key));
                }

                Assert.Equal(-1, keys.IndexOf("Key_101"));
            });
        }

        [Fact]
        public static void TestGetKeyList_IndexOf_Invalid()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                IList keys = sortList2.GetKeyList();
                Assert.Throws<ArgumentNullException>(() => keys.IndexOf(null)); // Value is null
                Assert.Throws<InvalidOperationException>(() => keys.IndexOf("hello")); // Value is a different object type
            });
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(10, 0)]
        [InlineData(100, 0)]
        [InlineData(0, 50)]
        [InlineData(1, 50)]
        [InlineData(10, 50)]
        [InlineData(100, 50)]
        public static void TestGetKeyList_CopyTo(int count, int index)
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(count);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                object[] array = new object[index + count];
                IList keys = sortList2.GetKeyList();
                keys.CopyTo(array, index);

                Assert.Equal(index + count, array.Length);
                for (int i = index; i < index + count; i++)
                {
                    Assert.Equal(keys[i - index], array[i]);
                }
            });
        }

        [Fact]
        public static void TestGetKeyList_CopyTo_Invalid()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                IList keys = sortList2.GetKeyList();
                Assert.Throws<ArgumentNullException>(() => keys.CopyTo(null, 0)); // Array is null
                Assert.Throws<ArgumentException>(() => keys.CopyTo(new object[10, 10], 0)); // Array is multidimensional

                Assert.Throws<ArgumentOutOfRangeException>(() => keys.CopyTo(new object[100], -1)); // Index < 0
                Assert.Throws<ArgumentException>(() => keys.CopyTo(new object[150], 51)); // Index + list.Count > array.Count
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public static void TestGetKeyList_GetEnumerator(int count)
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(count);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                IList keys = sortList2.GetKeyList();
                IEnumerator enumerator = sortList2.GetEnumerator();

                for (int i = 0; i < 2; i++)
                {
                    int counter = 0;
                    while (enumerator.MoveNext())
                    {
                        object key = keys[counter];
                        DictionaryEntry entry = (DictionaryEntry)enumerator.Current;
                        Assert.Equal(key, entry.Key);
                        Assert.Equal(sortList2[key], entry.Value);
                        counter++;
                    }
                    Assert.Equal(count, counter);

                    enumerator.Reset();
                }
            });
        }

        [Fact]
        public static void TestGetKeyList_GetEnumerator_Invalid()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                IList keys = sortList2.GetKeyList();
                // If the underlying collection is modified, MoveNext, Reset, Entry, Key and Value throw, but Current etc. doesn't
                IEnumerator enumerator = keys.GetEnumerator();
                enumerator.MoveNext();
                sortList2.Add(101, 101);

                Assert.NotNull(enumerator.Current);
                Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                Assert.Throws<InvalidOperationException>(() => enumerator.Reset());

                // Current etc. throw if index < 0
                enumerator = keys.GetEnumerator();
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);

                // Current etc. throw after resetting
                enumerator = keys.GetEnumerator();
                enumerator.MoveNext();

                enumerator.Reset();
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);

                // Current etc. throw if the current index is >= count
                enumerator = keys.GetEnumerator();
                while (enumerator.MoveNext()) ;
                Assert.False(enumerator.MoveNext());
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            });
        }

        [Fact]
        public static void TestGetKeyList_TryingToModifyCollection_Throws()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                IList keys = sortList2.GetKeyList();

                Assert.Throws<NotSupportedException>(() => keys.Add(101));
                Assert.Throws<NotSupportedException>(() => keys.Clear());
                Assert.Throws<NotSupportedException>(() => keys.Insert(0, 101));
                Assert.Throws<NotSupportedException>(() => keys.Remove(1));
                Assert.Throws<NotSupportedException>(() => keys.RemoveAt(0));
                Assert.Throws<NotSupportedException>(() => keys[0] = 101);
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        public static void TestGetKey(int count)
        {
            SortedList sortList1 = Helpers.CreateStringSortedList(count);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                for (int i = 0; i < count; i++)
                {
                    string key = "Key_" + i.ToString("D2");
                    Assert.Equal(key, sortList2.GetKey(sortList2.IndexOfKey(key)));
                }
            });
        }

        [Fact]
        public static void TestGetKey_Invalid()
        {
            SortedList sortList1 = Helpers.CreateStringSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => sortList2.GetKey(-1)); // Index < 0
                Assert.Throws<ArgumentOutOfRangeException>(() => sortList2.GetKey(sortList2.Count)); // Index >= count
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public static void TestGetValueList(int count)
        {
            SortedList sortList1 = Helpers.CreateStringSortedList(count);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                IList values1 = sortList2.GetValueList();
                IList values2 = sortList2.GetValueList();

                // Test we have copied the correct values
                Assert.Equal(count, values1.Count);
                Assert.Equal(count, values2.Count);

                for (int i = 0; i < values1.Count; i++)
                {
                    string value = "Value_" + i;
                    Assert.Equal(value, values1[i]);
                    Assert.Equal(value, values2[i]);

                    Assert.True(sortList2.ContainsValue(values2[i]));
                }
            });
        }

        [Fact]
        public static void TestGetValueList_IsSameAsValuesProperty()
        {
            var sortList = Helpers.CreateIntSortedList(10);
            Assert.Same(sortList.GetValueList(), sortList.Values);
        }

        [Fact]
        public static void TestGetValueList_IListProperties()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                IList values = sortList2.GetValueList();

                Assert.True(values.IsReadOnly);
                Assert.True(values.IsFixedSize);
                Assert.False(values.IsSynchronized);
                Assert.Equal(sortList2.SyncRoot, values.SyncRoot);
            });
        }

        [Fact]
        public static void TestGetValueList_Contains()
        {
            SortedList sortList1 = Helpers.CreateStringSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                IList values = sortList2.GetValueList();

                for (int i = 0; i < values.Count; i++)
                {
                    string value = "Value_" + i;
                    Assert.True(values.Contains(value));
                }

                // No such value
                Assert.False(values.Contains("Value_101"));
                Assert.False(values.Contains(101));
                Assert.False(values.Contains(null));
            });
        }

        [Fact]
        public static void TestGetValueList_IndexOf()
        {
            SortedList sortList1 = Helpers.CreateStringSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                IList values = sortList2.GetValueList();

                for (int i = 0; i < values.Count; i++)
                {
                    string value = "Value_" + i;
                    Assert.Equal(i, values.IndexOf(value));
                }

                Assert.Equal(-1, values.IndexOf(101));
            });
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(10, 0)]
        [InlineData(100, 0)]
        [InlineData(0, 50)]
        [InlineData(1, 50)]
        [InlineData(10, 50)]
        [InlineData(100, 50)]
        public static void TestGetValueList_CopyTo(int count, int index)
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(count);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                object[] array = new object[index + count];
                IList values = sortList2.GetValueList();
                values.CopyTo(array, index);

                Assert.Equal(index + count, array.Length);
                for (int i = index; i < index + count; i++)
                {
                    Assert.Equal(values[i - index], array[i]);
                }
            });
        }

        [Fact]
        public static void TestGetValueList_CopyTo_Invalid()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                IList values = sortList2.GetValueList();
                Assert.Throws<ArgumentNullException>(() => values.CopyTo(null, 0)); // Array is null
                Assert.Throws<ArgumentException>(() => values.CopyTo(new object[10, 10], 0)); // Array is multidimensional

                Assert.Throws<ArgumentOutOfRangeException>(() => values.CopyTo(new object[100], -1)); // Index < 0
                Assert.Throws<ArgumentException>(() => values.CopyTo(new object[150], 51)); // Index + list.Count > array.Count
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public static void TestGetValueList_GetEnumerator(int count)
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(count);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                IList values = sortList2.GetValueList();
                IEnumerator enumerator = sortList2.GetEnumerator();

                for (int i = 0; i < 2; i++)
                {
                    int counter = 0;
                    while (enumerator.MoveNext())
                    {
                        object key = values[counter];
                        DictionaryEntry entry = (DictionaryEntry)enumerator.Current;
                        Assert.Equal(key, entry.Key);
                        Assert.Equal(sortList2[key], entry.Value);
                        counter++;
                    }
                    Assert.Equal(count, counter);

                    enumerator.Reset();
                }
            });
        }

        [Fact]
        public static void TestValueList_GetEnumerator_Invalid()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                IList values = sortList2.GetValueList();
                // If the underlying collection is modified, MoveNext, Reset, Entry, Key and Value throw, but Current etc. doesn't
                IEnumerator enumerator = values.GetEnumerator();
                enumerator.MoveNext();
                sortList2.Add(101, 101);

                Assert.NotNull(enumerator.Current);
                Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                Assert.Throws<InvalidOperationException>(() => enumerator.Reset());

                // Current etc. throw if index < 0
                enumerator = values.GetEnumerator();
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);

                // Current etc. throw after resetting
                enumerator = values.GetEnumerator();
                enumerator.MoveNext();

                enumerator.Reset();
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);

                // Current etc. throw if the current index is >= count
                enumerator = values.GetEnumerator();
                while (enumerator.MoveNext()) ;
                Assert.False(enumerator.MoveNext());
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            });
        }

        [Fact]
        public static void TestGetValueList_TryingToModifyCollection_Throws()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                IList values = sortList2.GetValueList();

                Assert.Throws<NotSupportedException>(() => values.Add(101));
                Assert.Throws<NotSupportedException>(() => values.Clear());
                Assert.Throws<NotSupportedException>(() => values.Insert(0, 101));
                Assert.Throws<NotSupportedException>(() => values.Remove(1));
                Assert.Throws<NotSupportedException>(() => values.RemoveAt(0));
                Assert.Throws<NotSupportedException>(() => values[0] = 101);
            });
        }

        [Fact]
        public static void TestIndexOfKey()
        {
            SortedList sortList1 = Helpers.CreateStringSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                for (int i = 0; i < sortList2.Count; i++)
                {
                    string key = "Key_" + i.ToString("D2");
                    string value = "Value_" + i;

                    int index = sortList2.IndexOfKey(key);
                    Assert.Equal(i, index);
                    Assert.Equal(value, sortList2.GetByIndex(index));
                }

                Assert.Equal(-1, sortList2.IndexOfKey("Non Existent Key"));

                string removedKey = "Key_01";
                sortList2.Remove(removedKey);
                Assert.Equal(-1, sortList2.IndexOfKey(removedKey));
            });
        }

        [Fact]
        public static void TestIndexOfKey_Invalid()
        {
            SortedList sortList1 = Helpers.CreateStringSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                Assert.Throws<ArgumentNullException>(() => sortList2.IndexOfKey(null)); // Key is null
            });
        }

        [Fact]
        public static void TestIndexOfValue()
        {
            SortedList sortList1 = Helpers.CreateStringSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                for (int i = 0; i < sortList2.Count; i++)
                {
                    string value = "Value_" + i;

                    int index = sortList2.IndexOfValue(value);
                    Assert.Equal(i, index);
                    Assert.Equal(value, sortList2.GetByIndex(index));
                }

                Assert.Equal(-1, sortList2.IndexOfValue("Non Existent Value"));

                string removedKey = "Key_01";
                string removedValue = "Value_1";
                sortList2.Remove(removedKey);
                Assert.Equal(-1, sortList2.IndexOfValue(removedValue));

                Assert.Equal(-1, sortList2.IndexOfValue(null));
                sortList2.Add("Key_101", null);
                Assert.NotEqual(-1, sortList2.IndexOfValue(null));
            });
        }

        [Fact]
        public static void TestIndexOfValue_SameValue()
        {
            var sortList1 = new SortedList();
            sortList1.Add("Key_0", "Value_0");
            sortList1.Add("Key_1", "Value_Same");
            sortList1.Add("Key_2", "Value_Same");
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                Assert.Equal(1, sortList2.IndexOfValue("Value_Same"));
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(4)]
        [InlineData(5000)]
        public static void TestGetSetCapacity(int capacity)
        {
            var sortList = new SortedList();
            sortList.Capacity = capacity;
            Assert.Equal(capacity, sortList.Capacity);

            // Ensure nothing changes if we set capacity to the same value again
            sortList.Capacity = capacity;
            Assert.Equal(capacity, sortList.Capacity);
        }

        [Fact]
        public static void TestSetCapacity_Shrink()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => sortList2.Capacity = sortList2.Count - 1); // Capacity < count
            });
        }

        [Fact]
        public static void TestSetCapacity_Invalid()
        {
            var sortList1 = new SortedList();
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => sortList2.Capacity = -1); // Capacity < 0
                Assert.Throws<OutOfMemoryException>(() => sortList2.Capacity = int.MaxValue); // Capacity is too large
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        public static void TestGetObject(int count)
        {
            SortedList sortList1 = Helpers.CreateStringSortedList(count);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                for (int i = 0; i < count; i++)
                {
                    string key = "Key_" + i.ToString("D2");
                    string value = "Value_" + i;
                    Assert.Equal(value, sortList2[key]);
                }
                Assert.Null(sortList2["No Such Key"]);

                string removedKey = "Key_01";
                sortList2.Remove(removedKey);
                Assert.Null(sortList2[removedKey]);
            });
        }

        [Fact]
        public static void TestGetObject_DifferentCulture()
        {
            var sortList = new SortedList();

            CultureInfo currentCulture = CultureInfo.DefaultThreadCurrentCulture;
            try
            {
                CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
                var cultureNames = new string[]
                {
                    "cs-CZ","da-DK","de-DE","el-GR","en-US",
                    "es-ES","fi-FI","fr-FR","hu-HU","it-IT",
                    "ja-JP","ko-KR","nb-NO","nl-NL","pl-PL",
                    "pt-BR","pt-PT","ru-RU","sv-SE","tr-TR",
                    "zh-CN","zh-HK","zh-TW"
                };

                var installedCultures = new CultureInfo[cultureNames.Length];
                var cultureDisplayNames = new string[installedCultures.Length];
                int uniqueDisplayNameCount = 0;

                foreach (string cultureName in cultureNames)
                {
                    var culture = new CultureInfo(cultureName);
                    installedCultures[uniqueDisplayNameCount] = culture;
                    cultureDisplayNames[uniqueDisplayNameCount] = culture.DisplayName;
                    sortList.Add(cultureDisplayNames[uniqueDisplayNameCount], culture);

                    uniqueDisplayNameCount++;
                }

                // In Czech ch comes after h if the comparer changes based on the current culture of the thread
                // we will not be able to find some items
                CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("cs-CZ");

                for (int i = 0; i < uniqueDisplayNameCount; i++)
                {
                    Assert.Equal(installedCultures[i], sortList[installedCultures[i].DisplayName]);
                }
            }
            catch (CultureNotFoundException)
            {
            }
            finally
            {
                CultureInfo.DefaultThreadCurrentCulture = currentCulture;
            }
        }

        [Fact]
        public static void TestSetObject()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                // Change existing keys
                for (int i = 0; i < sortList2.Count; i++)
                {
                    sortList2[i] = i + 1;
                    Assert.Equal(i + 1, sortList2[i]);

                    // Make sure nothing bad happens when we try to set the key to its current valeu
                    sortList2[i] = i + 1;
                    Assert.Equal(i + 1, sortList2[i]);
                }

                // Add new keys
                sortList2[101] = 2048;
                Assert.Equal(2048, sortList2[101]);

                sortList2[102] = null;
                Assert.Equal(null, sortList2[102]);
            });
        }

        [Fact]
        public static void TestSetObject_Invalid()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                Assert.Throws<ArgumentNullException>(() => sortList2[null] = 101); // Key is null
            });
        }

        [Fact]
        public static void TestRemoveAt()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                // Remove from end
                for (int i = sortList2.Count - 1; i >= 0; i--)
                {
                    sortList2.RemoveAt(i);
                    Assert.False(sortList2.ContainsKey(i));
                    Assert.False(sortList2.ContainsValue(i));
                    Assert.Equal(i, sortList2.Count);
                }
            });
        }

        [Fact]
        public static void TestRemoveAt_Invalid()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => sortList2.RemoveAt(-1)); // Index < 0
                Assert.Throws<ArgumentOutOfRangeException>(() => sortList2.RemoveAt(sortList2.Count)); // Index >= count
            });
        }

        [Fact]
        public static void TestRemove()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                // Remove from the end
                for (int i = sortList2.Count - 1; i >= 0; i--)
                {
                    sortList2.Remove(i);
                    Assert.False(sortList2.ContainsKey(i));
                    Assert.False(sortList2.ContainsValue(i));
                    Assert.Equal(i, sortList2.Count);
                }

                sortList2.Remove(101); // No such key
            });
        }

        [Fact]
        public static void TestRemove_Invalid()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                Assert.Throws<ArgumentNullException>(() => sortList2.Remove(null)); // Key is null
            });
        }

        [Fact]
        public static void TestSetByIndex()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                for (int i = 0; i < sortList2.Count; i++)
                {
                    sortList2.SetByIndex(i, i + 1);
                    Assert.Equal(i + 1, sortList2.GetByIndex(i));
                }
            });
        }

        [Fact]
        public static void TestSetByIndex_Invalid()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => sortList2.SetByIndex(-1, 101)); // Index < 0
                Assert.Throws<ArgumentOutOfRangeException>(() => sortList2.SetByIndex(sortList2.Count, 101)); // Index >= list.Count
            });
        }
        
        [Fact]
        public static void TestSynchronized_IsSynchronized()
        {
            SortedList sortList = SortedList.Synchronized(new SortedList());
            Assert.True(sortList.IsSynchronized);
        }

        [Fact]
        public static void TestSynchronized_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => SortedList.Synchronized(null)); // List is null
        }

        [Fact]
        public static void TestTrimToSize()
        {
            SortedList sortList1 = Helpers.CreateIntSortedList(100);
            Helpers.PerformActionOnAllSortedListWrappers(sortList1, sortList2 =>
            {
                for (int i = 0; i < 10; i++)
                {
                    sortList2.RemoveAt(0);
                }
                sortList2.TrimToSize();
                Assert.Equal(sortList2.Count, sortList2.Capacity);

                sortList2.Clear();
                sortList2.TrimToSize();
                Assert.Equal(0, sortList2.Capacity);
            });
        }

        private class Foo
        {
            private string _stringValue = "Hello World";
            public string StringValue
            {
                get { return _stringValue; }
                set { _stringValue = value; }
            }
        }

        private class CustomComparer : IComparer
        {
            public int Compare(object obj1, object obj2)
            {
                return -string.Compare(obj1.ToString(), obj2.ToString());
            }
        }
    }

    public class SortedList_SyncRootTests
    {
        private SortedList _sortListDaughter;
        private SortedList _sortListGrandDaughter;
        private int _numElements = 100;

        [Fact]
        [OuterLoop]
        public void TestGetSyncRootBasic()
        {
            // Testing SyncRoot is not as simple as its implementation looks like. This is the working
            // scenrio we have in mind.
            // 1) Create your Down to earth mother SortedList
            // 2) Get a synchronized wrapper from it
            // 3) Get a Synchronized wrapper from 2)
            // 4) Get a synchronized wrapper of the mother from 1)
            // 5) all of these should SyncRoot to the mother earth

            var sortListMother = new SortedList();
            for (int i = 0; i < _numElements; i++)
            {
                sortListMother.Add("Key_" + i, "Value_" + i);
            }

            Assert.Equal(sortListMother.SyncRoot.GetType(), typeof(object));

            SortedList sortListSon = SortedList.Synchronized(sortListMother);
            _sortListGrandDaughter = SortedList.Synchronized(sortListSon);
            _sortListDaughter = SortedList.Synchronized(sortListMother);

            Assert.Equal(sortListSon.SyncRoot, sortListMother.SyncRoot);
            Assert.Equal(sortListMother.SyncRoot, sortListSon.SyncRoot);

            Assert.Equal(_sortListGrandDaughter.SyncRoot, sortListMother.SyncRoot);
            Assert.Equal(_sortListDaughter.SyncRoot, sortListMother.SyncRoot);
            Assert.Equal(sortListSon.SyncRoot, sortListMother.SyncRoot);

            //we are going to rumble with the SortedLists with some threads
            
            var workers = new Task[4];
            for (int i = 0; i < workers.Length; i += 2)
            {
                var name = "Thread_worker_" + i;
                var action1 = new Action(() => AddMoreElements(name));
                var action2 = new Action(RemoveElements);

                workers[i] = Task.Run(action1);
                workers[i + 1] = Task.Run(action2);
            }

            Task.WaitAll(workers);

            // Checking time
            // Now lets see how this is done.
            // Either there are some elements or none
            var sortListPossible = new SortedList();
            for (int i = 0; i < _numElements; i++)
            {
                sortListPossible.Add("Key_" + i, "Value_" + i);
            }
            for (int i = 0; i < workers.Length; i++)
            {
                sortListPossible.Add("Key_Thread_worker_" + i, "Thread_worker_" + i);
            }

            //lets check the values if
            IDictionaryEnumerator enumerator = sortListMother.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.True(sortListPossible.ContainsKey(enumerator.Key));
                Assert.True(sortListPossible.ContainsValue(enumerator.Value));
            }
        }

        private void AddMoreElements(string threadName)
        {
            _sortListGrandDaughter.Add("Key_" + threadName, threadName);
        }

        private void RemoveElements()
        {
            _sortListDaughter.Clear();
        }
    }
}
