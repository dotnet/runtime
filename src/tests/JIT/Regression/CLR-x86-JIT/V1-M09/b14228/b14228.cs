// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using Xunit;


namespace b14228
{
    public class MainClass
    {
        [OuterLoop]
        [Fact]
        public static void TestEntryPoint()
        {
            Decimal c1 = new Decimal();

            Console.WriteLine(c1);
        }
    }
}
