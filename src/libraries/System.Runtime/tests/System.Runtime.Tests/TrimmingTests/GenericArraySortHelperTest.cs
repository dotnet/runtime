// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;

class Program
{
    static int Main(string[] args)
    {
        var x = new[] { 5, 4, 3, 100, 2, 1 };
        var y = new[] { "a", "b", "c", "h", "d", "e" };
        // This will test that GenericArraySortHelper'2 called by reflection will be kept.
        Array.Sort(x, y);
        // This will test that GenericArraySortHelper'1 called by reflection will be kept.
        Array.Sort(y);
        return 100;
    }
}
