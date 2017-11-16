// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Collections.ObjectModel.Tests
{
    /// <summary>
    /// Tests the public properties and constructor in ObservableCollection<T>.
    /// </summary>
    public class ReadOnlyObservableCollectionTests
    {
        [Fact]
        public static void Ctor_Tests()
        {
            string[] anArray = new string[] { "one", "two", "three", "four", "five" };
            ReadOnlyObservableCollection<string> readOnlyCol =
                new ReadOnlyObservableCollection<string>(new ObservableCollection<string>(anArray));
            IReadOnlyList_T_Test<string> helper = new IReadOnlyList_T_Test<string>(readOnlyCol, anArray);
            helper.InitialItems_Tests();
            IList<string> readOnlyColAsIList = readOnlyCol;
            Assert.True(readOnlyColAsIList.IsReadOnly, "ReadOnlyObservableCollection should be readOnly.");
        }

        [Fact]
        public static void Ctor_Tests_Negative()
        {
            ReadOnlyObservableCollection<string> collection;
            Assert.Throws<ArgumentNullException>(() => collection = new ReadOnlyObservableCollection<string>(null));
        }

        [Fact]
        public static void GetItemTests()
        {
            string[] anArray = new string[] { "one", "two", "three", "four", "five" };
            ReadOnlyObservableCollection<string> readOnlyCol =
                new ReadOnlyObservableCollection<string>(new ObservableCollection<string>(anArray));
            IReadOnlyList_T_Test<string> helper = new IReadOnlyList_T_Test<string>(readOnlyCol, anArray);
            helper.Item_get_Tests();
        }

        [Fact]
        public static void GetItemTests_Negative()
        {
            string[] anArray = new string[] { "one", "two", "three", "four", "five" };
            ReadOnlyObservableCollection<string> readOnlyCol =
                new ReadOnlyObservableCollection<string>(new ObservableCollection<string>(anArray));
            IReadOnlyList_T_Test<string> helper = new IReadOnlyList_T_Test<string>(readOnlyCol, anArray);
            helper.Item_get_Tests_Negative();
        }

        /// <summary>
        /// Tests that contains returns true when the item is in the collection
        /// and false otherwise.
        /// </summary>
        [Fact]
        public static void ContainsTests()
        {
            string[] anArray = new string[] { "one", "two", "three", "four", "five" };
            ReadOnlyObservableCollection<string> readOnlyCol =
                new ReadOnlyObservableCollection<string>(new ObservableCollection<string>(anArray));

            for (int i = 0; i < anArray.Length; i++)
            {
                string item = anArray[i];
                Assert.True(readOnlyCol.Contains(item), "ReadOnlyCol did not contain item: " + anArray[i] + " at index: " + i);
            }

            Assert.False(readOnlyCol.Contains("randomItem"), "ReadOnlyCol should not have contained non-existent item");
            Assert.False(readOnlyCol.Contains(null), "ReadOnlyCol should not have contained null");
        }

        /// <summary>
        /// Tests that the collection can be copied into a destination array.
        /// </summary>
        [Fact]
        public static void CopyToTest()
        {
            string[] anArray = new string[] { "one", "two", "three", "four" };
            ReadOnlyObservableCollection<string> readOnlyCol =
                new ReadOnlyObservableCollection<string>(new ObservableCollection<string>(anArray));

            string[] aCopy = new string[anArray.Length];
            readOnlyCol.CopyTo(aCopy, 0);
            for (int i = 0; i < anArray.Length; ++i)
                Assert.Equal(anArray[i], aCopy[i]);

            // copy observable collection starting in middle, where array is larger than source.
            aCopy = new string[anArray.Length + 2];
            int offsetIndex = 1;
            readOnlyCol.CopyTo(aCopy, offsetIndex);
            for (int i = 0; i < aCopy.Length; i++)
            {
                string value = aCopy[i];
                if (i == 0)
                    Assert.True(null == value, "Should not have a value since we did not start copying there.");
                else if (i == (aCopy.Length - 1))
                    Assert.True(null == value, "Should not have a value since the collection is shorter than the copy array..");
                else
                {
                    int indexInCollection = i - offsetIndex;
                    Assert.Equal(readOnlyCol[indexInCollection], aCopy[i]);
                }
            }
        }

        /// <summary>
        /// Tests that:
        /// ArgumentOutOfRangeException is thrown when the Index is >= collection.Count
        /// or Index < 0.
        /// ArgumentException when the destination array does not have enough space to
        /// contain the source Collection.
        /// ArgumentNullException when the destination array is null.
        /// </summary>
        [Fact]
        public static void CopyToTest_Negative()
        {
            string[] anArray = new string[] { "one", "two", "three", "four" };
            ReadOnlyObservableCollection<string> readOnlyCol =
                new ReadOnlyObservableCollection<string>(new ObservableCollection<string>(anArray));
            int[] iArrInvalidValues = new Int32[] { -1, -2, -100, -1000, -10000, -100000, -1000000, -10000000, -100000000, -1000000000, Int32.MinValue };
            foreach (var index in iArrInvalidValues)
            {
                string[] aCopy = new string[anArray.Length];
                Assert.Throws<ArgumentOutOfRangeException>(() => readOnlyCol.CopyTo(aCopy, index));
            }

            int[] iArrLargeValues = new Int32[] { anArray.Length, Int32.MaxValue, Int32.MaxValue / 2, Int32.MaxValue / 10 };
            foreach (var index in iArrLargeValues)
            {
                string[] aCopy = new string[anArray.Length];
                AssertExtensions.Throws<ArgumentException>("destinationArray", null, () => readOnlyCol.CopyTo(aCopy, index));
            }

            Assert.Throws<ArgumentNullException>(() => readOnlyCol.CopyTo(null, 1));

            string[] copy = new string[anArray.Length - 1];
            AssertExtensions.Throws<ArgumentException>("destinationArray", "", () => readOnlyCol.CopyTo(copy, 0));

            copy = new string[0];
            AssertExtensions.Throws<ArgumentException>("destinationArray", "", () => readOnlyCol.CopyTo(copy, 0));
        }

        /// <summary>
        /// Tests that the index of an item can be retrieved when the item is
        /// in the collection and -1 otherwise.
        /// </summary>
        [Fact]
        public static void IndexOfTest()
        {
            string[] anArray = new string[] { "one", "two", "three", "four" };
            ReadOnlyObservableCollection<string> readOnlyCollection =
                new ReadOnlyObservableCollection<string>(new ObservableCollection<string>(anArray));

            for (int i = 0; i < anArray.Length; ++i)
                Assert.Equal(i, readOnlyCollection.IndexOf(anArray[i]));

            Assert.Equal(-1, readOnlyCollection.IndexOf("seven"));
            Assert.Equal(-1, readOnlyCollection.IndexOf(null));

            // testing that the first occurrence is the index returned.
            ObservableCollection<int> intCol = new ObservableCollection<int>();
            for (int i = 0; i < 4; ++i)
                intCol.Add(i % 2);
            ReadOnlyObservableCollection<int> intReadOnlyCol = new ReadOnlyObservableCollection<int>(intCol);
           
            Assert.Equal(0, intReadOnlyCol.IndexOf(0));
            Assert.Equal(1, intReadOnlyCol.IndexOf(1));

            IList colAsIList = (IList)intReadOnlyCol;
            var index = colAsIList.IndexOf("stringObj");
            Assert.Equal(-1, index);
        }

        /// <summary> 
        /// Tests that a ReadOnlyDictionary cannot be modified. That is, that
        /// Add, Remove, Clear does not work.
        /// </summary>
        [Fact]
        public static void CannotModifyDictionaryTests_Negative()
        {
            string[] anArray = new string[] { "one", "two", "three", "four", "five" };
            ReadOnlyObservableCollection<string> readOnlyCol =
                new ReadOnlyObservableCollection<string>(new ObservableCollection<string>(anArray));
            IReadOnlyList_T_Test<string> helper = new IReadOnlyList_T_Test<string>();
            IList<string> readOnlyColAsIList = readOnlyCol;

            Assert.Throws<NotSupportedException>(() => readOnlyColAsIList.Add("seven"));
            Assert.Throws<NotSupportedException>(() => readOnlyColAsIList.Insert(0, "nine"));
            Assert.Throws<NotSupportedException>(() => readOnlyColAsIList.Remove("one"));
            Assert.Throws<NotSupportedException>(() => readOnlyColAsIList.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => readOnlyColAsIList.Clear());
            helper.VerifyReadOnlyCollection(readOnlyCol, anArray);
        }

        [Fact]
        // skip the test on desktop as "new ObservableCollection<int>()" returns 0 length collection
        // skip the test on UapAot as the requires Reflection on internal framework types.
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework | TargetFrameworkMonikers.UapAot)]
        public static void DebuggerAttribute_Tests()
        {
            ReadOnlyObservableCollection<int> col = new ReadOnlyObservableCollection<int>(new ObservableCollection<int>(new[] {1, 2, 3, 4}));
            DebuggerAttributes.ValidateDebuggerDisplayReferences(col);
            DebuggerAttributeInfo info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(col);
            PropertyInfo itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>().State == DebuggerBrowsableState.RootHidden);
            int[] items = itemProperty.GetValue(info.Instance) as int[];
            Assert.Equal(col, items);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework | TargetFrameworkMonikers.UapAot, "Cannot do DebuggerAttribute testing on UapAot: requires internal Reflection on framework types.")]
        public static void DebuggerAttribute_NullCollection_ThrowsArgumentNullException()
        {
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => DebuggerAttributes.ValidateDebuggerTypeProxyProperties(typeof(ReadOnlyObservableCollection<int>), null));
            ArgumentNullException argumentNullException = Assert.IsType<ArgumentNullException>(ex.InnerException);
        }
    }

    internal class IReadOnlyList_T_Test<T>
    {
        private readonly IReadOnlyList<T> _collection;
        private readonly T[] _expectedItems;

        /// <summary>
        /// Initializes a new instance of the IReadOnlyList_T_Test.
        /// </summary>
        /// <param name="collection">The collection to run the tests on.</param>
        /// <param name="expectedItems">The items expected to be in the collection.</param>
        public IReadOnlyList_T_Test(IReadOnlyList<T> collection, T[] expectedItems)
        {
            _collection = collection;
            _expectedItems = expectedItems;
        }

        public IReadOnlyList_T_Test()
        {

        }

        /// <summary>
        /// This verifies that the collection contains the expected items.
        /// </summary>
        public void InitialItems_Tests()
        {
            // Verify Count returns the expected value
            Assert.Equal(_expectedItems.Length, _collection.Count);
            // Verify the initial items in the collection
            VerifyReadOnlyCollection(_collection, _expectedItems);
        }

        /// <summary>
        /// Runs all of the valid tests on get Item.
        /// </summary>
        public void Item_get_Tests()
        {
            // Verify get_Item with valid item on Collection
            Verify_get(_collection, _expectedItems);
        }

        /// <summary>
        /// Runs all of the argument checking(invalid) tests on get Item.
        /// </summary>
        public void Item_get_Tests_Negative()
        {
            // Verify get_Item with index=Int32.MinValue
            Assert.Throws<ArgumentOutOfRangeException>(() => { T item = _collection[Int32.MinValue]; });

            // Verify that the collection was not mutated 
            VerifyReadOnlyCollection(_collection, _expectedItems);

            // Verify get_Item with index=-1
            Assert.Throws<ArgumentOutOfRangeException>(() => { T item = _collection[-1]; });

            // Verify that the collection was not mutated 
            VerifyReadOnlyCollection(_collection, _expectedItems);

            if (_expectedItems.Length == 0)
            {
                // Verify get_Item with index=0 on Empty collection
                Assert.Throws<ArgumentOutOfRangeException>(() => { T item = _collection[0]; });

                // Verify that the collection was not mutated 
                VerifyReadOnlyCollection(_collection, _expectedItems);
            }
            else
            {
                // Verify get_Item with index=Count on Empty collection
                Assert.Throws<ArgumentOutOfRangeException>(() => { T item = _collection[_expectedItems.Length]; });

                // Verify that the collection was not mutated 
                VerifyReadOnlyCollection(_collection, _expectedItems);
            }
        }

        #region Helper Methods

        /// <summary>
        /// Verifies that the items in the collection match the expected items.
        /// </summary>
        internal void VerifyReadOnlyCollection(IReadOnlyList<T> collection, T[] items)
        {
            Verify_get(collection, items);
            VerifyGenericEnumerator(collection, items);
            VerifyEnumerator(collection, items);
        }

        /// <summary>
        /// Verifies that you can get all items that should be in the collection.
        /// </summary>
        private void Verify_get(IReadOnlyList<T> collection, T[] items)
        {
            Assert.Equal(items.Length, collection.Count);

            for (int i = 0; i < items.Length; i++)
            {
                int itemsIndex = i;
                Assert.Equal(items[itemsIndex], collection[i]);
            }
        }

        /// <summary>
        /// Verifies that the generic enumerator retrieves the correct items.
        /// </summary>
        private void VerifyGenericEnumerator(IReadOnlyList<T> collection, T[] expectedItems)
        {
            IEnumerator<T> enumerator = collection.GetEnumerator();
            int iterations = 0;
            int expectedCount = expectedItems.Length;
            
            // There is a sequential order to the collection, so we're testing for that.
            while ((iterations < expectedCount) && enumerator.MoveNext())
            {
                T currentItem = enumerator.Current;
                T tempItem;

                // Verify we have not gotten more items then we expected
                Assert.True(iterations < expectedCount,
                    "Err_9844awpa More items have been returned from the enumerator(" + iterations + " items) than are in the expectedElements(" + expectedCount + " items)");

                // Verify Current returned the correct value
                Assert.Equal(currentItem, expectedItems[iterations]);

                // Verify Current always returns the same value every time it is called
                for (int i = 0; i < 3; i++)
                {
                    tempItem = enumerator.Current;
                    Assert.Equal(currentItem, tempItem);
                }

                iterations++;
            }

            Assert.Equal(expectedCount, iterations);

            for (int i = 0; i < 3; i++)
            {
                Assert.False(enumerator.MoveNext(), "Err_2929ahiea Expected MoveNext to return false after" + iterations + " iterations");
            }

            enumerator.Dispose();
        }

        /// <summary>
        /// Verifies that the non-generic enumerator retrieves the correct items.
        /// </summary>
        private void VerifyEnumerator(IReadOnlyList<T> collection, T[] expectedItems)
        {
            IEnumerator enumerator = collection.GetEnumerator();
            int iterations = 0;
            int expectedCount = expectedItems.Length;
            
            // There is no sequential order to the collection, so we're testing that all the items
            // in the readonlydictionary exist in the array.
            bool[] itemsVisited = new bool[expectedCount];
            bool itemFound;
            while ((iterations < expectedCount) && enumerator.MoveNext())
            {
                object currentItem = enumerator.Current;
                object tempItem;

                // Verify we have not gotten more items then we expected                
                Assert.True(iterations < expectedCount,
                    "Err_9844awpa More items have been returned from the enumerator(" + iterations + " items) then are in the expectedElements(" + expectedCount + " items)");

                // Verify Current returned the correct value
                itemFound = false;

                for (int i = 0; i < itemsVisited.Length; ++i)
                {
                    if (!itemsVisited[i] && expectedItems[i].Equals(currentItem))
                    {
                        itemsVisited[i] = true;
                        itemFound = true;
                        break;
                    }
                }
                Assert.True(itemFound, "Err_1432pauy Current returned unexpected value=" + currentItem);

                // Verify Current always returns the same value every time it is called
                for (int i = 0; i < 3; i++)
                {
                    tempItem = enumerator.Current;
                    Assert.Equal(currentItem, tempItem);
                }

                iterations++;
            }

            for (int i = 0; i < expectedCount; ++i)
            {
                Assert.True(itemsVisited[i], "Err_052848ahiedoi Expected Current to return true for item: " + expectedItems[i] + "index: " + i);
            }

            Assert.Equal(expectedCount, iterations);

            for (int i = 0; i < 3; i++)
            {
                Assert.False(enumerator.MoveNext(), "Err_2929ahiea Expected MoveNext to return false after" + iterations + " iterations");
            }
        }
        #endregion
    }
}
