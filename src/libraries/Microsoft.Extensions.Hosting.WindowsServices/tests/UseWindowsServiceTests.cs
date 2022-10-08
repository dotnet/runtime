// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
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
            using IHost host = new HostBuilder()
                .UseWindowsService()
                .Build();

            var lifetime = host.Services.GetRequiredService<IHostLifetime>();
            Assert.IsType<ConsoleLifetime>(lifetime);
        }

        [Fact]
        public void ServiceCollectionExtensionMethodDefaultsToOffOutsideOfService()
        {
            var builder = new HostApplicationBuilder();

            builder.Services.AddWindowsService();
            // No reason to write event logs in this test. Event log may be unsupported anyway.
            builder.Logging.ClearProviders();

            using IHost host = builder.Build();

            var lifetime = host.Services.GetRequiredService<IHostLifetime>();
            Assert.IsType<ConsoleLifetime>(lifetime);
        }

        [Fact]
        public void ServiceCollectionExtensionMethodAddsWindowsServiceLifetimeInsideOfService()
        {
            var builder = new HostApplicationBuilder();

            // Emulate calling builder.Services.AddWindowsService() from inside a Windows service.
            AddWindowsServiceLifetime(builder.Services);

            Assert.Single(builder.Services, serviceDescriptor =>
                serviceDescriptor.ServiceType == typeof(IHostLifetime) &&
                serviceDescriptor.ImplementationType == typeof(WindowsServiceLifetime));
        }

        [Fact]
        public void ServiceCollectionExtensionMethodSetsEventLogSourceNameToApplicationNameInsideOfService()
        {
            string appName = Guid.NewGuid().ToString();

            var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings
            {
                ApplicationName = appName,
            }); 

            // Emulate calling builder.Services.AddWindowsService() from inside a Windows service.
            AddWindowsServiceLifetime(builder.Services);
            // No reason to write event logs in this test.
            builder.Logging.ClearProviders();

            using IHost host = builder.Build();

            var eventLogSettings = host.Services.GetRequiredService<IOptions<EventLogSettings>>().Value;
            Assert.Same(appName, eventLogSettings.SourceName);
        }

        [Fact]
        public void ServiceCollectionExtensionMethodCanBeCalledOnDefaultConfiguration()
        {
            var builder = new HostApplicationBuilder(); 

            // Emulate calling builder.Services.AddWindowsService() from inside a Windows service.
            AddWindowsServiceLifetime(builder.Services);
            // No reason to write event logs in this test.
            builder.Logging.ClearProviders();

            using IHost host = builder.Build();

            var lifetime = host.Services.GetRequiredService<IHostLifetime>();
            Assert.IsType<WindowsServiceLifetime>(lifetime);
        }

        private void AddWindowsServiceLifetime(IServiceCollection services, Action<WindowsServiceLifetimeOptions> configure = null)
        {
            _addWindowsServiceLifetimeMethod ??= typeof(WindowsServiceLifetimeHostBuilderExtensions).GetMethod("AddWindowsServiceLifetime",
                BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(IServiceCollection), typeof(Action<WindowsServiceLifetimeOptions>) }, null)
                ?? throw new MissingMethodException();

            configure ??= _ => { };
            _addWindowsServiceLifetimeMethod.Invoke(null, new object[] { services, configure });
        }
    }
}
