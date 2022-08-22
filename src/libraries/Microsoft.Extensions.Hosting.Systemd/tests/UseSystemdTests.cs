// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Systemd;
using Xunit;

namespace Microsoft.Extensions.Hosting
{
    public class UseSystemdTests
    {
        [Fact]
        public void DefaultsToOffOutsideOfService()
        {
            using IHost host = new HostBuilder()
                .UseSystemd()
                .Build();

            var lifetime = host.Services.GetRequiredService<IHostLifetime>();
            Assert.NotNull(lifetime);
            Assert.IsNotType<SystemdLifetime>(lifetime);
        }

        [Fact]
        public void ServiceCollectionExtensionMethodDefaultsToOffOutsideOfService()
        {
            var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings
            {
                // Disable defaults that may not be supported on the testing platform like EventLogLoggerProvider.
                DisableDefaults = true,
            });

            builder.Services.AddSystemd();
            using IHost host = builder.Build();

            var lifetime = host.Services.GetRequiredService<IHostLifetime>();
            Assert.NotNull(lifetime);
            Assert.IsNotType<SystemdLifetime>(lifetime);
        }
    }
}
