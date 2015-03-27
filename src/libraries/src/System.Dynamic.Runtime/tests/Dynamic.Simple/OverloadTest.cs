// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace SampleDynamicTests
{
    public class OverloadTest
    {
        public int Method(int i)
        {
            return 1;
        }

        public int Method(double d)
        {
            return 2;
        }

        public int Method2(int i)
        {
            return 3;
        }

        internal int Method2(double d)
        {
            return 4;
        }

        [Fact]
        public static void OverloadTest_RunTest()
        {
            dynamic d = new OverloadTest();

            Assert.Equal(1, (int)d.Method(0)); //int overload
            Assert.Equal(2, (int)d.Method(0.0)); //double overload
            Assert.Equal(3, (int)d.Method2(0)); //int overload

            try
            {
                Assert.Equal(4, (int)d.Method2(0.0)); //double overload
            }
            catch (System.Exception e)
            {
                // There is a separate test case that fails when this exception is thrown.
                // Here we make sure that overload resolution doesn't throw.
                Assert.Equal("System.MethodAccessException", e.GetType().FullName);
            }
        }
    }
}
