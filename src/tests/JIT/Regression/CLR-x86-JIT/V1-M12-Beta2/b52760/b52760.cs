// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace b52760
{
    using System;

    public class CC
    {
        static ulong AA_Static1()
        {
            ulong loc = 10;
            return loc *= loc;
        }
        [OuterLoop]
        [Fact]
        public static void TestEntryPoint()
        {
            AA_Static1();
        }
    }
}
