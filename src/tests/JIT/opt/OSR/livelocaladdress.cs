// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

// An example where OSR must preserve original method addreses for locals

class LiveLocalAddress
{
    public static unsafe int Main()        
    {
        long result = 0;
        int a = 0;
        int *c = &a;
        int b = 0;
        long distance = c - &b;
        
        for (int i = 0; i < 100_000; i++)
        {
            result += &a - &b;
        }

        return (int)(result / (1000 * distance));
    }
}
