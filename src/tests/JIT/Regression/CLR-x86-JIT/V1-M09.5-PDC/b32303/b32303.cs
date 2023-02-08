// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class Temp
    {
        [Fact]
        public static int TestEntryPoint()
        {
            int x = 10;
            switch (x)
            {
                case 10:
                    Console.WriteLine("10");
                    break;
            }
            return 100;
        }
    }

}
