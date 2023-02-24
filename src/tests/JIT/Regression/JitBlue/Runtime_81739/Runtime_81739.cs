// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;

public class Runtime_81739
{
    public static int Main()
    {
        Plane p;
        p.Normal = default;
        Consume(p.Normal);
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume<T>(T v)
    {
    }
}
