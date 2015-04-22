// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
namespace JitInliningTest
{
    class Args2
    {
        static public void FillArray(out int[] myArray)
        {
            // Initialize the array:
            myArray = new int[5] { 1, 2, 3, 4, 5 };
        }

        static public int Main()
        {
            int retval = 85;
            int[] myArray; // Initialization is not required

            // Pass the array to the callee using out:
            FillArray(out myArray);

            for (int i = 0; i < myArray.Length; i++)
                retval += myArray[i];

            return retval;
        }
    }
}
