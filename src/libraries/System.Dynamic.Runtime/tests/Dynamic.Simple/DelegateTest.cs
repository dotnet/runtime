// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace SampleDynamicTests
{
    public delegate int MultiplyDelegate(int a, int b);

    public class DelegateTest
    {
        public int Mul(int a, int b)
        {
            return a * b;
        }

        [Fact]
        public static void DelegateTest_RunTest()
        {
            DelegateTest del = new DelegateTest();
            dynamic d = (MultiplyDelegate)del.Mul;
            int result = d(4, 6);
            Assert.Equal(24, result);
        }
    }
}
