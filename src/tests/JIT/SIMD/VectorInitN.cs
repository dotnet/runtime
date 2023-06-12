// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    private class VectorInitNTest
    {
        private static float s_value = 1.0F;

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static float nextValue()
        {
            float returnValue = s_value;
            s_value += 1.0F;
            return returnValue;
        }

        public static int VectorInitN(float x, float y, float z, float w)
        {
            int returnVal = Pass;

            Vector2 v2 = new Vector2(x, y);
            if (v2.X != x)
            {
                Console.WriteLine("Vector2.X failed");
                returnVal = Fail;
            }
            if (v2.Y != y)
            {
                Console.WriteLine("Vector2.Y failed");
                returnVal = Fail;
            }
            v2 = new Vector2(nextValue(), nextValue());
            if (v2.X > v2.Y)
            {
                Console.WriteLine("Vector2 evaluation order failed.");
            }

            Vector3 v3 = new Vector3(x, y, z);
            if (v3.X != x)
            {
                Console.WriteLine("Vector3.X failed");
                returnVal = Fail;
            }
            if (v3.Y != y)
            {
                Console.WriteLine("Vector3.Y failed");
                returnVal = Fail;
            }
            if (v3.Z != z)
            {
                Console.WriteLine("Vector3.Z failed");
                returnVal = Fail;
            }
            v3 = new Vector3(nextValue(), nextValue(), nextValue());
            if ((v3.X > v3.Y) || (v3.Y > v3.Z))
            {
                Console.WriteLine("Vector3 evaluation order failed.");
            }

            Vector4 v4 = new Vector4(x, y, z, w);
            if (v4.X != x)
            {
                Console.WriteLine("Vector4.X failed");
                returnVal = Fail;
            }
            if (v4.Y != y)
            {
                Console.WriteLine("Vector4.Y failed");
                returnVal = Fail;
            }
            if (v4.Z != z)
            {
                Console.WriteLine("Vector4.Z failed");
                returnVal = Fail;
            }
            if (v4.W != w)
            {
                Console.WriteLine("Vector4.W failed");
                returnVal = Fail;
            }
            v4 = new Vector4(nextValue(), nextValue(), nextValue(), nextValue());
            if ((v4.X > v4.Y) || (v4.Y > v4.Z) || (v4.Z > v4.W))
            {
                Console.WriteLine("Vector4 evaluation order failed.");
            }


            return returnVal;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int returnVal = Pass;

        if (VectorInitNTest.VectorInitN(0.5f, -0.5f, 0f, 1.0f) == Fail) returnVal = Fail;

        JitLog jitLog = new JitLog();
        if (!jitLog.Check("System.Numerics.Vector2:.ctor(float,float)")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector3:.ctor(float,float,float)")) returnVal = Fail;
        if (!jitLog.Check("System.Numerics.Vector4:.ctor(float,float,float,float)")) returnVal = Fail;
        jitLog.Dispose();

        return returnVal;
    }
}

