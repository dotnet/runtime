// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using static System.Runtime.Intrinsics.X86.Avx;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using System;

public static class VectorMath
{
    static readonly Vector256<float> MaxValue = Vector256.Create(88.0f);
    static readonly Vector256<float> MinValue = Vector256.Create(-88.0f);
    static readonly Vector256<float> Log2 = Vector256.Create(1.44269502f);
    static readonly Vector256<float> C1 = Vector256.Create(0.693359375f);
    static readonly Vector256<float> C2 = Vector256.Create(-0.0002121944417f);
    static readonly Vector256<float> P0 = Vector256.Create(0.0001987569121f);
    static readonly Vector256<float> P1 = Vector256.Create(0.001398199936f);
    static readonly Vector256<float> P2 = Vector256.Create(0.008333452046f);
    static readonly Vector256<float> P3 = Vector256.Create(0.04166579619f);
    static readonly Vector256<float> P4 = Vector256.Create(0.1666666567f);
    static readonly Vector256<float> LogP0 = Vector256.Create(0.07037683576f);
    static readonly Vector256<float> LogP1 = Vector256.Create(-0.1151461005f);
    static readonly Vector256<float> LogP2 = Vector256.Create(0.1167699844f);
    static readonly Vector256<float> LogP3 = Vector256.Create(-0.1242014095f);
    static readonly Vector256<float> LogP4 = Vector256.Create(0.1424932331f);
    static readonly Vector256<float> LogP5 = Vector256.Create(-0.1666805744f);
    static readonly Vector256<float> LogP6 = Vector256.Create(0.2000071406f);
    static readonly Vector256<float> LogP7 = Vector256.Create(-0.2499999404f);
    static readonly Vector256<float> LogP8 = Vector256.Create(0.3333333135f);
    static readonly Vector256<float> LogQ1 = Vector256.Create(-0.0002121944417f);
    static readonly Vector256<float> LogQ2 = Vector256.Create(0.693359375f);
    static readonly Vector256<float> Point5 = Vector256.Create(0.5f);
    static readonly Vector256<float> Sqrthf = Vector256.Create(0.7071067691f);
    static readonly Vector256<float> One = Vector256.Create(1.0f);
    static readonly Vector256<int> Ox7 = Vector256.Create(127);
    static readonly Vector256<int> MinNormPos = Vector256.Create(8388608);
    static readonly Vector256<int> MantMask = Vector256.Create(-2139095041);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<float> Pow(Vector256<float> left, Vector256<float> right)
    {
        return Exp(Multiply(right, Log(left)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<float> Exp(Vector256<float> value)
    {
        value = Min(value, MaxValue);
        value = Max(value, MinValue);
        Vector256<float> fx = Multiply(value, Log2);
        fx = Floor(Add(fx, Point5));

        Vector256<float> tmp = Multiply(fx, C1);
        Vector256<float> z = Multiply(fx, C2);
        Vector256<float> x = Subtract(value, tmp);
        x = Subtract(x, z);
        z = Multiply(x, x);
        Vector256<float> y = P0;
        y = Add(Multiply(y, x), P1);
        y = Add(Multiply(y, x), P2);
        y = Add(Multiply(y, x), P3);
        y = Add(Multiply(y, x), P4);
        y = Add(Multiply(y, x), Point5);
        y = Add(Add(Multiply(y, z), x), One);

        Vector256<int> pow2n = ConvertToVector256Int32(fx);
        pow2n = Avx2.Add(pow2n, Ox7);
        pow2n = Avx2.ShiftLeftLogical(pow2n, 23);

        return Multiply(y, pow2n.AsSingle());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<float> Log(Vector256<float> value)
    {
        Vector256<float> invalidMask = Compare(value, Vector256<float>.Zero, FloatComparisonMode.OrderedLessThanOrEqualNonSignaling);
        Vector256<float> x = Max(value, MinNormPos.AsSingle());
        Vector256<int> ei = Avx2.ShiftRightLogical(x.AsInt32(), 23);
        x = Or(And(x, MantMask.AsSingle()), Point5);
        ei = Avx2.Subtract(ei, Ox7);
        Vector256<float> e = Add(ConvertToVector256Single(ei), One);
        Vector256<float> mask = Compare(x, Sqrthf, FloatComparisonMode.OrderedLessThanNonSignaling);
        Vector256<float> tmp = And(x, mask);
        x = Subtract(x, One);
        e = Subtract(e, And(One, mask));
        x = Add(x, tmp);
        Vector256<float> z = Multiply(x, x);
        Vector256<float> y = LogP0;
        y = Add(Multiply(y, x), LogP1);
        y = Add(Multiply(y, x), LogP2);
        y = Add(Multiply(y, x), LogP3);
        y = Add(Multiply(y, x), LogP4);
        y = Add(Multiply(y, x), LogP5);
        y = Add(Multiply(y, x), LogP6);
        y = Add(Multiply(y, x), LogP7);
        y = Add(Multiply(y, x), LogP8);
        y = Multiply(Multiply(y, x), z);
        y = Add(y, Multiply(e, LogQ1));
        y = Subtract(y, Multiply(z, Point5));
        x = Add(Add(x, y), Multiply(e, LogQ2));
        return Or(x, invalidMask);
    }

}
