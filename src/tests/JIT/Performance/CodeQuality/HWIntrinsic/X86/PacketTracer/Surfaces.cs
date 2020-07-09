// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using static System.Runtime.Intrinsics.X86.Avx;
using ColorPacket256 = VectorPacket256;

using System;

internal static class Surfaces
{

    private static readonly ColorPacket256 White = new ColorPacket256(Vector256.Create(1.0f));
    private static readonly ColorPacket256 Black = new ColorPacket256(0.02f, 0.0f, 0.14f);
    // Only works with X-Z plane.
    public static readonly Surface CheckerBoard =
        new Surface(
            delegate (VectorPacket256 pos)
            {
                var floored = ConvertToVector256Int32(Add(Floor(pos.Zs), Floor(pos.Xs)));
                var modMask = Vector256.Create(1);
                var evenMaskint = Avx2.And(floored, modMask);
                var evenMask = Avx2.CompareEqual(evenMaskint, modMask);

                var resultX = BlendVariable(Black.Xs, White.Xs, evenMask.AsSingle());
                var resultY = BlendVariable(Black.Ys, White.Ys, evenMask.AsSingle());
                var resultZ = BlendVariable(Black.Zs, White.Zs, evenMask.AsSingle());

                return new ColorPacket256(resultX, resultY, resultZ);
            },
            new VectorPacket256(1f, 1f, 1f),
            delegate (VectorPacket256 pos)
            {
                var floored = ConvertToVector256Int32(Add(Floor(pos.Zs), Floor(pos.Xs)));
                var modMask = Vector256.Create(1);
                var evenMaskUint = Avx2.And(floored, modMask);
                var evenMask = Avx2.CompareEqual(evenMaskUint, modMask);

                return BlendVariable(Vector256.Create(0.5f), Vector256.Create(0.1f), evenMask.AsSingle());
            },
            150f);



    public static readonly Surface Shiny =
        new Surface(
            delegate (VectorPacket256 pos) { return new VectorPacket256(1f, 1f, 1f); },
            new VectorPacket256(.5f, .5f, .5f),
            delegate (VectorPacket256 pos) { return Vector256.Create(0.7f); },
            250f);

    public static readonly Surface MatteShiny =
        new Surface(
            delegate (VectorPacket256 pos) { return new VectorPacket256(1f, 1f, 1f); },
            new VectorPacket256(.25f, .25f, .25f),
            delegate (VectorPacket256 pos) { return Vector256.Create(0.7f); },
            250f);
}
