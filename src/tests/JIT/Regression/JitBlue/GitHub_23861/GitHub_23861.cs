// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using Xunit;

namespace GitHub_23861
{
    public class Program
    {
        static int returnVal = 100;
        [Fact]
        public static int TestEntryPoint()
        {
            LessThanAllDouble();

            if (returnVal == 100)
            {
                Console.WriteLine("Pass");                
            }
            else
            {
                Console.WriteLine("FAIL");
            }
            return returnVal;
        }

        internal static void LessThanAllDouble() { TestVectorLessThanAll<double>(); }

        private static void TestVectorLessThanAll<T>() where T : struct, INumber<T>
        {
            T[] values1 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values1[g] = T.CreateTruncating<int>(g);
            }
            Vector<T> vec1 = new Vector<T>(values1);

            T[] values2 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values2[g] = T.CreateTruncating<int>(g + 25);
            }
            Vector<T> vec2 = new Vector<T>(values2);

            if (!Vector.LessThanAll(vec1, vec2))
            {
                returnVal = -1;
            }
            if (!Vector.LessThanAll(Vector<T>.Zero, Vector<T>.One))
            {
                returnVal = -1;
            }

            T[] values3 = new T[Vector<T>.Count];
            for (int g = 0; g < Vector<T>.Count; g++)
            {
                values3[g] = (g < Vector<T>.Count / 2) ? Vector<T>.Zero[0] : Vector<T>.One[0];
            }
            Vector<T> vec3 = new Vector<T>(values3);
            if (Vector.LessThanAll(vec3, Vector<T>.One))
            {
                returnVal = -1;
            }
        }
    }
}

