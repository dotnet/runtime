// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Diagnostics.CodeAnalysis.Tests
{
    public class FeatureSwitchDefinitionAttributeTests
    {
        [Fact]
        public void TestConstructor()
        {
            var attr = new FeatureSwitchDefinitionAttribute("SwitchName");
            Assert.Equal("SwitchName", attr.SwitchName);
        }
    }
}
