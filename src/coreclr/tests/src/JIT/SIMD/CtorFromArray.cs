// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using Point = System.Numerics.Vector<int>;

namespace VectorMathTests
{
    class Program
    {
        static int[] GenerateArray(int size, int value)
        {
            int[] arr = new int[size];
            for (int i = 0; i < size; ++i)
            {
                arr[i] = value;
            }
            return arr;
        }

        static int Main(string[] args)
        {
            int v = 2;
            int[] arr = GenerateArray(20, v);
            Point p = new Point(arr);
            for (int i = 0; i < Point.Count; ++i)
            {
                if (p[i] != v)
                {
                    return 0;
                }
            }
            return 100;
        }
    }
}
