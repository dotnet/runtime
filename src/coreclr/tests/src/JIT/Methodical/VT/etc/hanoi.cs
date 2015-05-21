// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace JitTest
{
    internal struct CI
    {
        public int index;
    }

    internal class Test
    {
        private static int[][] s_cols;
        private static int[] s_heights;

        private static void test()
        {
            for (int c = 0; c < 3; c++)
            {
                for (int i = 1; i < s_heights[c]; i++)
                {
                    if (s_cols[c][i - 1] <= s_cols[c][i])
                        throw new Exception();
                }
            }
        }

        private static void move1(CI from, CI to)
        {
            s_cols[to.index][s_heights[to.index]++] = s_cols[from.index][--s_heights[from.index]];
        }

        private static int move(CI from, CI to, int num)
        {
            if (num == 1)
            {
                move1(from, to);
                return 1;
            }
            else
            {
                CI F, T;
                F.index = from.index;
                T.index = 3 - from.index - to.index;
                int c = move(F, T, num - 1);
                move1(from, to);
                F.index = 3 - from.index - to.index;
                T.index = to.index;
                return c + 1 + move(F, T, num - 1);
            }
        }

        private static int Main()
        {
            int NUM = 17;
            s_cols = new int[3][];
            s_cols[0] = new int[NUM];
            s_cols[1] = new int[NUM];
            s_cols[2] = new int[NUM];
            s_heights = new int[] { NUM, 0, 0 };
            for (int i = 0; i < NUM; i++)
                s_cols[0][i] = NUM - i;
            test();

            CI F, T;
            F.index = 0;
            T.index = 1;
            return 100;
        }
    }
}
