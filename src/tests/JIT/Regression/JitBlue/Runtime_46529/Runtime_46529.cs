// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public unsafe class Test
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Foo(byte* bytes) => (int)(((ulong)bytes[0]) << 56);

    public static int Main()
    {
        byte p = 0xFF;
        int result = Foo(&p);
        if (result == 0)
        {
            Console.WriteLine("Passed");
            return 100;
        }
        else
        {
            Console.WriteLine("Failed: {0:x}",result);
            return 101;
        }
    }
}
