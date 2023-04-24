// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using Xunit;


namespace DefaultNamespace
{
    public class MainClass
    {
        [Fact]
        public static int TestEntryPoint()
        {
            Decimal c1 = new Decimal();

            Console.WriteLine(c1);
            return 100;
        }
    }
}
