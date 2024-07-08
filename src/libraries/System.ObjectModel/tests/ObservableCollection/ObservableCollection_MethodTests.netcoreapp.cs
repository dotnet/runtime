// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Collections.ObjectModel.Tests
{
    public partial class ObservableCollection_MethodTests
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

        [Fact]
        public static void AddRange_NotifyCollectionChanged_EventArgs_Test()
        {
            int[] dataToAdd = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            int[] actualDataAdded = new int[0];
            ObservableCollection<int> collection = new ObservableCollection<int>();
            collection.CollectionChanged += (o, e) => actualDataAdded = e.NewItems.Cast<int>().ToArray();

            collection.AddRange(dataToAdd);

            Assert.Equal(dataToAdd, actualDataAdded);
        }

        [Fact]
        public static void InsertRange_NotifyCollectionChanged_EventArgs_Test()
        {
            int[] dataToAdd = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            int[] actualDataAdded = new int[0];
            ObservableCollection<int> collection = new ObservableCollection<int>();
            collection.CollectionChanged += (o, e) => actualDataAdded = e.NewItems.Cast<int>().ToArray();

            collection.InsertRange(0, dataToAdd);

            Assert.Equal(dataToAdd, actualDataAdded);
        }

        [Fact]
        public static void InsertRange_NotifyCollectionChanged_EventArgs_Middle_Test()
        {
            int[] dataToAdd = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            int[] actualDataAdded = new int[0];
            ObservableCollection<int> collection = new ObservableCollection<int>();
            for (int i = 0; i < 4; i++)
            {
                collection.Add(i);
            }

            collection.CollectionChanged += (o, e) => actualDataAdded = e.NewItems.Cast<int>().ToArray();
            collection.InsertRange(2, dataToAdd);

            Assert.Equal(dataToAdd, actualDataAdded);
        }

        [Fact]
        public static void InsertRange_NotifyCollectionChanged_EventArgs_End_Test()
        {
            int[] dataToAdd = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            int[] actualDataAdded = new int[0];
            ObservableCollection<int> collection = new ObservableCollection<int>();
            for (int i = 0; i < 4; i++)
            {
                collection.Add(i);
            }

            collection.CollectionChanged += (o, e) => actualDataAdded = e.NewItems.Cast<int>().ToArray();
            collection.InsertRange(4, dataToAdd);

            Assert.Equal(dataToAdd, actualDataAdded);
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

        [Fact]
        public static void RemoveRange_NotifyCollectionChanged_EventArgs_IndexOfZero_Test()
        {
            int[] initialData = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            int[] actualDataRemoved = new int[0];
            int numberOfItemsToRemove = 4;
            ObservableCollection<int> collection = new ObservableCollection<int>();
            foreach (int item in initialData)
            {
                collection.Add(item);
            }

            collection.CollectionChanged += (o, e) => actualDataRemoved = e.OldItems.Cast<int>().ToArray();
            collection.RemoveRange(0, numberOfItemsToRemove);

            Assert.Equal(initialData.Length - numberOfItemsToRemove, collection.Count);
            Assert.Equal(initialData.AsSpan(0, numberOfItemsToRemove).ToArray(), actualDataRemoved);
        }

        [Fact]
        public static void RemoveRange_NotifyCollectionChanged_EventArgs_IndexMiddle_Test()
        {
            int[] initialData = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            int[] actualDataRemoved = new int[0];
            int numberOfItemsToRemove = 4;
            int startIndex = 3;
            ObservableCollection<int> collection = new ObservableCollection<int>();
            foreach (int item in initialData)
            {
                collection.Add(item);
            }

            collection.CollectionChanged += (o, e) => actualDataRemoved = e.OldItems.Cast<int>().ToArray();
            collection.RemoveRange(startIndex, numberOfItemsToRemove);

            Assert.Equal(initialData.Length - numberOfItemsToRemove, collection.Count);
            Assert.Equal(initialData.AsSpan(startIndex, numberOfItemsToRemove).ToArray(), actualDataRemoved);
        }

        [Fact]
        public static void ReplaceRange_NotifyCollectionChanged_Test()
        {
            int[] initialData = new int[] { 10, 11, 12, 13 };
            int[] dataToReplace = new int[] { 3, 8 };
            int eventCounter = 0;
            ObservableCollection<int> collection = new ObservableCollection<int>(initialData);
            collection.CollectionChanged += (o, e) => eventCounter++;

            collection.ReplaceRange(0, 2, dataToReplace);

            Assert.Equal(initialData.Length, collection.Count);
            Assert.Equal(1, eventCounter);

            int[] collectionAssertion = collection.ToArray();
            Assert.Equal(dataToReplace, collectionAssertion.AsSpan(0, 2).ToArray());
            Assert.Equal(initialData.AsSpan(2, 2).ToArray(), collectionAssertion.AsSpan(2, 2).ToArray());
        }
    }
}
