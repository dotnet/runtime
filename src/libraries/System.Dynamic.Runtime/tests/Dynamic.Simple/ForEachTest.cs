// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
