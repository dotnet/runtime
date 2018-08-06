// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using static System.Runtime.Intrinsics.X86.Avx;
using ColorPacket256 = VectorPacket256;

using System;

internal static class Surfaces
{

    private static readonly ColorPacket256 White = new ColorPacket256(SetAllVector256(1.0f));
    private static readonly ColorPacket256 Black = new ColorPacket256(0.02f, 0.0f, 0.14f);
    // Only works with X-Z plane.
    public static readonly Surface CheckerBoard =
        new Surface(
            delegate (VectorPacket256 pos)
            {
                var floored = ConvertToVector256Int32(Add(Floor(pos.Zs), Floor(pos.Xs)));
                var modMask = SetAllVector256<int>(1);
                var evenMaskint = Avx2.And(floored, modMask);
                var evenMask = Avx2.CompareEqual(evenMaskint, modMask);

                var resultX = BlendVariable(Black.Xs, White.Xs, StaticCast<int, float>(evenMask));
                var resultY = BlendVariable(Black.Ys, White.Ys, StaticCast<int, float>(evenMask));
                var resultZ = BlendVariable(Black.Zs, White.Zs, StaticCast<int, float>(evenMask));

                return new ColorPacket256(resultX, resultY, resultZ);
            },
            new VectorPacket256(1f, 1f, 1f),
            delegate (VectorPacket256 pos)
            {
                var floored = ConvertToVector256Int32(Add(Floor(pos.Zs), Floor(pos.Xs)));
                var modMask = SetAllVector256<int>(1);
                var evenMaskUint = Avx2.And(floored, modMask);
                var evenMask = Avx2.CompareEqual(evenMaskUint, modMask);

                return BlendVariable(SetAllVector256(0.5f), SetAllVector256(0.1f), StaticCast<int, float>(evenMask));
            },
            150f);



    public static readonly Surface Shiny =
        new Surface(
            delegate (VectorPacket256 pos) { return new VectorPacket256(1f, 1f, 1f); },
            new VectorPacket256(.5f, .5f, .5f),
            delegate (VectorPacket256 pos) { return SetAllVector256<float>(0.7f); },
            250f);

    public static readonly Surface MatteShiny =
        new Surface(
            delegate (VectorPacket256 pos) { return new VectorPacket256(1f, 1f, 1f); },
            new VectorPacket256(.25f, .25f, .25f),
            delegate (VectorPacket256 pos) { return SetAllVector256<float>(0.7f); },
            250f);
}
