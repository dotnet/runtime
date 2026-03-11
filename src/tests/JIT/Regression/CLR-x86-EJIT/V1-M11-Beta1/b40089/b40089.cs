// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace b40089
{
    using System;

    public struct BB
    {
        public BB Method1(float param2)
        {
            return new BB();
        }

        [OuterLoop]
        [Fact]
        public static void TestEntryPoint()
        {
            new BB().Method1(0.0f);
        }
    }
}
