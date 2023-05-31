// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitInliningTest
{
    public class Args3
    {
        internal static void FillArray(ref int[] arr)
        {
            if (arr == null)
                arr = new int[10];
            arr[0] = 123;
            arr[4] = 1024;
        }

        [Fact]
        static public int TestEntryPoint()
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

