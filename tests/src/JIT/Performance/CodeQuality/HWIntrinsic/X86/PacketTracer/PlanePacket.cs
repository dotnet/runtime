// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System.Runtime.Intrinsics.X86;
using static System.Runtime.Intrinsics.X86.Avx;
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;

internal sealed class PlanePacket256 : ObjectPacket256
{
    public VectorPacket256 Norms;
    public Vector256<float> Offsets;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PlanePacket256(VectorPacket256 norms, Vector256<float> offsets, Surface surface) : base(surface)
    {
        Norms = norms;
        Offsets = offsets;
    }

    public override VectorPacket256 Normals(VectorPacket256 pos)
    {
        return Norms;
    }

    public override Vector256<float> Intersect(RayPacket256 rayPacket256)
    {
        var denom = VectorPacket256.DotProduct(Norms, rayPacket256.Dirs);
        var dist = Divide(Add(VectorPacket256.DotProduct(Norms, rayPacket256.Starts), Offsets), Subtract(Vector256<float>.Zero, denom));
        var gtMask = Compare(denom, Vector256<float>.Zero, FloatComparisonMode.GreaterThanOrderedNonSignaling);
        return BlendVariable(dist, Intersections.NullDistance, gtMask);
    }
}
