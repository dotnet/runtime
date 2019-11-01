// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

class Test 
{
    static int SIZE = 100;

    public static unsafe int Main()
    {
        byte* source = stackalloc byte[SIZE];
        byte* dest   = stackalloc byte[SIZE];

        for (int i = 0; i < SIZE; i++)
        {
            source[i] = (byte)(i % 255);
            dest[i] = 0;
        }

        Unsafe.CopyBlock(dest, source, (uint) SIZE);

        bool result = true;

        for (int i = 0; i < SIZE; i++)
        {
            result &= (source[i] == dest[i]);
        }

        return (result ? 100 : -1);
    }
}
