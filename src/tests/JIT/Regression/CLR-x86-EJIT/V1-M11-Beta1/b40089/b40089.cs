// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public struct BB
    {
        public BB Method1(float param2)
        {
            return new BB();
        }

        [Fact]
        public static int TestEntryPoint()
        {
            new BB().Method1(0.0f);
            return 100;
        }
    }
}
