// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
