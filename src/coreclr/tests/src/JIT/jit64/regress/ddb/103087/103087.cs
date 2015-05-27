// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
