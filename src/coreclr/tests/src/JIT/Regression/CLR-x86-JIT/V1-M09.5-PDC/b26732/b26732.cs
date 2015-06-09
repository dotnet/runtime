// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Test
{
    using System;

    class WeirdObject
    {
        public int Member;
        public static int[] Static = new int[7];

        public static int[] CheckHeap(ref int param1, int[] param2, ref int[] param3, int[] param4)
        {
            GC.Collect();
            return null;
        }

        public static int Main()
        {
            int L = 2;
            int[] F = new int[2];
            CheckHeap(ref L, F, ref F,
            CheckHeap(ref L, F, ref F,
            CheckHeap(ref L, F, ref F,
            CheckHeap(ref L, F, ref F,
            CheckHeap(ref L, F, ref F,
            CheckHeap(ref new WeirdObject().Member, F, ref Static,
            CheckHeap(ref new WeirdObject().Member, F, ref F, null)
            ))))));
            return 100;
        }
    }
}
