// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace SampleDynamicTests
{
    public class OperatorTest
    {
        private int _total = 0;

        public static string operator +(OperatorTest o, int i)
        {
            o._total += i;
            return string.Format("The total is {0}", o._total);
        }

        [Fact]
        public static void OperatorTest_RunTest()
        {
            dynamic d = new OperatorTest();
            dynamic temp = d + 5;
            dynamic result = d + 2;

            Assert.Equal("The total is 7", (string)result);
        }
    }
}
