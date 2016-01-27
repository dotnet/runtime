// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//This test is used to check whether a method that takes a generic struct is inlined.

using System;
internal struct XY<T>
{
    private T _x;
    private T _y;

    public XY(T a, T b)
    {
        _x = a;
        _y = b;
    }

    public T X
    {
        get { return _x; }
        set { _x = value; }
    }

    public T Y
    {
        get { return _y; }
        set { _y = value; }
    }
}

internal class StructTest
{
    private static void XYZ_Inline(XY<int> xy)
    {
        xy.X = 10;
        xy.Y = 20;
    }
    public static int Main()
    {
        try
        {
            XY<int> xy = new XY<int>(1, 2);
            XY<double> xy2 = new XY<double>(8.0, 9.0);
            Console.WriteLine(xy.X + ", " + xy.Y);

            Console.WriteLine(xy2.X + ", " + xy2.Y);
            XY<int> ab = new XY<int>();
            XYZ_Inline(ab);
            Console.WriteLine(xy.X + ", " + xy.Y);
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 666;
        }
    }
}


