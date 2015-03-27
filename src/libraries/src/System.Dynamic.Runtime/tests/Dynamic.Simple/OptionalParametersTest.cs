// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace SampleDynamicTests
{
    public class OptionalParametersTest
    {
        public void Foo([Optional] decimal? d)
        {
            Assert.Equal((decimal?)null, d);
        }

        [Fact]
        public static void OptionalParametersTest_RunTest()
        {
            dynamic d = new OptionalParametersTest();
            d.Foo();
        }
    }
}
