// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

