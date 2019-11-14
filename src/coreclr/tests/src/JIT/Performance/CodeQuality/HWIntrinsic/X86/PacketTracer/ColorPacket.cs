// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using static System.Runtime.Intrinsics.X86.Avx;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;

using ColorPacket256 = VectorPacket256;

internal static class ColorPacket256Helper
{

    private static readonly Vector256<float> One = Vector256.Create(1.0f);
    private static readonly Vector256<float> Max = Vector256.Create(255.0f);
    public static Int32RGBPacket256 ConvertToIntRGB(this VectorPacket256 colors)
    {

        var rsMask = Compare(colors.Xs, One, FloatComparisonMode.OrderedGreaterThanNonSignaling);
        var gsMask = Compare(colors.Ys, One, FloatComparisonMode.OrderedGreaterThanNonSignaling);
        var bsMask = Compare(colors.Zs, One, FloatComparisonMode.OrderedGreaterThanNonSignaling);

        var rs = BlendVariable(colors.Xs, One, rsMask);
        var gs = BlendVariable(colors.Ys, One, gsMask);
        var bs = BlendVariable(colors.Zs, One, bsMask);

        var rsInt = ConvertToVector256Int32(Multiply(rs, Max));
        var gsInt = ConvertToVector256Int32(Multiply(gs, Max));
        var bsInt = ConvertToVector256Int32(Multiply(bs, Max));

        return new Int32RGBPacket256(rsInt, gsInt, bsInt);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ColorPacket256 Times(ColorPacket256 left, ColorPacket256 right)
    {
        return new VectorPacket256(Multiply(left.Xs, right.Xs), Multiply(left.Ys, right.Ys), Multiply(left.Zs, right.Zs));
    }

    public static readonly ColorPacket256 BackgroundColor = new ColorPacket256(Vector256<float>.Zero);
    public static readonly ColorPacket256 DefaultColor = new ColorPacket256(Vector256<float>.Zero);
}

internal struct Int32RGBPacket256
{
    public Vector256<int> Rs;
    public Vector256<int> Gs;
    public Vector256<int> Bs;

    public Int32RGBPacket256(Vector256<int> rs, Vector256<int> gs, Vector256<int> bs)
    {
        Rs = rs;
        Gs = gs;
        Bs = bs;
    }
}
