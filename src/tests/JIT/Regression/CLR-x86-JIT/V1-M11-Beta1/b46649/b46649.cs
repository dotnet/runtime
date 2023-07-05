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
            int L = -111;
            object O = null;
            while (L > 0)
            {
                bool[] bb;
                for (; (bool)O; bb = (bool[])O)
                {
                    while (285.34 >= L)
                    {
                        throw new Exception();
                    }
                }
            }
            return 100;
        }
    }
}
