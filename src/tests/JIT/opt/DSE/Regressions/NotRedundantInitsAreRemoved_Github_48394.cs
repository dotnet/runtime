// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public class NotRedundantInitsAreRemoved_Github_48394
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ValidateAndAssignValue(ref object obj)
    {
        if (obj != null)
            throw new Exception("array was expected to be null here");

        obj = new object();
    }

    public static int Main()
    {
        object obj = null;
        int i = 0;
        do
        {
            obj = null; // JIT shouldn't remove this store.
            ValidateAndAssignValue(ref obj);
        }
        while (i++ < 2);
        return 100;
    }
}
