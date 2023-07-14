// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

class RayTracer
{
    private static IEnumerable<object> hittables;
    
    static RayTracer()
    {
        var list = new List<object>();
        list.Add(new object());
        hittables = list;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Ray r = GetRay();
        Consume(r.Direction);
        traceRay(r); 
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Consume(object o)
    {
        var _ = o.ToString();
    }

    public static Ray GetRay()
    {
        return new Ray(Vector3.Zero, -Vector3.UnitY);
    }

    private static Vector3 traceRay(Ray r)
    {
        foreach (object h in hittables) {
            TestHit(r);
            return Vector3.One;
        }
        return Vector3.Zero;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void TestHit(Ray ray)
    {
        return;
    }
}
