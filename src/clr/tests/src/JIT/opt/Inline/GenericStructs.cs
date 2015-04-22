// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//This test is used to check whether a method that takes a generic struct is inlined.

using System;
//Generic struct
struct XY<T>
{
    T x;
    T y;

    public XY(T a, T b)
    {
        x = a;
        y = b;
    }

    public T X
    {
        get { return x; }
        set { x = value; }
    }

    public T Y
    {
        get { return y; }
        set { y = value; }
    }
}

class StructTest
{

    static void XYZ_Inline(XY<int> xy)
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


