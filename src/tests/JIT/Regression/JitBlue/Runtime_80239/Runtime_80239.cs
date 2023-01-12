﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;

class Runtime_80239
{
    static int Main(string[] args)
    {
        Unsafe.SkipInit(out Vector3 test);
        test.X = 500.0f;
        Consume(test);
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(Vector3 test)
    {
    }
}
