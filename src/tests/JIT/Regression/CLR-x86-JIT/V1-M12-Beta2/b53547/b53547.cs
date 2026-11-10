// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using Xunit;


namespace b53547
{
    public class AA
    {
        [OuterLoop]
        [Fact]
        public static unsafe void TestEntryPoint()
        {
            byte* p = stackalloc byte[new sbyte[] { 10 }[0]];
        }
    }
}
