// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.Extensions.Options.Tests
{
    public class OptionsMonitorTest
    {
        [Fact]
        public void MonitorUsesFactory()
        {
            var services = new ServiceCollection()
                .AddSingleton<IOptionsFactory<FakeOptions>, FakeOptionsFactory>()
                .Configure<FakeOptions>(o => o.Message = "Ignored")
                .BuildServiceProvider();

            var monitor = services.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            Assert.Equal(FakeOptionsFactory.Options, monitor.CurrentValue);
            Assert.Equal(FakeOptionsFactory.Options, monitor.Get("1"));
            Assert.Equal(FakeOptionsFactory.Options, monitor.Get("bsdfsdf"));
        }

        public int SetupInvokeCount { get; set; }

        private class CountIncrement : IConfigureNamedOptions<FakeOptions>
        {
            private OptionsMonitorTest _test;

            public CountIncrement(OptionsMonitorTest test)
            {
                _test = test;
            }

            public void Configure(FakeOptions options) => Configure(Options.DefaultName, options);

            public void Configure(string name, FakeOptions options)
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

            public string Name { get; set; }

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

        [Fact]
        public void CanClearNamedOptions()
        {
            var services = new ServiceCollection().AddOptions().AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));

            var sp = services.BuildServiceProvider();

            var monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            var cache = sp.GetRequiredService<IOptionsMonitorCache<FakeOptions>>();
            Assert.Equal("1", monitor.Get("#1").Message);
            Assert.Equal("2", monitor.Get("#2").Message);
            Assert.Equal("1", monitor.Get("#1").Message);
            Assert.Equal("2", monitor.Get("#2").Message);
            cache.Clear();
            Assert.Equal("3", monitor.Get("#1").Message);
            Assert.Equal("4", monitor.Get("#2").Message);
            Assert.Equal("3", monitor.Get("#1").Message);
            Assert.Equal("4", monitor.Get("#2").Message);

            cache.Clear();
            Assert.Equal("5", monitor.Get("#1").Message);
            Assert.Equal("6", monitor.Get("#2").Message);
            Assert.Equal("5", monitor.Get("#1").Message);
            Assert.Equal("6", monitor.Get("#2").Message);
        }

        [Fact]
        public void CanWatchNamedOptions()
        {
            var services = new ServiceCollection().AddOptions().AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            var changeToken = new FakeChangeToken();
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(new FakeSource(changeToken) { Name = "#1" });
            var changeToken2 = new FakeChangeToken();
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(new FakeSource(changeToken2) { Name = "#2" });

            var sp = services.BuildServiceProvider();

            var monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            Assert.NotNull(monitor);
            Assert.Equal("1", monitor.Get("#1").Message);

            string updatedMessage = null;
            monitor.OnChange((o, n) => updatedMessage = o.Message + n);

            changeToken.InvokeChangeCallback();
            Assert.Equal("2#1", updatedMessage);
            Assert.Equal("2", monitor.Get("#1").Message);

            changeToken2.InvokeChangeCallback();
            Assert.Equal("3#2", updatedMessage);
            Assert.Equal("3", monitor.Get("#2").Message);
        }

        [Fact]
        public void CanWatchOptions()
        {
            var services = new ServiceCollection().AddOptions();
            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            var changeToken = new FakeChangeToken();
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(new FakeSource(changeToken));

            var sp = services.BuildServiceProvider();

            var monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            Assert.NotNull(monitor);
            Assert.Equal("1", monitor.CurrentValue.Message);

            string updatedMessage = null;
            monitor.OnChange(o => updatedMessage = o.Message);
            changeToken.InvokeChangeCallback();
            Assert.Equal("2", updatedMessage);

            // Verify old watch is changed too
            Assert.Equal("2", monitor.CurrentValue.Message);
        }

        [Fact]
        public void CanWatchOptionsWithMultipleSourcesAndCallbacks()
        {
            var services = new ServiceCollection().AddOptions();
            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            var changeToken = new FakeChangeToken();
            var tracker = new FakeSource(changeToken);
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(tracker);
            var changeToken2 = new FakeChangeToken();
            var tracker2 = new FakeSource(changeToken2);
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(tracker2);

            var sp = services.BuildServiceProvider();

            var monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            Assert.NotNull(monitor);
            Assert.Equal("1", monitor.CurrentValue.Message);

            string updatedMessage = null;
            string updatedMessage2 = null;
            var cleanup = monitor.OnChange(o => updatedMessage = o.Message);
            var cleanup2 = monitor.OnChange(o => updatedMessage2 = o.Message);
            changeToken.InvokeChangeCallback();
            Assert.Equal("2", updatedMessage);
            Assert.Equal("2", updatedMessage2);

            // Verify old watch is changed too
            Assert.Equal("2", monitor.CurrentValue.Message);

            changeToken2.InvokeChangeCallback();
            Assert.Equal("3", updatedMessage);
            Assert.Equal("3", updatedMessage2);

            // Verify old watch is changed too
            Assert.Equal("3", monitor.CurrentValue.Message);

            cleanup.Dispose();
            changeToken.InvokeChangeCallback();
            changeToken2.InvokeChangeCallback();

            // Verify only the second message changed
            Assert.Equal("3", updatedMessage);
            Assert.Equal("5", updatedMessage2);

            cleanup2.Dispose();
            changeToken.InvokeChangeCallback();
            changeToken2.InvokeChangeCallback();

            // Verify no message changed
            Assert.Equal("3", updatedMessage);
            Assert.Equal("5", updatedMessage2);
        }

        [Fact]
        public void CanWatchOptionsWithMultipleSources()
        {
            var services = new ServiceCollection().AddOptions();
            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            var changeToken = new FakeChangeToken();
            var tracker = new FakeSource(changeToken);
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(tracker);
            var changeToken2 = new FakeChangeToken();
            var tracker2 = new FakeSource(changeToken2);
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(tracker2);

            var sp = services.BuildServiceProvider();

            var monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            Assert.NotNull(monitor);
            Assert.Equal("1", monitor.CurrentValue.Message);

            string updatedMessage = null;
            var cleanup = monitor.OnChange(o => updatedMessage = o.Message);
            changeToken.InvokeChangeCallback();
            Assert.Equal("2", updatedMessage);

            // Verify old watch is changed too
            Assert.Equal("2", monitor.CurrentValue.Message);

            changeToken2.InvokeChangeCallback();
            Assert.Equal("3", updatedMessage);

            // Verify old watch is changed too
            Assert.Equal("3", monitor.CurrentValue.Message);

            cleanup.Dispose();
            changeToken.InvokeChangeCallback();
            changeToken2.InvokeChangeCallback();

            // Verify messages aren't changed
            Assert.Equal("3", updatedMessage);
        }

        [Fact]
        public void CanMonitorConfigBoundOptions()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

            var services = new ServiceCollection().AddOptions();
            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            services.Configure<FakeOptions>(config);

            var sp = services.BuildServiceProvider();

            var monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            Assert.NotNull(monitor);
            Assert.Equal("1", monitor.CurrentValue.Message);

            string updatedMessage = null;

            var cleanup = monitor.OnChange(o => updatedMessage = o.Message);

            config.Reload();
            Assert.Equal("2", updatedMessage);

            // Verify old watch is changed too
            Assert.Equal("2", monitor.CurrentValue.Message);

            cleanup.Dispose();
            config.Reload();

            // Verify our message don't change after the subscription is disposed
            Assert.Equal("2", updatedMessage);

            // But the monitor still gets updated with the latest current value
            Assert.Equal("3", monitor.CurrentValue.Message);
        }

        [Fact]
        public void CanMonitorConfigBoundNamedOptions()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

            var services = new ServiceCollection().AddOptions();
            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            services.Configure<FakeOptions>("config", config);

            var sp = services.BuildServiceProvider();

            var monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            Assert.NotNull(monitor);
            Assert.Equal("1", monitor.CurrentValue.Message);

            string updatedMessage = null;

            var cleanup = monitor.OnChange((o, n) => updatedMessage = o.Message + "#" + n);

            config.Reload();
            Assert.Equal("2#config", updatedMessage);

            // Verify non-named option is unchanged
            Assert.Equal("1", monitor.CurrentValue.Message);

            cleanup.Dispose();
            config.Reload();

            // Verify our message don't change after the subscription is disposed
            Assert.Equal("2#config", updatedMessage);

            // But the monitor still gets updated with the latest current value
            Assert.Equal("3", monitor.Get("config").Message);
            Assert.Equal("1", monitor.CurrentValue.Message);
        }

        public class ControllerWithMonitor : IDisposable
        {
            IDisposable _watcher;
            FakeOptions _options;

            public ControllerWithMonitor(IOptionsMonitor<FakeOptions> watcher)
            {
                _watcher = watcher.OnChange(o => _options = o);
            }

            public void Dispose() => _watcher?.Dispose();

            public string Message => _options?.Message;
        }

        [Fact]
        public void ControllerCanWatchOptionsThatTrackConfigChanges()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

            var services = new ServiceCollection().AddOptions();
            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            services.AddTransient<ControllerWithMonitor, ControllerWithMonitor>();
            services.Configure<FakeOptions>(config);

            var sp = services.BuildServiceProvider();

            var controller = sp.GetRequiredService<ControllerWithMonitor>();
            Assert.Null(controller.Message);

            config.Reload();
            Assert.Equal("1", controller.Message);

            config.Reload();
            Assert.Equal("2", controller.Message);
        }

        [Fact]
        public void DisposingOptionsMonitorDisposesChangeTokenRegistrations()
        {
            var token = new ChangeToken();

            for (int i = 0; i < 10; i++)
            {
                var services = new ServiceCollection();
                services.AddOptions();
                services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(new ChangeTokenSource<FakeOptions>(token));
                using (var sp = services.BuildServiceProvider())
                {
                    var monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
                    using (monitor.OnChange(o => { }))
                    {

                    }
                }
            }

            Assert.Empty(token.Callbacks);
        }

        public class ChangeToken : IChangeToken
        {
            public List<(Action<object>, object)> Callbacks { get; } = new List<(Action<object>, object)>();

            public bool HasChanged => false;

            public bool ActiveChangeCallbacks => true;

            public IDisposable RegisterChangeCallback(Action<object> callback, object state)
            {
                var item = (callback, state);
                Callbacks.Add(item);
                return new DisposableAction(() => Callbacks.Remove(item));
            }

            private class DisposableAction : IDisposable
            {
                private Action _action;

                public DisposableAction(Action action)
                {
                    _action = action;
                }

                public void Dispose()
                {
                    var a = _action;
                    if (a != null)
                    {
                        _action = null;
                        a();
                    }
                }
            }
        }
        
        public class ChangeTokenSource<T> : IOptionsChangeTokenSource<T>
        {
            private readonly IChangeToken _changeToken;
            public ChangeTokenSource(IChangeToken changeToken)
            {
                _changeToken = changeToken;
            }

            public string Name => null;

            public IChangeToken GetChangeToken() => _changeToken;
        }

        [Fact]
        public void CallsPublicGetOrAddForCustomOptionsCache()
        {
            DerivedOptionsCache derivedOptionsCache = new();
            CreateMonitor(derivedOptionsCache).Get(null);
            Assert.Equal(1, derivedOptionsCache.GetOrAddCalls);

            ImplementedOptionsCache implementedOptionsCache = new();
            CreateMonitor(implementedOptionsCache).Get(null);
            Assert.Equal(1, implementedOptionsCache.GetOrAddCalls);

            static OptionsMonitor<FakeOptions> CreateMonitor(IOptionsMonitorCache<FakeOptions> cache) =>
                new OptionsMonitor<FakeOptions>(
                    new OptionsFactory<FakeOptions>(Enumerable.Empty<IConfigureOptions<FakeOptions>>(), Enumerable.Empty<IPostConfigureOptions<FakeOptions>>()),
                    Enumerable.Empty<IOptionsChangeTokenSource<FakeOptions>>(),
                    cache);
        }

        private sealed class DerivedOptionsCache : OptionsCache<FakeOptions>
        {
            public int GetOrAddCalls { get; private set; }

            public override FakeOptions GetOrAdd(string? name, Func<FakeOptions> createOptions)
            {
                GetOrAddCalls++;
                return base.GetOrAdd(name, createOptions);
            }
        }

        private sealed class ImplementedOptionsCache : IOptionsMonitorCache<FakeOptions>
        {
            public int GetOrAddCalls { get; private set; }

            public void Clear() => throw new NotImplementedException();

            public FakeOptions GetOrAdd(string? name, Func<FakeOptions> createOptions)
            {
                GetOrAddCalls++;
                return createOptions();
            }

            public bool TryAdd(string? name, FakeOptions options) => throw new NotImplementedException();

            public bool TryRemove(string? name) => throw new NotImplementedException();
        }

#if NET // need GC.GetAllocatedBytesForCurrentThread()
        /// <summary>
        /// Tests the fix for https://github.com/dotnet/runtime/issues/61086
        /// </summary>
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/67611", TestRuntimes.Mono)]
        public void TestCurrentValueDoesNotAllocateOnceValueIsCached()
        {
            var monitor = new OptionsMonitor<FakeOptions>(
                new OptionsFactory<FakeOptions>(Enumerable.Empty<IConfigureOptions<FakeOptions>>(), Enumerable.Empty<IPostConfigureOptions<FakeOptions>>()),
                Enumerable.Empty<IOptionsChangeTokenSource<FakeOptions>>(),
                new OptionsCache<FakeOptions>());
            Assert.NotNull(monitor.CurrentValue); // populate the cache

            long initialBytes = GC.GetAllocatedBytesForCurrentThread();
            _ = monitor.CurrentValue;
            Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - initialBytes);
        }
#endif

        /// <summary>
        /// Replicates https://github.com/dotnet/runtime/issues/79529
        /// </summary>
        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Synchronous wait is not supported on browser")]
        public void InstantiatesOnlyOneOptionsInstance()
        {
            using AutoResetEvent @event = new(initialState: false);

            OptionsMonitor<FakeOptions> monitor = new(
                // WaitHandleConfigureOptions makes instance configuration slow enough to force a race condition
                new OptionsFactory<FakeOptions>(new[] { new WaitHandleConfigureOptions(@event) }, Enumerable.Empty<IPostConfigureOptions<FakeOptions>>()),
                Enumerable.Empty<IOptionsChangeTokenSource<FakeOptions>>(),
                new OptionsCache<FakeOptions>());

            using Barrier barrier = new(participantCount: 2);
            Task<FakeOptions>[] instanceTasks = Enumerable.Range(0, 2)
                .Select(_ => Task.Factory.StartNew(
                    () =>
                    {
                        barrier.SignalAndWait();
                        return monitor.Get("someName");
                    },
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default)
                )
                .ToArray();

            // No tasks can finish yet; but give them a chance to run and get blocked on the WaitHandle
            Assert.Equal(-1, Task.WaitAny(instanceTasks, TimeSpan.FromSeconds(0.01)));

            // 1 release should be sufficient to complete both tasks
            @event.Set();
            Assert.True(Task.WaitAll(instanceTasks, TimeSpan.FromSeconds(30)));
            Assert.Equal(1, instanceTasks.Select(t => t.Result).Distinct().Count());
        }

        private class WaitHandleConfigureOptions : IConfigureNamedOptions<FakeOptions>
        {
            private readonly WaitHandle _waitHandle;

            public WaitHandleConfigureOptions(WaitHandle waitHandle)
            {
                _waitHandle = waitHandle;
            }

            void IConfigureNamedOptions<FakeOptions>.Configure(string? name, FakeOptions options) => _waitHandle.WaitOne();
            void IConfigureOptions<FakeOptions>.Configure(FakeOptions options) => _waitHandle.WaitOne();
        }
    }
}
