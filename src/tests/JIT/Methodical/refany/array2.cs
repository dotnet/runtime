// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitTest_array2_refany_cs
{
    public class Test
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

        [Fact]
        public static int TestEntryPoint()
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
