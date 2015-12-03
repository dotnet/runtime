// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
internal class ReproBoxProblem
{
    public static int Main(string[] args)
    {
        Console.WriteLine(DoOp(77.5, 77.5));
        return 100;
    }

    private static Object DoOp(Object v1, Object v2)
    {
        int i = (int)(double)v1;
        int j = (int)(double)v2;
        return (((uint)i) & 0xFFFFFFFFL) >> (j & 0x1F);
    }
}
