// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace b46170
{
    using System;

    public class AA
    {
        [OuterLoop]
        [Fact]
        public static void TestEntryPoint()
        {
            bool[] ab = new bool[2];
        }
    }
}
