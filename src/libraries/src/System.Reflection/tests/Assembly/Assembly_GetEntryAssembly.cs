// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;

namespace System.Reflection.Tests
{
    public class AssemblyGetEntryAssemblyTest
    {
        [Fact]
        public void Test_GetEntryAssembly()
        {
            Assert.NotNull(Assembly.GetEntryAssembly());
            Assert.True(Assembly.GetEntryAssembly().ToString().StartsWith("xunit.console.netcore", StringComparison.OrdinalIgnoreCase));
        }
    }
}

