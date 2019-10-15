// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;
using static System.Runtime.Intrinsics.X86.Avx;
using static System.Runtime.Intrinsics.X86.Avx2;
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using System;

internal class Scene
{
    public ObjectPacket256[] Things;
    public LightPacket256[] Lights;
    public Camera Camera;

    public Scene(ObjectPacket256[] things, LightPacket256[] lights, Camera camera) { Things = things; Lights = lights; Camera = camera; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VectorPacket256 Normals(Vector256<int> things, VectorPacket256 pos)
    {
        VectorPacket256 norms = new VectorPacket256(1, 1, 1);

        for (int i = 0; i < Things.Length; i++)
        {
            Vector256<float> mask = CompareEqual(things, Vector256.Create(i)).AsSingle();
            var n = Things[i].Normals(pos);
            norms.Xs = BlendVariable(norms.Xs, n.Xs, mask);
            norms.Ys = BlendVariable(norms.Ys, n.Ys, mask);
            norms.Zs = BlendVariable(norms.Zs, n.Zs, mask);
        }

        return norms;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector256<float> Reflect(Vector256<int> things, VectorPacket256 pos)
    {
        Vector256<float> rfl = Vector256.Create(1.0f);
        for (int i = 0; i < Things.Length; i++)
        {
            Vector256<float> mask = CompareEqual(things, Vector256.Create(i)).AsSingle();
            rfl = BlendVariable(rfl, Things[i].Surface.Reflect(pos), mask);
        }
        return rfl;
    }

}
