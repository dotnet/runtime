// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

internal struct Vector
{
    private Vector3 _simdVector;
    public float X { get { return _simdVector.X; } }
    public float Y { get { return _simdVector.Y; } set { _simdVector = new Vector3(_simdVector.X, value, _simdVector.Z); } }
    public float Z { get { return _simdVector.Z; } }

    public Vector(double x, double y, double z)
    {
        _simdVector = new Vector3((float)x, (float)y, (float)z);
    }
    public Vector(string str)
    {
        string[] nums = str.Split(',');
        if (nums.Length != 3) throw new ArgumentException();
        _simdVector = new Vector3(float.Parse(nums[0]), float.Parse(nums[1]), float.Parse(nums[2]));
    }
    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static Vector Times(double n, Vector v)
    {
        Vector result;
        result._simdVector = (float)n * v._simdVector;
        return result;
    }
    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static Vector Minus(Vector v1, Vector v2)
    {
        Vector result;
        result._simdVector = v1._simdVector - v2._simdVector;
        return result;
    }
    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static Vector Plus(Vector v1, Vector v2)
    {
        Vector result;
        result._simdVector = v1._simdVector + v2._simdVector;
        return result;
    }
    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static float Dot(Vector v1, Vector v2)
    {
        return Vector3.Dot(v1._simdVector, v2._simdVector);
    }
    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static float Mag(Vector v) { return (float)Math.Sqrt(Dot(v, v)); }
    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static Vector Norm(Vector v)
    {
        float mag = Mag(v);
        float div = mag == 0 ? float.PositiveInfinity : 1 / mag;
        return Times(div, v);
    }
    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static Vector Cross(Vector v1, Vector v2)
    {
        return new Vector(((v1.Y * v2.Z) - (v1.Z * v2.Y)),
                          ((v1.Z * v2.X) - (v1.X * v2.Z)),
                          ((v1.X * v2.Y) - (v1.Y * v2.X)));
    }
    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(Vector v1, Vector v2)
    {
        return v1._simdVector.Equals(v2._simdVector);
    }

    public static Vector Null { get { Vector result; result._simdVector = Vector3.Zero; return result; } }
}

