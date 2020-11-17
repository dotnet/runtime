// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

// Test case where throw helper is in the try entry block.
//
// Throw helper merging is run lexically backwards,
// so the optimization may introduce a jump into the middle of the try.

class ThrowHelperAtTryEntry
{
    static void ThrowHelper()
    {
        throw new Exception();
    }

    public static int Main()
    {
        int x = 0;
        bool p = true;
        try
        {
            ThrowHelper();
            x = -1;
            if (p) ThrowHelper();
        }
        catch (Exception)
        {
            x = 100;
        }

        return x;
    }
}
