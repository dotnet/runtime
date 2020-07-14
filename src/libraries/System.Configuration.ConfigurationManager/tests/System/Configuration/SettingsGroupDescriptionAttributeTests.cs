// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Configuration
{
    public class SettingsGroupDescriptionAttributeTests
    {
        [Fact]
        public void Constructor_DescriptionIsExpected()
        {
            var attribute = new SettingsGroupDescriptionAttribute("ThisIsATest");
            Assert.Equal("ThisIsATest", attribute.Description);
        }
    }
}
