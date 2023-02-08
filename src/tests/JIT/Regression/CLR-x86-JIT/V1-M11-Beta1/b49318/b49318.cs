// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class AA
    {
        static void Main1()
        {
            int N = 0;
#pragma warning disable 1718
            while (checked(N >= N))
            {
#pragma warning restore 1718
                throw new Exception();
            }
            try
            {
                return;
            }
            catch (Exception) { }
        }
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                Main1();
            }
            catch (Exception) { }
            return 100;
        }
    }

}
