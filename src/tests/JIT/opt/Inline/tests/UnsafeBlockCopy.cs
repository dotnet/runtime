// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Test_UnsafeBlockCopy 
{
    static int SIZE = 100;

    [Fact]
    public static unsafe int TestEntryPoint()
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
