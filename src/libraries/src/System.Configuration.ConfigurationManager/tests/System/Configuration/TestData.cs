// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ConfigurationTests
{
    public static class TestData
    {
        public static string ImplicitMachineConfig =
@"<configuration>
    <configSections>
        <section name='appSettings' type='System.Configuration.AppSettingsSection, System.Configuration.ConfigurationManager' restartOnExternalChanges='false' requirePermission='false'/>
    </configSections>
</configuration>";

        public static string EmptyConfig =
@"<?xml version='1.0' encoding='utf-8' ?>
<configuration>
</configuration>";

        public static string SimpleConfig =
@"<?xml version='1.0' encoding='utf-8' ?>
<configuration>
  <appSettings>
    <add key='FooKey' value='FooValue' />
    <add key='BarKey' value='BarValue' />
  </appSettings>
</configuration>";
    }
}
