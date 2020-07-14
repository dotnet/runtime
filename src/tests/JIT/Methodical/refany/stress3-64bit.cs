// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace JitTest
{
    using System;

    internal class StressTest
    {
        private const int ITERATIONS = 4500;

        private static void PackRef(ref String refee, int iterCount)
        {
            if (++iterCount == ITERATIONS)
            {
                Console.WriteLine(ITERATIONS.ToString() + " refs created.");
            }
            else
            {
                TypedReference _ref = __makeref(refee);
                PackRef(ref refee, iterCount);
                if (__reftype(_ref) != typeof(String) ||
                    __refvalue(_ref, String) != "Hello")
                    throw new Exception();
            }
        }

        private static int Main()
        {
            try
            {
                String N = "Hello";
                PackRef(ref N, 0);
                return 100;
            }
            catch (Exception)
            {
                return 1;
            }
        }
    }
}
