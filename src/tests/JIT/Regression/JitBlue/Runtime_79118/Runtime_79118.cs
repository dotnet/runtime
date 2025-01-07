// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public readonly struct Ray
{
    public readonly Vector3 Origin;
    public readonly Vector3 Direction;

    public Ray(Vector3 origin, Vector3 direction)
    {
        Origin = origin;
        Direction = direction;
    }
}

public class RayTracer
{
    private static IEnumerable<object> hittables;

    static RayTracer()
    {
        var list = new List<object>();
        list.Add(new object());
        hittables = list;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Ray r = GetRay();
        Consume(r.Direction);
        traceRay(r);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(object o)
    {
        var _ = o.ToString();
    }

    private static Ray GetRay()
    {
        return new Ray(Vector3.Zero, -Vector3.UnitY);
    }

    private static Vector3 traceRay(Ray r)
    {
        foreach (object h in hittables)
        {
            TestHit(r);
            return Vector3.One;
        }
        return Vector3.Zero;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TestHit(Ray ray)
    {
        return;
    }
}
