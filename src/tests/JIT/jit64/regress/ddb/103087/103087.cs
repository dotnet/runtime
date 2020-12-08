// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal class Ddb103087
{
    public static int Main(string[] args)
    {
        double v1 = args.Length == 0 ? -0.0 : 0.0;
        double v2 = args.Length != 0 ? -0.0 : 0.0;
        if (!Double.IsNegativeInfinity(1 / v1)) return 101;
        if (!Double.IsInfinity(1 / v2)) return 101;
        return 100;
    }
}
