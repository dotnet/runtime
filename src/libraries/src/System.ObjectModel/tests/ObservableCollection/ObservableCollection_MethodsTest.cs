// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Xunit;

namespace System.Collections.ObjectModel.Tests
{
    /// <summary>
    /// Tests the public methods in ObservableCollection<T> as well as verifies
    /// that the CollectionChanged events and eventargs are fired and populated
    /// properly.
    /// </summary>
    public static class PublicMethodsTest
    {
        /// <summary>
        /// Tests that is possible to Add an item to the collection.
        /// </summary>
        [Fact]
        public static void AddTest()
        {
            string[] anArray = { "one", "two", "three" };
            ObservableCollection<string> col = new ObservableCollection<string>(anArray);
            CollectionAndPropertyChangedTester helper = new CollectionAndPropertyChangedTester();
            helper.AddOrInsertItemTest(col, "four");
        }

        /// <summary>
        /// Tests that it is possible to remove an item from the collection.
        /// - Removing an item from the collection results in a false.
        /// - Removing null from collection returns a false.
        /// - Removing an item that has duplicates only takes out the first instance.
        /// </summary>
        [Fact]
        public static void RemoveTest()
        {
            // trying to remove item in collection.
            string[] anArray = { "one", "two", "three", "four" };
            ObservableCollection<string> col = new ObservableCollection<string>(anArray);
            CollectionAndPropertyChangedTester helper = new CollectionAndPropertyChangedTester();
            helper.RemoveItemTest(col, 2, "three", true, hasDuplicates: false);

            // trying to remove item not in collection.
            anArray = new string[] { "one", "two", "three", "four" };
            col = new ObservableCollection<string>(anArray);
            helper = new CollectionAndPropertyChangedTester();
            helper.RemoveItemTest(col, -1, "three2", false, hasDuplicates: false);

            // removing null
            anArray = new string[] { "one", "two", "three", "four" };
            col = new ObservableCollection<string>(anArray);
            helper = new CollectionAndPropertyChangedTester();
            helper.RemoveItemTest(col, -1, null, false, hasDuplicates: false);

            // trying to remove item in collection that has duplicates.
            anArray = new string[] { "one", "three", "two", "three", "four" };
            col = new ObservableCollection<string>(anArray);
            helper = new CollectionAndPropertyChangedTester();
            helper.RemoveItemTest(col, 1, "three", true, hasDuplicates: true);
            // want to ensure that there is one "three" left in collection and not both were removed.
            int occurrencesThree = 0;
            foreach (var item in col)
            {
                if (item.Equals("three"))
                    occurrencesThree++;
            }
            Assert.Equal(1, occurrencesThree);
        }

        /// <summary>
        /// Tests that a collection can be cleared.
        /// </summary>
        [Fact]
        public static void ClearTest()
        {
            string[] anArray = { "one", "two", "three", "four" };
            ObservableCollection<string> col = new ObservableCollection<string>(anArray);

            col.Clear();
            Assert.Equal(0, col.Count);
            Assert.Empty(col);

            Assert.Throws<ArgumentOutOfRangeException>(() => col[1]);

            //tests that the collectionChanged events are fired.
            CollectionAndPropertyChangedTester helper = new CollectionAndPropertyChangedTester();
            col = new ObservableCollection<string>(anArray);
            helper.ClearTest(col);
        }

        /// <summary>
        /// Tests that we can remove items at a specific index, at the middle beginning and end.
        /// </summary>
        [Fact]
        public static void RemoveAtTest()
        {
            Guid[] anArray = { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            ObservableCollection<Guid> col0 = new ObservableCollection<Guid>((IEnumerable<Guid>)anArray);
            ObservableCollection<Guid> col1 = new ObservableCollection<Guid>((IEnumerable<Guid>)anArray);
            ObservableCollection<Guid> col2 = new ObservableCollection<Guid>((IEnumerable<Guid>)anArray);

            col0.RemoveAt(0);
            string collectionString = "";
            foreach (var item in col1)
                collectionString += item + ", ";
            Assert.False(col0.Contains(anArray[0]), "Collection0 should no longer contain the item: " + anArray[0] + " Collection: " + collectionString);

            col1.RemoveAt(1);
            collectionString = "";
            foreach (var item in col1)
                collectionString += item + ", ";
            Assert.False(col1.Contains(anArray[1]), "Collection1 should no longer contain the item: " + anArray[1] + " Collection: " + collectionString);

            col2.RemoveAt(2);
            collectionString = "";
            foreach (var item in col2)
                collectionString += item + ", ";
            Assert.False(col2.Contains(anArray[2]), "Collection2 should no longer contain the item: " + anArray[2] + " Collection: " + collectionString);

            string[] anArrayString = { "one", "two", "three", "four" };
            ObservableCollection<string> col = new ObservableCollection<string>(anArrayString);
            CollectionAndPropertyChangedTester helper = new CollectionAndPropertyChangedTester();
            helper.RemoveItemAtTest(col, 1);
        }

        /// <summary>
        /// Tests that exceptions are thrown:
        /// ArgumentOutOfRangeException when index < 0 or index >= collection.Count.
        /// And that the collection does not change.
        /// </summary>
        [Fact]
        public static void RemoveAtTest_Negative()
        {
            Guid[] anArray = { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            ObservableCollection<Guid> collection = new ObservableCollection<Guid>(anArray);
            collection.CollectionChanged += (o, e) => { throw new ShouldNotBeInvokedException(); };
            int[] iArrInvalidValues = new Int32[] { -1, -2, -100, -1000, -10000, -100000, -1000000, -10000000, -100000000, -1000000000, Int32.MinValue };
            foreach (var index in iArrInvalidValues)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => collection.RemoveAt(index));
                Assert.Equal(anArray.Length, collection.Count);
            }

            int[] iArrLargeValues = new Int32[] { collection.Count, Int32.MaxValue, Int32.MaxValue / 2, Int32.MaxValue / 10 };
            foreach (var index in iArrLargeValues)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => collection.RemoveAt(index));
                Assert.Equal(anArray.Length, collection.Count);
            }
        }

        /// <summary>
        /// Tests that items can be moved throughout a collection whether from 
        /// beginning to end, etc.
        /// </summary>
        [Fact]
        public static void MoveTest()
        {
            Guid[] anArray = { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            ObservableCollection<Guid> col01 = new ObservableCollection<Guid>((IEnumerable<Guid>)anArray);
            ObservableCollection<Guid> col10 = new ObservableCollection<Guid>((IEnumerable<Guid>)anArray);
            ObservableCollection<Guid> col12 = new ObservableCollection<Guid>((IEnumerable<Guid>)anArray);
            ObservableCollection<Guid> col21 = new ObservableCollection<Guid>((IEnumerable<Guid>)anArray);
            ObservableCollection<Guid> col20 = new ObservableCollection<Guid>((IEnumerable<Guid>)anArray);

            col01.Move(0, 1);
            Assert.Equal(anArray[0], col01[1]);

            col10.Move(1, 0);
            Assert.Equal(anArray[1], col10[0]);

            col12.Move(1, 2);
            Assert.Equal(anArray[1], col12[2]);

            col21.Move(2, 1);
            Assert.Equal(anArray[2], col21[1]);

            col20.Move(2, 0);
            Assert.Equal(anArray[2], col20[0]);

            CollectionAndPropertyChangedTester helper = new CollectionAndPropertyChangedTester();
            string[] anArrayString = new string[] { "one", "two", "three", "four" };
            ObservableCollection<string> collection = new ObservableCollection<string>(anArrayString);
            helper.MoveItemTest(collection, 0, 2);
            helper.MoveItemTest(collection, 3, 0);
            helper.MoveItemTest(collection, 1, 2);
        }

        /// <summary>
        /// Tests that:
        /// ArgumentOutOfRangeException is thrown when the source or destination 
        /// Index is >= collection.Count or Index < 0.
        /// </summary>
        /// <remarks>
        /// When the sourceIndex is valid, the item actually is removed from the list.
        /// </remarks>
        [Fact]
        public static void MoveTest_Negative()
        {
            string[] anArray = new string[] { "one", "two", "three", "four" };
            ObservableCollection<string> collection = null;

            int validIndex = 2;
            int[] iArrInvalidValues = new Int32[] { -1, -2, -100, -1000, -10000, -100000, -1000000, -10000000, -100000000, -1000000000, Int32.MinValue };
            int[] iArrLargeValues = new Int32[] { anArray.Length, Int32.MaxValue, Int32.MaxValue / 2, Int32.MaxValue / 10 };

            foreach (var index in iArrInvalidValues)
            {
                collection = new ObservableCollection<string>(anArray);
                collection.CollectionChanged += (o, e) => { throw new ShouldNotBeInvokedException(); };

                // invalid startIndex, valid destination index.
                Assert.Throws<ArgumentOutOfRangeException>(() => collection.Move(index, validIndex));
                Assert.Equal(anArray.Length, collection.Count);

                // valid startIndex, invalid destIndex.
                Assert.Throws<ArgumentOutOfRangeException>(() => collection.Move(validIndex, index));
                //NOTE: It actually moves the item right out of the collection.So the count is one less.
                //Assert.Equal(anArray.Length, collection.Count, "Collection should not have changed. index: " + index);
            }

            foreach (var index in iArrLargeValues)
            {
                collection = new ObservableCollection<string>(anArray);
                collection.CollectionChanged += (o, e) => { throw new ShouldNotBeInvokedException(); };

                // invalid startIndex, valid destination index.
                Assert.Throws<ArgumentOutOfRangeException>(() => collection.Move(index, validIndex));
                Assert.Equal(anArray.Length, collection.Count);

                // valid startIndex, invalid destIndex.
                Assert.Throws<ArgumentOutOfRangeException>(() => collection.Move(validIndex, index));
                //NOTE: It actually moves the item right out of the collection. So the count is one less.
                //Assert.Equal(anArray.Length, collection.Count, "Collection should not have changed.");
            }
        }

        /// <summary>
        /// Tests that an item can be inserted throughout the collection.
        /// </summary>
        [Fact]
        public static void InsertTest()
        {
            Guid[] anArray = { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            ObservableCollection<Guid> col0 = new ObservableCollection<Guid>((IEnumerable<Guid>)anArray);
            ObservableCollection<Guid> col1 = new ObservableCollection<Guid>((IEnumerable<Guid>)anArray);
            ObservableCollection<Guid> col3 = new ObservableCollection<Guid>((IEnumerable<Guid>)anArray);

            //inserting item at the beginning.
            Guid g0 = Guid.NewGuid();
            col0.Insert(0, g0);
            Assert.Equal(g0, col0[0]);

            // inserting item in the middle
            Guid g1 = Guid.NewGuid();
            col1.Insert(1, g1);
            Assert.Equal(g1, col1[1]);

            // inserting item at the end.
            Guid g3 = Guid.NewGuid();
            col3.Insert(col3.Count, g3);
            Assert.Equal(g3, col3[col3.Count - 1]);

            string[] anArrayString = new string[] { "one", "two", "three", "four" };
            ObservableCollection<string> collection = new ObservableCollection<string>((IEnumerable<string>)anArrayString);
            CollectionAndPropertyChangedTester helper = new CollectionAndPropertyChangedTester();
            helper.AddOrInsertItemTest(collection, "seven", 2);
            helper.AddOrInsertItemTest(collection, "zero", 0);
            helper.AddOrInsertItemTest(collection, "eight", collection.Count);
        }

        /// <summary>
        /// Tests that:
        /// ArgumentOutOfRangeException is thrown when the Index is >= collection.Count
        /// or Index < 0.  And ensures that the collection does not change.
        /// </summary>
        [Fact]
        public static void InsertTest_Negative()
        {
            Guid[] anArray = { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            ObservableCollection<Guid> collection = new ObservableCollection<Guid>(anArray);
            collection.CollectionChanged += (o, e) => { throw new ShouldNotBeInvokedException(); };

            Guid itemToInsert = Guid.NewGuid();
            int[] iArrInvalidValues = new Int32[] { -1, -2, -100, -1000, -10000, -100000, -1000000, -10000000, -100000000, -1000000000, Int32.MinValue };
            foreach (var index in iArrInvalidValues)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => collection.Insert(index, itemToInsert));
                Assert.Equal(anArray.Length, collection.Count);
            }

            int[] iArrLargeValues = new Int32[] { collection.Count + 1, Int32.MaxValue, Int32.MaxValue / 2, Int32.MaxValue / 10 };
            foreach (var index in iArrLargeValues)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => collection.Insert(index, itemToInsert));
                Assert.Equal(anArray.Length, collection.Count);
            }
        }

        /// <summary>
        /// Tests that the appropriate collectionchanged event is fired when
        /// an item is replaced in the collection.
        /// </summary>
        [Fact]
        public static void ReplaceItemTest()
        {
            string[] anArray = new string[] { "one", "two", "three", "four" };
            ObservableCollection<string> collection = new ObservableCollection<string>((IEnumerable<string>)anArray);
            CollectionAndPropertyChangedTester helper = new CollectionAndPropertyChangedTester();
            helper.ReplaceItemTest(collection, 1, "seven");
            helper.ReplaceItemTest(collection, 3, "zero");
        }

        /// <summary>
        /// Tests that contains returns true when the item is in the collection
        /// and false otherwise.
        /// </summary>
        [Fact]
        public static void ContainsTest()
        {
            string[] anArray = new string[] { "one", "two", "three", "four" };
            ObservableCollection<string> collection = new ObservableCollection<string>((IEnumerable<string>)anArray);
            string collectionString = "";

            foreach (var item in collection)
                collectionString += item + ", ";

            for (int i = 0; i < collection.Count; ++i)
                Assert.True(collection.Contains(anArray[i]), "ObservableCollection did not contain the item: " + anArray[i] + " Collection: " + collectionString);

            string g = "six";
            Assert.False(collection.Contains(g), "Collection contained an item that should not have been there. guid: " + g + " Collection: " + collectionString);
            Assert.False(collection.Contains(null), "Collection should not have contained null. Collection: " + collectionString);
        }

        /// <summary>
        /// Tests that the index of an item can be retrieved when the item is
        /// in the collection and -1 otherwise.
        /// </summary>
        [Fact]
        public static void IndexOfTest()
        {
            string[] anArray = new string[] { "one", "two", "three", "four" };
            ObservableCollection<string> collection = new ObservableCollection<string>((IEnumerable<string>)anArray);

            for (int i = 0; i < anArray.Length; ++i)
                Assert.Equal(i, collection.IndexOf(anArray[i]));

            Assert.Equal(-1, collection.IndexOf("seven"));
            Assert.Equal(-1, collection.IndexOf(null));

            // testing that the first occurrence is the index returned.
            ObservableCollection<int> intCol = new ObservableCollection<int>();
            for (int i = 0; i < 4; ++i)
                intCol.Add(i % 2);

            Assert.Equal(0, intCol.IndexOf(0));
            Assert.Equal(1, intCol.IndexOf(1));

            IList colAsIList = (IList)intCol;
            var index = colAsIList.IndexOf("stringObj");
            Assert.Equal(-1, index);
        }

        /// <summary>
        /// Tests that the collection can be copied into a destination array.
        /// </summary>
        [Fact]
        public static void CopyToTest()
        {
            string[] anArray = new string[] { "one", "two", "three", "four" };
            ObservableCollection<string> collection = new ObservableCollection<string>((IEnumerable<string>)anArray);

            string[] aCopy = new string[collection.Count];
            collection.CopyTo(aCopy, 0);
            for (int i = 0; i < anArray.Length; ++i)
                Assert.Equal(anArray[i], aCopy[i]);

            // copy observable collection starting in middle, where array is larger than source.
            aCopy = new string[collection.Count + 2];
            int offsetIndex = 1;
            collection.CopyTo(aCopy, offsetIndex);
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
                    Assert.Equal(collection[indexInCollection], aCopy[i]);
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
            ObservableCollection<string> collection = new ObservableCollection<string>(anArray);

            int[] iArrInvalidValues = new Int32[] { -1, -2, -100, -1000, -10000, -100000, -1000000, -10000000, -100000000, -1000000000, Int32.MinValue };
            foreach (var index in iArrInvalidValues)
            {
                string[] aCopy = new string[collection.Count];
                Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(aCopy, index));
            }

            int[] iArrLargeValues = new Int32[] { collection.Count, Int32.MaxValue, Int32.MaxValue / 2, Int32.MaxValue / 10 };
            foreach (var index in iArrLargeValues)
            {
                string[] aCopy = new string[collection.Count];
                AssertExtensions.Throws<ArgumentException>("destinationArray", null, () => collection.CopyTo(aCopy, index));
            }

            Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null, 1));

            string[] copy = new string[collection.Count - 1];
            AssertExtensions.Throws<ArgumentException>("destinationArray", "", () => collection.CopyTo(copy, 0));

            copy = new string[0];
            AssertExtensions.Throws<ArgumentException>("destinationArray", "", () => collection.CopyTo(copy, 0));
        }

        /// <summary>
        /// Tests that it is possible to iterate through the collection with an
        /// Enumerator.
        /// </summary>
        [Fact]
        public static void GetEnumeratorTest()
        {
            Guid[] anArray = { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            ObservableCollection<Guid> col = new ObservableCollection<Guid>((IEnumerable<Guid>)anArray);

            int i = 0;
            IEnumerator<Guid> e;
            for (e = col.GetEnumerator(); e.MoveNext(); ++i)
            {
                Assert.Equal(anArray[i], e.Current);
            }
            Assert.Equal(col.Count, i);
            e.Dispose();
        }
    }

    /// <summary>
    /// Helper class to test the CollectionChanged and PropertyChanged Events.
    /// </summary>
    public class CollectionAndPropertyChangedTester
    {
        #region Properties

        private const string COUNT = "Count";
        private const string ITEMARRAY = "Item[]";

        // Number of collection changed events that were ACTUALLY fired.
        public int NumCollectionChangedFired { get; private set; }
        // Number of collection changed events that are EXPECTED to be fired.
        public int ExpectedCollectionChangedFired { get; private set; }

        public int ExpectedNewStartingIndex { get; private set; }
        public NotifyCollectionChangedAction ExpectedAction { get; private set; }
        public IList ExpectedNewItems { get; private set; }
        public IList ExpectedOldItems { get; private set; }
        public int ExpectedOldStartingIndex { get; private set; }

        private PropertyNameExpected[] _expectedPropertyChanged;

        #endregion

        #region Helper Methods

        /// <summary>
        /// Will perform an Add or Insert on the given Collection depending on whether the 
        /// insertIndex is null or not. If it is null, will Add, otherwise, will Insert.
        /// </summary>
        public void AddOrInsertItemTest(ObservableCollection<string> collection, string itemToAdd, int? insertIndex = null)
        {
            INotifyPropertyChanged collectionPropertyChanged = collection;
            collectionPropertyChanged.PropertyChanged += Collection_PropertyChanged;
            _expectedPropertyChanged = new[]
            {
                new PropertyNameExpected(COUNT),
                new PropertyNameExpected(ITEMARRAY)
            };

            collection.CollectionChanged += Collection_CollectionChanged;

            ExpectedCollectionChangedFired++;
            ExpectedAction = NotifyCollectionChangedAction.Add;
            ExpectedNewItems = new string[] { itemToAdd };
            if (insertIndex.HasValue)
                ExpectedNewStartingIndex = insertIndex.Value;
            else
                ExpectedNewStartingIndex = collection.Count;
            ExpectedOldItems = null;
            ExpectedOldStartingIndex = -1;

            int expectedCount = collection.Count + 1;

            if (insertIndex.HasValue)
            {
                collection.Insert(insertIndex.Value, itemToAdd);
                Assert.Equal(itemToAdd, collection[insertIndex.Value]);
            }
            else
            {
                collection.Add(itemToAdd);
                Assert.Equal(itemToAdd, collection[collection.Count - 1]);
            }

            Assert.Equal(expectedCount, collection.Count);
            Assert.Equal(ExpectedCollectionChangedFired, NumCollectionChangedFired);


            foreach (var item in _expectedPropertyChanged)
                Assert.True(item.IsFound, "The propertychanged event should have fired for" + item.Name + ", since we just added an item");

            collection.CollectionChanged -= Collection_CollectionChanged;
            collectionPropertyChanged.PropertyChanged -= Collection_PropertyChanged;
        }

        /// <summary>
        /// Clears the given Collection.
        /// </summary>
        public void ClearTest(ObservableCollection<string> collection)
        {
            INotifyPropertyChanged collectionPropertyChanged = collection;
            collectionPropertyChanged.PropertyChanged += Collection_PropertyChanged;
            _expectedPropertyChanged = new[]
            {
                new PropertyNameExpected(COUNT),
                new PropertyNameExpected(ITEMARRAY)
            };

            collection.CollectionChanged += Collection_CollectionChanged;
            ExpectedCollectionChangedFired++;
            ExpectedAction = NotifyCollectionChangedAction.Reset;
            ExpectedNewItems = null;
            ExpectedNewStartingIndex = -1;
            ExpectedOldItems = null;
            ExpectedOldStartingIndex = -1;

            collection.Clear();
            Assert.Equal(0, collection.Count);
            Assert.Equal(ExpectedCollectionChangedFired, NumCollectionChangedFired);

            foreach (var item in _expectedPropertyChanged)
                Assert.True(item.IsFound, "The propertychanged event should have fired for" + item.Name + ", since we just cleared the collection");

            collection.CollectionChanged -= Collection_CollectionChanged;
            collectionPropertyChanged.PropertyChanged -= Collection_PropertyChanged;
        }

        /// <summary>
        /// Given a collection, will move an item from the oldIndex to the newIndex.
        /// </summary>
        public void MoveItemTest(ObservableCollection<string> collection, int oldIndex, int newIndex)
        {
            INotifyPropertyChanged collectionPropertyChanged = collection;
            collectionPropertyChanged.PropertyChanged += Collection_PropertyChanged;
            _expectedPropertyChanged = new[] { new PropertyNameExpected(ITEMARRAY) };

            collection.CollectionChanged += Collection_CollectionChanged;

            string itemAtOldIndex = collection[oldIndex];

            ExpectedCollectionChangedFired++;
            ExpectedAction = NotifyCollectionChangedAction.Move;
            ExpectedNewItems = new string[] { itemAtOldIndex };
            ExpectedNewStartingIndex = newIndex;
            ExpectedOldItems = new string[] { itemAtOldIndex };
            ExpectedOldStartingIndex = oldIndex;

            int expectedCount = collection.Count;

            collection.Move(oldIndex, newIndex);
            Assert.Equal(expectedCount, collection.Count);
            Assert.Equal(itemAtOldIndex, collection[newIndex]);
            Assert.Equal(ExpectedCollectionChangedFired, NumCollectionChangedFired);

            foreach (var item in _expectedPropertyChanged)
                Assert.True(item.IsFound, "The propertychanged event should have fired for" + item.Name + ", since we just moved an item");

            collection.CollectionChanged -= Collection_CollectionChanged;
            collectionPropertyChanged.PropertyChanged -= Collection_PropertyChanged;
        }

        /// <summary>
        /// Will set that new item at the specified index in the given collection.
        /// </summary>
        public void ReplaceItemTest(ObservableCollection<string> collection, int index, string newItem)
        {
            INotifyPropertyChanged collectionPropertyChanged = collection;
            collectionPropertyChanged.PropertyChanged += Collection_PropertyChanged;
            _expectedPropertyChanged = new[] { new PropertyNameExpected(ITEMARRAY) };

            collection.CollectionChanged += Collection_CollectionChanged;

            string itemAtOldIndex = collection[index];

            ExpectedCollectionChangedFired++;
            ExpectedAction = NotifyCollectionChangedAction.Replace;
            ExpectedNewItems = new string[] { newItem };
            ExpectedNewStartingIndex = index;
            ExpectedOldItems = new string[] { itemAtOldIndex };
            ExpectedOldStartingIndex = index;

            int expectedCount = collection.Count;

            collection[index] = newItem;
            Assert.Equal(expectedCount, collection.Count);
            Assert.Equal(newItem, collection[index]);
            Assert.Equal(ExpectedCollectionChangedFired, NumCollectionChangedFired);

            foreach (var item in _expectedPropertyChanged)
                Assert.True(item.IsFound, "The propertychanged event should have fired for" + item.Name + ", since we just replaced an item");

            collection.CollectionChanged -= Collection_CollectionChanged;
            collectionPropertyChanged.PropertyChanged -= Collection_PropertyChanged;
        }

        /// <summary>
        /// Given a collection, index and item to remove, will try to remove that item
        /// from the index. If the item has duplicates, will verify that only the first
        /// instance was removed.
        /// </summary>
        public void RemoveItemTest(ObservableCollection<string> collection, int itemIndex, string itemToRemove, bool isSuccessfulRemove, bool hasDuplicates)
        {
            INotifyPropertyChanged collectionPropertyChanged = collection;
            collectionPropertyChanged.PropertyChanged += Collection_PropertyChanged;
            _expectedPropertyChanged = new[]
            {
                new PropertyNameExpected(COUNT),
                new PropertyNameExpected(ITEMARRAY)
            };

            collection.CollectionChanged += Collection_CollectionChanged;

            if (isSuccessfulRemove)
                ExpectedCollectionChangedFired++;

            ExpectedAction = NotifyCollectionChangedAction.Remove;
            ExpectedNewItems = null;
            ExpectedNewStartingIndex = -1;
            ExpectedOldItems = new string[] { itemToRemove };
            ExpectedOldStartingIndex = itemIndex;

            int expectedCount = isSuccessfulRemove ? collection.Count - 1 : collection.Count;

            bool removedItem = collection.Remove(itemToRemove);
            Assert.Equal(expectedCount, collection.Count);
            Assert.Equal(ExpectedCollectionChangedFired, NumCollectionChangedFired);

            if (isSuccessfulRemove)
            {
                foreach (var item in _expectedPropertyChanged)
                    Assert.True(item.IsFound, "The propertychanged event should have fired for" + item.Name + ", since there were items removed.");

                Assert.True(removedItem, "Should have been successful in removing the item.");
            }
            else
            {
                foreach (var item in _expectedPropertyChanged)
                    Assert.False(item.IsFound, "The propertychanged event should have fired for" + item.Name + ", since there were no items removed.");

                Assert.False(removedItem, "Should not have been successful in removing the item.");
            }
            if (hasDuplicates)
                return;

            Assert.DoesNotContain(itemToRemove, collection);

            collection.CollectionChanged -= Collection_CollectionChanged;
            collectionPropertyChanged.PropertyChanged -= Collection_PropertyChanged;
        }

        /// <summary>
        /// Verifies that the item is removed from a given index in the collection.
        /// </summary>
        public void RemoveItemAtTest(ObservableCollection<string> collection, int itemIndex)
        {
            INotifyPropertyChanged collectionPropertyChanged = collection;
            collectionPropertyChanged.PropertyChanged += Collection_PropertyChanged;
            _expectedPropertyChanged = new[]
            {
                new PropertyNameExpected(COUNT),
                new PropertyNameExpected(ITEMARRAY)
            };

            collection.CollectionChanged += Collection_CollectionChanged;

            string itemAtOldIndex = collection[itemIndex];

            ExpectedCollectionChangedFired++;
            ExpectedAction = NotifyCollectionChangedAction.Remove;
            ExpectedNewItems = null;
            ExpectedNewStartingIndex = -1;
            ExpectedOldItems = new string[] { itemAtOldIndex };
            ExpectedOldStartingIndex = itemIndex;

            int expectedCount = collection.Count - 1;

            collection.RemoveAt(itemIndex);
            Assert.Equal(expectedCount, collection.Count);
            Assert.Equal(ExpectedCollectionChangedFired, NumCollectionChangedFired);

            foreach (var item in _expectedPropertyChanged)
                Assert.True(item.IsFound, "The propertychanged event should have fired for" + item.Name + ", since we just removed an item");

            collection.CollectionChanged -= Collection_CollectionChanged;
            collectionPropertyChanged.PropertyChanged -= Collection_PropertyChanged;
        }

        /// <summary>
        /// Verifies that the eventargs fired matches the expected results.
        /// </summary>
        private void VerifyEventArgs(NotifyCollectionChangedEventArgs e)
        {
            Assert.Equal(ExpectedNewStartingIndex, e.NewStartingIndex);
            Assert.Equal(ExpectedOldStartingIndex, e.OldStartingIndex);

            if (ExpectedNewItems != null)
            {
                foreach (var newItem in e.NewItems)
                    Assert.True(ExpectedNewItems.Contains(newItem), "newItem was not in the ExpectedNewItems. newItem: " + newItem);
                foreach (var expectedItem in ExpectedNewItems)
                    Assert.True(e.NewItems.Contains(expectedItem), "expectedItem was not in e.NewItems. expectedItem: " + expectedItem);
            }
            else
            {
                Assert.Null(e.NewItems);
            }

            if (ExpectedOldItems != null)
            {
                foreach (var oldItem in e.OldItems)
                    Assert.True(ExpectedOldItems.Contains(oldItem), "oldItem was not in the ExpectedOldItems. oldItem: " + oldItem);
                foreach (var expectedItem in ExpectedOldItems)
                    Assert.True(e.OldItems.Contains(expectedItem), "expectedItem was not in e.OldItems. expectedItem: " + expectedItem);
            }
            else
            {
                Assert.Null(e.OldItems);
            }
        }

        private void Collection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NumCollectionChangedFired++;
            Assert.Equal(ExpectedAction, e.Action);
            switch (ExpectedAction)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Reset:
                case NotifyCollectionChangedAction.Replace:
                    VerifyEventArgs(e);
                    break;
                default:
                    throw new NotSupportedException("Does not support this action yet.");
            }
        }

        private void Collection_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            foreach (var item in _expectedPropertyChanged)
            {
                if (item.Name == e.PropertyName)
                    item.IsFound = true;
            }
        }

        /// <summary>
        /// Helper class to keep track of what propertychanges we expect and whether they were found or not.
        /// </summary>
        private class PropertyNameExpected
        {
            internal PropertyNameExpected(string name)
            {
                Name = name;
            }

            internal string Name { get; private set; }
            internal bool IsFound { get; set; }
        }
        #endregion
    }
}
