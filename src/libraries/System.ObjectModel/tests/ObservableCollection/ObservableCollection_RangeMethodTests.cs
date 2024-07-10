// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Specialized;
using System.Linq;
using Xunit;

namespace System.Collections.ObjectModel.Tests
{
    public partial class ObservableCollection_RangeMethodTests
    {
        [Fact]
        public static void InsertRange_NotifyCollectionChanged_Beginning_Test()
        {
            int[] dataToInsert = new int[] { 1, 2, 3, 4, 5 };
            int[] initialData = new int[] { 10, 11, 12, 13 };
            int eventCounter = 0;
            ObservableCollection<int> collection = new ObservableCollection<int>(initialData);
            collection.CollectionChanged += (o, e) => eventCounter++;

            collection.InsertRange(0, dataToInsert);

            Assert.Equal(dataToInsert.Length + initialData.Length, collection.Count);
            Assert.Equal(1, eventCounter);

            int[] collectionAssertion = collection.ToArray();
            Assert.Equal(dataToInsert, collectionAssertion.AsSpan(0, 5).ToArray());
            Assert.Equal(initialData, collectionAssertion.AsSpan(5).ToArray());
        }

        [Fact]
        public static void InsertRange_NotifyCollectionChanged_Middle_Test()
        {
            int[] dataToInsert = new int[] { 1, 2, 3, 4, 5 };
            int[] initialData = new int[] { 10, 11, 12, 13 };
            int eventCounter = 0;
            ObservableCollection<int> collection = new ObservableCollection<int>(initialData);
            collection.CollectionChanged += (o, e) => eventCounter++;

            collection.InsertRange(2, dataToInsert);

            Assert.Equal(dataToInsert.Length + initialData.Length, collection.Count);
            Assert.Equal(1, eventCounter);

            int[] collectionAssertion = collection.ToArray();
            Assert.Equal(initialData.AsSpan(0, 2).ToArray(), collectionAssertion.AsSpan(0, 2).ToArray());
            Assert.Equal(dataToInsert, collectionAssertion.AsSpan(2, 5).ToArray());
            Assert.Equal(initialData.AsSpan(2, 2).ToArray(), collectionAssertion.AsSpan(7, 2).ToArray());
        }

        [Fact]
        public static void InsertRange_NotifyCollectionChanged_End_Test()
        {
            int[] dataToInsert = new int[] { 1, 2, 3, 4, 5 };
            int[] initialData = new int[] { 10, 11, 12, 13 };
            int eventCounter = 0;
            ObservableCollection<int> collection = new ObservableCollection<int>(initialData);
            collection.CollectionChanged += (o, e) => eventCounter++;

            collection.InsertRange(4, dataToInsert);

            Assert.Equal(dataToInsert.Length + initialData.Length, collection.Count);
            Assert.Equal(1, eventCounter);

            int[] collectionAssertion = collection.ToArray();
            Assert.Equal(initialData, collectionAssertion.AsSpan(0, 4).ToArray());
            Assert.Equal(dataToInsert, collectionAssertion.AsSpan(4).ToArray());
        }

        [Fact]
        public static void AddRange_NotifyCollectionChanged_Test()
        {
            int[] dataToInsert = new int[] { 1, 2, 3, 4, 5 };
            int[] initialData = new int[] { 10, 11, 12, 13 };
            int eventCounter = 0;
            ObservableCollection<int> collection = new ObservableCollection<int>(initialData);
            collection.CollectionChanged += (o, e) => eventCounter++;

            collection.AddRange(dataToInsert);

            Assert.Equal(dataToInsert.Length + initialData.Length, collection.Count);
            Assert.Equal(1, eventCounter);

            int[] collectionAssertion = collection.ToArray();
            Assert.Equal(initialData, collectionAssertion.AsSpan(0, 4).ToArray());
            Assert.Equal(dataToInsert, collectionAssertion.AsSpan(4).ToArray());
        }

        [Theory]
        [InlineData(true, Skip = "Reenable when AppContext switch is added to opt into multiple items in NotifyCollectionChangedEventArgs")]
        [InlineData(false)]
        public static void AddRange_NotifyCollectionChanged_EventArgs_Test(bool batchCollectionChanged)
        {
            int[] dataToAdd = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            ObservableCollection<int> collection = new();
            NotifyCollectionChangedEventArgs? args = null;

            collection.CollectionChanged += (o, e) => args = e;
            collection.AddRange(dataToAdd);

            Assert.NotNull(args);
            if (batchCollectionChanged)
            {
                Assert.Equal(NotifyCollectionChangedAction.Add, args.Action);
                Assert.Equal(0, args.NewStartingIndex);
                Assert.Equal(dataToAdd, args.NewItems.Cast<int>().ToArray());
            }
            else
            {
                Assert.Equal(NotifyCollectionChangedAction.Reset, args.Action);
            }
        }

        [Theory]
        [InlineData(true, Skip = "Reenable when AppContext switch is added to opt into multiple items in NotifyCollectionChangedEventArgs")]
        [InlineData(false)]
        public static void InsertRange_NotifyCollectionChanged_EventArgs_Test(bool batchCollectionChanged)
        {
            int[] dataToAdd = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            ObservableCollection<int> collection = new();
            NotifyCollectionChangedEventArgs? args = null;

            collection.CollectionChanged += (o, e) => args = e;
            collection.InsertRange(0, dataToAdd);

            Assert.NotNull(args);
            if (batchCollectionChanged)
            {
                Assert.Equal(NotifyCollectionChangedAction.Add, args.Action);
                Assert.Equal(0, args.NewStartingIndex);
                Assert.Equal(dataToAdd, args.NewItems.Cast<int>().ToArray());
            }
            else
            {
                Assert.Equal(NotifyCollectionChangedAction.Reset, args.Action);
            }
        }

        [Theory]
        [InlineData(true, Skip = "Reenable when AppContext switch is added to opt into multiple items in NotifyCollectionChangedEventArgs")]
        [InlineData(false)]
        public static void InsertRange_NotifyCollectionChanged_EventArgs_Middle_Test(bool batchCollectionChanged)
        {
            int[] dataToAdd = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            ObservableCollection<int> collection = new ObservableCollection<int>();
            NotifyCollectionChangedEventArgs? args = null;

            for (int i = 0; i < 4; i++)
            {
                collection.Add(i);
            }

            collection.CollectionChanged += (o, e) => args = e;
            collection.InsertRange(2, dataToAdd);

            Assert.NotNull(args);
            if (batchCollectionChanged)
            {
                Assert.Equal(NotifyCollectionChangedAction.Add, args.Action);
                Assert.Equal(2, args.NewStartingIndex);
                Assert.Equal(dataToAdd, args.NewItems.Cast<int>().ToArray());
            }
            else
            {
                Assert.Equal(NotifyCollectionChangedAction.Reset, args.Action);
            }
        }

        [Theory]
        [InlineData(true, Skip = "Reenable when AppContext switch is added to opt into multiple items in NotifyCollectionChangedEventArgs")]
        [InlineData(false)]
        public static void InsertRange_NotifyCollectionChanged_EventArgs_End_Test(bool batchCollectionChanged)
        {
            int[] dataToAdd = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            ObservableCollection<int> collection = new ObservableCollection<int>();
            NotifyCollectionChangedEventArgs? args = null;

            for (int i = 0; i < 4; i++)
            {
                collection.Add(i);
            }

            collection.CollectionChanged += (o, e) => args = e;
            collection.InsertRange(4, dataToAdd);

            Assert.NotNull(args);
            if (batchCollectionChanged)
            {
                Assert.Equal(NotifyCollectionChangedAction.Add, args.Action);
                Assert.Equal(4, args.NewStartingIndex);
                Assert.Equal(dataToAdd, args.NewItems.Cast<int>().ToArray());
            }
            else
            {
                Assert.Equal(NotifyCollectionChangedAction.Reset, args.Action);
            }
        }

        [Fact]
        public static void RemoveRange_NotifyCollectionChanged_FirstTwo_Test()
        {
            int[] initialData = new int[] { 10, 11, 12, 13 };
            int itemsToRemove = 2;
            int eventCounter = 0;
            ObservableCollection<int> collection = new ObservableCollection<int>(initialData);
            collection.CollectionChanged += (o, e) => eventCounter++;

            collection.RemoveRange(0, itemsToRemove);

            Assert.Equal(initialData.Length - itemsToRemove, collection.Count);
            Assert.Equal(1, eventCounter);
            Assert.Equal(initialData.AsSpan(2).ToArray(), collection.ToArray());
        }

        [Fact]
        public static void RemoveRange_NotifyCollectionChanged_MiddleTwo_Test()
        {
            int[] initialData = new int[] { 10, 11, 12, 13 };
            int itemsToRemove = 2;
            int eventCounter = 0;
            ObservableCollection<int> collection = new ObservableCollection<int>(initialData);
            collection.CollectionChanged += (o, e) => eventCounter++;

            collection.RemoveRange(1, itemsToRemove);

            Assert.Equal(initialData.Length - itemsToRemove, collection.Count);
            Assert.Equal(1, eventCounter);
            Assert.Equal(initialData[0], collection[0]);
            Assert.Equal(initialData[3], collection[1]);
        }

        [Fact]
        public static void RemoveRange_NotifyCollectionChanged_LastTwo_Test()
        {
            int[] initialData = new int[] { 10, 11, 12, 13 };
            int itemsToRemove = 2;
            int eventCounter = 0;
            ObservableCollection<int> collection = new ObservableCollection<int>(initialData);
            collection.CollectionChanged += (o, e) => eventCounter++;

            collection.RemoveRange(2, itemsToRemove);

            Assert.Equal(initialData.Length - itemsToRemove, collection.Count);
            Assert.Equal(1, eventCounter);
            Assert.Equal(initialData.AsSpan(0, 2).ToArray(), collection.ToArray());
        }

        [Fact]
        public static void RemoveRange_NotifyCollectionChanged_IntMaxValueOverflow_Test()
        {
            int count = 500;
            ObservableCollection<int> collection = new ObservableCollection<int>();
            for (int i = 0; i < count; i++)
            {
                collection.Add(i);
            }

            Assert.Throws<ArgumentException>(() => collection.RemoveRange(collection.Count - 2, int.MaxValue));
        }

        [Theory]
        [InlineData(true, Skip = "Reenable when AppContext switch is added to opt into multiple items in NotifyCollectionChangedEventArgs")]
        [InlineData(false)]
        public static void RemoveRange_NotifyCollectionChanged_EventArgs_IndexOfZero_Test(bool batchCollectionChanged)
        {
            int[] initialData = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            int numberOfItemsToRemove = 4;
            ObservableCollection<int> collection = new(initialData);
            NotifyCollectionChangedEventArgs? args = null;

            collection.CollectionChanged += (o, e) => args = e;
            collection.RemoveRange(0, numberOfItemsToRemove);

            Assert.Equal(initialData.Length - numberOfItemsToRemove, collection.Count);

            Assert.NotNull(args);
            if (batchCollectionChanged)
            {
                Assert.Equal(NotifyCollectionChangedAction.Remove, args.Action);
                Assert.Equal(0, args.OldStartingIndex);
                Assert.Equal(initialData.AsSpan(0, numberOfItemsToRemove).ToArray(), args.OldItems.Cast<int>().ToArray());
            }
            else
            {
                Assert.Equal(NotifyCollectionChangedAction.Reset, args.Action);
            }
        }

        [Theory]
        [InlineData(true, Skip = "Reenable when AppContext switch is added to opt into multiple items in NotifyCollectionChangedEventArgs")]
        [InlineData(false)]
        public static void RemoveRange_NotifyCollectionChanged_EventArgs_IndexMiddle_Test(bool batchCollectionChanged)
        {
            int[] initialData = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            int numberOfItemsToRemove = 4;
            int startIndex = 3;

            ObservableCollection<int> collection = new(initialData);
            NotifyCollectionChangedEventArgs? args = null;

            collection.CollectionChanged += (o, e) => args = e;
            collection.RemoveRange(startIndex, numberOfItemsToRemove);

            Assert.Equal(initialData.Length - numberOfItemsToRemove, collection.Count);

            Assert.NotNull(args);
            if (batchCollectionChanged)
            {
                Assert.Equal(NotifyCollectionChangedAction.Remove, args.Action);
                Assert.Equal(startIndex, args.OldStartingIndex);
                Assert.Equal(initialData.AsSpan(startIndex, numberOfItemsToRemove).ToArray(), args.OldItems.Cast<int>().ToArray());
            }
            else
            {
                Assert.Equal(NotifyCollectionChangedAction.Reset, args.Action);
            }
        }

        [Theory]
        [InlineData(true, Skip = "Reenable when AppContext switch is added to opt into multiple items in NotifyCollectionChangedEventArgs")]
        [InlineData(false)]
        public static void ReplaceRange_NotifyCollectionChanged_Test(bool batchCollectionChanged)
        {
            int[] initialData = new int[] { 10, 11, 12, 13 };
            int[] dataToReplace = new int[] { 3, 8 };
            int[] expectedResult = new int[] { 10, 3, 8 ,13 };
            int startIndex = 1;
            int count = 2;

            ObservableCollection<int> collection = new ObservableCollection<int>(initialData);
            NotifyCollectionChangedEventArgs? args = null;

            collection.CollectionChanged += (o, e) => { Assert.Null(args); args = e; };

            collection.ReplaceRange(startIndex, count, dataToReplace);

            Assert.Equal(expectedResult, collection.ToArray());

            Assert.NotNull(args);
            if (batchCollectionChanged)
            {
                Assert.Equal(NotifyCollectionChangedAction.Replace, args.Action);
                Assert.Equal(startIndex, args.OldStartingIndex);
                Assert.Equal(startIndex, args.NewStartingIndex);
                Assert.Equal(initialData.AsSpan(startIndex, count).ToArray(), args.OldItems.Cast<int>().ToArray());
                Assert.Equal(dataToReplace, args.NewItems.Cast<int>().ToArray());
            }
            else
            {
                Assert.Equal(NotifyCollectionChangedAction.Reset, args.Action);
            }
        }
    }
}
