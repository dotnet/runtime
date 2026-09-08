// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class Runtime_131713
{
    [Fact]
    public static void Test()
    {
        CustomList<Bar> bars = [new Bar(), new Bar(), new Bar()];
        CustomList<Foo> foos = [new Foo(), new Foo()];

        int expected = Count(bars, foos);

        // Count must reach tier-1 with PGO data.
        for (int i = 0; i < 50; i++)
        {
            for (int j = 0; j < 1000; j++)
            {
                int actual = Count(bars, foos);
                if (actual != expected)
                {
                    Assert.Fail($"expected {expected}, got {actual}");
                }
            }

            Thread.Sleep(5);
        }
    }

    // The repro needs all of the following:
    //
    //  * one non-generic IEnumerator local, shared by two loops, so that the
    //    enumerator var has two defining GDVs,
    //  * each def coming directly from a GetEnumerator that returns a boxed
    //    struct enumerator, and
    //  * a null check on the enumerator in that same block, which is what makes
    //    the allocation block conditional.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Count(CustomList<Bar> bars, CustomList<Foo> foos)
    {
        int n = 0;

        IEnumerator e = bars.GetEnumerator();
        if (e != null)
        {
            while (e.MoveNext())
            {
                if (e.Current is Bar)
                {
                    n += 1;
                }
            }
        }

        e = foos.GetEnumerator();
        if (e != null)
        {
            while (e.MoveNext())
            {
                if (e.Current is Foo)
                {
                    n += 100;
                }
            }
        }

        return n;
    }

    public class Foo
    {
    }

    public class Bar
    {
    }

    public class CustomList<T> : IEnumerable<T>
    {
        private readonly List<T> _innerList = [];

        public void Add(T item) => _innerList.Add(item);

        public IEnumerator<T> GetEnumerator() => _innerList.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _innerList.GetEnumerator();
    }
}
