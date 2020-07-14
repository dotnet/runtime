// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System.Runtime.Intrinsics.X86;
using static System.Runtime.Intrinsics.X86.Avx;
using System.Runtime.Intrinsics;
internal class Camera
{

    public Camera(VectorPacket256 pos, VectorPacket256 forward, VectorPacket256 up, VectorPacket256 right) { Pos = pos; Forward = forward; Up = up; Right = right; }

    public VectorPacket256 Pos;
    public VectorPacket256 Forward;
    public VectorPacket256 Up;
    public VectorPacket256 Right;

    public static Camera Create(VectorPacket256 pos, VectorPacket256 lookAt)
    {
        VectorPacket256 forward = (lookAt - pos).Normalize();
        VectorPacket256 down = new VectorPacket256(Vector256<float>.Zero, Vector256.Create(-1.0f), Vector256<float>.Zero);
        Vector256<float> OnePointFive = Vector256.Create(1.5f);
        VectorPacket256 right = OnePointFive * VectorPacket256.CrossProduct(forward, down).Normalize();
        VectorPacket256 up = OnePointFive * VectorPacket256.CrossProduct(forward, right).Normalize();

        return new Camera(pos, forward, up, right);
    }

}
