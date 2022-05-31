// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

public unsafe class Runtime_54956
{
    public static int Main()
    {
        try
        {
            SideEffects(null, new Vector<int>[] { });
        }
        catch (Exception exception)
        {
            if (exception is NullReferenceException)
            {
                return 100;
            }
        }

        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<int> SideEffects(Vector<int>* left, Vector<int>[] right)
    {
        var result = Vector.AndNot(*left, right[0]);
        return result;
    }
}
