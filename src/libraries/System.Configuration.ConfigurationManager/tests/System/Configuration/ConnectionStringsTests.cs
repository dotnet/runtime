// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;
using Xunit;

namespace System.ConfigurationTests
{
    public class ConnectionStringsTests
    {
        public static string SimpleConnectionStringConfiguration =
@"<?xml version='1.0' encoding='utf-8' ?>
<configuration>
    <connectionStrings>
        <add name='fooName' connectionString='fooConnectionString' providerName='fooProviderName' />
    </connectionStrings>
</configuration>";

        [Fact]
        public void SimpleConnectionString()
        {
            using (var temp = new TempConfig(SimpleConnectionStringConfiguration))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                Assert.NotNull(config.ConnectionStrings);
                Assert.NotNull(config.ConnectionStrings.ConnectionStrings);
                ConnectionStringSettings connection = config.ConnectionStrings.ConnectionStrings["fooName"];
                Assert.Equal("fooName", connection.Name);
                Assert.Equal("fooConnectionString", connection.ConnectionString);
                Assert.Equal("fooProviderName", connection.ProviderName);
            }
        }
    }
}
