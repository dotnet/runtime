// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

// LLM-Fuzz Mutation 5

public class Runtime_115495
{
    private const int Pass = 100;
    private const int Fail = -1;
    
    public class Assert
    {
        public static void Equal(float a, float b)
        {
            if (a != b)
                throw new Exception("Assert.Equal failed");
        }
        
        public static void NotEqual(float a, float b)
        {
            if (a == b)
                throw new Exception("Assert.NotEqual failed");
        }
    }
    
    public struct SimpleVector3
    {
        // Introduce lambda expressions to process array values.
        static Func<int, int> multiply = x => x * 2;
        
        static void Vector3GetHashCodeTest(ref int checksum)
        {
            Vector3 one = new Vector3(2.0f, 3.0f, 3.3f);
            Vector3 two = new Vector3(2.0f, 3.0f, 3.3f);
            Vector3 three = new Vector3(3.0f, 2.0f, 3.3f);
            Assert.Equal(one.GetHashCode(), two.GetHashCode());
            Assert.NotEqual(one.GetHashCode(), three.GetHashCode());
            Vector3 zero = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 oneAxis = new Vector3(1.0f, 0.0f, 0.0f);
            Assert.NotEqual(zero.GetHashCode(), oneAxis.GetHashCode());

            // Create an array and then use a for-loop with invariant bounds.
            float[] items = new float[] { one.X, one.Y, one.Z, three.X, three.Y };
            for (int i = 0, cnt = items.Length; i < cnt; i++)
            {
                checksum += (int)(items[i] * 1.0f);
            }
            // Use a for-each loop conditionally.
            if (checksum % 4 == 0)
            {
                foreach (float f in items)
                {
                    checksum += multiply((int)f);
                }
            }
            Console.WriteLine("Intermediate Checksum (Mutation 5): " + checksum);
        }

        [Fact]
        public static int Problem()
        {
            int checksum = 19;
            int returnVal = Pass;
            try
            {
                Vector3GetHashCodeTest(ref checksum);
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAILED: " + ex.Message);
                returnVal = Fail;
            }
            // Additional clonable loop using same array in a different context.
            int[] reusedArray = { 2, 4, 6, 8, 10 };
            for (int i = 0, length = reusedArray.Length; i < length; i++)
            {
                checksum += (i % 2 == 0) ? reusedArray[i] : -reusedArray[i];
            }
            Console.WriteLine("Final Checksum (Mutation 5): " + checksum);
            return returnVal;
        }
    }
}
