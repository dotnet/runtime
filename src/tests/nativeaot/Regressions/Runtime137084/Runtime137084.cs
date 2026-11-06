// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

enum Color { Red = 1, Green = 2 }

// The enumerator of a Color[] is a SZGenericArrayEnumerator<Color>, and IEnumerator<int>.Current has to
// reach its IEnumerator<Color> slot through the array casting rule. A call to IEnumerator<T>.Current over
// a reference type anywhere in this program would keep that slot and hide a regression.
class Program
{
    static int Main()
    {
        object colors = new[] { Color.Red, Color.Green };

        int sum = 0;
        foreach (int value in (IEnumerable<int>)colors)
            sum += value;

        return sum == 3 ? 100 : 1;
    }
}
