// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace CrossBoundaryLayout
{
    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            int failure = ATest.Test();
            failure += BTest.Test();
            failure += CTest.Test();
            failure += C1Test.Test();

            return 100 + failure;
        }
    }
}
