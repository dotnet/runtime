// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

class FldSeqsInPromotedCpBlk
{
    public static int Main()
    {
        PromotableStruct s = default;
        return Problem(new PromotableStruct[1]) == 2 ? 100 : 101;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Problem(PromotableStruct[] a)
    {
        ref var firstElem = ref a[0];

        firstElem.SecondLngValue = 1;
        var b = new PromotableStruct() { SecondLngValue = 2 };

        if (firstElem.SecondLngValue == 1)
        {
            firstElem = b;
            return firstElem.SecondLngValue;
        }

        return -1;
    }
}

struct PromotableStruct
{
    public long FirstLngValue;
    public long SecondLngValue;
}
