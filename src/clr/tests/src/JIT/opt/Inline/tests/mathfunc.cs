// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace JitInliningTest
{
    internal class MathFunc
    {
        public static int Main()
        {
            Random r = new Random();

            int a = Math.Abs(r.Next(100) - r.Next(100));
            a += Math.Max(r.Next(100), r.Next(100));
            a -= Math.Min(r.Next(100), r.Next(100));

            double b = Math.Acos(r.Next(1));
            b += Math.Asin(r.Next(1));
            b += Math.Atan(r.Next(1, 5));
            b -= Math.Cos(r.Next(1, 5));
            b -= Math.Cosh(r.Next(1, 5));
            b -= Math.Atan2(r.Next(1, 5), r.Next(1, 5));
            b -= Math.Exp(r.Next(2));
            b += Math.IEEERemainder(r.Next(1, 5), r.Next(1, 5));
            b -= Math.Log(r.Next(1, 5));
            b += Math.Log(r.Next(1, 5), r.Next(1, 5));
            b -= Math.Log10(r.Next(1, 5));
            b += Math.Pow(r.Next(1, 5), r.Next(1, 3));
            b -= Math.Round((double)r.Next(1, 5));
            b -= Math.Sinh(r.Next(1, 5));
            b += Math.Sqrt(r.Next(1, 100));
            b -= Math.Tan(r.Next(1, 5));
            b += Math.Tanh(r.Next(1, 5));

            Console.WriteLine(b);
            return 100;
        }
    }
}
