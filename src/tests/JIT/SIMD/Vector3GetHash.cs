// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This was copied from corefx\src\System.Numerics.Vectors\tests\Vector3Tests.cs.
// It exposed a bug in morph, in which a SIMD field was being morphed in
// a MACK_Ind context, even though it was under a GT_IND(G_ADDR()).
// This was https://github.com/dotnet/runtime/issues/20080, and was "fixed" with
// https://github.com/dotnet/coreclr/pull/9496 and a new issue,
// https://github.com/dotnet/runtime/issues/7405, has been filed to track the underlying unexpected
// MACK_Ind context, which causes us to mark the Vector3 local as do-not-enregister.

using System;
using System.Numerics;
using Xunit;

public partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    public class Assert
    {
        public static void Equal(float f1, float f2)
        {
            if (f1 != f2)
            {
                throw new Exception("Assert.Equal failed");
            }
        }
        public static void NotEqual(float f1, float f2)
        {
            if (f1 == f2)
            {
                throw new Exception("Assert.NotEqual failed");
            }
        }
    }
    public struct SimpleVector3
    {
        public static void Vector3GetHashCodeTest()
        {
            Vector3 v1 = new Vector3(2.0f, 3.0f, 3.3f);
            Vector3 v2 = new Vector3(2.0f, 3.0f, 3.3f);
            Vector3 v3 = new Vector3(2.0f, 3.0f, 3.3f);
            Vector3 v5 = new Vector3(3.0f, 2.0f, 3.3f);
            Assert.Equal(v1.GetHashCode(), v1.GetHashCode());
            Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
            Assert.NotEqual(v1.GetHashCode(), v5.GetHashCode());
            Assert.Equal(v1.GetHashCode(), v3.GetHashCode());
            Vector3 v4 = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 v6 = new Vector3(1.0f, 0.0f, 0.0f);
            Vector3 v7 = new Vector3(0.0f, 1.0f, 0.0f);
            Vector3 v8 = new Vector3(1.0f, 1.0f, 1.0f);
            Vector3 v9 = new Vector3(1.0f, 1.0f, 0.0f);
            Assert.NotEqual(v4.GetHashCode(), v6.GetHashCode());
            Assert.NotEqual(v4.GetHashCode(), v7.GetHashCode());
            Assert.NotEqual(v4.GetHashCode(), v8.GetHashCode());
            Assert.NotEqual(v7.GetHashCode(), v6.GetHashCode());
            Assert.NotEqual(v8.GetHashCode(), v6.GetHashCode());
            Assert.NotEqual(v8.GetHashCode(), v9.GetHashCode());
            Assert.NotEqual(v7.GetHashCode(), v9.GetHashCode());
        }

        [Fact]
        public static int TestEntryPoint()
        {
            int returnVal = Pass;

            try
            {
                Vector3GetHashCodeTest();
            }
            catch (Exception e)
            {
                Console.WriteLine("FAILED: " + e.Message);
                returnVal = Fail;
            }
            return returnVal;
        }
    }
}

