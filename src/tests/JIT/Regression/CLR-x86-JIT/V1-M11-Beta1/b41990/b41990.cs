// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;


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
