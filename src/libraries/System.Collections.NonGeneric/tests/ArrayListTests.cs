// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Tests
{
    public static class ArrayListTests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var arrList = new ArrayList();
            Assert.Equal(0, arrList.Count);
            Assert.Equal(0, arrList.Capacity);

            Assert.False(arrList.IsFixedSize);
            Assert.False(arrList.IsReadOnly);
            Assert.False(arrList.IsSynchronized);
        }

        [Theory]
        [InlineData(16)]
        [InlineData(0)]
        public static void Ctor_Int(int capacity)
        {
            var arrList = new ArrayList(capacity);
            Assert.Equal(capacity, arrList.Capacity);

            Assert.False(arrList.IsFixedSize);
            Assert.False(arrList.IsReadOnly);
            Assert.False(arrList.IsSynchronized);
        }

        [Fact]
        public static void Ctor_Int_NegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => new ArrayList(-1)); // Capacity < 0
        }

        [Fact]
        public static void Ctor_ICollection()
        {
            ArrayList sourceList = Helpers.CreateIntArrayList(100);
            var arrList = new ArrayList(sourceList);

            Assert.Equal(sourceList.Count, arrList.Count);
            for (int i = 0; i < arrList.Count; i++)
            {
                Assert.Equal(sourceList[i], arrList[i]);
            }

            Assert.False(arrList.IsFixedSize);
            Assert.False(arrList.IsReadOnly);
            Assert.False(arrList.IsSynchronized);
        }

        [Fact]
        public static void Ctor_ICollection_Empty()
        {
            ICollection arrListCollection = new ArrayList();
            ArrayList arrList = new ArrayList(arrListCollection);

            Assert.Equal(0, arrList.Count);

            Assert.False(arrList.IsFixedSize);
            Assert.False(arrList.IsReadOnly);
            Assert.False(arrList.IsSynchronized);
        }

        [Fact]
        public static void Ctor_ICollection_NullCollection_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("c", () => new ArrayList(null)); // Collection is null
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Cannot do DebuggerAttribute testing on UapAot: requires internal Reflection on framework types.")]
        public static void DebuggerAttribute()
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(new ArrayList());
            DebuggerAttributes.ValidateDebuggerTypeProxyProperties(new ArrayList() { "a", 1, "b", 2 });

            bool threwNull = false;
            try
            {
                DebuggerAttributes.ValidateDebuggerTypeProxyProperties(typeof(ArrayList), null);
            }
            catch (TargetInvocationException ex)
            {
                threwNull = ex.InnerException is ArgumentNullException;
            }

            Assert.True(threwNull);
        }

        [Fact]
        public static void Adapter_ArrayList()
        {
            ArrayList arrList = ArrayList.Adapter(new ArrayList());
            Assert.False(arrList.IsFixedSize);
            Assert.False(arrList.IsReadOnly);
            Assert.False(arrList.IsSynchronized);
        }

        [Fact]
        public static void Adapter_FixedSizeArrayList()
        {
            ArrayList arrList = ArrayList.Adapter(ArrayList.FixedSize(new ArrayList()));
            Assert.True(arrList.IsFixedSize);
            Assert.False(arrList.IsReadOnly);
            Assert.False(arrList.IsSynchronized);
        }

        [Fact]
        public static void Adapter_ReadOnlyArrayList()
        {
            ArrayList arrList = ArrayList.Adapter(ArrayList.ReadOnly(new ArrayList()));
            Assert.True(arrList.IsFixedSize);
            Assert.True(arrList.IsReadOnly);
            Assert.False(arrList.IsSynchronized);
        }

        [Fact]
        public static void Adapter_SynchronizedArrayList()
        {
            ArrayList arrList = ArrayList.Adapter(ArrayList.Synchronized(new ArrayList()));
            Assert.False(arrList.IsFixedSize);
            Assert.False(arrList.IsReadOnly);
            Assert.True(arrList.IsSynchronized);
        }

        [Fact]
        public static void Adapter_PopulateChangesToList()
        {
            const string FromBefore = " from before";

            // Make sure changes through listAdapter show up in list
            ArrayList arrList = Helpers.CreateStringArrayList(count: 10, optionalString: FromBefore);
            ArrayList adapter = ArrayList.Adapter(arrList);
            adapter.Reverse(0, adapter.Count);

            int j = 9;
            for (int i = 0; i < adapter.Count; i++)
            {
                Assert.Equal(j.ToString() + FromBefore, adapter[i]);
                j--;
            }
        }

        [Fact]
        public static void Adapter_ClearList()
        {
            // Make sure changes through list show up in listAdapter
            ArrayList arrList = Helpers.CreateIntArrayList(100);
            ArrayList adapter = ArrayList.Adapter(arrList);
            arrList.Clear();
            Assert.Equal(0, adapter.Count);
        }

        [Fact]
        public static void Adapter_Enumerators()
        {
            // Test to see if enumerators are correctly enumerate through elements
            ArrayList arrList = Helpers.CreateIntArrayList(10);
            IEnumerator enumeratorBasic = arrList.GetEnumerator();
            ArrayList adapter = ArrayList.Adapter(arrList);
            IEnumerator enumeratorWrapped = arrList.GetEnumerator();

            int j = 0;
            while (enumeratorBasic.MoveNext())
            {
                Assert.Equal(j, enumeratorBasic.Current);
                j++;
            }

            j = 0;
            while (enumeratorWrapped.MoveNext())
            {
                Assert.Equal(j, enumeratorWrapped.Current);
                j++;
            }
        }

        [Fact]
        public static void Adapter_EnumeratorsModifiedList()
        {
            // Test to see if enumerators are correctly getting invalidated with list modified through list
            ArrayList arrList = Helpers.CreateIntArrayList(10);
            IEnumerator enumeratorBasic = arrList.GetEnumerator();
            ArrayList adapter = ArrayList.Adapter(arrList);
            IEnumerator numeratorWrapped = arrList.GetEnumerator();
            
            enumeratorBasic.MoveNext();
            numeratorWrapped.MoveNext();

            // Now modify list through arrList
            arrList.Add(100);

            // Make sure accessing enumeratorBasic and enuemratorWrapped throws
            Assert.Throws<InvalidOperationException>(() => enumeratorBasic.MoveNext());
            Assert.Throws<InvalidOperationException>(() => numeratorWrapped.MoveNext());
        }

        [Fact]
        public static void Adapter_EnumeratorsModifiedAdapter()
        {
            // Test to see if enumerators are correctly getting invalidated with list modified through listAdapter
            ArrayList arrList = Helpers.CreateStringArrayList(10);
            IEnumerator enumeratorBasic = arrList.GetEnumerator();
            ArrayList adapter = ArrayList.Adapter(arrList);
            IEnumerator enumeratorWrapped = arrList.GetEnumerator();
            
            enumeratorBasic.MoveNext();
            enumeratorWrapped.MoveNext();

            // Now modify list through adapter
            adapter.Add("Hey this is new element");

            // Make sure accessing enumeratorBasic and enuemratorWrapped throws
            Assert.Throws<InvalidOperationException>(() => enumeratorBasic.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumeratorWrapped.MoveNext());
        }

        [Fact]
        public static void Adapter_InsertRange()
        {
            // Test to see if listAdaptor modified using InsertRange works
            // Populate the list
            ArrayList arrList = Helpers.CreateIntArrayList(10);
            ArrayList adapter = ArrayList.Adapter(arrList);

            // Now add a few more elements using InsertRange
            ArrayList arrListSecond = Helpers.CreateIntArrayList(10);
            adapter.InsertRange(adapter.Count, arrListSecond);

            Assert.Equal(20, adapter.Count);
        }
        
        [Fact]
        public static void Adapter_Capacity_Set()
        {
            ArrayList arrList = Helpers.CreateIntArrayList(10);
            ArrayList adapter = ArrayList.Adapter(arrList);

            adapter.Capacity = 10;
            Assert.Equal(10, adapter.Capacity);
        }

        [Fact]
        public static void Adapter_NullList_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("list", () => ArrayList.Adapter(null)); // List is null
        }

        [Fact]
        public static void AddRange_Basic()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            ArrayList arrList2 = Helpers.CreateIntArrayList(20, 10);

            VerifyAddRange(arrList1, arrList2);
        }

        [Fact]
        public static void AddRange_DifferentCollection()
        {
            ArrayList arrList = Helpers.CreateIntArrayList(10);
            Queue queue = new Queue();
            for (int i = 10; i < 20; i++)
            {
                queue.Enqueue(i);
            }
            VerifyAddRange(arrList, queue);
        }

        [Fact]
        public static void AddRange_Self()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsFixedSize)
                {
                    return;
                }

                arrList2.AddRange(arrList2);
                for (int i = 0; i < arrList2.Count / 2; i++)
                {
                    Assert.Equal(i, arrList2[i]);
                }

                for (int i = arrList2.Count / 2; i < arrList2.Count; i++)
                {
                    Assert.Equal(i - arrList2.Count / 2, arrList2[i]);
                }
            });
        }

        private static void VerifyAddRange(ArrayList arrList1, ICollection c)
        {
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsFixedSize)
                {
                    return;
                }

                int expectedCount = arrList2.Count + c.Count;
                arrList2.AddRange(c);

                Assert.Equal(expectedCount, arrList2.Count);
                for (int i = 0; i < arrList2.Count; i++)
                {
                    Assert.Equal(i, arrList2[i]);
                }
                // Assumes that the array list and collection contain integer types 
                // and the first item in the collection is the count of the array list
                for (int i = arrList2.Count; i < c.Count; i++)
                {
                    Assert.Equal(i, arrList2[i]);
                }
            });
        }

        [Fact]
        public static void AddRange_DifferentObjectTypes()
        {
            // Add an ICollection with different type objects
            ArrayList arrList1 = Helpers.CreateIntArrayList(10); // Array list contains only integers currently
            var queue = new Queue(); // Queue contains strings
            for (int i = 10; i < 20; i++)
                queue.Enqueue("String_" + i);

            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsFixedSize)
                {
                    return;
                }

                arrList2.AddRange(queue);

                for (int i = 0; i < 10; i++)
                {
                    Assert.Equal(i, arrList2[i]);
                }

                for (int i = 10; i < 20; i++)
                {
                    Assert.Equal("String_" + i, arrList2[i]);
                }
            });
        }

        [Fact]
        public static void AddRange_EmptyCollection()
        {
            var emptyCollection = new Queue();
            ArrayList arrList1 = Helpers.CreateIntArrayList(100);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsFixedSize)
                {
                    return;
                }

                arrList2.AddRange(emptyCollection);
                Assert.Equal(100, arrList2.Count);
            });
        }

        [Fact]
        public static void AddRange_NullCollection_ThrowsArgumentNullException()
        {
            var arrList1 = new ArrayList();
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsFixedSize)
                {
                    return;
                }

                AssertExtensions.Throws<ArgumentNullException>("c", () => arrList2.AddRange(null)); // Collection is null
            });
        }
        
        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public static void Add_SmallCapacity(int count)
        {
            ArrayList arrList1 = new ArrayList(1);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsFixedSize)
                {
                    return;
                }

                for (int i = 0; i < count; i++)
                {
                    arrList2.Add(i);
                    Assert.Equal(i, arrList2[i]);
                    Assert.Equal(i + 1, arrList2.Count);
                    Assert.True(arrList2.Capacity >= arrList2.Count);
                }

                Assert.Equal(count, arrList2.Count);

                for (int i = 0; i < count; i++)
                {
                    arrList2.RemoveAt(0);
                }
                Assert.Equal(0, arrList2.Count);
            });
        }

        [Fact]
        public static void BinarySearch_Basic()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                foreach (object value in arrList2)
                {
                    int ndx = arrList2.BinarySearch(value);
                    Assert.Equal(arrList2[ndx], value);
                }
            });
        }

        [Fact]
        public static void BinarySearch_Basic_NotFoundReturnsNextElementIndex()
        {
            // The zero-based index of the value in the sorted ArrayList, if value is found; otherwise, a negative number,
            // which is the bitwise complement of the index of the next element.
            ArrayList arrList1 = Helpers.CreateIntArrayList(100);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                Assert.Equal(100, ~arrList2.BinarySearch(150));

                // Searching for null items should return -1.
                Assert.Equal(-1, arrList2.BinarySearch(null));
            });
        }

        [Fact]
        public static void BinarySearch_Basic_NullObject()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(100);
            arrList1.Add(null);
            arrList1.Sort();
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                Assert.Equal(0, arrList2.BinarySearch(null));
            });
        }

        [Fact]
        public static void BinarySearch_Basic_DuplicateResults()
        {
            // If we have duplicate results, return the first.
            var arrList1 = new ArrayList();
            for (int i = 0; i < 100; i++)
                arrList1.Add(5);

            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                // Remember, this is BinarySearch.
                Assert.Equal(49, arrList2.BinarySearch(5));
            });
        }

        [Fact]
        public static void BinarySearch_IComparer()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                foreach (object value in arrList1)
                {
                    int ndx = arrList2.BinarySearch(value, new BinarySearchComparer());
                    Assert.Equal(arrList2[ndx], value);
                }
            });
        }

        [Fact]
        public static void BinarySearch_IComparer_NotFoundReturnsNextElementIndex()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(100);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                Assert.Equal(100, ~arrList2.BinarySearch(150, new BinarySearchComparer()));

                // Searching for null items should return -1.
                Assert.Equal(-1, arrList2.BinarySearch(null, new BinarySearchComparer()));
            });
        }

        [Fact]
        public static void BinarySearch_IComparer_NullObject()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(100);
            arrList1.Add(null);
            arrList1.Sort();
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                Assert.Equal(0, arrList2.BinarySearch(null));
            });
        }

        [Fact]
        public static void BinarySearch_IComparer_DuplicateResults()
        {
            // If we have duplicate results, return the first.
            var arrList1 = new ArrayList();
            for (int i = 0; i < 100; i++)
                arrList1.Add(5);

            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                // Remember, this is BinarySearch.
                Assert.Equal(49, arrList2.BinarySearch(5, new BinarySearchComparer()));
            });
        }

        [Fact]
        public static void BinarySearch_Int_Int_IComparer()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                foreach (object value in arrList2)
                {
                    int ndx = arrList2.BinarySearch(0, arrList2.Count, value, new BinarySearchComparer());
                    Assert.Equal(arrList2[ndx], value);
                }
            });
        }

        [Fact]
        public static void BinarySearch_Int_Int_IComparer_ObjectOutsideIndex()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                // Index > list.IndexOf(object)
                int ndx = arrList2.BinarySearch(1, arrList2.Count - 1, 0, new BinarySearchComparer());
                Assert.Equal(-2, ndx);

                // Index + count < list.IndexOf(object)
                ndx = arrList2.BinarySearch(0, arrList2.Count - 2, 9, new BinarySearchComparer());
                Assert.Equal(-9, ndx);
            });
        }

        [Fact]
        public static void BinarySearch_Int_Int_IComparer_NullComparer()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                // Locating item in list using a null comparer uses default comparer.
                int ndx1 = arrList2.BinarySearch(0, arrList2.Count, 5, null);
                int ndx2 = arrList2.BinarySearch(5, null);
                int ndx3 = arrList2.BinarySearch(5);
                Assert.Equal(ndx1, ndx2);
                Assert.Equal(ndx1, ndx3);
                Assert.Equal(5, ndx1);
            });
        }

        [Fact]
        public static void BinarySearch_Int_Int_IComparer_Invalid()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                IComparer comparer = new BinarySearchComparer();

                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => arrList2.BinarySearch(-1, 1000, arrList2.Count, comparer)); // Index < 0
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => arrList2.BinarySearch(-1, 1000, 1, comparer)); // Index < 0
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => arrList2.BinarySearch(-1, arrList2.Count, 1, comparer)); // Index < 0
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => arrList2.BinarySearch(0, -1, 1, comparer)); // Count < 0

                AssertExtensions.Throws<ArgumentException>(null, () => arrList2.BinarySearch(1, arrList2.Count, 1, comparer)); // Index + Count >= list.Count
                AssertExtensions.Throws<ArgumentException>(null, () => arrList2.BinarySearch(3, arrList2.Count - 2, 1, comparer)); // Index + Count >= list.Count
            });
        }

        [Fact]
        public static void Capacity_Get()
        {
            var arrList = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList, arrList2 =>
            {
                Assert.True(arrList2.Capacity >= arrList2.Count);
            });
        }

        [Fact]
        public static void Capacity_Set()
        {
            var arrList = Helpers.CreateIntArrayList(10);
            int nCapacity = 2 * arrList.Capacity;
            arrList.Capacity = nCapacity;
            Assert.Equal(nCapacity, arrList.Capacity);

            // Synchronized 
            arrList = ArrayList.Synchronized(new ArrayList(Helpers.CreateIntArray(10)));
            arrList.Capacity = 1000;
            Assert.Equal(1000, arrList.Capacity);

            // Range ignores setter
            arrList = new ArrayList(Helpers.CreateIntArray(10)).GetRange(0, arrList.Count);
            arrList.Capacity = 1000;
            Assert.NotEqual(1000, arrList.Capacity);
        }

        [Fact]
        public static void Capacity_Set_Zero()
        {
            var arrList = new ArrayList(1);

            arrList.Capacity = 0;
            Assert.Equal(4, arrList.Capacity);

            for (int i = 0; i < 32; i++)
                arrList.Add(i);

            for (int i = 0; i < 32; i++)
                Assert.Equal(i, arrList[i]);
        }

        [Fact]
        public static void Capacity_Set_One()
        {
            var arrList = new ArrayList(4);

            arrList.Capacity = 1;
            Assert.Equal(1, arrList.Capacity);

            for (int i = 0; i < 32; i++)
                arrList.Add(i);

            for (int i = 0; i < 32; i++)
                Assert.Equal(i, arrList[i]);
        }

        [Fact]
        public static void Capacity_Set_InvalidValue_ThrowsArgumentOutOfRangeException()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsFixedSize)
                {
                    return;
                }

                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => arrList2.Capacity = -1); // Capacity < 0
                AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => arrList2.Capacity = arrList1.Count - 1); // Capacity < list.Count
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public static void Clone(int count)
        {
            // Clone should exactly replicate a collection to another object reference
            // afterwards these 2 should not hold the same object references
            ArrayList arrList1 = Helpers.CreateIntArrayList(count);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                ArrayList clone = (ArrayList)arrList2.Clone();

                Assert.Equal(arrList2.Count, clone.Count);

                Assert.Equal(arrList2.IsReadOnly, clone.IsReadOnly);
                Assert.Equal(arrList2.IsSynchronized, clone.IsSynchronized);
                Assert.Equal(arrList2.IsFixedSize, clone.IsFixedSize);

                for (int i = 0; i < arrList2.Count; i++)
                {
                    Assert.Equal(arrList2[i], clone[i]);
                }
            });
        }

        [Fact]
        public static void Clone_IsShallowCopy()
        {
            var arrList = new ArrayList();
            for (int i = 0; i < 10; i++)
            {
                arrList.Add(new Foo());
            }

            ArrayList clone = (ArrayList)arrList.Clone();

            string stringValue = "Hello World";
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(stringValue, ((Foo)clone[i]).StringValue);
            }

            // Now we remove an object from the original list, but this should still be present in the clone
            arrList.RemoveAt(9);
            Assert.Equal(stringValue, ((Foo)clone[9]).StringValue);

            stringValue = "Good Bye";
            ((Foo)arrList[0]).StringValue = stringValue;
            Assert.Equal(stringValue, ((Foo)arrList[0]).StringValue);
            Assert.Equal(stringValue, ((Foo)clone[0]).StringValue);

            // If we change the object, of course, the previous should not happen
            clone[0] = new Foo();

            stringValue = "Good Bye";
            Assert.Equal(stringValue, ((Foo)arrList[0]).StringValue);

            stringValue = "Hello World";
            Assert.Equal(stringValue, ((Foo)clone[0]).StringValue);
        }
        
        [Fact]
        public static void CopyTo_Int()
        {
            int index = 1;
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                var arrCopy = new int[arrList2.Count + index];
                arrCopy.SetValue(400, 0);
                arrList2.CopyTo(arrCopy, index);
                Assert.Equal(arrList2.Count + index, arrCopy.Length);

                for (int i = 0; i < arrCopy.Length; i++)
                {
                    if (i == 0)
                    {
                        Assert.Equal(400, arrCopy.GetValue(i));
                    }
                    else
                    {
                        Assert.Equal(arrList2[i - 1], arrCopy.GetValue(i));
                    }
                }
            });
        }

        [Fact]
        public static void CopyTo_Int_EqualToLength()
        {
            var arrList1 = new ArrayList();
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                var arrCopy = new string[2];
                arrList2.CopyTo(arrCopy, arrCopy.Length); // Should not throw
            });
        }

        [Fact]
        public static void CopyTo_Int_EmptyArrayListToFilledArray()
        {
            var arrList1 = new ArrayList();
            var arrCopy = new string[10];
            for (int i = 0; i < arrCopy.Length; i++)
            {
                arrCopy[i] = "a";
            }
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                arrList2.CopyTo(arrCopy, 3);

                // Make sure sentinels stay the same
                for (int i = 0; i < arrCopy.Length; i++)
                {
                    Assert.Equal("a", arrCopy[i]);
                }
            });
        }

        [Fact]
        public static void CopyTo_Int_EmptyArray()
        {
            var arrList1 = new ArrayList();
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                var arrCopy = new string[0];
                arrList2.CopyTo(arrCopy, 0);
                Assert.Equal(0, arrCopy.Length);
            });
        }

        [Fact]
        public static void CopyTo_Int_Invalid()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                var arrCopy = new int[arrList2.Count];

                Assert.Throws<ArgumentNullException>(() => arrList2.CopyTo(null)); // Array is null
                AssertExtensions.Throws<ArgumentException>("array", null, () => arrList2.CopyTo(new object[10, 10])); // Array is multidimensional

                Assert.Throws<ArgumentOutOfRangeException>(() => arrList2.CopyTo(arrCopy, -1)); // Index < 0

                bool hasParamName = arrList2.GetType().Name != "Range";
                AssertExtensions.Throws<ArgumentException>(hasParamName ? "destinationArray" : null, hasParamName ? "" : null, () => arrList2.CopyTo(new object[11], 2)); // Invalid index and length
            });
        }

        [Fact]
        public static void CopyTo_Int_Int()
        {
            int index = 3;
            int count = 3;
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                var arrCopy = new int[100];
                arrList2.CopyTo(index, arrCopy, index, count);
                Assert.Equal(100, arrCopy.Length);
                for (int i = index; i < index + count; i++)
                {
                    Assert.Equal(arrList2[i], arrCopy[i]);
                }
            });
        }

        [Fact]
        public static void CopyTo_Int_Int_EmptyArrayListToFilledArray()
        {
            var arrList1 = new ArrayList();
            var arrCopy = new string[10];
            for (int i = 0; i < arrCopy.Length; i++)
            {
                arrCopy[i] = "a";
            }
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                arrList2.CopyTo(0, arrCopy, 3, 0);

                // Make sure sentinels stay the same
                for (int i = 0; i < arrCopy.Length; i++)
                {
                    Assert.Equal("a", arrCopy[i]);
                }
            });
        }

        [Fact]
        public static void CopyTo_Int_Int_Invalid()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                var arrCopy = new string[10];
                Assert.ThrowsAny<ArgumentException>(() => arrList2.CopyTo(0, arrCopy, -1, 1000)); // Array index < 0 (should throw ArgumentOutOfRangeException)
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList2.CopyTo(-1, arrCopy, 0, 1)); // Index < 0
                Assert.Throws<ArgumentOutOfRangeException>(() => arrList2.CopyTo(0, arrCopy, 0, -1)); // Count < 0

                AssertExtensions.Throws<ArgumentException>(null, () =>
                {
                    arrCopy = new string[100];
                    arrList2.CopyTo(arrList2.Count - 1, arrCopy, 0, 24);
                });

                Assert.Throws<ArgumentNullException>(() => arrList2.CopyTo(0, null, 3, 3)); // Array is null
                AssertExtensions.Throws<ArgumentException>("array", null, () => arrList2.CopyTo(0, new object[arrList2.Count, arrList2.Count], 0, arrList2.Count)); // Array is multidimensional

                // Array index and count is out of bounds
                AssertExtensions.Throws<ArgumentException>(null, () =>
                {
                    arrCopy = new string[1];
                    arrList2.CopyTo(0, arrCopy, 3, 15);
                });
                
                Assert.ThrowsAny<ArgumentException>(() => arrList2.CopyTo(0, new object[arrList2.Count, arrList2.Count], 0, -1)); // Should throw ArgumentOutOfRangeException
            });
        }

        [Fact]
        public static void FixedSize_ArrayList()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            ArrayList arrList2 = ArrayList.FixedSize(arrList1);

            Assert.True(arrList2.IsFixedSize);
            Assert.False(arrList2.IsReadOnly);
            Assert.False(arrList2.IsSynchronized);

            Assert.Equal(arrList1.Count, arrList2.Count);
            for (int i = 0; i < arrList1.Count; i++)
            {
                Assert.Equal(arrList1[i], arrList2[i]);
            }

            // Remove an object from the original list and verify the object underneath has been cut
            arrList1.RemoveAt(9);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => arrList2[9]);

            // We cant remove or add to the fixed list
            Assert.Throws<NotSupportedException>(() => arrList2.RemoveRange(0, 1));
            Assert.Throws<NotSupportedException>(() => arrList2.AddRange(new ArrayList()));
            Assert.Throws<NotSupportedException>(() => arrList2.InsertRange(0, new ArrayList()));

            Assert.Throws<NotSupportedException>(() => arrList2.TrimToSize());
            Assert.Throws<NotSupportedException>(() => arrList2.Capacity = 10);
        }

        [Fact]
        public static void FixedSize_ReadOnlyArrayList()
        {
            ArrayList arrList = ArrayList.FixedSize(ArrayList.ReadOnly(new ArrayList()));
            Assert.True(arrList.IsFixedSize);
            Assert.True(arrList.IsReadOnly);
            Assert.False(arrList.IsSynchronized);
        }

        [Fact]
        public static void FixedSize_SynchronizedArrayList()
        {
            ArrayList arrList = ArrayList.FixedSize(ArrayList.Synchronized(new ArrayList()));
            Assert.True(arrList.IsFixedSize);
            Assert.False(arrList.IsReadOnly);
            Assert.True(arrList.IsSynchronized);
        }

        [Fact]
        public static void FixedSize_RangeArrayList()
        {
            ArrayList arrList = ArrayList.FixedSize(new ArrayList()).GetRange(0, 0);
            Assert.True(arrList.IsFixedSize);
        }

        [Fact]
        public static void FixedSize_ArrayList_CanChangeExistingItems()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            ArrayList arrList2 = ArrayList.FixedSize(arrList1);

            arrList2[0] = 10;
            Assert.Equal(10, arrList2[0]);
        }

        [Fact]
        public static void FixedSize_ArrayList_NullCollection_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("list", () => ArrayList.FixedSize(null)); // List is null
        }

        [Fact]
        public static void FixedSize_IList()
        {
            ArrayList arrList = Helpers.CreateIntArrayList(10);
            IList iList = ArrayList.FixedSize((IList)arrList);

            Assert.True(iList.IsFixedSize);
            Assert.False(iList.IsReadOnly);
            Assert.False(iList.IsSynchronized);

            Assert.Equal(arrList.Count, iList.Count);
            for (int i = 0; i < arrList.Count; i++)
            {
                Assert.Equal(arrList[i], iList[i]);
            }
        }

        [Fact]
        public static void FixedSize_SynchronizedIList()
        {
            IList iList = ArrayList.FixedSize((IList)ArrayList.Synchronized(new ArrayList()));
            Assert.True(iList.IsFixedSize);
            Assert.False(iList.IsReadOnly);
            Assert.True(iList.IsSynchronized);
        }

        [Fact]
        public static void FixedSize_IList_ModifyingUnderlyingCollection_CutsFacade()
        {
            ArrayList arrList = Helpers.CreateIntArrayList(10);
            IList iList = ArrayList.FixedSize((IList)arrList);

            // Remove an object from the original list. Verify the object underneath has been cut
            arrList.RemoveAt(9);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => iList[9]);
        }

        [Fact]
        public static void FixedSize_IList_NullList_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("list", () => ArrayList.FixedSize((IList)null)); // List is null
        }
        
        [Fact]
        public static void GetEnumerator_Basic_ArrayListContainingItself()
        {
            // Verify the enumerator works correctly when the ArrayList itself is in the ArrayList
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            arrList1.Add(arrList1);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                IEnumerator enumerator = arrList2.GetEnumerator();

                for (int i = 0; i < 2; i++)
                {
                    int index = 0;
                    while (enumerator.MoveNext())
                    {
                        Assert.StrictEqual(enumerator.Current, arrList2[index]);
                        index++;
                    }
                    enumerator.Reset();
                }
            });
        }

        [Fact]
        public static void GetEnumerator_Basic_DerivedArrayList()
        {
            // The enumerator for a derived (subclassed) ArrayList is different to a normal ArrayList as it does not run an optimized MoveNext() function
            var arrList = new DerivedArrayList(Helpers.CreateIntArrayList(10));
            IEnumerator enumerator = arrList.GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                int index = 0;
                while (enumerator.MoveNext())
                {
                    Assert.StrictEqual(enumerator.Current, arrList[index]);
                    index++;
                }
                enumerator.Reset();
            }
        }
                
        [Fact]
        public static void GetEnumerator_Int_Int()
        {
            int index = 3;
            int count = 3;

            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                IEnumerator enumerator = arrList2.GetEnumerator(index, count);
                Assert.NotNull(enumerator);

                for (int i = index; i < index + count; i++)
                {
                    Assert.True(enumerator.MoveNext());
                    Assert.Equal(arrList2[i], enumerator.Current);
                }

                Assert.False(enumerator.MoveNext());
            });
        }

        [Fact]
        public static void GetEnumerator_Int_Int_ArrayListContainingItself()
        {
            // Verify the enumerator works correctly when the ArrayList itself is in the ArrayList
            int[] data = Helpers.CreateIntArray(10);
            ArrayList arrList1 = new ArrayList(data);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsFixedSize)
                {
                    return;
                }

                arrList2.Insert(0, arrList2);
                arrList2.Insert(arrList2.Count, arrList2);
                arrList2.Insert(arrList2.Count / 2, arrList2);

                var tempArray = new object[data.Length + 3];
                tempArray[0] = arrList2;
                tempArray[tempArray.Length / 2] = arrList2;
                tempArray[tempArray.Length - 1] = arrList2;

                Array.Copy(data, 0, tempArray, 1, data.Length / 2);
                Array.Copy(data, data.Length / 2, tempArray, (tempArray.Length / 2) + 1, data.Length - (data.Length / 2));

                // Enumerate the entire collection
                IEnumerator enumerator = arrList2.GetEnumerator(0, tempArray.Length);

                for (int loop = 0; loop < 2; ++loop)
                {
                    for (int i = 0; i < tempArray.Length; i++)
                    {
                        enumerator.MoveNext();
                        Assert.StrictEqual(tempArray[i], enumerator.Current);
                    }

                    Assert.False(enumerator.MoveNext());
                    enumerator.Reset();
                }

                // Enumerate only part of the collection
                enumerator = arrList2.GetEnumerator(1, tempArray.Length - 2);

                for (int loop = 0; loop < 2; ++loop)
                {
                    for (int i = 1; i < tempArray.Length - 1; i++)
                    {
                        enumerator.MoveNext();
                        Assert.StrictEqual(tempArray[i], enumerator.Current);
                    }

                    Assert.False(enumerator.MoveNext());
                    enumerator.Reset();
                }
            });
        }

        [Fact]
        public static void GetEnumerator_Int_Int_ZeroCount()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                IEnumerator enumerator = arrList2.GetEnumerator(0, 0);
                Assert.False(enumerator.MoveNext());
                Assert.False(enumerator.MoveNext());
            });
        }

        [Fact]
        public static void GetEnumerator_Int_Int_Invalid()
        {
            int index = 3;
            int count = 3;

            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                IEnumerator enumerator = arrList2.GetEnumerator(index, count);
                // If the underlying collection is modified, MoveNext and Reset throw, but Current doesn't
                if (!arrList2.IsReadOnly)
                {
                    enumerator.MoveNext();

                    object originalValue = arrList2[0];
                    arrList2[0] = 10;

                    object temp = enumerator.Current;

                    Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                    Assert.Throws<InvalidOperationException>(() => enumerator.Reset());

                    arrList2[0] = originalValue;
                }

                // Current throws after resetting
                enumerator = arrList2.GetEnumerator(index, count);
                enumerator.Reset();
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);

                // Current throws if the current index is < 0 or >= count
                enumerator = arrList2.GetEnumerator(index, count);
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);

                while (enumerator.MoveNext()) ;
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);

                // Invalid parameters    
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => arrList2.GetEnumerator(-1, arrList2.Count)); // Index < 0
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => arrList2.GetEnumerator(0, -1)); // Count < 0
                AssertExtensions.Throws<ArgumentException>(null, () => arrList2.GetEnumerator(0, arrList2.Count + 1)); // Count + list.Count
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => arrList2.GetEnumerator(-1, arrList2.Count + 1)); // Index < 0 and count > list.Count
            });
        }

        [Fact]
        public static void GetRange()
        {
            int index = 10;
            int count = 50;

            ArrayList arrList1 = Helpers.CreateIntArrayList(100);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                ArrayList range = arrList2.GetRange(index, count);

                Assert.Equal(count, range.Count);

                for (int i = 0; i < range.Count; i++)
                {
                    Assert.Equal(arrList2[i + index], range[i]);
                }

                Assert.Equal(arrList2.IsFixedSize, range.IsFixedSize);
                Assert.Equal(arrList2.IsReadOnly, range.IsReadOnly);
                Assert.False(range.IsSynchronized);

                Assert.Throws<NotSupportedException>(() => range.TrimToSize());
            });
        }

        [Fact]
        public static void GetRange_ChangeUnderlyingCollection()
        {
            int index = 10;
            int count = 50;

            ArrayList arrList1 = Helpers.CreateIntArrayList(100);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                ArrayList range = arrList2.GetRange(index, count);// We can change the underlying collection through the range and this[int index]
                if (!range.IsReadOnly)
                {
                    for (int i = 0; i < 50; i++)
                        range[i] = (int)range[i] + 1;

                    for (int i = 0; i < 50; i++)
                    {
                        Assert.Equal(i + 10 + 1, range[i]);
                    }

                    for (int i = 0; i < 50; i++)
                        range[i] = (int)range[i] - 1;
                }

                // We can change the underlying collection through the range and Add
                if (!range.IsFixedSize)
                {
                    for (int i = 0; i < 100; i++)
                        range.Add(i + 1000);

                    Assert.Equal(150, range.Count);
                    Assert.Equal(200, arrList2.Count);

                    for (int i = 0; i < 50; i++)
                    {
                        Assert.Equal(i + 10, range[i]);
                    }

                    for (int i = 0; i < 100; i++)
                    {
                        Assert.Equal(i + 1000, range[50 + i]);
                    }
                }
            });
        }

        [Fact]
        public static void GetRange_ChangeUnderlyingCollection_Invalid()
        {
            int index = 10;
            int count = 50;

            ArrayList arrList1 = Helpers.CreateIntArrayList(100);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                ArrayList range = arrList2.GetRange(index, count);

                // If we change the underlying collection through set this[int index] range will start to throw
                if (arrList2.IsReadOnly)
                {
                    Assert.Throws<NotSupportedException>(() => arrList2[arrList2.Count - 1] = -1);
                    int iTemp = range.Count;
                }
                else
                {
                    arrList2[arrList2.Count - 1] = -1;
                    Assert.Throws<InvalidOperationException>(() => range.Count);
                }

                // If we change the underlying collection through add range will start to throw
                range = arrList2.GetRange(10, 50);
                if (arrList2.IsFixedSize)
                {
                    Assert.Throws<NotSupportedException>(() => arrList2.Add(arrList2.Count + 1000));
                    int iTemp = range.Count;
                }
                else
                {
                    arrList2.Add(arrList2.Count + 1000);
                    Assert.Throws<InvalidOperationException>(() => range.Count);
                }
            });
        }

        [Fact]
        public static void GetRange_Invalid()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(100);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => arrList2.GetRange(-1, 50)); // Index < 0
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => arrList2.GetRange(0, -1)); // Count < 0

                AssertExtensions.Throws<ArgumentException>(null, () => arrList2.GetRange(0, 500)); // Index + count > list.count
                AssertExtensions.Throws<ArgumentException>(null, () => arrList2.GetRange(arrList2.Count, 1)); // Index >= list.count
            });
        }

        [Fact]
        public static void GetRange_Empty()
        {
            // We should be able to get a range of 0
            var arrList1 = new ArrayList();
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                ArrayList range = arrList2.GetRange(0, 0);
                Assert.Equal(0, range.Count);
            });
        }

        [Fact]
        public static void SetRange()
        {
            int index = 3;

            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            IList setRange = Helpers.CreateIntArrayList(5);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsReadOnly)
                {
                    return;
                }

                arrList2.SetRange(index, setRange);

                // Verify set
                for (int i = 0; i < setRange.Count; i++)
                {
                    Assert.Equal(setRange[i], arrList2[index + i]);
                }
            });
        }

        [Fact]
        public static void SetRange_Invalid()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsReadOnly)
                {
                    return;
                }

                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => arrList2.SetRange(3, arrList2)); // Index + collection.Count > list.Count

                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => arrList2.SetRange(-1, new object[1])); // Index < 0
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => arrList2.SetRange(arrList2.Count, new object[1])); // Index > list.Count

                AssertExtensions.Throws<ArgumentNullException>("c", () => arrList2.SetRange(0, null)); // Collection is null
            });
        }

        [Fact]
        public static void SetRange_EmptyCollection()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            ICollection emptyCollection = new ArrayList();
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsReadOnly)
                {
                    return;
                }

                // No change
                arrList2.SetRange(0, emptyCollection);
                for (int i = 0; i < arrList2.Count; i++)
                {
                    Assert.Equal(i, arrList2[i]);
                }
            });
        }

        [Fact]
        public static void IndexOf_Basic_DuplicateItems()
        {
            var arrList1 = new ArrayList();
            arrList1.Add(null);
            arrList1.Add(arrList1);
            arrList1.Add(null);

            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                Assert.Equal(0, arrList2.IndexOf(null));
            });
        }

        [Fact]
        public static void IndexOf_Int()
        {
            int startIndex = 3;

            var data = new string[21];
            for (int i = 0; i < 10; i++)
            {
                data[i] = i.ToString();
            }
            for (int i = 10; i < 20; i++)
            {
                data[i] = (i - 10).ToString();
            }
            data[20] = null;
            var arrList1 = new ArrayList(data);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                foreach (object value in data)
                {
                    Assert.Equal(Array.IndexOf(data, value, startIndex), arrList2.IndexOf(value, startIndex));
                }
            });
        }

        [Fact]
        public static void IndexOf_Int_NonExistentObject()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                Assert.Equal(-1, arrList2.IndexOf(null, 0));
                Assert.Equal(-1, arrList2.IndexOf("hello", 1));
                Assert.Equal(-1, arrList2.IndexOf(10, 2));
            });
        }

        [Fact]
        public static void IndexOf_Int_ExistentObjectNotInRange()
        {
            // Find an existing object before the index (expects -1)
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                Assert.Equal(-1, arrList2.IndexOf(0, 1));
            });
        }

        [Fact]
        public static void IndexOf_Int_InvalidStartIndex_ThrowsArgumentOutOfRangeException()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => arrList2.IndexOf("Batman", -1)); // Start index < 0                
                AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => arrList2.IndexOf("Batman", arrList2.Count + 1)); // Start index > list.Count

                Assert.Equal(-1, arrList2.IndexOf("Batman", arrList2.Count, 0)); // Index = list.Count
            });
        }

        [Fact]
        public static void IndexOf_Int_Int()
        {
            int startIndex = 0;
            int count = 5;

            var data = new string[21];
            for (int i = 0; i < 10; i++)
            {
                data[i] = i.ToString();
            }
            for (int i = 10; i < 20; i++)
            {
                data[i] = (i - 10).ToString();
            }
            data[20] = null;
            var arrList1 = new ArrayList(data);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                foreach (object value in arrList2)
                {
                    Assert.Equal(Array.IndexOf(data, value, startIndex, count), arrList2.IndexOf(value, startIndex, count));
                }
            });
        }

        [Fact]
        public static void IndexOf_Int_Int_NonExistentObject()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                Assert.Equal(-1, arrList2.IndexOf(null, 0, arrList2.Count));
                Assert.Equal(-1, arrList2.IndexOf("hello", 1, arrList2.Count - 1));
                Assert.Equal(-1, arrList2.IndexOf(10, 2, arrList2.Count - 2));
            });
        }

        [Fact]
        public static void IndexOf_Int_Int_ExistentObjectNotInRange()
        {
            // Find an existing object before the startIndex or after startIndex + count (expects -1)
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                Assert.Equal(-1, arrList2.IndexOf(0, 1, arrList2.Count - 1));
                Assert.Equal(-1, arrList2.IndexOf(10, 0, 5));
            });
        }

        [Fact]
        public static void IndexOf_Int_Int_InvalidIndexCount_ThrowsArgumentOutOfRangeException()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => arrList2.IndexOf("Batman", -1, arrList2.Count)); // Start index < 0
                AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => arrList2.IndexOf("Batman", arrList2.Count + 1, arrList2.Count)); // Start index > Count
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => arrList2.IndexOf("Batman", 0, -1)); // Count < 0
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => arrList2.IndexOf("Batman", 3, arrList2.Count + 1)); // Count > list.Count

                Assert.Equal(-1, arrList2.IndexOf("Batman", arrList2.Count, 0)); // Index = list.Count
            });
        }

        [Fact]
        public static void InsertRange()
        {
            int index = 3;

            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            IList arrInsert = Helpers.CreateIntArrayList(10, 10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsFixedSize)
                {
                    return;
                }

                // Insert collection into array list and verify.
                arrList2.InsertRange(index, arrInsert);
                for (int i = 0; i < arrInsert.Count; i++)
                {
                    Assert.Equal(arrInsert[i], arrList2[i + index]);
                }
            });
        }

        [Fact]
        public static void InsertRange_LargeCapacity()
        {
            // Add a range large enough to increase the capacity of the arrayList by more than a factor of two
            var arrList1 = new ArrayList();
            ArrayList arrInsert = Helpers.CreateIntArrayList(128);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsFixedSize)
                {
                    return;
                }

                arrList2.InsertRange(0, arrInsert);

                for (int i = 0; i < arrInsert.Count; i++)
                {
                    Assert.Equal(i, arrList2[i]);
                }
            });
        }

        [Fact]
        public static void InsertRange_EmptyCollection()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            var emptyCollection = new Queue();
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsFixedSize)
                {
                    return;
                }

                arrList2.InsertRange(0, emptyCollection);
                Assert.Equal(arrList1.Count, arrList2.Count);
            });
        }

        [Fact]
        public static void InsertRange_WrappedNonArrayList()
        {
            // Create an array list by wrapping a non-ArrayList object (e.g. List<T>)
            var list = new List<int>(Helpers.CreateIntArray(10));
            ArrayList arrList = ArrayList.Adapter(list);
            IList arrInsert = Helpers.CreateIntArrayList(10);

            arrList.InsertRange(3, arrInsert);
            for (int i = 0; i < arrInsert.Count; i++)
            {
                Assert.Equal(arrInsert[i], arrList[i + 3]);
            }
        }

        [Fact]
        public static void InsertRange_Itself()
        {
            int[] data = Helpers.CreateIntArray(10);
            var arrList1 = new ArrayList(data);
            int start = 3;
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsFixedSize)
                {
                    return;
                }

                arrList2.InsertRange(start, arrList2);
                for (int i = 0; i < arrList2.Count; i++)
                {
                    int expectedItem;

                    if (i < start)
                    {
                        expectedItem = data[i];
                    }
                    else if (start <= i && i - start < data.Length)
                    {
                        expectedItem = data[i - start];
                    }
                    else
                    {
                        expectedItem = data[i - data.Length];
                    }
                    Assert.Equal(expectedItem, arrList2[i]);
                }

                // Verify that ArrayList does not pass the internal array to CopyTo
                arrList2.Clear();
                for (int i = 0; i < 64; i++)
                {
                    arrList2.Add(i);
                }

                ArrayList arrInsert = Helpers.CreateIntArrayList(4);

                MyCollection myCollection = new MyCollection(arrInsert);
                arrList2.InsertRange(4, myCollection);

                Assert.Equal(0, myCollection.StartIndex);

                Assert.Equal(4, myCollection.Array.Length);
            });
        }

        [Fact]
        public static void InsertRange_Invalid()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsFixedSize)
                {
                    return;
                }

                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => arrList2.InsertRange(-1, new object[1])); // Index < 0
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => arrList2.InsertRange(1000, new object[1])); // Index > count

                AssertExtensions.Throws<ArgumentNullException>("c", () => arrList2.InsertRange(3, null)); // Collection is null
            });
        }

        [Fact]
        public static void LastIndexOf_Basic()
        {
            var data = new string[20];
            for (int i = 0; i < 10; i++)
            {
                data[i] = i.ToString();
            }
            for (int i = 10; i < data.Length; i++)
            {
                data[i] = (i - 10).ToString();
            }
            var arrList1 = new ArrayList(data);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                foreach (object value in arrList2)
                {
                    Assert.Equal(Array.LastIndexOf(data, value), arrList2.LastIndexOf(value));
                }
                
                Assert.Equal(-1, arrList2.LastIndexOf(null));
            });
        }

        [Fact]
        public static void LastIndexOf_Basic_EmptyArrayList()
        {
            var arrList1 = new ArrayList();
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                Assert.Equal(-1, arrList2.IndexOf("Batman"));
            });
        }

        [Fact]
        public static void LastIndexOf_Basic_NonExistentObject()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                Assert.Equal(-1, arrList2.IndexOf("hello"));
                Assert.Equal(-1, arrList2.IndexOf(10));
                Assert.Equal(-1, arrList2.IndexOf(null));
            });
        }

        [Fact]
        public static void LastIndexOf_Int()
        {
            int startIndex = 3;

            var data = new string[21];
            for (int i = 0; i < 10; i++)
            {
                data[i] = i.ToString();
            }
            for (int i = 10; i < 20; i++)
            {
                data[i] = (i - 10).ToString();
            }
            data[20] = null;

            var arrList1 = new ArrayList(data);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                foreach (object value in arrList2)
                {
                    Assert.Equal(Array.LastIndexOf(data, value, startIndex), arrList2.LastIndexOf(value, startIndex));
                }
                Assert.Equal(20, arrList2.LastIndexOf(null, data.Length - 1));
            });
        }

        [Fact]
        public static void LastIndexOf_Int_NonExistentObject()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                Assert.Equal(-1, arrList2.IndexOf(11, 0));
                Assert.Equal(-1, arrList2.IndexOf("8", 0));
                Assert.Equal(-1, arrList2.IndexOf(null, 0));
            });
        }

        [Fact]
        public static void LastIndexOf_Int_ObjectOutOfRange()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                int ndx = arrList1.IndexOf(0, 1);
                Assert.Equal(-1, ndx);
            });
        }

        [Fact]
        public static void LastIndexOf_Int_InvalidStartIndex_ThrowsArgumentOutOfRangeException()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => arrList2.LastIndexOf(0, -1)); // StartIndex < 0
                AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => arrList2.LastIndexOf(0, arrList2.Count)); // StartIndex >= list.Count
            });
        }

        [Fact]
        public static void LastIndexOf_Int_Int()
        {
            int startIndex = 15;
            int count = 10;

            var data = new string[21];
            for (int i = 0; i < 10; i++)
            {
                data[i] = i.ToString();
            }
            for (int i = 10; i < 20; i++)
            {
                data[i] = (i - 10).ToString();
            }
            data[20] = null;

            var arrList1 = new ArrayList(data);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                foreach (object value in arrList2)
                {
                    Assert.Equal(Array.LastIndexOf(data, value, startIndex, count), arrList2.LastIndexOf(value, startIndex, count));
                }
                Assert.Equal(20, arrList2.LastIndexOf(null, data.Length - 1, 2));
            });
        }

        [Fact]
        public static void LastIndexOf_Int_Int_EmptyArrayList()
        {
            var arrList1 = new ArrayList();
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                Assert.Equal(-1, arrList2.LastIndexOf("hello", 0, 0));
            });
        }

        [Fact]
        public static void LastIndexOf_Int_Int_NonExistentObject()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                Assert.Equal(-1, arrList2.IndexOf(100, 0, arrList2.Count));
            });
        }

        [Fact]
        public static void LastIndexOf_Int_Int_ObjectOutOfRange()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                int ndx = arrList2.IndexOf(0, 1, arrList2.Count - 1); // Start index > object's index
                Assert.Equal(-1, ndx);

                ndx = arrList2.IndexOf(10, 0, arrList2.Count - 2); // Start index + count < object's index
                Assert.Equal(-1, ndx);
            });
        }

        [Fact]
        public static void LastIndexOf_Int_Int_InvalidStartIndexCount_ThrowsArgumentOutOfRangeException()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => arrList2.LastIndexOf(0, -1, 2)); // Index < 0
                AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => arrList2.LastIndexOf(0, arrList2.Count, 2)); // Index >= list.Count

                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => arrList2.LastIndexOf(0, 0, -1)); // Count < 0
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => arrList2.LastIndexOf(0, 0, arrList2.Count + 1)); // Count > list.Count

                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => arrList2.LastIndexOf(0, 4, arrList2.Count - 4)); // Index + count > list.Count
            });
        }

        [Fact]
        public static void ReadOnly_ArrayList()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            ArrayList arrList2 = ArrayList.ReadOnly(arrList1);

            Assert.True(arrList2.IsFixedSize);
            Assert.True(arrList2.IsReadOnly);
            Assert.False(arrList2.IsSynchronized);

            Assert.Equal(arrList1.Count, arrList2.Count);
            for (int i = 0; i < arrList1.Count; i++)
            {
                Assert.Equal(arrList1[i], arrList2[i]);
            }

            // Remove an object from the original list and verify the object underneath has been cut
            arrList1.RemoveAt(9);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => arrList2[9]);

            // We cant remove, change or add to the readonly list
            Assert.Throws<NotSupportedException>(() => arrList2.RemoveRange(0, 1));
            Assert.Throws<NotSupportedException>(() => arrList2.AddRange(new ArrayList()));
            Assert.Throws<NotSupportedException>(() => arrList2.InsertRange(0, new ArrayList()));

            Assert.Throws<NotSupportedException>(() => arrList2.Reverse());
            Assert.Throws<NotSupportedException>(() => arrList2.Sort());

            Assert.Throws<NotSupportedException>(() => arrList2.TrimToSize());
            Assert.Throws<NotSupportedException>(() => arrList2.Capacity = 10);

            Assert.Throws<NotSupportedException>(() => arrList2[2] = 5);
            Assert.Throws<NotSupportedException>(() => arrList2.SetRange(0, new ArrayList()));

            // We can get a readonly from this readonly 
            ArrayList arrList3 = ArrayList.ReadOnly(arrList2);
            Assert.True(arrList2.IsReadOnly);
            Assert.True(arrList3.IsReadOnly);

            // Verify we cant access remove, change or add to the readonly list
            Assert.Throws<NotSupportedException>(() => arrList2.RemoveAt(0));
        }

        [Fact]
        public static void ReadOnly_SynchronizedArrayList()
        {
            ArrayList arrList = ArrayList.ReadOnly(ArrayList.Synchronized(new ArrayList()));
            Assert.True(arrList.IsFixedSize);
            Assert.True(arrList.IsReadOnly);
            Assert.True(arrList.IsSynchronized);
        }

        [Fact]
        public static void ReadOnly_ArrayList_NullList_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("list", () => ArrayList.ReadOnly(null)); // List is null
        }

        [Fact]
        public static void ReadOnly_IList()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            IList iList = ArrayList.ReadOnly((IList)arrList1);

            Assert.True(iList.IsFixedSize);
            Assert.True(iList.IsReadOnly);
            Assert.False(iList.IsSynchronized);

            Assert.Equal(arrList1.Count, iList.Count);
            for (int i = 0; i < iList.Count; i++)
            {
                Assert.Equal(arrList1[i], iList[i]);
            }
        }

        [Fact]
        public static void ReadOnly_SynchronizedIList()
        {
            IList iList = ArrayList.ReadOnly((IList)ArrayList.Synchronized(new ArrayList()));
            Assert.True(iList.IsFixedSize);
            Assert.True(iList.IsReadOnly);
            Assert.True(iList.IsSynchronized);
        }

        [Fact]
        public static void ReadOnly_IList_ModifiyingUnderlyingCollection_CutsFacade()
        {
            ArrayList arrList = Helpers.CreateIntArrayList(10);
            IList iList = ArrayList.ReadOnly((IList)arrList);

            // Remove an object from the original list. Verify the object underneath has been cut
            arrList.RemoveAt(9);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => iList[9]);
        }

        [Fact]
        public static void ReadOnly_IList_NullList_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("list", () => ArrayList.ReadOnly((IList)null)); // List is null
        }

        [Fact]
        public static void RemoveRange()
        {
            int index = 3;
            int count = 3;

            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            var expected = new int[] { 0, 1, 2, 6, 7, 8, 9 };
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsFixedSize)
                {
                    return;
                }

                arrList2.RemoveRange(index, count);

                // Verify remove
                for (int i = 0; i < arrList2.Count; i++)
                {
                    Assert.Equal(expected[i], arrList2[i]);
                }
            });
        }

        [Fact]
        public static void RemoveRange_ZeroCount()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsFixedSize)
                {
                    return;
                }

                arrList2.RemoveRange(3, 0);
                Assert.Equal(arrList1.Count, arrList2.Count);

                // No change
                for (int i = 0; i < arrList2.Count; i++)
                {
                    Assert.Equal(i, arrList2[i]);
                }
            });
        }

        [Fact]
        public static void RemoveRange_Invalid()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsFixedSize)
                {
                    return;
                }

                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => arrList2.RemoveRange(-1, 1)); // Index < 0
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => arrList2.RemoveRange(1, -1)); // Count < 0

                AssertExtensions.Throws<ArgumentException>(null, () => arrList2.RemoveRange(arrList2.Count, 1)); // Index > list.Count
                AssertExtensions.Throws<ArgumentException>(null, () => arrList2.RemoveRange(0, arrList2.Count + 1)); // Count > list.Count
                AssertExtensions.Throws<ArgumentException>(null, () => arrList2.RemoveRange(5, arrList2.Count - 1)); // Index + count > list.Count
            });
        }

        [Fact]
        public static void Remove_Null()
        {
            var arrList = new ArrayList();

            arrList.Add(null);
            arrList.Add(arrList);
            arrList.Add(null);
            arrList.Remove(arrList);
            arrList.Remove(null);
            arrList.Remove(null);

            Assert.Equal(0, arrList.Count);
        }

        [Fact]
        public static void Repeat()
        {
            ArrayList arrList = ArrayList.Repeat(5, 100);
            for (int i = 0; i < arrList.Count; i++)
            {
                Assert.Equal(5, arrList[i]);
            }
        }

        [Fact]
        public static void Repeat_Null()
        {
            ArrayList arrList = ArrayList.Repeat(null, 100);
            for (int i = 0; i < arrList.Count; i++)
            {
                Assert.Null(arrList[i]);
            }
        }

        [Fact]
        public static void Repeat_ZeroCount()
        {
            ArrayList arrList = ArrayList.Repeat(5, 0);
            Assert.Equal(0, arrList.Count);
        }

        [Fact]
        public static void Repeat_NegativeCount_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => ArrayList.Repeat(5, -1)); // Count < 0
        }

        [Fact]
        public static void Reverse_Basic()
        {
            int[] expected = Helpers.CreateIntArray(10);
            var arrList1 = new ArrayList(expected);
            Array.Reverse(expected);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsReadOnly)
                {
                    return;
                }

                arrList2.Reverse();

                Assert.Equal(arrList1.Count, arrList2.Count);
                for (int i = 0; i < arrList2.Count; i++)
                {
                    Assert.Equal(expected[i], arrList2[i]);
                }
            });
        }

        [Fact]
        public static void Reverse_Basic_EmptyArrayList()
        {
            var arrList1 = new ArrayList();
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsReadOnly)
                {
                    return;
                }

                arrList2.Reverse();
                Assert.Equal(0, arrList2.Count);
            });
        }

        [Fact]
        public static void Reverse_Basic_SingleObjectArrayList()
        {
            var arrList1 = new ArrayList();
            arrList1.Add(0);

            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsReadOnly)
                {
                    return;
                }

                arrList2.Reverse();

                Assert.Equal(0, arrList2[0]);
                Assert.Equal(1, arrList2.Count);
            });
        }

        [Fact]
        public static void Reverse_Int_Int()
        {
            int index = 5;
            int count = 4;

            int[] expected = Helpers.CreateIntArray(10);
            var arrList1 = new ArrayList(expected);
            Array.Reverse(expected, index, count);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsReadOnly)
                {
                    return;
                }

                arrList2.Reverse(index, count);

                Assert.Equal(arrList1.Count, arrList2.Count);
                for (int i = 0; i < arrList2.Count; i++)
                {
                    Assert.Equal(expected[i], arrList2[i]);
                }
            });
        }

        [Fact]
        public static void Reverse_Int_Int_ZeroCount()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsReadOnly)
                {
                    return;
                }

                arrList2.Reverse(0, 0);

                // No change
                Assert.Equal(arrList1.Count, arrList2.Count);
                for (int i = 0; i < arrList2.Count; i++)
                {
                    Assert.Equal(arrList1[i], arrList2[i]);
                }
            });
        }

        [Fact]
        public static void Reverse_Int_Int_Invalid()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsReadOnly)
                {
                    return;
                }

                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => arrList2.Reverse(-1, arrList2.Count)); // Index < 0
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => arrList2.Reverse(0, -1)); // Count < 0
                AssertExtensions.Throws<ArgumentException>(null, () => arrList2.Reverse(1000, arrList2.Count)); // Index is too big
            });
        }

        [Fact]
        public static void Sort_Basic()
        {
            int[] expected = Helpers.CreateIntArray(10);
            var arrList1 = new ArrayList(expected);
            Array.Sort(expected);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsReadOnly)
                {
                    return;
                }

                arrList2.Sort();
                for (int i = 0; i < arrList2.Count; i++)
                {
                    Assert.Equal(expected[i], arrList2[i]);
                }
            });
        }

        [Fact]
        public static void Sort_Basic_EmptyArrayList()
        {
            var arrList1 = new ArrayList();
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsReadOnly)
                {
                    return;
                }

                arrList2.Sort();
                Assert.Equal(0, arrList2.Count);
            });
        }

        [Fact]
        public static void Sort_Basic_SingleObjectArrayList()
        {
            var arrList1 = new ArrayList();
            arrList1.Add(1);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsReadOnly)
                {
                    return;
                }

                arrList2.Sort();
                Assert.Equal(1, arrList2.Count);
            });
        }

        [Fact]
        public static void Sort_IComparer()
        {
            int[] data = Helpers.CreateIntArray(10);
            int[] ascendingData = Helpers.CreateIntArray(10);
            int[] descendingData = Helpers.CreateIntArray(10);
            Array.Sort(ascendingData, new AscendingComparer());
            Array.Sort(descendingData, new DescendingComparer());

            var arrList1 = new ArrayList(data);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsReadOnly)
                {
                    return;
                }

                arrList2.Sort(null);
                for (int i = 0; i < arrList2.Count; i++)
                {
                    Assert.Equal(ascendingData[i], arrList2[i]);
                }

                arrList2.Sort(new AscendingComparer());
                for (int i = 0; i < arrList2.Count; i++)
                {
                    Assert.Equal(ascendingData[i], arrList2[i]);
                }

                arrList2.Sort(new DescendingComparer());
                for (int i = 0; i < arrList2.Count; i++)
                {
                    Assert.Equal(descendingData[i], arrList2[i]);
                }
            });
        }

        [Fact]
        public static void Sort_Int_Int_IComparer()
        {
            int index = 3;
            int count = 5;

            int[] expected = Helpers.CreateIntArray(10);
            var arrList1 = new ArrayList(expected);
            Array.Sort(expected, index, count, new AscendingComparer());
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsReadOnly)
                {
                    return;
                }

                arrList2.Sort(3, 5, new AscendingComparer());
                for (int i = 0; i < arrList2.Count; i++)
                {
                    Assert.Equal(expected[i], arrList2[i]);
                }
            });
        }

        [Fact]
        public static void Sort_Int_Int_IComparer_Invalid()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsReadOnly)
                {
                    return;
                }

                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => arrList2.Sort(-1, arrList2.Count, null)); // Index < 0
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => arrList2.Sort(0, -1, null)); // Count < 0

                AssertExtensions.Throws<ArgumentException>(null, () => arrList2.Sort(arrList2.Count, arrList2.Count, null)); // Index >= list.Count
                AssertExtensions.Throws<ArgumentException>(null, () => arrList2.Sort(0, arrList2.Count + 1, null)); // Count = list.Count
            });
        }

        [Fact]
        public static void Sort_MultipleDataTypes_ThrowsInvalidOperationException()
        {
            var arrList1 = new ArrayList();
            arrList1.Add((short)1);
            arrList1.Add(1);
            arrList1.Add((long)1);
            arrList1.Add((ushort)1);
            arrList1.Add((uint)1);
            arrList1.Add((ulong)1);

            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                if (arrList2.IsReadOnly)
                {
                    return;
                }

                Assert.Throws<InvalidOperationException>(() => arrList2.Sort());
            });
        }

        [Fact]
        public static void Synchronized_ArrayList()
        {
            ArrayList arrList = ArrayList.Synchronized(new ArrayList());
            Assert.False(arrList.IsFixedSize);
            Assert.False(arrList.IsReadOnly);
            Assert.True(arrList.IsSynchronized);
        }

        [Fact]
        public static void Synchronized_FixedSizeArrayList()
        {
            ArrayList arrList = ArrayList.Synchronized(ArrayList.FixedSize(new ArrayList()));
            Assert.True(arrList.IsFixedSize);
            Assert.False(arrList.IsReadOnly);
            Assert.True(arrList.IsSynchronized);
        }

        [Fact]
        public static void Synchronized_ReadOnlyArrayList()
        {
            ArrayList arrList = ArrayList.Synchronized(ArrayList.ReadOnly(new ArrayList()));
            Assert.True(arrList.IsFixedSize);
            Assert.True(arrList.IsReadOnly);
            Assert.True(arrList.IsSynchronized);
        }

        [Fact]
        public static void Synchronized_ArrayList_NullList_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("list", () => ArrayList.Synchronized(null)); // List is null
        }

        [Fact]
        public static void Synchronized_FixedSizeIList()
        {
            IList iList = ArrayList.Synchronized((IList)ArrayList.FixedSize(new ArrayList()));
            Assert.True(iList.IsFixedSize);
            Assert.False(iList.IsReadOnly);
            Assert.True(iList.IsSynchronized);
        }

        [Fact]
        public static void Synchronized_ReadOnlyIList()
        {
            IList iList = ArrayList.Synchronized((IList)ArrayList.ReadOnly(new ArrayList()));
            Assert.True(iList.IsFixedSize);
            Assert.True(iList.IsReadOnly);
            Assert.True(iList.IsSynchronized);
        }

        [Fact]
        public static void Synchronized_IList_NullList_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("list", () => ArrayList.Synchronized((IList)null)); // List is null
        }

        [Fact]
        public static void ToArray()
        {
            // ToArray returns an array of this. We will not extensively test this method as
            // this is a thin wrapper on Array.Copy which is extensively tested
            ArrayList arrList1 = Helpers.CreateIntArrayList(10);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                object[] arr1 = arrList2.ToArray();
                Array arr2 = arrList2.ToArray(typeof(int));

                for (int i = 0; i < 10; i++)
                {
                    Assert.Equal(i, arr1[i]);
                    Assert.Equal(i, arr2.GetValue(i));
                }
            });
        }

        [Fact]
        public static void ToArray_EmptyArrayList()
        {
            var arrList1 = new ArrayList();
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                object[] arr1 = arrList2.ToArray();
                Assert.Equal(0, arr1.Length);

                Array arr2 = arrList2.ToArray(typeof(object));
                Assert.Equal(0, arr2.Length);
            });
        }

        [Fact]
        public static void ToArray_Invalid()
        {
            ArrayList arrList1 = Helpers.CreateIntArrayList(100);
            Helpers.PerformActionOnAllArrayListWrappers(arrList1, arrList2 =>
            {
                // This should be covered in Array.Copy, but lets do it for completion's sake
                Assert.Throws<InvalidCastException>(() => arrList2.ToArray(typeof(string))); // Objects stored are not strings
                AssertExtensions.Throws<ArgumentNullException>("type", () => arrList2.ToArray(null)); // Type is null
            });
        }

        [Fact]
        public static void TrimToSize()
        {
            ArrayList arrList = Helpers.CreateIntArrayList(10);
            arrList.Capacity = 2 * arrList.Count;
            Assert.True(arrList.Capacity > arrList.Count);

            arrList.TrimToSize();
            Assert.Equal(arrList.Count, arrList.Capacity);

            // Test on Adapter
            arrList = ArrayList.Adapter(arrList);
            arrList.TrimToSize();

            // Test on Synchronized
            arrList = ArrayList.Synchronized(Helpers.CreateIntArrayList(10));
            arrList.TrimToSize();
            Assert.Equal(arrList.Count, arrList.Capacity);
        }

        private class BinarySearchComparer : IComparer
        {
            public virtual int Compare(object x, object y)
            {
                if (x is string)
                {
                    return ((string)x).CompareTo((string)y);
                }

                var comparer = new Comparer(Globalization.CultureInfo.InvariantCulture);
                if (x is int || y is string)
                {
                    return comparer.Compare(x, y);
                }

                return -1;
            }
        }

        private class Foo
        {
            public string StringValue { get; set; } = "Hello World";
        }

        private class MyCollection : ICollection
        {
            private ICollection _collection;
            private Array _array;
            private int _startIndex;

            public MyCollection(ICollection collection)
            {
                _collection = collection;
            }

            public Array Array => _array;

            public int StartIndex => _startIndex;

            public int Count =>_collection.Count;

            public object SyncRoot => _collection.SyncRoot;

            public bool IsSynchronized => _collection.IsSynchronized;

            public void CopyTo(Array array, int startIndex)
            {
                _array = array;
                _startIndex = startIndex;
                _collection.CopyTo(array, startIndex);
            }

            public IEnumerator GetEnumerator()
            {
                throw new NotSupportedException();
            }
        }

        private class AscendingComparer : IComparer
        {
            public virtual int Compare(object x, object y) => ((int)x).CompareTo((int)y);
        }

        private class DescendingComparer : IComparer
        {
            public virtual int Compare(object x, object y) => -((int)x).CompareTo((int)y);
        }

        private class DerivedArrayList : ArrayList
        {
            public DerivedArrayList(ICollection c) : base(c) { }
        }
    }

    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)] // Changed behavior
    public class ArrayList_SyncRootTests
    {
        private ArrayList _arrDaughter;
        private ArrayList _arrGrandDaughter;

        [Fact]
        public void GetSyncRoot()
        {
            const int NumberOfElements = 100;
            const int NumberOfWorkers = 10;

            // Testing SyncRoot is not as simple as its implementation looks like. This is the working
            // scenario we have in mind.
            // 1) Create your Down to earth mother ArrayList
            // 2) Get a Fixed wrapper from it
            // 3) Get a Synchronized wrapper from 2)
            // 4) Get a synchronized wrapper of the mother from 1)
            // 5) all of these should SyncRoot to the mother earth

            ArrayList arrMother1 = Helpers.CreateIntArrayList(NumberOfElements);
            Helpers.PerformActionOnAllArrayListWrappers(arrMother1, arrMother2 =>
            {
                ArrayList arrSon1 = ArrayList.FixedSize(arrMother2);
                ArrayList arrSon2 = ArrayList.ReadOnly(arrMother2);

                _arrGrandDaughter = ArrayList.Synchronized(arrMother2);
                _arrDaughter = ArrayList.Synchronized(arrMother2);

                Assert.True(arrMother2.SyncRoot is ArrayList);
                Assert.True(arrSon1.SyncRoot is ArrayList);
                Assert.True(arrSon2.SyncRoot is ArrayList);
                Assert.True(_arrDaughter.SyncRoot is ArrayList);
                Assert.Equal(arrSon1.SyncRoot, arrMother2.SyncRoot);
                Assert.True(_arrGrandDaughter.SyncRoot is ArrayList);

                arrMother2 = new ArrayList();
                for (int i = 0; i < NumberOfElements; i++)
                {
                    arrMother2.Add(i);
                }

                arrSon1 = ArrayList.FixedSize(arrMother2);
                arrSon2 = ArrayList.ReadOnly(arrMother2);
                _arrGrandDaughter = ArrayList.Synchronized(arrSon1);
                _arrDaughter = ArrayList.Synchronized(arrMother2);

                // We are going to rumble with the ArrayLists with 2 threads
                var workers = new Task[NumberOfWorkers];
                var action1 = new Action(SortElements);
                var action2 = new Action(ReverseElements);
                for (int iThreads = 0; iThreads < NumberOfWorkers; iThreads += 2)
                {
                    workers[iThreads] = Task.Run(action1);
                    workers[iThreads + 1] = Task.Run(action2);
                }

                Task.WaitAll(workers);

                // Checking time
                // Now lets see how this is done.
                // Reverse and sort - ascending more likely
                // Sort followed up Reverse - descending
                bool fDescending = ((int)arrMother2[0]).CompareTo((int)arrMother2[1]) > 0;

                int valye = (int)arrMother2[0];
                for (int i = 1; i < NumberOfElements; i++)
                {
                    if (fDescending)
                    {
                        Assert.True(valye.CompareTo((int)arrMother2[i]) > 0);
                    }
                    else
                    {
                        Assert.True(valye.CompareTo((int)arrMother2[i]) < 0);
                    }
                    valye = (int)arrMother2[i];
                }
            });
        }

        private void SortElements() => _arrGrandDaughter.Sort();

        private void ReverseElements() => _arrDaughter.Reverse();
    }

    public class ArrayList_SynchronizedTests
    {
        public static string[] s_synchronizedTestData = new string[]
        {
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America",
            "Captain Atom",
            "Catwoman",
            "Cyborg",
            "Flash",
            "Green Arrow",
            "Green Lantern",
            "Hawkman",
            null,
            "Ironman",
            "Nightwing",
            "Robin",
            "SpiderMan",
            "Steel",
            null,
            "Thor",
            "Wildcat",
            null,
            "Aquaman",
            "Atom",
            "Batman",
            "Black Canary",
            "Captain America"
        };

        private IList _iList;
        private const int NumberOfElements = 10;
        private const string Prefix = "String_";

        public ArrayList _arrList;
        public Hashtable _hash; // This will verify that threads will only add elements the num of times they are specified to

        [Fact]
        public void Synchronized_ArrayList()
        {
            // Make 40 threads which add strHeroes to an ArrayList
            // the outcome is that the length of the ArrayList should be the same size as the strHeroes array
            _arrList = ArrayList.Synchronized(new ArrayList());
            _hash = Hashtable.Synchronized(new Hashtable());

            // Initialize the threads
            var workers = new Task[7];
            for (int i = 0; i < workers.Length; i++)
            {
                string name = "ThreadID " + i.ToString();
                Action delegStartMethod = () => AddElems(name);
                workers[i] = Task.Run(delegStartMethod);
            }

            Task.WaitAll(workers);

            Assert.Equal(workers.Length * s_synchronizedTestData.Length, _arrList.Count);
        }

        [Fact]
        public void Synchronized_IList()
        {
            int iNumberOfWorkers = 10;

            _iList = ArrayList.Synchronized((IList)new ArrayList());

            var workers = new Task[10];
            var action = new Action(AddElements);

            for (int i = 0; i < workers.Length; i++)
            {
                workers[i] = Task.Run(action);
            }

            Task.WaitAll(workers);

            // Checking time
            Assert.Equal(NumberOfElements * iNumberOfWorkers, _iList.Count);

            for (int i = 0; i < NumberOfElements; i++)
            {
                int iNumberOfTimes = 0;
                for (int j = 0; j < _iList.Count; j++)
                {
                    if (((string)_iList[j]).Equals(Prefix + i))
                        iNumberOfTimes++;
                }

                Assert.Equal(iNumberOfTimes, iNumberOfWorkers);
            }

            workers = new Task[iNumberOfWorkers];
            action = new Action(RemoveElements);

            for (int i = 0; i < workers.Length; i++)
            {
                workers[i] = Task.Run(action);
            }

            Task.WaitAll(workers);

            Assert.Equal(0, _iList.Count);
        }

        public void AddElems(string currThreadName)
        {
            int iNumTimesThreadUsed = 0;

            for (int i = 0; i < s_synchronizedTestData.Length; i++)
            {
                // To test that we only use the right threads the right number of times  keep track with the hashtable
                // how many times we use this thread
                try
                {
                    _hash.Add(currThreadName, null);
                    // This test assumes ADD will throw for dup elements
                }
                catch (ArgumentException)
                {
                    iNumTimesThreadUsed++;
                }

                Assert.NotNull(_arrList);
                Assert.True(_arrList.IsSynchronized);

                _arrList.Add(s_synchronizedTestData[i]);
            }

            Assert.Equal(s_synchronizedTestData.Length - 1, iNumTimesThreadUsed);
        }

        private void AddElements()
        {
            for (int i = 0; i < NumberOfElements; i++)
            {
                _iList.Add(Prefix + i);
            }
        }

        private void RemoveElements()
        {
            for (int i = 0; i < NumberOfElements; i++)
            {
                _iList.Remove(Prefix + i);
            }
        }
    }
}
