// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using Xunit;

namespace Microsoft.Extensions.Hosting
{
    public class UseWindowsServiceTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        public void DefaultsToOffOutsideOfService()
        {
            var host = new HostBuilder()
                .UseWindowsService()
                .Build();

            using (host)
            {
                var lifetime = host.Services.GetRequiredService<IHostLifetime>();
                Assert.IsType<ConsoleLifetime>(lifetime);
            }
        }
    }
}
