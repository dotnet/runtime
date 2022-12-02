// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

readonly struct Ray
{
    public readonly Vector3 Origin;
    public readonly Vector3 Direction;

    public Ray(Vector3 origin, Vector3 direction)
    {
        Origin = origin;
        Direction = direction;
    }
}

class Runtime_79118
{
    static IEnumerable<object> hittables;

    static Runtime_79118()
    {
        var list = new List<object>();
        list.Add(new object());
        hittables = list;
    }

    static int Main()
    {
        Ray r = GetRay();
        traceRay(r);
        return 100;
    }

    static Ray GetRay()
    {
        return new Ray(Vector3.Zero, -Vector3.UnitY);
    }

    [MethodImpl(MethodImplOptions.NoInlining | 
                MethodImplOptions.AggressiveOptimization)]
    static Vector3 traceRay(Ray r)
    {
        foreach (object h in hittables)
        {
            TestHit(r);
            return Vector3.One;
        }
        return Vector3.Zero;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestHit(Ray ray)
    {
    }
}
