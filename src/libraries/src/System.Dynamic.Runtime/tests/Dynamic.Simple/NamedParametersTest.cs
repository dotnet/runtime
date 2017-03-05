// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
