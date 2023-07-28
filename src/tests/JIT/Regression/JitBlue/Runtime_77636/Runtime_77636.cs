// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class Runtime_77636
{
    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Problem(null);
        }
        catch (NullReferenceException)
        {
            return 100;
        }

        return 101;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Problem(StructWithIndex* s)
    {
        return *(int*)((nint)(int*)&s->Value | (-1 & ~1));
    }
    
    struct StructWithIndex
    {
        public int Index;
        public int Value;
    }
}
