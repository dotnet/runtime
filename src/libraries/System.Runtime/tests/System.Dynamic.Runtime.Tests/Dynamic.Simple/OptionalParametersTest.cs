// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
