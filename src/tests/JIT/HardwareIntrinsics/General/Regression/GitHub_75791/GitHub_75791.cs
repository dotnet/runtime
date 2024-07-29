// Licensed to the .NET Foundation under one or more agreements.(
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

namespace GitHub_75791;

public static class Program
{
    [Fact]
    public static void TestVector512()
    {
        Assert.Equal(2.9288656946658405, Vector512.Dot(
            Vector512.Create(0.28391050802823925, 0.33383399868214914, 0.6393570290293658, 0.9486867509591725, 0.03499340831637021, 0.8440502669865283, 0.9382154950924279, 0.9638167025024158),
            Vector512.Create(0.1982700660390313,  0.3166301813003236,  0.6653102738185168, 0.9267781773486286, 0.8465606085980281,  0.9969451724464531, 0.2048630727349331, 0.4139565663463529)
        ));

        Assert.Equal(3.1271863f, Vector512.Dot(
            Vector512.Create(0.9997464f, 0.3368471f,  0.4043606f,  0.40446985f, 0.8205302f,  0.96234834f,  0.033238932f, 0.4785298f,  0.5980946f, 0.035252146f, 0.466319f, 0.48365256f, 0.95094615f, 0.76286495f, 0.058176957f, 0.044761457f),
            Vector512.Create(0.192693f,  0.65009266f, 0.73478293f, 0.45294976f, 0.32503143f, 0.096156664f, 0.20120476f,  0.09926898f, 0.4980145f, 0.89575404f,  0.700502f, 0.92765516f, 0.06838739f, 0.7633024f,  0.5538336f,   0.83792007f)
        ));

        Assert.Equal(5.161698f, Vector512.Dot(
            Vector512.Create(0.024523573f, 0.9157307f,  0.81342965f, 0.975703f,  0.7373163f,  0.17368627f, 0.78028226f, 0.5694267f, 0.06921448f, 0.5432087f, 0.69687283f, 0.96234965f, 0.5256825f, 0.49651694f, 0.969829f,  0.3919952f),
            Vector512.Create(0.90546f,     0.61288774f, 0.12319762f, 0.6599227f, 0.18527496f, 0.28016788f, 0.4850578f,  0.5630629f, 0.8017178f,  0.7248057f, 0.6808885f,  0.35055026f, 0.740812f,  0.5224796f,  0.6798979f, 0.97064143f)
        ));
    }

    [Fact]
    public static void TestVector256()
    {
        Assert.Equal(1.8913451f, Vector256.Dot(
            Vector256.Create(0.4797493f, 0.12908089f, 0.65387505f, 0.3784436f,  0.82727f,   0.25609037f, 0.72052014f, 0.11474159f),
            Vector256.Create(0.4786259f, 0.17519481f, 0.47962677f, 0.19132254f, 0.9456166f, 0.88822955f, 0.291491f,   0.2903679f)
        ));
    }

    [Fact]
    public static void TestVector128()
    {
        Assert.Equal(1.2333127f, Vector128.Dot(
            Vector128.Create(0.5355215f, 0.9093521f, 0.9310961f,  0.11439307f),
            Vector128.Create(0.500749f,  0.7528007f, 0.26315397f, 0.31093028f)
        ));
    }
}
