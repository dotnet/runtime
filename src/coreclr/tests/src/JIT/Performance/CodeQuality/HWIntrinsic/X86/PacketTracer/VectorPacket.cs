// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using static System.Runtime.Intrinsics.X86.Avx;
using static System.Runtime.Intrinsics.X86.Sse;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using System;

internal struct VectorPacket256
{
    public Vector256<float> Xs;
    public Vector256<float> Ys;
    public Vector256<float> Zs;
    public Vector256<float> Lengths
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return Sqrt(DotProduct(this, this));
        }
    }


    public readonly static int Packet256Size = 8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VectorPacket256(Vector256<float> init)
    {
        Xs = init;
        Ys = init;
        Zs = init;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VectorPacket256(float xs, float ys, float zs)
    {
        Xs = Vector256.Create(xs);
        Ys = Vector256.Create(ys);
        Zs = Vector256.Create(zs);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VectorPacket256(Vector256<float> _Xs, Vector256<float> _ys, Vector256<float> _Zs)
    {
        Xs = _Xs;
        Ys = _ys;
        Zs = _Zs;
    }

    // Convert AoS vectors to SoA Packet256
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe VectorPacket256(float* vectors)
    {
        Vector256<float> m03 = LoadVector128(&vectors[0]).ToVector256(); // load lower halves
        Vector256<float> m14 = LoadVector128(&vectors[4]).ToVector256();
        Vector256<float> m25 = LoadVector128(&vectors[8]).ToVector256();
        m03 = InsertVector128(m03, &vectors[12], 1);  // load higher halves
        m14 = InsertVector128(m14, &vectors[16], 1);
        m25 = InsertVector128(m25, &vectors[20], 1);

        var xy = Shuffle(m14, m25, 2 << 6 | 1 << 4 | 3 << 2 | 2);
        var yz = Shuffle(m03, m14, 1 << 6 | 0 << 4 | 2 << 2 | 1);
        var _Xs = Shuffle(m03, xy, 2 << 6 | 0 << 4 | 3 << 2 | 0);
        var _ys = Shuffle(yz, xy, 3 << 6 | 1 << 4 | 2 << 2 | 0);
        var _Zs = Shuffle(yz, m25, 3 << 6 | 0 << 4 | 3 << 2 | 1);

        Xs = _Xs;
        Ys = _ys;
        Zs = _Zs;
    }

    // Convert SoA VectorPacket256 to AoS
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VectorPacket256 Transpose()
    {
        var rxy = Shuffle(Xs, Ys, 2 << 6 | 0 << 4 | 2 << 2 | 0);
        var ryz = Shuffle(Ys, Zs, 3 << 6 | 1 << 4 | 3 << 2 | 1);
        var rzx = Shuffle(Zs, Xs, 3 << 6 | 1 << 4 | 2 << 2 | 0);

        var r03 = Shuffle(rxy, rzx, 2 << 6 | 0 << 4 | 2 << 2 | 0);
        var r14 = Shuffle(ryz, rxy, 3 << 6 | 1 << 4 | 2 << 2 | 0);
        var r25 = Shuffle(rzx, ryz, 3 << 6 | 1 << 4 | 3 << 2 | 1);

        var m0 = r03.GetLower();
        var m1 = r14.GetLower();
        var m2 = r25.GetLower();
        var m3 = ExtractVector128(r03, 1);
        var m4 = ExtractVector128(r14, 1);
        var m5 = ExtractVector128(r25, 1);

        var _Xs = Vector256.Create(m0, m1);
        var _ys = Vector256.Create(m2, m3);
        var _Zs = Vector256.Create(m4, m5);

        return new VectorPacket256(_Xs, _ys, _Zs);
    }

    // Convert SoA VectorPacket256 to an incomplete AoS
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VectorPacket256 FastTranspose()
    {
        var rxy = Shuffle(Xs, Ys, 2 << 6 | 0 << 4 | 2 << 2 | 0);
        var ryz = Shuffle(Ys, Zs, 3 << 6 | 1 << 4 | 3 << 2 | 1);
        var rzx = Shuffle(Zs, Xs, 3 << 6 | 1 << 4 | 2 << 2 | 0);

        var r03 = Shuffle(rxy, rzx, 2 << 6 | 0 << 4 | 2 << 2 | 0);
        var r14 = Shuffle(ryz, rxy, 3 << 6 | 1 << 4 | 2 << 2 | 0);
        var r25 = Shuffle(rzx, ryz, 3 << 6 | 1 << 4 | 3 << 2 | 1);

        return new VectorPacket256(r03, r14, r25);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VectorPacket256 operator +(VectorPacket256 left, VectorPacket256 right)
    {
        return new VectorPacket256(Add(left.Xs, right.Xs), Add(left.Ys, right.Ys), Add(left.Zs, right.Zs));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VectorPacket256 operator -(VectorPacket256 left, VectorPacket256 right)
    {
        return new VectorPacket256(Subtract(left.Xs, right.Xs), Subtract(left.Ys, right.Ys), Subtract(left.Zs, right.Zs));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VectorPacket256 operator /(VectorPacket256 left, VectorPacket256 right)
    {
        return new VectorPacket256(Divide(left.Xs, right.Xs), Divide(left.Ys, right.Ys), Divide(left.Zs, right.Zs));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<float> DotProduct(VectorPacket256 left, VectorPacket256 right)
    {
        var x2 = Multiply(left.Xs, right.Xs);
        var y2 = Multiply(left.Ys, right.Ys);
        var z2 = Multiply(left.Zs, right.Zs);
        return Add(Add(x2, y2), z2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VectorPacket256 CrossProduct(VectorPacket256 left, VectorPacket256 right)
    {
        return new VectorPacket256(Subtract(Multiply(left.Ys, right.Zs), Multiply(left.Zs, right.Ys)),
                                   Subtract(Multiply(left.Zs, right.Xs), Multiply(left.Xs, right.Zs)),
                                   Subtract(Multiply(left.Xs, right.Ys), Multiply(left.Ys, right.Xs)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VectorPacket256 operator *(Vector256<float> left, VectorPacket256 right)
    {
        return new VectorPacket256(Multiply(left, right.Xs), Multiply(left, right.Ys), Multiply(left, right.Zs));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VectorPacket256 Normalize()
    {
        var length = this.Lengths;
        return new VectorPacket256(Divide(Xs, length), Divide(Ys, length), Divide(Zs, length));
    }
}
