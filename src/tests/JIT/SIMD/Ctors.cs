// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Numerics;
using Xunit;

public partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    public class Program
    {		
        const float EPS = Single.Epsilon * 5;

        static bool CheckEQ(float a, float b)
        {
            return Math.Abs(a - b) < EPS;
        }

        static bool CheckNEQ(float a, float b)
        {
            return !CheckEQ(a, b);
        }

        static int Vector2Ctors()
        {
            Vector2 a = new Vector2(45, 12);
            if (CheckNEQ(a.X, 45) || CheckNEQ(a.Y, 12))
                return 0;
            a.X = 100;
            Vector2 b = new Vector2(65);

            if (CheckNEQ(b.X, 65) || CheckNEQ(b.Y, 65))
                return 0;
            return 100;
        }

        static int Vector3Ctors()
        {
            Vector3 a = new Vector3(0, 1, 2);
            if (CheckNEQ(a.X, 0) || CheckNEQ(a.Y, 1) || CheckNEQ(a.Z, 2))
                return 0;
            Vector3 b = new Vector3(2);
            if (CheckNEQ(b.X, 2) || CheckNEQ(b.Y, 2) || CheckNEQ(b.Z, 2))
                return 0;
            Vector2 q = new Vector2(10, 1);
            Vector3 c = new Vector3(q, 5);
            if (CheckNEQ(c.X, q.X) || CheckNEQ(c.Y, q.Y) || CheckNEQ(c.Z, 5))
                return 0;
            return 100;
        }

        static int Vector4Ctors()
        {
            Vector4 a = new Vector4(0, 1, 2, 3);
            if (CheckNEQ(a.X, 0) || CheckNEQ(a.Y, 1) || CheckNEQ(a.Z, 2) || CheckNEQ(a.W, 3))
                return 0;
            Vector4 b = new Vector4(2);
            if (CheckNEQ(b.X, 2) || CheckNEQ(b.Y, 2) || CheckNEQ(b.Z, 2) || CheckNEQ(b.W, 2))
                return 0;
            Vector2 q = new Vector2(10, 1);
            Vector4 c = new Vector4(q, 10, -1);
            if (CheckNEQ(c.X, q.X) || CheckNEQ(c.Y, q.Y) || CheckNEQ(c.Z, 10) || CheckNEQ(c.W, -1))
                return 0;
            Vector3 w = new Vector3(5);
            Vector4 d = new Vector4(w, 2);
            if (CheckNEQ(d.X, w.X) || CheckNEQ(d.Y, w.Y) || CheckNEQ(d.Z, w.Z) || CheckNEQ(d.W, 2))
                return 0;
            return 100;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            int returnVal = Pass;
            
            if (Vector2Ctors() != 100 || Vector3Ctors() != 100 || Vector4Ctors() != 100)
            {
                returnVal = Fail;
            }

            JitLog jitLog = new JitLog();
            if (!jitLog.Check("System.Numerics.Vector2:.ctor(float,float)")) returnVal = Fail;
            if (!jitLog.Check("System.Numerics.Vector3:.ctor(float,float,float)")) returnVal = Fail;
            if (!jitLog.Check("System.Numerics.Vector4:.ctor(float,float,float,float)")) returnVal = Fail;
            jitLog.Dispose();

            return returnVal;
        }
    }
}
