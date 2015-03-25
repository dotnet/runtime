// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace SampleDynamicTests
{
    public class ForEachTest
    {
        [Fact]
        public static void ForEachTest_RunTest()
        {
            var numbers = new double[] { 1.0, 2.0, 3.0 };
            double sum = 0;
            foreach (int i in (dynamic)numbers)
            {
                sum += i;
            }

            Assert.Equal(6, sum);
        }
    }
}
