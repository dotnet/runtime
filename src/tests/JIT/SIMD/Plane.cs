// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Numerics;
using Xunit;

public class PlaneTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    public static int PlaneCreateFromVerticesTest()
    {
        int returnVal = Pass;

        Vector3 point1 = new Vector3(0.0f, 1.0f, 1.0f);
        Vector3 point2 = new Vector3(0.0f, 0.0f, 1.0f);
        Vector3 point3 = new Vector3(1.0f, 0.0f, 1.0f);

        Plane target = Plane.CreateFromVertices(point1, point2, point3);
        Plane expected = new Plane(new Vector3(0, 0, 1), -1.0f);
        if (!target.Equals(expected))
        {
            returnVal = Fail;
        }
        return returnVal;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return PlaneCreateFromVerticesTest();
    }
}
