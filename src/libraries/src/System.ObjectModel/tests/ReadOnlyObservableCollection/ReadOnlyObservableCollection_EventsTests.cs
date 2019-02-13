// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Specialized;
using System.ComponentModel;
using Xunit;

namespace System.Collections.ObjectModel.Tests
{
    /// <summary>
    /// Tests that the INotifyCollectionChanged and IPropertyChanged events are fired 
    /// when an item is changed in the underlying ObservableCollection<T>.
    /// </summary>
    public class ReadOnlyObservableCollection_EventsTests
    {
        /// <summary>
        /// Tests for an Add action.
        /// </summary>
        [Fact]
        public static void AddTest()
        {
            string[] anArray = { "one", "two", "three" };
            ObservableCollection<string> col = new ObservableCollection<string>(anArray);
            ReadOnlyObservableCollection<string> readonlyCol = new ReadOnlyObservableCollection<string>(col);
            ReadOnlyCollectionAndPropertyChangedTester helper = new ReadOnlyCollectionAndPropertyChangedTester();
            helper.AddOrInsertItemTest(readonlyCol, col, "four");
        }

        /// <summary>
        /// Tests that it is possible to remove an item from the collection and the events
        /// are forwarded.
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
            ReadOnlyObservableCollection<string> readonlyCol = new ReadOnlyObservableCollection<string>(col);
            ReadOnlyCollectionAndPropertyChangedTester helper = new ReadOnlyCollectionAndPropertyChangedTester();
            helper.RemoveItemTest(readonlyCol, col, 2, "three", true, hasDuplicates: false);

            // trying to remove item not in collection.
            anArray = new string[] { "one", "two", "three", "four" };
            col = new ObservableCollection<string>(anArray);
            readonlyCol = new ReadOnlyObservableCollection<string>(col);
            helper = new ReadOnlyCollectionAndPropertyChangedTester();
            helper.RemoveItemTest(readonlyCol, col, -1, "three2", false, hasDuplicates: false);

            // removing null
            anArray = new string[] { "one", "two", "three", "four" };
            col = new ObservableCollection<string>(anArray);
            readonlyCol = new ReadOnlyObservableCollection<string>(col);
            helper = new ReadOnlyCollectionAndPropertyChangedTester();
            helper.RemoveItemTest(readonlyCol, col, -1, null, false, hasDuplicates: false);

            // trying to remove item in collection that has duplicates.
            anArray = new string[] { "one", "three", "two", "three", "four" };
            col = new ObservableCollection<string>(anArray);
            readonlyCol = new ReadOnlyObservableCollection<string>(col);
            helper = new ReadOnlyCollectionAndPropertyChangedTester();
            helper.RemoveItemTest(readonlyCol, col, 1, "three", true, hasDuplicates: true);
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
        /// Tests that a collection can be cleared and its events are forwarded.
        /// </summary>
        [Fact]
        public static void ClearTest()
        {
            string[] anArray = { "one", "two", "three", "four" };
            //tests that the collectionChanged events are fired.
            ReadOnlyCollectionAndPropertyChangedTester helper = new ReadOnlyCollectionAndPropertyChangedTester();
            ObservableCollection<string> col = new ObservableCollection<string>(anArray);
            ReadOnlyObservableCollection<string> readonlyCol = new ReadOnlyObservableCollection<string>(col);
            helper.ClearTest(readonlyCol, col);
        }

        /// <summary>
        /// Tests that we can remove items at a specific index, at the middle beginning and end.
        /// And its events are forwarded.
        /// </summary>
        [Fact]
        public static void RemoveAtTest()
        {
            string[] anArrayString = { "one", "two", "three", "four" };
            ObservableCollection<string> col = new ObservableCollection<string>(anArrayString);
            ReadOnlyObservableCollection<string> readonlyCol = new ReadOnlyObservableCollection<string>(col);
            ReadOnlyCollectionAndPropertyChangedTester helper = new ReadOnlyCollectionAndPropertyChangedTester();
            helper.RemoveItemAtTest(readonlyCol, col, 1);
        }

        /// <summary>
        /// Tests that exceptions are thrown:
        /// ArgumentOutOfRangeException when index < 0 or index >= collection.Count.
        /// And that the collection does not change.
        /// And no events are forwarded to the collection.
        /// </summary>
        [Fact]
        public static void RemoveAtTest_Negative()
        {
            Guid[] anArray = { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            ObservableCollection<Guid> collection = new ObservableCollection<Guid>(anArray);
            ReadOnlyObservableCollection<Guid> readonlyCol = new ReadOnlyObservableCollection<Guid>(collection);
            ((INotifyCollectionChanged)readonlyCol).CollectionChanged += (o, e) => { throw new ShouldNotBeInvokedException(); };

            int[] iArrInvalidValues = new int[] { -1, -2, -100, -1000, -10000, -100000, -1000000, -10000000, -100000000, -1000000000, int.MinValue };
            foreach (var index in iArrInvalidValues)
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collection.RemoveAt(index));
                Assert.Equal(anArray.Length, readonlyCol.Count);
            }

            int[] iArrLargeValues = new int[] { collection.Count, int.MaxValue, int.MaxValue / 2, int.MaxValue / 10 };
            foreach (var index in iArrLargeValues)
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collection.RemoveAt(index));
                Assert.Equal(anArray.Length, readonlyCol.Count);
            }
        }

        /// <summary>
        /// Tests that items can be moved throughout a collection whether from 
        /// beginning to end, etc. And the events are forwarded.
        /// </summary>
        [Fact]
        public static void MoveTest()
        {
            ReadOnlyCollectionAndPropertyChangedTester helper = new ReadOnlyCollectionAndPropertyChangedTester();
            string[] anArrayString = new string[] { "one", "two", "three", "four" };
            ObservableCollection<string> collection = new ObservableCollection<string>(anArrayString);
            ReadOnlyObservableCollection<string> readonlyCol = new ReadOnlyObservableCollection<string>(collection);
            helper.MoveItemTest(readonlyCol, collection, 0, 2);
            helper.MoveItemTest(readonlyCol, collection, 3, 0);
            helper.MoveItemTest(readonlyCol, collection, 1, 2);
        }

        /// <summary>
        /// Tests that:
        /// ArgumentOutOfRangeException is thrown when the source or destination 
        /// Index is >= collection.Count or Index < 0.
        /// And the events are not forwarded.
        /// </summary>
        /// <remarks>
        /// When the sourceIndex is valid, the item actually is removed from the list.
        /// </remarks>
        [Fact]
        public static void MoveTest_Negative()
        {
            string[] anArray = new string[] { "one", "two", "three", "four" };
            ObservableCollection<string> collection = new ObservableCollection<string>(anArray);
            ReadOnlyObservableCollection<string> readonlyCol = new ReadOnlyObservableCollection<string>(collection);
            ((INotifyCollectionChanged)readonlyCol).CollectionChanged += (o, e) => { throw new ShouldNotBeInvokedException(); };

            int validIndex = 2;
            int[] iArrInvalidValues = new int[] { -1, -2, -100, -1000, -10000, -100000, -1000000, -10000000, -100000000, -1000000000, int.MinValue };
            int[] iArrLargeValues = new int[] { anArray.Length, int.MaxValue, int.MaxValue / 2, int.MaxValue / 10 };

            foreach (var index in iArrInvalidValues)
            {
                // invalid startIndex, valid destination index.
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collection.Move(index, validIndex));
                Assert.Equal(anArray.Length, collection.Count);
            }

            foreach (var index in iArrLargeValues)
            {
                // invalid startIndex, valid destination index.
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collection.Move(index, validIndex));
                Assert.Equal(anArray.Length, collection.Count);
            }
        }

        /// <summary>
        /// Tests that an item can be inserted throughout the collection.
        /// And the events are forwarded.
        /// </summary>
        [Fact]
        public static void InsertTest()
        {
            string[] anArrayString = new string[] { "one", "two", "three", "four" };
            ObservableCollection<string> collection = new ObservableCollection<string>(anArrayString);
            ReadOnlyObservableCollection<string> readonlyCol = new ReadOnlyObservableCollection<string>(collection);
            ReadOnlyCollectionAndPropertyChangedTester helper = new ReadOnlyCollectionAndPropertyChangedTester();
            helper.AddOrInsertItemTest(readonlyCol, collection, "seven", 2);
            helper.AddOrInsertItemTest(readonlyCol, collection, "zero", 0);
            helper.AddOrInsertItemTest(readonlyCol, collection, "eight", collection.Count);
        }

        /// <summary>
        /// Tests that:
        /// ArgumentOutOfRangeException is thrown when the Index is >= collection.Count
        /// or Index < 0.  And ensures that the collection does not change.
        /// And the events are not forwarded.
        /// </summary>
        [Fact]
        public static void InsertTest_Negative()
        {
            Guid[] anArray = { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            ObservableCollection<Guid> collection = new ObservableCollection<Guid>(anArray);
            ReadOnlyObservableCollection<Guid> readonlyCol = new ReadOnlyObservableCollection<Guid>(collection);
            ((INotifyCollectionChanged)readonlyCol).CollectionChanged += (o, e) => { throw new ShouldNotBeInvokedException(); };

            Guid itemToInsert = Guid.NewGuid();
            int[] iArrInvalidValues = new int[] { -1, -2, -100, -1000, -10000, -100000, -1000000, -10000000, -100000000, -1000000000, int.MinValue };
            foreach (var index in iArrInvalidValues)
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collection.Insert(index, itemToInsert));
                Assert.Equal(anArray.Length, collection.Count);
            }

            int[] iArrLargeValues = new int[] { collection.Count + 1, int.MaxValue, int.MaxValue / 2, int.MaxValue / 10 };
            foreach (var index in iArrLargeValues)
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collection.Insert(index, itemToInsert));
                Assert.Equal(anArray.Length, collection.Count);
            }
        }

        /// <summary>
        /// Tests that the appropriate collectionchanged and propertychanged events are 
        /// fired when an item is replaced in the collection.
        /// </summary>
        [Fact]
        public static void ReplaceItemTest()
        {
            string[] anArray = new string[] { "one", "two", "three", "four" };
            ObservableCollection<string> collection = new ObservableCollection<string>(anArray);
            ReadOnlyObservableCollection<string> readonlyCol = new ReadOnlyObservableCollection<string>(collection);
            ReadOnlyCollectionAndPropertyChangedTester helper = new ReadOnlyCollectionAndPropertyChangedTester();
            helper.ReplaceItemTest(readonlyCol, collection, 1, "seven");
            helper.ReplaceItemTest(readonlyCol, collection, 3, "zero");
        }
    }

    /// <summary>
    /// Helper class to test the CollectionChanged and PropertyChanged Events.
    /// </summary>
    public class ReadOnlyCollectionAndPropertyChangedTester
    {
        #region Properties

        private const string COUNT = "Count";
        private const string ITEMARRAY = "Item[]";

        // Number of collection changed events that were ACTUALLY fired.
        private int _numCollectionChangedFired;
        // Number of collection changed events that are EXPECTED to be fired.
        private int _expectedCollectionChangedFired;

        private int _expectedNewStartingIndex;
        private NotifyCollectionChangedAction _expectedAction;
        private IList _expectedNewItems;
        private IList _expectedOldItems;
        private int _expectedOldStartingIndex;

        private PropertyNameExpected[] _expectedPropertyChanged;

        #endregion

        /// <summary>
        /// Will perform an Add or Insert on the given Collection depending on whether the 
        /// insertIndex is null or not. If it is null, will Add, otherwise, will Insert.
        /// </summary>
        public void AddOrInsertItemTest(ReadOnlyObservableCollection<string> readOnlyCol, ObservableCollection<string> collection,
            string itemToAdd, int? insertIndex = null)
        {
            INotifyPropertyChanged readOnlyPropertyChanged = readOnlyCol;
            readOnlyPropertyChanged.PropertyChanged += Collection_PropertyChanged;
            _expectedPropertyChanged = new[]
            {
                new PropertyNameExpected(COUNT),
                new PropertyNameExpected(ITEMARRAY)
            };

            INotifyCollectionChanged readOnlyCollectionChanged = readOnlyCol;
            readOnlyCollectionChanged.CollectionChanged += Collection_CollectionChanged;
            _expectedCollectionChangedFired++;
            _expectedAction = NotifyCollectionChangedAction.Add;
            _expectedNewItems = new string[] { itemToAdd };
            if (insertIndex.HasValue)
                _expectedNewStartingIndex = insertIndex.Value;
            else
                _expectedNewStartingIndex = collection.Count;
            _expectedOldItems = null;
            _expectedOldStartingIndex = -1;

            int expectedCount = collection.Count + 1;

            if (insertIndex.HasValue)
            {
                collection.Insert(insertIndex.Value, itemToAdd);
                Assert.Equal(itemToAdd, readOnlyCol[insertIndex.Value]);
            }
            else
            {
                collection.Add(itemToAdd);
                Assert.Equal(itemToAdd, readOnlyCol[collection.Count - 1]);
            }

            Assert.Equal(expectedCount, readOnlyCol.Count);
            Assert.Equal(_expectedCollectionChangedFired, _numCollectionChangedFired);

            foreach (var item in _expectedPropertyChanged)
                Assert.True(item.IsFound, "The propertychanged event should have fired for" + item.Name + ", since we just added an item");

            readOnlyCollectionChanged.CollectionChanged -= Collection_CollectionChanged;
            readOnlyPropertyChanged.PropertyChanged -= Collection_PropertyChanged;
        }

        /// <summary>
        /// Clears the given Collection.
        /// </summary>
        public void ClearTest(ReadOnlyObservableCollection<string> readOnlyCol, ObservableCollection<string> collection)
        {
            INotifyPropertyChanged readOnlyPropertyChanged = readOnlyCol;
            readOnlyPropertyChanged.PropertyChanged += Collection_PropertyChanged;
            _expectedPropertyChanged = new[]
            {
                new PropertyNameExpected(COUNT),
                new PropertyNameExpected(ITEMARRAY)
            };

            INotifyCollectionChanged readOnlyCollectionChange = readOnlyCol;
            readOnlyCollectionChange.CollectionChanged += Collection_CollectionChanged;
            _expectedCollectionChangedFired++;
            _expectedAction = NotifyCollectionChangedAction.Reset;
            _expectedNewItems = null;
            _expectedNewStartingIndex = -1;
            _expectedOldItems = null;
            _expectedOldStartingIndex = -1;

            collection.Clear();
            Assert.Equal(0, readOnlyCol.Count);
            Assert.Equal(_expectedCollectionChangedFired, _numCollectionChangedFired);

            foreach (var item in _expectedPropertyChanged)
                Assert.True(item.IsFound, "The propertychanged event should have fired for" + item.Name + ", since we just cleared the collection.");

            readOnlyCollectionChange.CollectionChanged -= Collection_CollectionChanged;
            readOnlyPropertyChanged.PropertyChanged -= Collection_PropertyChanged;
        }

        /// <summary>
        /// Given a collection, will move an item from the oldIndex to the newIndex.
        /// </summary>
        public void MoveItemTest(ReadOnlyObservableCollection<string> readOnlyCol, ObservableCollection<string> collection,
            int oldIndex, int newIndex)
        {
            INotifyPropertyChanged readOnlyPropertyChanged = readOnlyCol;
            readOnlyPropertyChanged.PropertyChanged += Collection_PropertyChanged;
            _expectedPropertyChanged = new[] { new PropertyNameExpected(ITEMARRAY) };

            INotifyCollectionChanged readOnlyCollectionChange = readOnlyCol;
            readOnlyCollectionChange.CollectionChanged += Collection_CollectionChanged;

            string itemAtOldIndex = collection[oldIndex];

            _expectedCollectionChangedFired++;
            _expectedAction = NotifyCollectionChangedAction.Move;
            _expectedNewItems = new string[] { itemAtOldIndex };
            _expectedNewStartingIndex = newIndex;
            _expectedOldItems = new string[] { itemAtOldIndex };
            _expectedOldStartingIndex = oldIndex;

            collection.Move(oldIndex, newIndex);
            Assert.Equal(collection.Count, readOnlyCol.Count);
            Assert.Equal(itemAtOldIndex, readOnlyCol[newIndex]);
            Assert.Equal(_expectedCollectionChangedFired, _numCollectionChangedFired);

            foreach (var item in _expectedPropertyChanged)
                Assert.True(item.IsFound, "The propertychanged event should have fired for" + item.Name + ", since we moved an item.");

            readOnlyCollectionChange.CollectionChanged -= Collection_CollectionChanged;
            readOnlyPropertyChanged.PropertyChanged -= Collection_PropertyChanged;
        }

        /// <summary>
        /// Will set that new item at the specified index in the given collection.
        /// </summary>
        public void ReplaceItemTest(ReadOnlyObservableCollection<string> readOnlyCol, ObservableCollection<string> collection,
            int index, string newItem)
        {
            INotifyPropertyChanged readOnlyPropertyChanged = readOnlyCol;
            readOnlyPropertyChanged.PropertyChanged += Collection_PropertyChanged;
            _expectedPropertyChanged = new[] { new PropertyNameExpected(ITEMARRAY) };

            INotifyCollectionChanged readOnlyCollectionChange = readOnlyCol;
            readOnlyCollectionChange.CollectionChanged += Collection_CollectionChanged;

            string itemAtOldIndex = collection[index];

            _expectedCollectionChangedFired++;
            _expectedAction = NotifyCollectionChangedAction.Replace;
            _expectedNewItems = new string[] { newItem };
            _expectedNewStartingIndex = index;
            _expectedOldItems = new string[] { itemAtOldIndex };
            _expectedOldStartingIndex = index;

            int expectedCount = collection.Count;

            collection[index] = newItem;
            Assert.Equal(expectedCount, readOnlyCol.Count);
            Assert.Equal(newItem, readOnlyCol[index]);
            Assert.Equal(_expectedCollectionChangedFired, _numCollectionChangedFired);

            foreach (var item in _expectedPropertyChanged)
                Assert.True(item.IsFound, "The propertychanged event should have fired for" + item.Name + ", since we replaced an item");

            readOnlyCollectionChange.CollectionChanged -= Collection_CollectionChanged;
            readOnlyPropertyChanged.PropertyChanged -= Collection_PropertyChanged;
        }

        /// <summary>
        /// Given a collection, index and item to remove, will try to remove that item
        /// from the index. If the item has duplicates, will verify that only the first
        /// instance was removed.
        /// </summary>
        public void RemoveItemTest(ReadOnlyObservableCollection<string> readOnlyCol, ObservableCollection<string> collection,
            int itemIndex, string itemToRemove, bool isSuccessfulRemove, bool hasDuplicates)
        {
            INotifyPropertyChanged readOnlyPropertyChanged = readOnlyCol;
            readOnlyPropertyChanged.PropertyChanged += Collection_PropertyChanged;
            _expectedPropertyChanged = new[]
            {
                new PropertyNameExpected(COUNT),
                new PropertyNameExpected(ITEMARRAY)
            };

            INotifyCollectionChanged readOnlyCollectionChange = readOnlyCol;
            readOnlyCollectionChange.CollectionChanged += Collection_CollectionChanged;

            if (isSuccessfulRemove)
                _expectedCollectionChangedFired++;

            _expectedAction = NotifyCollectionChangedAction.Remove;
            _expectedNewItems = null;
            _expectedNewStartingIndex = -1;
            _expectedOldItems = new string[] { itemToRemove };
            _expectedOldStartingIndex = itemIndex;

            int expectedCount = isSuccessfulRemove ? collection.Count - 1 : collection.Count;

            bool removedItem = collection.Remove(itemToRemove);
            Assert.Equal(expectedCount, readOnlyCol.Count);
            Assert.Equal(_expectedCollectionChangedFired, _numCollectionChangedFired);

            if (isSuccessfulRemove)
            {
                foreach (var item in _expectedPropertyChanged)
                    Assert.True(item.IsFound, "The propertychanged event should have fired for" + item.Name + ", since an item was removed");

                Assert.True(removedItem, "Should have been successful in removing the item.");
            }
            else
            {
                foreach (var item in _expectedPropertyChanged)
                    Assert.False(item.IsFound, "The propertychanged event should have fired for" + item.Name + ", since no items were removed.");

                Assert.False(removedItem, "Should not have been successful in removing the item.");
            }
            if (hasDuplicates)
                return;

            Assert.DoesNotContain(itemToRemove, collection);

            readOnlyCollectionChange.CollectionChanged -= Collection_CollectionChanged;
            readOnlyPropertyChanged.PropertyChanged -= Collection_PropertyChanged;
        }

        /// <summary>
        /// Verifies that the item is removed from a given index in the collection.
        /// </summary>
        public void RemoveItemAtTest(ReadOnlyObservableCollection<string> readOnlyCol, ObservableCollection<string> collection,
            int itemIndex)
        {
            INotifyPropertyChanged readOnlyPropertyChanged = readOnlyCol;
            readOnlyPropertyChanged.PropertyChanged += Collection_PropertyChanged;
            _expectedPropertyChanged = new[]
            {
                new PropertyNameExpected(COUNT),
                new PropertyNameExpected(ITEMARRAY)
            };

            INotifyCollectionChanged readOnlyCollectionChange = readOnlyCol;
            readOnlyCollectionChange.CollectionChanged += Collection_CollectionChanged;

            string itemAtOldIndex = collection[itemIndex];

            _expectedCollectionChangedFired++;
            _expectedAction = NotifyCollectionChangedAction.Remove;
            _expectedNewItems = null;
            _expectedNewStartingIndex = -1;
            _expectedOldItems = new string[] { itemAtOldIndex };
            _expectedOldStartingIndex = itemIndex;

            int expectedCount = collection.Count - 1;

            collection.RemoveAt(itemIndex);
            Assert.Equal(expectedCount, readOnlyCol.Count);
            Assert.Equal(_expectedCollectionChangedFired, _numCollectionChangedFired);

            foreach (var item in _expectedPropertyChanged)
                Assert.True(item.IsFound, "The propertychanged event should have fired for" + item.Name + ", since we replaced an item");

            readOnlyCollectionChange.CollectionChanged -= Collection_CollectionChanged;
            readOnlyPropertyChanged.PropertyChanged -= Collection_PropertyChanged;
        }

        #region Helper Methods / Classes

        /// <summary>
        /// Verifies that the eventargs fired matches the expected results.
        /// </summary>
        private void VerifyEventArgs(NotifyCollectionChangedEventArgs e)
        {
            Assert.Equal(_expectedNewStartingIndex, e.NewStartingIndex);
            Assert.Equal(_expectedOldStartingIndex, e.OldStartingIndex);

            if (_expectedNewItems != null)
            {
                foreach (var newItem in e.NewItems)
                    Assert.True(_expectedNewItems.Contains(newItem), "newItem was not in the ExpectedNewItems. newItem: " + newItem);
                foreach (var expectedItem in _expectedNewItems)
                    Assert.True(e.NewItems.Contains(expectedItem), "expectedItem was not in e.NewItems. expectedItem: " + expectedItem);
            }
            else
            {
                Assert.Null(e.NewItems);
            }

            if (_expectedOldItems != null)
            {
                foreach (var oldItem in e.OldItems)
                    Assert.True(_expectedOldItems.Contains(oldItem), "oldItem was not in the ExpectedOldItems. oldItem: " + oldItem);
                foreach (var expectedItem in _expectedOldItems)
                    Assert.True(e.OldItems.Contains(expectedItem), "expectedItem was not in e.OldItems. expectedItem: " + expectedItem);
            }
            else
            {
                Assert.Null(e.OldItems);
            }
        }

        private void Collection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Assert.True(sender is ReadOnlyObservableCollection<string>, "The sender of this event should be the ReadOnlyObservableCollection.");

            _numCollectionChangedFired++;
            Assert.Equal(_expectedAction, e.Action);
            switch (_expectedAction)
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
            Assert.True(sender is ReadOnlyObservableCollection<string>, "The sender of this event should be the ReadOnlyObservableCollection.");
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
