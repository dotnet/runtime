// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace HelloWorld
{
    internal class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Vector4 test(Vector4 a, Vector4 b)
        {
            // return AdvSimd.Multiply(AdvSimd.Subtract(AdvSimd.Add(a, b), c), b);
            // return Vector64.Multiply(Vector64.Subtract(Vector64.Add(a, b), c), b);
            return Vector4.Min(a,b);
        }

        private static void Main(string[] args)
        {
            Vector4 a = new Vector4(1, 2, 3, 4);
            Vector4 b = new Vector4(2, 2, 1, 1);

            var result = test(a, b);
            Console.WriteLine(result);
        }
    }
}
