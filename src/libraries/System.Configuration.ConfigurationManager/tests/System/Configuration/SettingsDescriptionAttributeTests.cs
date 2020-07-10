// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Configuration
{
    public class SettingsDescriptionAttributeTests
    {
        [Fact]
        public void GetValueIsExpected()
        {
            var testSettingsDescriptionAttribute = new SettingsDescriptionAttribute("ThisIsATest");
            Assert.Equal("ThisIsATest", testSettingsDescriptionAttribute.Description);
        }
    }
}
