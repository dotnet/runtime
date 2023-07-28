// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using Xunit;


namespace Test
{
    public class AA
    {
        [Fact]
        public static unsafe int TestEntryPoint()
        {
            byte* p = stackalloc byte[new sbyte[] { 10 }[0]];
            return 100;
        }
    }
}
