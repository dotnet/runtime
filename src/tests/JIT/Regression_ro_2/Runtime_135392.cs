// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_135392
{
    [Theory]
    [InlineData(3.0f)]
    [InlineData(-5.0f)]
    public static void TestEntryPoint(float value)
    {
        Assert.Equal(new Vector4(value + 1, value, value, value), StorePure(value));
        Assert.Equal(new Vector4(1, 42, value, value), StoreVector4(value));
        Assert.Equal(new Vector2(1, 42), StoreVector2(value));
        Assert.Equal(new Vector3(1, 42, value), StoreVector3(value));
        Assert.Equal(new Quaternion(1, 42, value, value), StoreQuaternion(value));
        Assert.Equal(new Plane(new Vector3(42), 1), StorePlaneDistance(value));
        Assert.Equal(new Plane(new Vector3(1), 42), StorePlaneNormal(value));
        Assert.Equal(new Vector4(43, 42, value, value), StoreAssignment(value));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void StoreVectorHalves(bool upper)
    {
        Vector64<float> written64 = Vector64.Create(1.0f);
        Vector64<float> modified64 = Vector64.Create(42.0f);
        Assert.Equal(upper ? Vector128.Create(modified64, written64) : Vector128.Create(written64, modified64),
            StoreVector128(upper));

        Vector128<float> written128 = Vector128.Create(1.0f);
        Vector128<float> modified128 = Vector128.Create(42.0f);
        Assert.Equal(upper ? Vector256.Create(modified128, written128) : Vector256.Create(written128, modified128),
            StoreVector256(upper));

        Vector256<float> written256 = Vector256.Create(1.0f);
        Vector256<float> modified256 = Vector256.Create(42.0f);
        Assert.Equal(upper ? Vector512.Create(modified256, written256) : Vector512.Create(written256, modified256),
            StoreVector512(upper));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector2 StoreVector2(float value)
    {
        Vector2 v = new Vector2(value);
        v.X = Modify(ref v);
        return v;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector3 StoreVector3(float value)
    {
        Vector3 v = new Vector3(value);
        v.X = Modify(ref v);
        return v;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector4 StoreVector4(float value)
    {
        Vector4 v = new Vector4(value);
        v.X = Modify(ref v);
        return v;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Quaternion StoreQuaternion(float value)
    {
        Quaternion v = new Quaternion(value, value, value, value);
        v.X = Modify(ref v);
        return v;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Plane StorePlaneDistance(float value)
    {
        Plane v = new Plane(new Vector3(value), value);
        v.D = ModifyNormal(ref v);
        return v;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Plane StorePlaneNormal(float value)
    {
        Plane v = new Plane(new Vector3(value), value);
        v.Normal = new Vector3(ModifyDistance(ref v));
        return v;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector4 StoreAssignment(float value)
    {
        Vector4 v = new Vector4(value);
        v.X = (v.Y = 42) + 1;
        return v;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector4 StorePure(float value)
    {
        Vector4 v = new Vector4(value);
        v.X = value + 1;
        return v;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<float> StoreVector128(bool upper)
    {
        Vector128<float> v = Vector128.Create(3.0f);
        if (upper)
        {
            Unsafe.Add(ref Unsafe.As<Vector128<float>, Vector64<float>>(ref v), 1) = Vector64.Create(Modify(ref v));
        }
        else
        {
            Unsafe.As<Vector128<float>, Vector64<float>>(ref v) = Vector64.Create(Modify(ref v));
        }
        return v;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector256<float> StoreVector256(bool upper)
    {
        Vector256<float> v = Vector256.Create(3.0f);
        if (upper)
        {
            Unsafe.Add(ref Unsafe.As<Vector256<float>, Vector128<float>>(ref v), 1) = Vector128.Create(Modify(ref v));
        }
        else
        {
            Unsafe.As<Vector256<float>, Vector128<float>>(ref v) = Vector128.Create(Modify(ref v));
        }
        return v;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector512<float> StoreVector512(bool upper)
    {
        Vector512<float> v = Vector512.Create(3.0f);
        if (upper)
        {
            Unsafe.Add(ref Unsafe.As<Vector512<float>, Vector256<float>>(ref v), 1) = Vector256.Create(Modify(ref v));
        }
        else
        {
            Unsafe.As<Vector512<float>, Vector256<float>>(ref v) = Vector256.Create(Modify(ref v));
        }
        return v;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float Modify(ref Vector2 v)
    {
        v.Y = 42;
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float Modify(ref Vector3 v)
    {
        v.Y = 42;
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float Modify(ref Vector4 v)
    {
        v.Y = 42;
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float Modify(ref Quaternion v)
    {
        v.Y = 42;
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float ModifyNormal(ref Plane v)
    {
        v.Normal = new Vector3(42);
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float ModifyDistance(ref Plane v)
    {
        v.D = 42;
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float Modify(ref Vector128<float> v)
    {
        v = Vector128.Create(42.0f);
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float Modify(ref Vector256<float> v)
    {
        v = Vector256.Create(42.0f);
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float Modify(ref Vector512<float> v)
    {
        v = Vector512.Create(42.0f);
        return 1;
    }
}
