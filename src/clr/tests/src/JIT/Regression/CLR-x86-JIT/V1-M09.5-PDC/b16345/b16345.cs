// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
