// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

public static class LinqReplacements
{
    public static T First<T>(this IEnumerable<T> items)
    {
        foreach (T item in items)
        {
            return item;
        }

        return default(T);
    }

    public static T FirstOrDefault<T>(this IEnumerable<T> items)
    {
        foreach (T item in items)
        {
            return item;
        }

        return default(T);
    }

    public static List<T> ToList<T>(this IEnumerable<T> items)
    {
        var list = new List<T>();
        list.AddRange(items);
        return list;
    }

    public static int Count<T>(this IEnumerable<T> items)
    {
        int count = 0;
        foreach (Object i in items)
        {
            count++;
        }
        return count;
    }

    public static int Count<T>(this IEnumerable<T> items, Func<T, bool> predicate)
    {
        int count = 0;
        foreach (T i in items)
        {
            if (predicate(i)) count++;
        }
        return count;
    }

    public static IEnumerable<T> Where<T>(this IEnumerable<T> items, Func<T, bool> predicate)
    {
        foreach (T i in items)
        {
            if (predicate(i)) yield return i;
        }
    }
}
