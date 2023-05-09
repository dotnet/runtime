// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class BB
    {
        static void Method1() { }
        [Fact]
        public static int TestEntryPoint()
        {
            bool local1 = false;
            for (; local1; Method1()) { }
            return 100;
        }
    }
}
