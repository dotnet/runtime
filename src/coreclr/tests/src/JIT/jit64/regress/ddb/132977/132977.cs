// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
