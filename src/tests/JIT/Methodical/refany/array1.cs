// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace JitTest
{
    internal class Test
    {
        private static void TestRef(TypedReference _ref)
        {
            if (__reftype(_ref) == typeof(Array))
            {
                for (int i = 0; i < __refvalue(_ref, Array).Length; i++)
                    __refvalue(_ref, Array).SetValue(new Test(), i);
            }
            if (__reftype(_ref) == typeof(long[]))
            {
                for (int i = 0; i < __refvalue(_ref, long[]).Length; i++)
                    __refvalue(_ref, long[])[i]++;
            }
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
            if (__reftype(_ref) == typeof(ulong[][]))
            {
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        __refvalue(_ref, ulong[][])[i][j]--;
                    }
                }
            }
        }

        private static int Main()
        {
            Array genericArray = Array.CreateInstance(typeof(Test), 16);
            TestRef(__makeref(genericArray));
            for (int i = 0; i < 16; i++)
            {
                if (genericArray.GetValue(i) == null ||
                    genericArray.GetValue(i).GetType() != typeof(Test))
                    return 1;
            }

            long[] al = new long[] { 1, 2, 3 };
            TestRef(__makeref(al));
            if (al[0] != 2 || al[1] != 3 || al[2] != 4)
                return 2;

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

            ulong[][] aul1 = new ulong[][] { new ulong[] { 1, 2, 3 }, new ulong[] { 4, 5, 6 } };
            TestRef(__makeref(aul1));
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (aul1[i][j] != (ulong)(i * 3 + j))
                        return 3;
                }
            }

            return 100;
        }
    }
}
