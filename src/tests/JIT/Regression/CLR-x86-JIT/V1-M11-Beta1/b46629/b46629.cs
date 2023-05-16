// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class AA
    {
        [Fact]
        public static int TestEntryPoint()
        {
            int L = 2;
            while (1u > L)
            {
                GC.Collect();
                break;
            }
            return 100;
        }
    }
}
