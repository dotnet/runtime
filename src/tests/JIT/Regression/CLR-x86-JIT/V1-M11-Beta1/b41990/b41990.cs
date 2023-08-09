// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using Xunit;


namespace Test
{
    public class AA
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
        [Fact]
        public static int TestEntryPoint()
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
