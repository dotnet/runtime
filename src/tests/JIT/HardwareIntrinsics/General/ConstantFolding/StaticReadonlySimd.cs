// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Threading;
using Xunit;

public class StaticReadonlySimd
{
    [Fact]
    public static void TestEntryPoint()
    {
        for (int i = 0; i < 100; i++)
        {
            // Warm up Test so the Tier1 version will be statically initialized.
            Test();
            Thread.Sleep(15);
        }
    }

    static readonly Vector2 v1 = new Vector2(-1.0f, 2.0f);
    static readonly Vector3 v2 = new Vector3(-1.0f, 2.0f, -0.0f);
    static readonly Vector4 v3 = new Vector4(-1.0f, 2.0f, -3.0f, 4.0f);
    static readonly Vector<long> v4 = new Vector<long>(new long[] { 1,2,3,4 });
    static readonly Vector<float> v5 = new Vector<float>(new float[] { 1,2,3,4,5,6,7,8 });
    static readonly Vector64<float> v6 = Vector64.Create(-3.14f);
    static readonly Vector64<long> v7 = Vector64.Create((long)42);
    static readonly Vector128<ulong> v8 = Vector128.Create((ulong)1111111111,2222222222);
    static readonly Vector128<double> v9 = Vector128.Create(1111111111.0, -2222222222.0);
    static readonly Vector256<byte> v10 = Vector256.Create(1111111111, 2222222222, ulong.MaxValue, 0).AsByte();
    static readonly Vector256<ulong> v11 = Vector256.Create(444444444, 2222222222, 0, ulong.MaxValue);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test()
    {
        AssertEquals(v1, new Vector2(-1.0f, 2.0f));
        AssertEquals(v2, new Vector3(-1.0f, 2.0f, -0.0f));
        AssertEquals(v3, new Vector4(-1.0f, 2.0f, -3.0f, 4.0f));
        AssertEquals(v4, new Vector<long>(new long[] { 1,2,3,4 }));
        AssertEquals(v5, new Vector<float>(new float[] { 1,2,3,4,5,6,7,8 }));
        AssertEquals(v6, Vector64.Create(-3.14f));
        AssertEquals(v7, Vector64.Create((long)42));
        AssertEquals(v8, Vector128.Create((ulong)1111111111, 2222222222));
        AssertEquals(v9, Vector128.Create(1111111111.0, -2222222222.0));
        AssertEquals(v10, Vector256.Create(1111111111, 2222222222, ulong.MaxValue, 0).AsByte());
        AssertEquals(v11, Vector256.Create(444444444, 2222222222, 0, ulong.MaxValue));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void AssertEquals<T>(T t1, T t2) where T : IEquatable<T>
    {
        if (!t1.Equals(t2))
            throw new Exception($"{t1} != {t2}");
    }
}
