// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

// Example where local address is live in a stackalloc region

class LiveLocalStackalloc
{
    static int n = 100;
    static int j = 30;

    public static unsafe int Main()
    {
        int nn = n;
        int** ptrs = stackalloc int*[nn];
        int a = 100;
        *(ptrs + j) = &a;
        int result = 0;

        for (int i = 0; i < nn; i++)
        {
            int* p = *(ptrs + i);
            if (p != null)  result += *p;
        }

        return result;
    }
}
