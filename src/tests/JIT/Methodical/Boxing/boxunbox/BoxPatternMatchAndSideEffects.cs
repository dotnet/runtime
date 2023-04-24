// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class BoxPatternMatchAndSideEffects
{
    [Fact]
    public static int TestEntryPoint()
    {
        if (!Problem(new Struct[0]))
        {
            return 101;
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Problem(Struct[] a)
    {
        bool result = false;

        try
        {
            // Make sure the "box(x) != null" optimization does not drop the side-effect of indexing into the array.
            if ((object)a[int.MaxValue] != null)
            {
                result = false;
            }
        }
        catch (IndexOutOfRangeException)
        {
            result = true;
        }

        return result;
    }

    struct Struct { }
}
