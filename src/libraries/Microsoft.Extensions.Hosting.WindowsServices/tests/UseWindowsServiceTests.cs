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
        private static MethodInfo? _useWindowsServiceUncheckedMethod = null;

        public static bool SupportsServiceBase
        {
            get
            {
                if (!PlatformDetection.IsWindows)
                {
                    return false;
                }

                try
                {
                    new ServiceBase();
                }
                catch (PlatformNotSupportedException)
                {
                    // PlatformNotSupportedException : ServiceController enables manipulating and accessing Windows services and it is not applicable for other operating systems.
                    //   at System.ServiceProcess.ServiceBase..ctor() in C:\dev\dotnet\runtime\artifacts\obj\System.ServiceProcess.ServiceController\Debug\net7.0\System.ServiceProcess.ServiceController.notsupported.cs:line 24
                    //   at Microsoft.Extensions.Hosting.WindowsServices.WindowsServiceLifetime..ctor(IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory, IOptions`1 optionsAccessor, IOptions`1 windowsServiceOptionsAccessor) in C:\dev\dotnet\runtime\src\libraries\Microsoft.Extensions.Hosting.WindowsServices\src\WindowsServiceLifetime.cs:line 26

                    // REVIEW: This seems similar to https://github.com/dotnet/sdk/issues/16049 which was closed. I'm not sure why this is still happening in tests on Windows.
                    // net462 tests can construct ServiceBase just fine, but not net7.0. Does anyone know what the real issue is here?
                    return false;
                }

                return true;
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        public void DefaultsToOffOutsideOfService()
        {
            using IHost host = new HostBuilder()
                .UseWindowsService()
                .Build();

            var lifetime = host.Services.GetRequiredService<IHostLifetime>();
            Assert.IsType<ConsoleLifetime>(lifetime);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        public void ServiceCollectionExtensionMethodDefaultsToOffOutsideOfService()
        {
            var builder = new HostApplicationBuilder();

            builder.Services.UseWindowsService();
            // No reason to write event logs in this test. Event log may be unsupported anyway.
            builder.Logging.ClearProviders();

            using IHost host = builder.Build();

            var lifetime = host.Services.GetRequiredService<IHostLifetime>();
            Assert.IsType<ConsoleLifetime>(lifetime);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        public void ServiceCollectionExtensionMethodAddsWindowsServiceLifetimeInsideOfService()
        {
            var builder = new HostApplicationBuilder();

            // Emulate calling builder.Services.UseWindowsService() from inside a Windows service.
            UseWindowsServiceUnchecked(builder.Services);

            Assert.Single(builder.Services, serviceDescriptor =>
                serviceDescriptor.ServiceType == typeof(IHostLifetime) &&
                serviceDescriptor.ImplementationType == typeof(WindowsServiceLifetime));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        public void ServiceCollectionExtensionMethodSetsEventLogSourceNameToApplicationNameInsideOfService()
        {
            string appName = Guid.NewGuid().ToString();

            var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings
            {
                ApplicationName = appName,
            }); 

            // Emulate calling builder.Services.UseWindowsService() from inside a Windows service.
            UseWindowsServiceUnchecked(builder.Services);
            // No reason to write event logs in this test. Event log may be unsupported anyway.
            builder.Logging.ClearProviders();

            // Remove WindowsServiceLifetime descriptor so we can run this test even when SupportsServiceBase is false.
            var lifetimeDescriptor = Assert.Single(builder.Services, serviceDescriptor =>
                serviceDescriptor.ServiceType == typeof(IHostLifetime) &&
                serviceDescriptor.ImplementationType == typeof(WindowsServiceLifetime));
            builder.Services.Remove(lifetimeDescriptor);

            using IHost host = builder.Build();

            var eventLogSettings = host.Services.GetRequiredService<IOptions<EventLogSettings>>().Value;
            Assert.Same(appName, eventLogSettings.SourceName);
        }

        [ConditionalFact(typeof(UseWindowsServiceTests), nameof(SupportsServiceBase))]
        public void ServiceCollectionExtensionMethodCanBeCalledOnDefaultConfiguration()
        {
            var builder = new HostApplicationBuilder(); 

            // Emulate calling builder.Services.UseWindowsService() from inside a Windows service.
            UseWindowsServiceUnchecked(builder.Services);
            // No reason to write event logs in this test.
            builder.Logging.ClearProviders();

            using IHost host = builder.Build();

            var lifetime = host.Services.GetRequiredService<IHostLifetime>();
            Assert.IsType<WindowsServiceLifetime>(lifetime);
        }


        [ConditionalFact(typeof(UseWindowsServiceTests), nameof(SupportsServiceBase))]
        public void ServiceCollectionExtensionMethodThrowsGivenWrongContentRoot()
        {
            var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings
            {
                ContentRootPath = Path.GetTempPath(),
            }); 

            // Emulate calling builder.Services.UseWindowsService() from inside a Windows service.
            UseWindowsServiceUnchecked(builder.Services);
            // No reason to write event logs in this test.
            builder.Logging.ClearProviders();

            var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.Equal($"The IHostEnvironment.ConentRootPath value of '{Path.GetTempPath()}' must match the AppContext.BaseDirectory value of '{AppContext.BaseDirectory}' but is unequal.", ex.Message);
        }

        private void UseWindowsServiceUnchecked(IServiceCollection services, Action<WindowsServiceLifetimeOptions> configure = null)
        {
            _useWindowsServiceUncheckedMethod ??= typeof(WindowsServiceLifetimeHostBuilderExtensions).GetMethod("UseWindowsServiceUnchecked",
                BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(IServiceCollection), typeof(Action<WindowsServiceLifetimeOptions>) }, null)
                ?? throw new MissingMethodException();

            configure ??= _ => { };
            _useWindowsServiceUncheckedMethod.Invoke(null, new object[] { services, configure });
        }
    }
}
