// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

#pragma warning disable CS0253 // Possible unintended reference comparison

class ExceptionSets
{
    public static int Main()
    {
        try
        {
            TestObjGetType(null, 0);
            return 101;
        }
        catch (NullReferenceException) { }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TestObjGetType(object a, int i)
    {
        var fls = false;
        var c1 = i == 0;
        var c2 = c1;

        if (((a.GetType() == a) & fls) | (i == 0))
        {
            return true;
        }

        return c2;
    }
}
