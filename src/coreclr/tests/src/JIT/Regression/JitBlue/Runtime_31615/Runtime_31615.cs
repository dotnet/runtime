// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

struct Rgba32
{
    public static readonly Vector4 MaxBytes = new Vector4(255);

    public byte R, G, B, A;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Rgba32(uint packed)
    {
        this = default;
        Rgba = packed;
    }

    public uint Rgba
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.As<Rgba32, uint>(ref this);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Unsafe.As<Rgba32, uint>(ref this) = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector4 ToVector4() => new Vector4(R, G, B, A) / MaxBytes;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector4 ToScaledVector4() => ToVector4();
}

struct RgbaVector
{
    public float R, G, B, A;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FromRgba32(Rgba32 source) => FromScaledVector4(source.ToScaledVector4());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FromScaledVector4(Vector4 vector) => FromVector4(vector);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FromVector4(Vector4 v)
    {
        v = Vector4.Clamp(v, Vector4.Zero, Vector4.One);
        R = v.X;
        G = v.Y;
        B = v.Z;
        A = v.W;
    }
}

class Program
{
    static int Main()
    {
        RgbaVector a = Test(0x01020304);
        Vector4 e = new Vector4(4, 3, 2, 1) / Rgba32.MaxBytes;

        return a.R == e.X && a.G == e.Y && a.B == e.Z && a.A == e.W ? 100 : 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static RgbaVector Test(uint packed)
    {
        Rgba32 source = new Rgba32(packed);
        RgbaVector result = default;
        result.FromRgba32(source);
        return result;
    }
}
