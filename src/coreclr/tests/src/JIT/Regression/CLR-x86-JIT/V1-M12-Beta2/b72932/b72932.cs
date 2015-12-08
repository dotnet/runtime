// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
internal unsafe class testout1
{
    public struct VT
    {
        public long a1;
    }

    public static int Main()
    {
        VT vt = new VT();
        vt.a1 = 500L;

        long* a0 = stackalloc long[1];
        *a0 = -6L;
        Console.WriteLine("Should be 500");
        Console.WriteLine((long)(vt.a1));
        return 100;
    }
}
