// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;
using System.IO;
using Xunit;

namespace System.ConfigurationTests
{
    public class SmokeTest
    {
        [Fact]
        public void CreateExe()
        {
            // Normally the "built-in" configuration section types come from the machine wide config.
            // Core doesn't have one that it installs, adding appSettings explicitly for now.
            const string SimpleConfig =
@"<?xml version='1.0' encoding='utf-8' ?>
<configuration>
  <appSettings>
    <add key='Setting1' value='May 5, 2014'/>
    <add key='Setting2' value='May 6, 2014'/>
  </appSettings>
</configuration>";

            using (var temp = new TempConfig(SimpleConfig))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                Assert.NotNull(config);
                Assert.Equal(2, config.AppSettings.Settings.Count);
            }
        }
    }
}
