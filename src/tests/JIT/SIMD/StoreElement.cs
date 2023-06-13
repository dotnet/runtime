// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using Point = System.Numerics.Vector<int>;
using Xunit;

namespace VectorTests
{
    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            Point p = new Point(1);
            Point[] arr = new Point[10];
            arr[0] = p; // Loadelem bytecode.
            arr[2] = p;
            if (arr[0] == arr[1] || arr[2] != p)
            {
                return 0;
            }
            return 100;
        }
    }
}
