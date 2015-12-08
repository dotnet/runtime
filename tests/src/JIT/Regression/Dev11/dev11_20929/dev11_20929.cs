// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

internal static class Repro
{
    private struct foo
    {
        public int x, y;
    }

    private static int Main()
    {

        foo f = new foo();
        f.x = f.y = 1;
        Console.WriteLine(f.x + f.y);

        Console.WriteLine(BitConverter.Int64BitsToDouble(unchecked((long)0x8000000000000000UL)));

        Console.WriteLine("PASS!");
        return 100;
    }
}
