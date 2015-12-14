// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace JitTest
{
    using System;

    internal class StressTest
    {
        private const int ITERATIONS = 2000;
        private const ulong MAGIC = 0x7700001492000077;

        private static ulong UnpackRef(TypedReference _ref, int iterCount)
        {
            if (iterCount++ == ITERATIONS)
            {
                Console.WriteLine(ITERATIONS.ToString() + " refs unpacked.");
                if (__refvalue(_ref, ulong) == MAGIC)
                {
                    Console.WriteLine("Passed.");
                    throw new ArgumentException();  //cleanup in an unusual way
                }
                else
                {
                    Console.WriteLine("failed.");
                    throw new Exception();
                }
            }
            else
                return __refvalue(_ref, ulong);
        }

        private static void PackRef(TypedReference _ref, int iterCount)
        {
            if (++iterCount == ITERATIONS)
            {
                Console.WriteLine(ITERATIONS.ToString() + " refs packed.");
                UnpackRef(_ref, iterCount);
            }
            else
            {
                ulong N = UnpackRef(_ref, 0);
                PackRef(__makeref(N), iterCount);
            }
        }

        private static int Main()
        {
            try
            {
                ulong N = MAGIC;
                PackRef(__makeref(N), 0);
                return 2;
            }
            catch (ArgumentException)
            {
                return 100;
            }
            catch (Exception)
            {
                return 1;
            }
        }
    }
}
