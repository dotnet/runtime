// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Bug: OSR bug causing bad GC pointers
// a GC root becomes an interior pointer when added.
internal class Repro
{
    private int[] _arr;

    private void Bug()
    {
        _arr = new int[128];

        for (int i = 0; i < 128; i++)
        {
            _arr[i] = 1;
        }
    }

    private static int Main()
    {
        new Repro().Bug();
        // will fail with an assert under GCSTRESS=4
        return 100;
    }
}
