// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class LocalMorphBackedge
{
    [Fact]
    public static int TestEntryPoint()
    {
        int x = 1234;
        int y = 5678;
        int* px;
        int** ppx = null;

        for (int i = 100; i < GetUpper(); i++)
        {
            px = &x;

            if (ppx != null)
            {
                *ppx = &y;
            }

            *px = i;
            ppx = &px;
        }

        return x;
    }
	
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int GetUpper() => 102;
}