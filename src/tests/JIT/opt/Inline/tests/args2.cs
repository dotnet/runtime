// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitInliningTest
{
    public class Args2
    {
        static internal void FillArray(out int[] myArray)
        {
            myArray = new int[5] { 1, 2, 3, 4, 5 };
        }

        [Fact]
        static public int TestEntryPoint()
        {
            int retval = 85;
            int[] myArray;

            FillArray(out myArray);

            for (int i = 0; i < myArray.Length; i++)
                retval += myArray[i];

            return retval;
        }
    }
}
