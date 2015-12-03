// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace JitInliningTest
{
    internal class Args3
    {
        public static void FillArray(ref int[] arr)
        {
            if (arr == null)
                arr = new int[10];
            arr[0] = 123;
            arr[4] = 1024;
        }

        static public int Main()
        {
            int retval = -1056;
            int[] myArray = { 1, 2, 3, 4, 5 };

            FillArray(ref myArray);

            for (int i = 0; i < myArray.Length; i++)
                retval += myArray[i];

            return retval;
        }
    }
}

