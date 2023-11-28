// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;
using Xunit;

public partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    private class VectorInitTest
    {
        public static int VectorInit(float x)
        {
            int returnVal = Pass;

            Vector2 v2 = new Vector2(x);
            Vector3 v3 = new Vector3(v2.X);
            Vector4 v4 = new Vector4(v3.Y);

            float result2 = Vector2.Dot(v2, v2);
            float result3 = Vector3.Dot(v3, v3);
            float result4 = Vector4.Dot(v4, v4);

            Console.WriteLine("result2 : " + result2);
            Console.WriteLine("result3 : " + result3);
            Console.WriteLine("result4 : " + result4);

            if (result2 != 2f * x * x) return Fail;
            if (result3 != 3f * x * x) return Fail;
            if (result4 != 4f * x * x) return Fail;

            return returnVal;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int returnVal = Pass;

        if (VectorInitTest.VectorInit(2f) == Fail) returnVal = Fail;
        return returnVal;
    }
}

