// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System.Runtime.Intrinsics;
using System;
using ColorPacket256 = VectorPacket256;

internal class Surface
{
    public Func<VectorPacket256, ColorPacket256> Diffuse;
    public VectorPacket256 Specular;
    public Func<VectorPacket256, Vector256<float>> Reflect;
    public float Roughness;

    public Surface(Func<VectorPacket256, ColorPacket256> Diffuse,
                    VectorPacket256 Specular,
                    Func<VectorPacket256, Vector256<float>> Reflect,
                    float Roughness)
    {
        this.Diffuse = Diffuse;
        this.Specular = Specular;
        this.Reflect = Reflect;
        this.Roughness = Roughness;
    }
}
