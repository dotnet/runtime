// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace JitTest
{
    using System;
    using System.Text;
    using System.Runtime.InteropServices;

    internal class Test
    {
        [DllImport("msvcrt", EntryPoint = "sin", CallingConvention = CallingConvention.Cdecl)]
        private static extern double sin(double x);

        [DllImport("msvcrt", EntryPoint = "acos", CallingConvention = CallingConvention.Cdecl)]
        private static extern double acos(double x);

        private static int Main()
        {
            for (double x = 0.0; x <= 3.1415926535897; x += 0.14)
            {
                if (Math.Abs(sin(x) - Math.Sin(x)) > 0.00001)
                {
                    Console.WriteLine("=== FAILED ===");
                    Console.WriteLine("for x = " + x.ToString());
                    return 101;
                }
            }
            for (double x = -1.0; x <= 1.0; x += 0.1)
            {
                if (Math.Abs(acos(x) - Math.Acos(x)) > 0.00001)
                {
                    Console.WriteLine("=== FAILED ===");
                    Console.WriteLine("for x = " + x.ToString());
                    return 102;
                }
            }
            Console.WriteLine("=== PASSED ===");
            return 100;
        }
    }
}
