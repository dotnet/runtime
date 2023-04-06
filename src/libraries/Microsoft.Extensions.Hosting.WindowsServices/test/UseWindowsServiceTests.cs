// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.ServiceProcess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Hosting
{
    public class UseWindowsServiceTests
    {
        private static MethodInfo? _addWindowsServiceLifetimeMethod = null;

        [Fact]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPrivilegedProcess))]
        public void CanCreateService()
        {
            using var serviceTester = WindowsServiceTester.Create(() =>
            {
                using IHost host = new HostBuilder()
                    .UseWindowsService()
                    .Build();
                host.Run();
            });

            serviceTester.Start();
            serviceTester.WaitForStatus(ServiceControllerStatus.Running);
            serviceTester.Stop();
            serviceTester.WaitForStatus(ServiceControllerStatus.Stopped);

            var status = serviceTester.QueryServiceStatus();
            Assert.Equal(0, status.win32ExitCode);
        }
    }
}
