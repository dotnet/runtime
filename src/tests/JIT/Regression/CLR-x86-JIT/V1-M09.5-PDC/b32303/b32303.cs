// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace b32303
{
    using System;

    public class Temp
    {
        [OuterLoop]
        [Fact]
        public static void TestEntryPoint()
        {
            int x = 10;
            switch (x)
            {
                case 10:
                    Console.WriteLine("10");
                    break;
            }
        }
    }

}
