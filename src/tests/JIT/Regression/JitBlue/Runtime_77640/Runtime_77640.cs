// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class Runtime_77640
{
    [Fact]
    public static int TestEntryPoint()
    {
        int result = 101;
        try
        {
            Problem(null);
        }
        catch (NullReferenceException)
        {
            result = 100;
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int* Problem(StructWithField* p) => &p->Field;

    struct StructWithField
    {
        public int Field;
    }
}
