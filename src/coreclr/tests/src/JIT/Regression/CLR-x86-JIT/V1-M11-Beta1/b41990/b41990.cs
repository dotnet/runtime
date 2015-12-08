

using System;
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Test
{
    internal class AA
    {
        private static void Main1()
        {
            try
            {
                try
                {
                    throw new Exception();
                }
                catch (Exception)
                {
                    int[] L = new int[2];
                    L[2] = 0;
                }
            }
            catch (NullReferenceException) { }
        }
        private static int Main()
        {
            try
            {
                Main1();
                return -1;
            }
            catch (IndexOutOfRangeException)
            {
                return 100;
            }
        }
    }
}
