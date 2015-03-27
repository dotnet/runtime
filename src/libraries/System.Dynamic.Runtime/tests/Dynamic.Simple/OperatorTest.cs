// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
