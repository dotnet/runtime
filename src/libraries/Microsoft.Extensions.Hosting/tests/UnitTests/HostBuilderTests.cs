// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting.Fakes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Hosting
{
    public class HostBuilderTests
    {
        [Fact]
        public void DefaultConfigIsMutable()
        {
            var host = new HostBuilder()
                .Build();

            using (host)
            {
                var config = host.Services.GetRequiredService<IConfiguration>();
                config["key1"] = "value";
                Assert.Equal("value", config["key1"]);
            }
        }

        [Fact]
        public void ConfigureHostConfigurationPropagated()
        {
            var host = new HostBuilder()
                .ConfigureHostConfiguration(configBuilder =>
                {
                    configBuilder.AddInMemoryCollection(new[]
                    {
                        new KeyValuePair<string, string>("key1", "value1")
                    });
                })
                .ConfigureHostConfiguration(configBuilder =>
                {
                    configBuilder.AddInMemoryCollection(new[]
                    {
                        new KeyValuePair<string, string>("key2", "value2")
                    });
                })
                .ConfigureHostConfiguration(configBuilder =>
                {
                    configBuilder.AddInMemoryCollection(new[]
                    {
                        // Hides value2
                        new KeyValuePair<string, string>("key2", "value3")
                    });
                })
                .ConfigureAppConfiguration((context, configBuilder) =>
                {
                    Assert.Equal("value1", context.Configuration["key1"]);
                    Assert.Equal("value3", context.Configuration["key2"]);
                    var config = configBuilder.Build();
                    Assert.Equal("value1", config["key1"]);
                    Assert.Equal("value3", config["key2"]);
                })
                .Build();

            using (host)
            {
                var config = host.Services.GetRequiredService<IConfiguration>();
                Assert.Equal("value1", config["key1"]);
                Assert.Equal("value3", config["key2"]);
            }
        }

        [Fact]
        public void CanConfigureAppConfigurationAndRetrieveFromDI()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureAppConfiguration((configBuilder) =>
                {
                    configBuilder.AddInMemoryCollection(
                            new KeyValuePair<string, string>[]
                            {
                                new KeyValuePair<string, string>("key1", "value1")
                            });
                })
                .ConfigureAppConfiguration((configBuilder) =>
                {
                    configBuilder.AddInMemoryCollection(
                            new KeyValuePair<string, string>[]
                            {
                                new KeyValuePair<string, string>("key2", "value2")
                            });
                })
                .ConfigureAppConfiguration((configBuilder) =>
                {
                    configBuilder.AddInMemoryCollection(
                            new KeyValuePair<string, string>[]
                            {
                                // Hides value2
                                new KeyValuePair<string, string>("key2", "value3")
                            });
                });

            using (var host = hostBuilder.Build())
            {
                var config = host.Services.GetService<IConfiguration>();
                Assert.NotNull(config);
                Assert.Equal("value1", config["key1"]);
                Assert.Equal("value3", config["key2"]);
            }
        }

        [Fact]
        public void CanConfigureAppConfigurationFromFile()
        {
            var hostBuilder = new HostBuilder()
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureAppConfiguration((context, configBuilder) =>
                {
                    configBuilder.AddJsonFile("appSettings.json", optional: false);
                });

            using (var host = hostBuilder.Build())
            {
                var config = host.Services.GetService<IConfiguration>();
                Assert.NotNull(config);
                Assert.Equal("value", config["key"]);
            }
        }

        [Fact]
        public void DefaultIHostEnvironmentValues()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureAppConfiguration((hostContext, appConfig) =>
                {
                    var env = hostContext.HostingEnvironment;
                    Assert.Equal(Environments.Production, env.EnvironmentName);
#if NETCOREAPP
                    Assert.NotNull(env.ApplicationName);
#elif NET472
                    // Note GetEntryAssembly returns null for the net4x console test runner.
                    Assert.Null(env.ApplicationName);
#else
#error TFMs need to be updated
#endif
                    Assert.Equal(AppContext.BaseDirectory, env.ContentRootPath);
                    Assert.IsAssignableFrom<PhysicalFileProvider>(env.ContentRootFileProvider);
                });

            using (var host = hostBuilder.Build())
            {
                var env = host.Services.GetRequiredService<IHostEnvironment>();
                Assert.Equal(Environments.Production, env.EnvironmentName);
#if NETCOREAPP
                Assert.NotNull(env.ApplicationName);
#elif NET472
                // Note GetEntryAssembly returns null for the net4x console test runner.
                Assert.Null(env.ApplicationName);
#else
#error TFMs need to be updated
#endif
                Assert.Equal(AppContext.BaseDirectory, env.ContentRootPath);
                Assert.IsAssignableFrom<PhysicalFileProvider>(env.ContentRootFileProvider);
            }
        }

        [Fact]
        public void ConfigBasedSettingsConfigBasedOverride()
        {
            var settings = new Dictionary<string, string>
            {
                { HostDefaults.EnvironmentKey, "EnvA" }
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var overrideSettings = new Dictionary<string, string>
            {
                { HostDefaults.EnvironmentKey, "EnvB" }
            };

            var overrideConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(overrideSettings)
                .Build();

            var hostBuilder = new HostBuilder()
                .ConfigureHostConfiguration(configBuilder => configBuilder.AddConfiguration(config))
                .ConfigureHostConfiguration(configBuilder => configBuilder.AddConfiguration(overrideConfig));

            using (var host = hostBuilder.Build())
            {
                Assert.Equal("EnvB", host.Services.GetRequiredService<IHostEnvironment>().EnvironmentName);
            }
        }

        [Fact]
        public void UseEnvironmentIsNotOverriden()
        {
            var vals = new Dictionary<string, string>
            {
                { "ENV", "Dev" },
            };
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();

            var expected = "MY_TEST_ENVIRONMENT";


            using (var host = new HostBuilder()
                .ConfigureHostConfiguration(configBuilder => configBuilder.AddConfiguration(config))
                .UseEnvironment(expected)
                .Build())
            {
                Assert.Equal(expected, host.Services.GetService<IHostEnvironment>().EnvironmentName);
            }
        }

        [Fact]
        public void BuildAndDispose()
        {
            using (var host = new HostBuilder()
                .Build()) { }
        }

        [Fact]
        public void UseBasePathConfiguresBasePath()
        {
            var vals = new Dictionary<string, string>
            {
                { "ENV", "Dev" },
            };
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(vals);
            var config = builder.Build();

            using (var host = new HostBuilder()
                .ConfigureHostConfiguration(configBuilder => configBuilder.AddConfiguration(config))
                .UseContentRoot("/")
                .Build())
            {
                Assert.Equal("/", host.Services.GetService<IHostEnvironment>().ContentRootPath);
            }
        }

        [Fact]
        public void HostConfigParametersReadCorrectly()
        {
            var parameters = new Dictionary<string, string>()
            {
                { "applicationName", "MyProjectReference"},
                { "environment", Environments.Development},
                { "contentRoot", Path.GetFullPath(".") }
            };

            using (var host = new HostBuilder()
                .ConfigureHostConfiguration(config =>
                {
                    config.AddInMemoryCollection(parameters);
                }).Build())
            {
                var env = host.Services.GetRequiredService<IHostEnvironment>();

                Assert.Equal("MyProjectReference", env.ApplicationName);
                Assert.Equal(Environments.Development, env.EnvironmentName);
                Assert.Equal(Path.GetFullPath("."), env.ContentRootPath);
            }
        }

        [Fact]
        public void RelativeContentRootIsResolved()
        {
            using (var host = new HostBuilder()
                .UseContentRoot("testroot")
                .Build())
            {
                var basePath = host.Services.GetRequiredService<IHostEnvironment>().ContentRootPath;
                Assert.True(Path.IsPathRooted(basePath));
                Assert.EndsWith(Path.DirectorySeparatorChar + "testroot", basePath);
            }
        }

        [Fact]
        public void DefaultContentRootIsApplicationBasePath()
        {
            using (var host = new HostBuilder()
                .Build())
            {
                var appBase = AppContext.BaseDirectory;
                Assert.Equal(appBase, host.Services.GetService<IHostEnvironment>().ContentRootPath);
            }
        }

        [Fact]
        public void DefaultServicesAreAvailable()
        {
            using (var host = new HostBuilder()
                .Build())
            {
#pragma warning disable CS0618 // Type or member is obsolete
                Assert.NotNull(host.Services.GetRequiredService<IHostingEnvironment>());
#pragma warning restore CS0618 // Type or member is obsolete
                Assert.NotNull(host.Services.GetRequiredService<IHostEnvironment>());
                Assert.NotNull(host.Services.GetRequiredService<IConfiguration>());
                Assert.NotNull(host.Services.GetRequiredService<HostBuilderContext>());
#pragma warning disable CS0618 // Type or member is obsolete
                Assert.NotNull(host.Services.GetRequiredService<IApplicationLifetime>());
#pragma warning restore CS0618 // Type or member is obsolete
                Assert.NotNull(host.Services.GetRequiredService<IHostApplicationLifetime>());
                Assert.NotNull(host.Services.GetRequiredService<ILoggerFactory>());
                Assert.NotNull(host.Services.GetRequiredService<IOptions<FakeOptions>>());
            }
        }

        [Fact]
        public void DefaultCreatesLoggerFactory()
        {
            var hostBuilder = new HostBuilder();

            using (var host = hostBuilder.Build())
            {
                Assert.NotNull(host.Services.GetService<ILoggerFactory>());
            }
        }

        [Fact]
        public void MultipleConfigureLoggingInvokedInOrder()
        {
            var callCount = 0; //Verify ordering
            var hostBuilder = new HostBuilder()
                .ConfigureLogging((hostContext, loggerFactory) =>
                {
                    Assert.Equal(0, callCount++);
                })
                .ConfigureLogging((hostContext, loggerFactory) =>
                {
                    Assert.Equal(1, callCount++);
                });

            using (hostBuilder.Build())
            {
                Assert.Equal(2, callCount);
            }
        }

        [Fact]
        public void HostingContextContainsAppConfigurationDuringConfigureServices()
        {
            var hostBuilder = new HostBuilder()
                 .ConfigureAppConfiguration((configBuilder) =>
                    configBuilder.AddInMemoryCollection(
                        new KeyValuePair<string, string>[]
                        {
                            new KeyValuePair<string, string>("key1", "value1")
                        }))
                 .ConfigureServices((context, factory) =>
                 {
                     Assert.Equal("value1", context.Configuration["key1"]);
                 });

            using (hostBuilder.Build()) { }
        }

        [Fact]
        public void ConfigureDefaultServiceProvider()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureServices((s) =>
                {
                    s.AddTransient<ServiceD>();
                    s.AddScoped<ServiceC>();
                })
                .ConfigureHostConfiguration(config =>
                {
                    config.AddInMemoryCollection(new[]
                    {
                        new KeyValuePair<string, string>("Key", "Value"),
                    });
                })
                .UseDefaultServiceProvider((context, options) =>
                {
                    Assert.NotNull(context);
                    Assert.Equal("Value", context.Configuration["Key"]);
                    Assert.NotNull(options);
                    options.ValidateScopes = true;
                });
            using (var host = hostBuilder.Build())
            {
                Assert.Throws<InvalidOperationException>(() => { host.Services.GetRequiredService<ServiceC>(); });
            }
        }

        [Fact]
        public void ConfigureCustomServiceProvider()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureServices((hostContext, s) =>
                {
                    s.AddTransient<ServiceD>();
                    s.AddScoped<ServiceC>();
                })
                .UseServiceProviderFactory(new FakeServiceProviderFactory())
                .ConfigureContainer<FakeServiceCollection>((container) =>
                {
                    Assert.Null(container.State);
                    container.State = "1";
                })
                .ConfigureContainer<FakeServiceCollection>((container) =>
                 {
                     Assert.Equal("1", container.State);
                     container.State = "2";
                 });
            using (var host = hostBuilder.Build())
            {
                var fakeServices = host.Services.GetRequiredService<FakeServiceCollection>();
                Assert.Equal("2", fakeServices.State);
            }
        }

        [Fact]
        public void CustomContainerTypeMismatchThrows()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureServices((s) =>
                {
                    s.AddTransient<ServiceD>();
                    s.AddScoped<ServiceC>();
                })
                .UseServiceProviderFactory(new FakeServiceProviderFactory())
                .ConfigureContainer<IServiceCollection>((container) =>
                {
                });
            Assert.Throws<InvalidCastException>(() => hostBuilder.Build());
        }

        [Fact]
        public void HostingContextContainsAppConfigurationDuringConfigureLogging()
        {
            var hostBuilder = new HostBuilder()
                 .ConfigureAppConfiguration((configBuilder) =>
                    configBuilder.AddInMemoryCollection(
                        new KeyValuePair<string, string>[]
                        {
                            new KeyValuePair<string, string>("key1", "value1")
                        }))
                 .ConfigureLogging((context, factory) =>
                 {
                     Assert.Equal("value1", context.Configuration["key1"]);
                 });

            using (hostBuilder.Build()) { }
        }

        [Fact]
        public void ConfigureServices_CanBeCalledMultipleTimes()
        {
            var callCount = 0; // Verify ordering
            var hostBuilder = new HostBuilder()
                .ConfigureServices((services) =>
                {
                    Assert.Equal(0, callCount++);
                    services.AddTransient<ServiceA>();
                })
                .ConfigureServices((services) =>
                {
                    Assert.Equal(1, callCount++);
                    services.AddTransient<ServiceB>();
                });

            using (var host = hostBuilder.Build())
            {
                Assert.Equal(2, callCount);

                Assert.NotNull(host.Services.GetRequiredService<ServiceA>());
                Assert.NotNull(host.Services.GetRequiredService<ServiceB>());
            }
        }

        [Fact]
        public void Build_DoesNotAllowBuildingMuiltipleTimes()
        {
            var builder = new HostBuilder();
            using (builder.Build())
            {
                var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
                Assert.Equal("Build can only be called once.", ex.Message);
            }
        }

        [Fact]
        public void SetsFullPathToContentRoot()
        {
            using (var host = new HostBuilder()
                .ConfigureHostConfiguration(config =>
                {
                    config.AddInMemoryCollection(new[]
                    {
                        new KeyValuePair<string, string>(HostDefaults.ContentRootKey, Path.GetFullPath("."))
                    });
                })
                .Build())
            {
                var env = host.Services.GetRequiredService<IHostEnvironment>();

                Assert.Equal(Path.GetFullPath("."), env.ContentRootPath);
                Assert.IsAssignableFrom<PhysicalFileProvider>(env.ContentRootFileProvider);
            }
        }

        [Fact]
        public void BuilderPropertiesAreAvailableInBuilderAndContext()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    Assert.Equal("value", hostContext.Properties["key"]);
                });

            hostBuilder.Properties.Add("key", "value");

            Assert.Equal("value", hostBuilder.Properties["key"]);

            using (hostBuilder.Build()) { }
        }

        private class ServiceC
        {
            public ServiceC(ServiceD serviceD) { }
        }

        internal class ServiceD { }

        internal class ServiceA { }

        internal class ServiceB { }
    }
}
