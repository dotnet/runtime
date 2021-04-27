// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Numerics;
using System.Diagnostics;

public class Runtime_49101
{
    struct S
    {
        public Vector3 v;
        public int anotherField;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test(int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8, int i9, float f1, float f2, float f3, float f4, float f5, float f6, float f7, float f8, float f9, Vector3 v)
    {
        Debug.Assert(v == Vector3.One);
        if (v == Vector3.One)
        {
            return 100;
        }
        return 101;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector3 Get()
    {
        return Vector3.One;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Main()
    {
        S s;
        s.v = Get();
        return Test(0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, s.v);
    }
}
