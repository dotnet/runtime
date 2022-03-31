// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting.Fakes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public class HostApplicationBuilderTests
    {
        [Fact]
        public void DefaultConfigIsMutable()
        {
            HostApplicationBuilder builder = CreateEmptyBuilder();

            builder.Configuration["key1"] = "value1";

            using IHost host = builder.Build();

            var config = host.Services.GetRequiredService<IConfiguration>();
            config["key2"] = "value2";

            Assert.Equal("value1", config["key1"]);
            Assert.Equal("value2", config["key2"]);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void BuildFiresEvents()
        {
            using var _ = RemoteExecutor.Invoke(() =>
            {
                var serviceA = new ServiceA();
                IHostBuilder hostBuilderFromEvent = null;
                IHost hostFromEvent = null;

                var listener = new HostingListener((pair) =>
                {
                    if (pair.Key == "HostBuilding")
                    {
                        hostBuilderFromEvent = (IHostBuilder)pair.Value;

                        hostBuilderFromEvent.ConfigureHostConfiguration(configBuilder =>
                        {
                            configBuilder.AddInMemoryCollection(new KeyValuePair<string, string>[]
                            {
                                new("foo", "bar" ),
                            });
                        });

                        hostBuilderFromEvent.ConfigureServices(services =>
                        {
                            services.AddSingleton(serviceA);
                        });
                    }

                    if (pair.Key == "HostBuilt")
                    {
                        hostFromEvent = (IHost)pair.Value;
                    }
                });

                using var _ = DiagnosticListener.AllListeners.Subscribe(listener);

                HostApplicationBuilder builder = CreateEmptyBuilder();
                IHost host = builder.Build();

                Assert.NotNull(hostBuilderFromEvent);
                Assert.Same(host, hostFromEvent);
                Assert.Same(serviceA, host.Services.GetRequiredService<ServiceA>());
                Assert.Equal("bar", host.Services.GetRequiredService<IConfiguration>()["foo"]);
            });
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ChangingEnvironmentWithDiagnosticListenerIsNotSupported()
        {
            using var _ = RemoteExecutor.Invoke(() =>
            {
                var listener = new HostingListener((pair) =>
                {
                    if (pair.Key != "HostBuilding")
                    {
                        return;
                    }

                    var hostBuilder = (IHostBuilder)pair.Value;

                    hostBuilder.ConfigureHostConfiguration(configBuilder =>
                    {
                        configBuilder.AddInMemoryCollection(new KeyValuePair<string, string>[]
                        {
                            new(HostDefaults.ApplicationKey, "Changed Name" ),
                        });
                    });
                });

                using var _ = DiagnosticListener.AllListeners.Subscribe(listener);

                HostApplicationBuilder builder = CreateEmptyBuilder();
                Assert.Throws<NotSupportedException>(() => builder.Build());
            });
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CanConfigureContainerWithDiagnosticListener()
        {
            using var _ = RemoteExecutor.Invoke(() =>
            {
                var listener = new HostingListener((pair) =>
                {
                    if (pair.Key != "HostBuilding")
                    {
                        return;
                    }

                    var hostBuilder = (IHostBuilder)pair.Value;

                    hostBuilder.UseServiceProviderFactory(new FakeServiceProviderFactory());

                    hostBuilder.ConfigureContainer<FakeServiceCollection>(fakeServices =>
                    {
                        fakeServices.State = "Hi!";
                    });
                });

                using var _ = DiagnosticListener.AllListeners.Subscribe(listener);

                HostApplicationBuilder builder = CreateEmptyBuilder();

                using IHost host = builder.Build();
                var fakeServices = host.Services.GetRequiredService<FakeServiceCollection>();
                Assert.Equal("Hi!", fakeServices.State);
            });
        }

        [Fact]
        public void CanConfigureAppConfigurationAndRetrieveFromDI()
        {
            HostApplicationBuilder builder = CreateEmptyBuilder();

            builder.Configuration.AddInMemoryCollection(
                    new KeyValuePair<string, string>[]
                    {
                        new KeyValuePair<string, string>("key1", "value1")
                    });

            builder.Configuration.AddInMemoryCollection(
                    new KeyValuePair<string, string>[]
                    {
                        new KeyValuePair<string, string>("key2", "value2")
                    });


            using IHost host = builder.Build();

            var config = host.Services.GetService<IConfiguration>();

            Assert.NotNull(config);
            Assert.Equal("value1", config["key1"]);
            Assert.Equal("value2", config["key2"]);

            builder.Configuration.AddInMemoryCollection(
                    new KeyValuePair<string, string>[]
                    {
                        new KeyValuePair<string, string>("key2", "value3")
                    });

            Assert.Equal("value1", config["key1"]);
            Assert.Equal("value3", config["key2"]);
        }

        [Fact]
        public void CanConfigureAppConfigurationFromFile()
        {
            HostApplicationBuilder builder = CreateEmptyBuilder();

            builder.Configuration.AddJsonFile("appSettings.json", optional: false);

            Assert.Equal("value", builder.Configuration["key"]);

            using IHost host = builder.Build();

            var config = host.Services.GetService<IConfiguration>();
            Assert.NotNull(config);
            Assert.Equal("value", config["key"]);
        }

        [Fact]
        public void DisableDefaultIHostEnvironmentValues()
        {
            HostApplicationBuilder builder = CreateEmptyBuilder();

            Assert.Equal(Environments.Production, builder.Environment.EnvironmentName);
#if NETCOREAPP
            Assert.NotNull(builder.Environment.ApplicationName);
#elif NETFRAMEWORK
            // Note GetEntryAssembly returns null for the net4x console test runner.
            Assert.Null(builder.Environment.ApplicationName);
#else
#error TFMs need to be updated
#endif
            Assert.Equal(AppContext.BaseDirectory, builder.Environment.ContentRootPath);
            Assert.IsAssignableFrom<PhysicalFileProvider>(builder.Environment.ContentRootFileProvider);

            using IHost host = builder.Build();

            var env = host.Services.GetRequiredService<IHostEnvironment>();
            Assert.Equal(Environments.Production, env.EnvironmentName);
#if NETCOREAPP
            Assert.NotNull(env.ApplicationName);
#elif NETFRAMEWORK
            // Note GetEntryAssembly returns null for the net4x console test runner.
            Assert.Null(env.ApplicationName);
#else
#error TFMs need to be updated
#endif
            Assert.Equal(AppContext.BaseDirectory, env.ContentRootPath);
            Assert.IsAssignableFrom<PhysicalFileProvider>(env.ContentRootFileProvider);
        }

        [Fact]
        public void ConfigurationSettingCanInfluenceEnvironment()
        {
            using var config = new ConfigurationManager();

            config.AddInMemoryCollection(new KeyValuePair<string, string>[]
            {
                new(HostDefaults.ApplicationKey, "AppA" ),
                new(HostDefaults.EnvironmentKey, "EnvA" ),
            });

            var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings
            {
                DisableDefaults = true,
                Configuration = config,
            });

            Assert.Equal("AppA", builder.Configuration[HostDefaults.ApplicationKey]);
            Assert.Equal("EnvA", builder.Configuration[HostDefaults.EnvironmentKey]);

            Assert.Equal("AppA", builder.Environment.ApplicationName);
            Assert.Equal("EnvA", builder.Environment.EnvironmentName);

            using IHost host = builder.Build();

            var hostEnvironmentFromServices = host.Services.GetRequiredService<IHostEnvironment>();
            Assert.Equal("AppA", hostEnvironmentFromServices.ApplicationName);
            Assert.Equal("EnvA", hostEnvironmentFromServices.EnvironmentName);
        }

        [Fact]
        public void DirectSetttingsOverrideConfigurationSetting()
        {
            using var config = new ConfigurationManager();

            config.AddInMemoryCollection(new KeyValuePair<string, string>[]
            {
                new(HostDefaults.ApplicationKey, "AppA" ),
                new(HostDefaults.EnvironmentKey, "EnvA" ),
            });

            var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings
            {
                DisableDefaults = true,
                Configuration = config,
                ApplicationName = "AppB",
                EnvironmentName = "EnvB",
            });

            Assert.Equal("AppB", builder.Configuration[HostDefaults.ApplicationKey]);
            Assert.Equal("EnvB", builder.Configuration[HostDefaults.EnvironmentKey]);

            Assert.Equal("AppB", builder.Environment.ApplicationName);
            Assert.Equal("EnvB", builder.Environment.EnvironmentName);

            using IHost host = builder.Build();

            var hostEnvironmentFromServices = host.Services.GetRequiredService<IHostEnvironment>();
            Assert.Equal("AppB", hostEnvironmentFromServices.ApplicationName);
            Assert.Equal("EnvB", hostEnvironmentFromServices.EnvironmentName);
        }

        [Fact]
        public void ChangingConfigurationPostBuilderConsturctionDoesNotChangeEnvironment()
        {
            using var config = new ConfigurationManager();

            config.AddInMemoryCollection(new KeyValuePair<string, string>[]
            {
                new(HostDefaults.ApplicationKey, "AppA" ),
                new(HostDefaults.EnvironmentKey, "EnvA" ),
            });

            var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings
            {
                DisableDefaults = true,
                Configuration = config,
            });

            config.AddInMemoryCollection(new KeyValuePair<string, string>[]
            {
                new(HostDefaults.ApplicationKey, "AppB" ),
                new(HostDefaults.EnvironmentKey, "EnvB" ),
            });

            Assert.Equal("AppB", builder.Configuration[HostDefaults.ApplicationKey]);
            Assert.Equal("EnvB", builder.Configuration[HostDefaults.EnvironmentKey]);

            Assert.Equal("AppA", builder.Environment.ApplicationName);
            Assert.Equal("EnvA", builder.Environment.EnvironmentName);

            using IHost host = builder.Build();

            var hostEnvironmentFromServices = host.Services.GetRequiredService<IHostEnvironment>();
            Assert.Equal("AppA", hostEnvironmentFromServices.ApplicationName);
            Assert.Equal("EnvA", hostEnvironmentFromServices.EnvironmentName);
        }

        [Fact]
        public void BuildAndDispose()
        {
            using IHost host = CreateEmptyBuilder().Build();
        }

        [Fact]
        public void ContentRootConfiguresBasePath()
        {
            var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings
            {
                DisableDefaults = true,
                ContentRootPath = "/",
            });

            using IHost host = builder.Build();
            Assert.Equal("/", host.Services.GetService<IHostEnvironment>().ContentRootPath);
        }

        [Fact]
        public void HostConfigParametersReadCorrectly()
        {
            var parameters = new Dictionary<string, string>()
            {
                { "applicationName", "MyProjectReference" },
                { "environment", Environments.Development },
                { "contentRoot", Path.GetFullPath(".") }
            };

            var config = new ConfigurationManager();
            config.AddInMemoryCollection(parameters);

            var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings
            {
                DisableDefaults = true,
                Configuration = config
            });

            Assert.Equal("MyProjectReference", builder.Environment.ApplicationName);
            Assert.Equal(Environments.Development, builder.Environment.EnvironmentName);
            Assert.Equal(Path.GetFullPath("."), builder.Environment.ContentRootPath);

            using IHost host = builder.Build(); 
            var env = host.Services.GetRequiredService<IHostEnvironment>();

            Assert.Equal("MyProjectReference", env.ApplicationName);
            Assert.Equal(Environments.Development, env.EnvironmentName);
            Assert.Equal(Path.GetFullPath("."), env.ContentRootPath);
        }

        [Fact]
        public void RelativeContentRootIsResolved()
        {
            var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings
            {
                DisableDefaults = true,
                ContentRootPath = "testroot",
            });

            Assert.True(Path.IsPathRooted(builder.Environment.ContentRootPath));
            Assert.EndsWith(Path.DirectorySeparatorChar + "testroot", builder.Environment.ContentRootPath);
        }

        [Fact]
        public void DisableDefaultContentRootIsApplicationBasePath()
        {
            HostApplicationBuilder builder = CreateEmptyBuilder();
            Assert.Equal(AppContext.BaseDirectory, builder.Environment.ContentRootPath);
        }

        [Fact]
        public void DefaultContentRootIsCurrentDirectory()
        {
            var builder = new HostApplicationBuilder();
            Assert.Equal(Directory.GetCurrentDirectory(), builder.Environment.ContentRootPath);
        }

        [Fact]
        public void DisableDefaultServicesAreAvailable()
        {
            using IHost host = CreateEmptyBuilder().Build();

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

        public static IEnumerable<object[]> ConfigureHostOptionsTestInput = new[]
        {
            new object[] { BackgroundServiceExceptionBehavior.Ignore, TimeSpan.FromDays(3) },
            new object[] { BackgroundServiceExceptionBehavior.StopHost, TimeSpan.FromTicks(long.MaxValue) },
        };

        [Theory]
        [MemberData(nameof(ConfigureHostOptionsTestInput))]
        public void CanConfigureHostOptionsWithDefaults(BackgroundServiceExceptionBehavior testBehavior, TimeSpan testShutdown)
        {
            var builder = new HostApplicationBuilder();

            builder.Services.Configure<HostOptions>(options =>
            {
                options.BackgroundServiceExceptionBehavior = testBehavior;
                options.ShutdownTimeout = testShutdown;
            });

            using IHost host = builder.Build();
            var options = host.Services.GetRequiredService<IOptions<HostOptions>>();

            Assert.NotNull(options.Value);

            HostOptions hostOptions = options.Value;
            Assert.Equal(testBehavior, hostOptions.BackgroundServiceExceptionBehavior);
            Assert.Equal(testShutdown, hostOptions.ShutdownTimeout);
        }

        [Fact]
        public void ConfigureDefaultServiceProvider()
        {
            HostApplicationBuilder builder = CreateEmptyBuilder();

            builder.Services.AddTransient<ServiceD>();
            builder.Services.AddScoped<ServiceC>();

            var factory = new DefaultServiceProviderFactory(new ServiceProviderOptions
            {
                ValidateScopes = true
            });

            builder.ConfigureContainer(factory);

            IHost host = builder.Build();

            Assert.Throws<InvalidOperationException>(() => { host.Services.GetRequiredService<ServiceC>(); });
        }

        [Fact]
        public void ConfigureCustomServiceProvider()
        {
            HostApplicationBuilder builder = CreateEmptyBuilder();

            builder.Services.AddTransient<ServiceD>();
            builder.Services.AddScoped<ServiceC>();

            builder.ConfigureContainer(new FakeServiceProviderFactory(), container => container.State = "Hi!");

            using IHost host = builder.Build();

            var fakeServices = host.Services.GetRequiredService<FakeServiceCollection>();
            Assert.Equal("Hi!", fakeServices.State);
        }


        [Fact]
        public void Build_DoesNotAllowBuildingMuiltipleTimes()
        {
            HostApplicationBuilder builder = CreateEmptyBuilder();
            using (builder.Build())
            {
                var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
                Assert.Equal("Build can only be called once.", ex.Message);
            }
        }

        [Fact]
        public void SetsFullPathToContentRoot()
        {
            var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings
            {
                DisableDefaults = true,
                ContentRootPath = Path.GetFullPath(".")
            });

            using IHost host = builder.Build();
            var env = host.Services.GetRequiredService<IHostEnvironment>();

            Assert.Equal(Path.GetFullPath("."), env.ContentRootPath);
            Assert.IsAssignableFrom<PhysicalFileProvider>(env.ContentRootFileProvider);
        }

        [Fact]
        public void HostServicesSameServiceProviderAsInHostBuilder()
        {
            HostApplicationBuilder builder = CreateEmptyBuilder();
            using IHost host = builder.Build();

            Type type = builder.GetType();
            FieldInfo field = type.GetField("_appServices", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var appServicesFromHostBuilder = (IServiceProvider)field.GetValue(builder)!;
            Assert.Same(appServicesFromHostBuilder, host.Services);
        }


        [Fact]
        public void HostApplicationBuilderThrowsExceptionIfServicesAlreadyBuilt()
        {
            HostApplicationBuilder builder = CreateEmptyBuilder();
            using IHost host = builder.Build();

            Assert.Throws<InvalidOperationException>(() => builder.Services.AddSingleton(new ServiceA()));
            Assert.Throws<InvalidOperationException>(() => builder.Services.Remove(ServiceDescriptor.Singleton(new ServiceA())));
            Assert.Throws<InvalidOperationException>(() => builder.Services[0] = ServiceDescriptor.Singleton(new ServiceA()));
            Assert.Throws<InvalidOperationException>(() => builder.Services.Clear());
            Assert.Throws<InvalidOperationException>(() => builder.Services.RemoveAt(0));
        }

        private static HostApplicationBuilder CreateEmptyBuilder()
        {
            return new HostApplicationBuilder(new HostApplicationBuilderSettings
            {
                DisableDefaults = true,
            });
        }

        private class HostingListener : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>
        {
            private IDisposable? _disposable;
            private readonly Action<KeyValuePair<string, object?>> _callback;

            public HostingListener(Action<KeyValuePair<string, object?>> callback)
            {
                _callback = callback;
            }

            public void OnCompleted() { _disposable?.Dispose(); }
            public void OnError(Exception error) { }
            public void OnNext(DiagnosticListener value)
            {
                if (value.Name == "Microsoft.Extensions.Hosting")
                {
                    _disposable = value.Subscribe(this);
                }
            }

            public void OnNext(KeyValuePair<string, object?> value)
            {
                _callback(value);
            }
        }

        private class ServiceC
        {
            public ServiceC(ServiceD serviceD) { }
        }

        private class ServiceD { }

        private class ServiceA { }
    }
}
