// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using Xunit;


namespace b14323
{
    public class AppStarter
    {
        [OuterLoop]
        [Fact]
        public static void TestEntryPoint()
        {
            int[] foo = new int[1];
            long j = 0;
            foo[(int)j] = 1;
        }
    };
}

