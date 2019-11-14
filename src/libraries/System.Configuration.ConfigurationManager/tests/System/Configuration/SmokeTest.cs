// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;
using Xunit;

namespace System.ConfigurationTests
{
    public class SmokeTest
    {
        [Fact]
        public void CreateExe()
        {
            using (var temp = new TempConfig(TestData.SimpleConfig))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                Assert.NotNull(config);
                Assert.Equal(2, config.AppSettings.Settings.Count);
            }
        }
    }
}
