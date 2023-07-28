// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class AA
    {
        static void Method1()
        {
            bool[] ab = new bool[7];
            if (ab[101])
            {
                int[] an = new int[2];
                while (an[-10] != 4)
                {
                    try { }
                    catch (Exception) { }
                }
            }
            else
            {
                try { }
                catch (Exception) { }
            }
        }
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                Method1();
            }
            catch (Exception) { }
            return 100;
        }
    }
}
