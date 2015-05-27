// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
public struct T : IComparable
{
    public int x;
    public int y;
    public int z;
    public T(int ix, int iy, int iz)
    {
        x = ix;
        y = iy;
        z = iz;
    }
    public int CompareTo(object b)
    {
        if (b is T)
        {
            T temp = (T)b;
            if (temp.x != x) return 1;
            if (temp.y != y) return 1;
            if (temp.z != z) return 1;
        }
        return 0;
    }
}

internal class foo
{
    public static int Main()
    {
        bar<T> b = new bar<T>();
        return b.test(new T(1, 2, 3));
    }
}

internal class bar<B> where B : System.IComparable
{
    private B[] _array;

    public bar()
    {
        _array = new B[100];
    }

    public int test(B t)
    {
        _array[1] = t;
        if (t.CompareTo(_array[1]) != 0)
            return -1;
        return 100;
    }
}

