// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.Extensions.Options.Tests
{
    public class OptionsSnapshotTest
    {
        [Fact]
        public void SnapshotUsesFactory()
        {
            var services = new ServiceCollection()
                .AddSingleton<IOptionsFactory<FakeOptions>, FakeOptionsFactory>()
                .Configure<FakeOptions>(o => o.Message = "Ignored")
                .BuildServiceProvider();

            var snap = services.GetRequiredService<IOptionsSnapshot<FakeOptions>>();
            Assert.Equal(FakeOptionsFactory.Options, snap.Value);
            Assert.Equal(FakeOptionsFactory.Options, snap.Get("1"));
            Assert.Equal(FakeOptionsFactory.Options, snap.Get("bsdfsdf"));
        }

        public int SetupInvokeCount { get; set; }

        private class CountIncrement : IConfigureOptions<FakeOptions>
        {
            private OptionsSnapshotTest _test;

            public CountIncrement(OptionsSnapshotTest test)
            {
                _test = test;
            }

            public void Configure(FakeOptions options)
            {
                _test.SetupInvokeCount++;
                options.Message += _test.SetupInvokeCount;
            }
        }


        public class FakeSource : IOptionsChangeTokenSource<FakeOptions>
        {
            public FakeSource(FakeChangeToken token)
            {
                Token = token;
            }

            public FakeChangeToken Token { get; set; }

            public string Name { get; }

            public IChangeToken GetChangeToken()
            {
                return Token;
            }

            public void Changed()
            {
                Token.HasChanged = true;
                Token.InvokeChangeCallback();
            }
        }

        public class ControllerWithSnapshot
        {
            FakeOptions _options;

            public ControllerWithSnapshot(IOptionsSnapshot<FakeOptions> snap)
            {
                _options = snap.Value;
            }

            public string Message => _options?.Message;
        }

        [Fact]
        public void SnapshotDoesNotChangeUntilNextRequestOnConfigChanges()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            services.Configure<FakeOptions>(config);

            var sp = services.BuildServiceProvider();

            // Snapshot only updated once per scope
            using (var scope = sp.CreateScope())
            {
                var snapshot = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>();
                Assert.Equal("1", snapshot.Value.Message);
                config.Reload();
                Assert.Equal("1", snapshot.Value.Message);
            }

            using (var scope = sp.CreateScope())
            {
                var snapshot = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>();
                Assert.Equal("2", snapshot.Value.Message);
                config.Reload();
                Assert.Equal("2", snapshot.Value.Message);
            }
        }

        private class TestConfigure : IConfigureNamedOptions<FakeOptions>
        {
            public static int ConfigureCount;
            public static int CtorCount;

            public TestConfigure()
            {
                CtorCount++;
            }

            public void Configure(string name, FakeOptions options)
            {
                ConfigureCount++;
            }

            public void Configure(FakeOptions options) => Configure(Options.DefaultName, options);
        }


        [Fact]
        public void SnapshotOptionsAreCachedPerScope()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var services = new ServiceCollection()
                .AddOptions()
                .AddSingleton<IConfigureOptions<FakeOptions>, TestConfigure>()
                .AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(new ConfigurationChangeTokenSource<FakeOptions>(Options.DefaultName, config))
                .AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(new ConfigurationChangeTokenSource<FakeOptions>("1", config))
                .BuildServiceProvider();

            var factory = services.GetRequiredService<IServiceScopeFactory>();
            FakeOptions options = null;
            FakeOptions namedOne = null;
            using (var scope = factory.CreateScope())
            {
                options = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Value;
                Assert.Equal(options, scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Value);
                Assert.Equal(1, TestConfigure.ConfigureCount);
                namedOne = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Get("1");
                Assert.Equal(namedOne, scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Get("1"));
                Assert.Equal(2, TestConfigure.ConfigureCount);

                // Reload triggers Configure for the two registered change tokens.
                config.Reload();
                Assert.Equal(4, TestConfigure.ConfigureCount);

                // Reload should not affect current scope.
                var options2 = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Value;
                Assert.Equal(options, options2);
                var namedOne2 = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Get("1");
                Assert.Equal(namedOne, namedOne2);
            }
            Assert.Equal(1, TestConfigure.CtorCount);

            // Reload should be reflected in a fresh scope.
            using (var scope = factory.CreateScope())
            {
                var options2 = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Value;
                Assert.NotEqual(options, options2);
                Assert.Equal(4, TestConfigure.ConfigureCount);
                var namedOne2 = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Get("1");
                Assert.NotEqual(namedOne, namedOne2);
                Assert.Equal(4, TestConfigure.ConfigureCount);
            }
            Assert.Equal(1, TestConfigure.CtorCount);
        }

        [Fact]
        public void CustomIConfigureOptionsShouldOnlyAffectDefaultInstance()
        {
            var services = new ServiceCollection().AddOptions();
            services.AddSingleton<IConfigureOptions<FakeOptions>, CustomSetup>();

            var sp = services.BuildServiceProvider();
            var option = sp.GetRequiredService<IOptionsSnapshot<FakeOptions>>();
            Assert.Equal("", option.Get("NotDefault").Message);
            Assert.Equal("Stomp", option.Get(Options.DefaultName).Message);
            Assert.Equal("Stomp", option.Value.Message);
            Assert.Equal("Stomp", sp.GetRequiredService<IOptions<FakeOptions>>().Value.Message);
        }

        private class CustomSetup : IConfigureOptions<FakeOptions>
        {
            public void Configure(FakeOptions options)
            {
                options.Message = "Stomp";
            }
        }

        [Fact]
        public void EnsureAddOptionsLifetimes()
        {
            var services = new ServiceCollection().AddOptions();
            CheckLifetime(services, typeof(IOptions<>), ServiceLifetime.Singleton);
            CheckLifetime(services, typeof(IOptionsMonitor<>), ServiceLifetime.Singleton);
            CheckLifetime(services, typeof(IOptionsSnapshot<>), ServiceLifetime.Scoped);
            CheckLifetime(services, typeof(IOptionsMonitorCache<>), ServiceLifetime.Singleton);
            CheckLifetime(services, typeof(IOptionsFactory<>), ServiceLifetime.Transient);
        }

        private void CheckLifetime(IServiceCollection services, Type serviceType, ServiceLifetime lifetime)
        {
            Assert.NotNull(services.Where(s => s.ServiceType == serviceType && s.Lifetime == lifetime).SingleOrDefault());
        }
    }
}
