// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

/// <summary>
/// Since <see cref="ReadOnlyCollection{T}"/> is just a wrapper base class around an <see cref="IList{T}"/>,
/// we just verify that the underlying list is what we expect, validate that the calls which
/// we expect are forwarded to the underlying list, and verify that the exceptions we expect
/// are thrown.
/// </summary>
public class ReadOnlyCollection : CollectionTestBase
{
    private static readonly ReadOnlyCollection<int> s_empty = new ReadOnlyCollection<int>(new int[0]);

    [Fact]
    public static void CreateFromNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ReadOnlyCollection<int>(null));
    }

    [Fact]
    public static void CreateFromIList()
    {
        var collection = new TestCollection<int>(s_intArray);
        Assert.Same(s_intArray, collection.GetItems());
    }

    [Fact]
    public static void Count()
    {
        var collection = new ReadOnlyCollection<int>(s_intArray);
        Assert.Equal(s_intArray.Length, collection.Count);
        Assert.Equal(0, s_empty.Count);
    }

    [Fact]
    public static void Indexer()
    {
        var collection = new Collection<int>(s_intArray);
        for (int i = 0; i < s_intArray.Length; i++)
        {
            Assert.Equal(s_intArray[i], collection[i]);
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => { var x = collection[-1]; });
        Assert.Throws<ArgumentOutOfRangeException>(() => { var x = collection[s_intArray.Length]; });
        Assert.Throws<ArgumentOutOfRangeException>(() => { var x = s_empty[0]; });
    }

    [Fact]
    public static void IsReadOnly()
    {
        var collection = new ReadOnlyCollection<int>(s_intArray);
        Assert.True(((IList)collection).IsReadOnly);
        Assert.True(((IList<int>)collection).IsReadOnly);
        Assert.True(((IList)s_empty).IsReadOnly);
        Assert.True(((IList<int>)s_empty).IsReadOnly);
    }

    [Fact]
    public static void Contains()
    {
        var collection = new Collection<int>(s_intArray);
        for (int i = 0; i < s_intArray.Length; i++)
        {
            Assert.True(collection.Contains(s_intArray[i]));
        }

        for (int i = 0; i < s_excludedFromIntArray.Length; i++)
        {
            Assert.False(collection.Contains(s_excludedFromIntArray[i]));
        }
    }

    [Fact]
    public static void CopyTo()
    {
        var collection = new ReadOnlyCollection<int>(s_intArray);
        const int targetIndex = 3;
        int[] intArray = new int[s_intArray.Length + targetIndex];

        Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null, 0));
        Assert.Throws<ArgumentException>(() => ((ICollection)collection).CopyTo(new int[s_intArray.Length, s_intArray.Length], 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(intArray, -1));
        Assert.Throws<ArgumentException>(() => collection.CopyTo(intArray, s_intArray.Length - 1));

        collection.CopyTo(intArray, targetIndex);
        for (int i = targetIndex; i < intArray.Length; i++)
        {
            Assert.Equal(collection[i - targetIndex], intArray[i]);
        }

        object[] objectArray = new object[s_intArray.Length + targetIndex];
        ((ICollection)collection).CopyTo(intArray, targetIndex);
        for (int i = targetIndex; i < intArray.Length; i++)
        {
            Assert.Equal(collection[i - targetIndex], intArray[i]);
        }
    }

    [Fact]
    public static void IndexOf()
    {
        var collection = new Collection<int>(s_intArray);

        for (int i = 0; i < s_intArray.Length; i++)
        {
            int item = s_intArray[i];
            Assert.Equal(Array.IndexOf(s_intArray, item), collection.IndexOf(item));
        }

        for (int i = 0; i < s_excludedFromIntArray.Length; i++)
        {
            Assert.Equal(-1, collection.IndexOf(s_excludedFromIntArray[i]));
        }
    }

    [Fact]
    public static void MembersForwardedToUnderlyingIList()
    {
        var expectedApiCalls =
            IListApi.Count |
            IListApi.IndexerGet |
            IListApi.Contains |
            IListApi.CopyTo |
            IListApi.GetEnumeratorGeneric |
            IListApi.IndexOf |
            IListApi.GetEnumerator;

        var list = new CallTrackingIList<int>(expectedApiCalls);
        var collection = new ReadOnlyCollection<int>(list);

        int count = collection.Count;
        bool readOnly = ((IList)collection).IsReadOnly;
        int x = collection[0];
        collection.Contains(x);
        collection.CopyTo(s_intArray, 0);
        collection.GetEnumerator();
        collection.IndexOf(x);
        ((IEnumerable)collection).GetEnumerator();

        list.AssertAllMembersCalled();
    }

    [Fact]
    public void MutatingMethodsThrow()
    {
        var collection = (IList<int>)new ReadOnlyCollection<int>(s_intArray);

        Assert.Throws<NotSupportedException>(() => { collection[0] = 0; });
        Assert.Throws<NotSupportedException>(() => collection.Add(0));
        Assert.Throws<NotSupportedException>(() => collection.Clear());
        Assert.Throws<NotSupportedException>(() => collection.Insert(0, 0));
        Assert.Throws<NotSupportedException>(() => collection.Remove(0));
        Assert.Throws<NotSupportedException>(() => collection.RemoveAt(0));
    }

    private class TestCollection<T> : ReadOnlyCollection<T>
    {
        public TestCollection(IList<T> items) : base(items)
        {
        }

        public IList<T> GetItems()
        {
            return this.Items;
        }
    }
}