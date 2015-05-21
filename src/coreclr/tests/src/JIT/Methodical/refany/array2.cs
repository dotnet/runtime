// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace JitTest
{
    internal class Test
    {
        private static void TestRef(TypedReference _ref)
        {
            if (__reftype(_ref) == typeof(ulong[,]))
            {
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        __refvalue(_ref, ulong[,])[i, j]--;
                    }
                }
            }
        }

        private static int Main()
        {
            ulong[,] aul2 = new ulong[,] { { 1, 2, 3 }, { 4, 5, 6 } };
            TestRef(__makeref(aul2));
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (aul2[i, j] != (ulong)(i * 3 + j))
                        return 3;
                }
            }

            return 100;
        }
    }
}
