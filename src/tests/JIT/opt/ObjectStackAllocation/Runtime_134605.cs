// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class Runtime_134605
{
    [Fact]
    public static void Test()
    {
        int calls = 0;

        for (int i = 0; i < 50; i++)
        {
            for (int j = 0; j < 1000; j++)
            {
                VersionedList<Item> list = new();
                for (int k = 0; k < 6; k++)
                {
                    list.Add(new Item { Pending = (calls % 4 == 0) && (k == 2) });
                }

                calls++;
                try
                {
                    Walk(list);
                }
                catch (InvalidOperationException)
                {
                    // Failures are checked after Tier1 compilation has settled.
                }
            }

            Thread.Sleep(5);
        }

        Thread.Sleep(100);

        int failures = 0;
        for (int i = 0; i < 10_000; i++)
        {
            VersionedList<Item> list = new();
            for (int k = 0; k < 6; k++)
            {
                list.Add(new Item { Pending = (calls % 4 == 0) && (k == 2) });
            }

            calls++;
            try
            {
                Walk(list);
            }
            catch (InvalidOperationException)
            {
                failures++;
            }
        }

        Assert.Equal(0, failures);

        VersionedList<Item> first = CreateList();
        VersionedList<Item> second = CreateList();
        for (int i = 0; i < 50; i++)
        {
            for (int j = 0; j < 1000; j++)
            {
                Assert.Equal(12, CountDisjoint(first, second));
            }

            Thread.Sleep(5);
        }

        long allocatedBytesBefore = GC.GetAllocatedBytesForCurrentThread();
        int count = CountDisjoint(first, second);
        long allocatedBytesAfter = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(12, count);
        Assert.Equal(0, allocatedBytesAfter - allocatedBytesBefore);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Handle(VersionedList<Item> list, Item item)
    {
        item.Pending = false;
        list.Add(new Item());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Walk(VersionedList<Item> list)
    {
        IEnumerable<Item> items = list;
        IEnumerator<Item> enumerator = items.GetEnumerator();
        while (enumerator.MoveNext())
        {
            Item item = enumerator.Current;
            if (item.Pending)
            {
                Handle(list, item);
                enumerator = items.GetEnumerator();
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CountDisjoint(VersionedList<Item> first, VersionedList<Item> second)
    {
        int count = 0;
        IEnumerable<Item> items = first;
        IEnumerator<Item> enumerator = items.GetEnumerator();
        while (enumerator.MoveNext())
        {
            count++;
        }

        items = second;
        enumerator = items.GetEnumerator();
        while (enumerator.MoveNext())
        {
            count++;
        }

        return count;
    }

    private static VersionedList<Item> CreateList()
    {
        VersionedList<Item> list = new();
        for (int i = 0; i < 6; i++)
        {
            list.Add(new Item());
        }

        return list;
    }

    private sealed class VersionedList<T> : Collection<T>, IEnumerable<T>
    {
        private int _version;

        protected override void InsertItem(int index, T item)
        {
            _version++;
            base.InsertItem(index, item);
        }

        public new Enumerator GetEnumerator() => new(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

#if STRUCT_ENUMERATOR
        public struct Enumerator : IEnumerator<T>
#else
        public sealed class Enumerator : IEnumerator<T>
#endif
        {
            private readonly VersionedList<T> _list;
            private readonly int _expectedVersion;
            private int _cursor;
            private T _current;

            internal Enumerator(VersionedList<T> list)
            {
                _list = list;
                _expectedVersion = list._version;
                _cursor = 0;
                _current = default!;
            }

            public T Current => _current;

            object? IEnumerator.Current => _current;

            public bool MoveNext()
            {
                if (_cursor == _list.Count)
                {
                    return false;
                }

                if (_list._version != _expectedVersion)
                {
                    throw new InvalidOperationException();
                }

                _current = _list[_cursor++];
                return true;
            }

            public void Reset() => throw new NotSupportedException();

            public void Dispose()
            {
            }
        }
    }

    private sealed class Item
    {
        public bool Pending;
    }
}
