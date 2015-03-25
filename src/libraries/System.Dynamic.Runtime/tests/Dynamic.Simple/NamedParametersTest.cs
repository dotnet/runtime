// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace SampleDynamicTests
{
    public class NamedParametersTest
    {
        public int Divide(int a, int b)
        {
            return a / b;
        }

        [Fact]
        public static void NamedParametersTest_RunTest()
        {
            dynamic d = new NamedParametersTest();
            int result = d.Divide(b: 3, a: 12);

            Assert.Equal(4, result);
        }
    }
}
