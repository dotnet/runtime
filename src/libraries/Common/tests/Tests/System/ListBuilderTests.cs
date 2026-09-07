// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Tests.System;

public class ListBuilderTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(8)]
    public void Add_GrowsAndPreservesItems(int? capacity)
    {
        ListBuilder<object> builder = capacity.HasValue ? new ListBuilder<object>(capacity.Value) : default;
        Assert.Equal(0, builder.Count);
        Assert.Same(Array.Empty<object>(), builder.ToArray());

        object[] items = new object[17];
        for (int i = 0; i < items.Length; i++)
        {
            items[i] = new object();
            builder.Add(items[i]);
            Assert.Equal(i + 1, builder.Count);
            for (int j = 0; j <= i; j++)
            {
                Assert.Same(items[j], builder[j]);
            }
        }

        Assert.Equal(items, builder.ToArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(8)]
    public void Add_FirstItemDoesNotAllocate(int capacity)
    {
        object item = new object();
        long before = GC.GetAllocatedBytesForCurrentThread();
        ListBuilder<object> builder = new ListBuilder<object>(capacity);
        builder.Add(item);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(before, after);
        Assert.Equal(1, builder.Count);
        Assert.Same(item, builder[0]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(8)]
    public void ToArray_PreservesStorageSemantics(int count)
    {
        ListBuilder<object> builder = new ListBuilder<object>(8);
        object[] items = new object[count];
        for (int i = 0; i < count; i++)
        {
            items[i] = new object();
            builder.Add(items[i]);
        }

        object[] array = builder.ToArray();
        Assert.Equal(items, array);
        Assert.Equal(count, builder.Count);

        if (count == 0)
        {
            Assert.Same(Array.Empty<object>(), array);
            Assert.Same(array, builder.ToArray());
        }
        else if (count == 1)
        {
            Assert.NotSame(array, builder.ToArray());
            array[0] = new object();
            Assert.Same(items[0], builder[0]);
        }
        else
        {
            Assert.Same(array, builder.ToArray());
            object replacement = new object();
            array[0] = replacement;
            Assert.Same(replacement, builder[0]);
        }
    }

    [Theory]
    [InlineData(4, 0)]
    [InlineData(4, 1)]
    [InlineData(4, 2)]
    [InlineData(4, 3)]
    [InlineData(4, 4)]
    [InlineData(8, 2)]
    [InlineData(8, 5)]
    public void Add_AfterToArray(int capacity, int count)
    {
        ListBuilder<object> builder = new ListBuilder<object>(capacity);
        object[] items = new object[count + 5];
        for (int i = 0; i < items.Length; i++)
        {
            items[i] = new object();
        }
        for (int i = 0; i < count; i++)
        {
            builder.Add(items[i]);
        }

        object[] original = builder.ToArray();
        for (int i = count; i < items.Length; i++)
        {
            builder.Add(items[i]);
        }

        Assert.Equal(items.Length, builder.Count);
        Assert.Equal(items, builder.ToArray());
        Assert.Equal(count, original.Length);
        for (int i = 0; i < original.Length; i++)
        {
            Assert.Same(items[i], original[i]);
        }
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 2)]
    [InlineData(1, 0)]
    [InlineData(1, 2)]
    [InlineData(2, 0)]
    [InlineData(2, 2)]
    [InlineData(5, 2)]
    public void CopyTo_CopiesOnlyItems(int count, int index)
    {
        string[] items = ["a", "b", "c", "d", "e"];
        ListBuilder<string> builder = default;
        for (int i = 0; i < count; i++)
        {
            builder.Add(items[i]);
        }

        object sentinel = new object();
        object[] destination = new object[index + count + 2];
        Array.Fill(destination, sentinel);
        builder.CopyTo(destination, index);

        Assert.Equal(count, builder.Count);
        for (int i = 0; i < destination.Length; i++)
        {
            Assert.Same(i >= index && i < index + count ? items[i - index] : sentinel, destination[i]);
        }
    }
}
