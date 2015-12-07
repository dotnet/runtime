// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
