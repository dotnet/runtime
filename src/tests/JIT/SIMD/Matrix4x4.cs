// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Numerics;
using Xunit;

public class Matrix4x4Test
{
    private const int Pass = 100;
    private const int Fail = -1;

    public static int Matrix4x4CreateScaleCenterTest3()
    {
        int returnVal = Pass;
        Vector3 scale = new Vector3(3, 4, 5);
        Vector3 center = new Vector3(23, 42, 666);

        Matrix4x4 scaleAroundZero = Matrix4x4.CreateScale(scale.X, scale.Y, scale.Z, Vector3.Zero);
        Matrix4x4 scaleAroundZeroExpected = Matrix4x4.CreateScale(scale.X, scale.Y, scale.Z);
        if (!scaleAroundZero.Equals(scaleAroundZeroExpected))
        {
            returnVal = Fail;
        }

        Matrix4x4 scaleAroundCenter = Matrix4x4.CreateScale(scale.X, scale.Y, scale.Z, center);
        Matrix4x4 scaleAroundCenterExpected = Matrix4x4.CreateTranslation(-center) * Matrix4x4.CreateScale(scale.X, scale.Y, scale.Z) * Matrix4x4.CreateTranslation(center);
        if (!scaleAroundCenter.Equals(scaleAroundCenterExpected))
        {
            returnVal = Fail;
        }
        return returnVal;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return Matrix4x4CreateScaleCenterTest3();
    }
}
