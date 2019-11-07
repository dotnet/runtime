// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace JitTest
{
    internal class StressTest
    {
        private const int ITERATIONS = 2000;
        private const ulong MAGIC = 0x7700001492000077;

        public virtual ulong UnpackRef(TypedReference _ref, int iterCount)
        {
            if (iterCount++ == ITERATIONS)
            {
                if (__refvalue(_ref, ulong) == MAGIC)
                {
                    throw new ArgumentException();  //cleanup in an unusual way
                }
                else
                {
                    throw new Exception();
                }
            }
            else
                return __refvalue(_ref, ulong);
        }

        public virtual void PackRef(TypedReference _ref, int iterCount)
        {
            if (++iterCount == ITERATIONS)
            {
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
                new StressTest().PackRef(__makeref(N), 0);
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
