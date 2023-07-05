// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Configuration
{
    public class SettingsManageabilityAttributeTests
    {
        [Fact]
        public void GetValueIsExpected()
        {
            var testSettingManageabilityAttribute = new SettingsManageabilityAttribute(SettingsManageability.Roaming);
            Assert.Equal(SettingsManageability.Roaming, testSettingManageabilityAttribute.Manageability);
        }
    }
}
