// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using Xunit;
using Microsoft.XmlSerializer.Generator;

namespace Microsoft.XmlSerializer.Generator.Tests
{
    public static class SgenTests
    {
        [Fact]
        public static void BasicTest()
        {
            int n = Sgen.Main(null);
            Assert.Equal(0, n);
        }
    }
}
