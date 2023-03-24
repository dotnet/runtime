// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using Xunit;


namespace Test
{
    public class AA
    {
        internal static void Main1()
        {
            try
            {
                bool local5 = true;
                if (local5)
                    throw new Exception();
                do
                {
                    while (local5)
                    {
                        return;
                    }
                } while (local5);
            }
            finally { }
        }
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                Main1();
            }
            catch (Exception)
            {
                return 100;
            }
            return 101;
        }
    }
}
