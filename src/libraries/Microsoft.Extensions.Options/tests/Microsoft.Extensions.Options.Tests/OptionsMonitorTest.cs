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

        private static TaskCompletionSource<T> CreateTaskCompletionSource<T>() =>
            new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        private static async Task<T> WaitAsync<T>(Task<T> task)
        {
            Task completedTask = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(30)));
            Assert.Same(task, completedTask);

            return await task;
        }

        private static async Task WaitAsync(Task task)
        {
            Task completedTask = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(30)));
            Assert.Same(task, completedTask);

            await task;
        }

        private static async Task<FakeOptions> CompleteCurrentValueValidationAsync(IOptionsMonitor<FakeOptions> monitor, ControllableAsyncValidator validator, bool result = true)
        {
            Task<FakeOptions> currentValueTask = Task.Run(() => monitor.CurrentValue);
            FakeOptions validatingOptions = await WaitAsync(validator.Started);
            validator.Complete(result);
            FakeOptions options = await WaitAsync(currentValueTask);
            validator.Reset();

            return options;
        }

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

        private sealed class ControllableAsyncValidator : IAsyncValidateOptions<FakeOptions>
        {
            private readonly object _lock = new object();
            private TaskCompletionSource<FakeOptions> _started = CreateTaskCompletionSource<FakeOptions>();
            private TaskCompletionSource<bool> _result = CreateTaskCompletionSource<bool>();

            public Task<FakeOptions> Started
            {
                get
                {
                    lock (_lock)
                    {
                        return _started.Task;
                    }
                }
            }

            public void Complete(bool result)
            {
                TaskCompletionSource<bool> resultSource;
                lock (_lock)
                {
                    resultSource = _result;
                }

                resultSource.SetResult(result);
            }

            public void Reset()
            {
                lock (_lock)
                {
                    _started = CreateTaskCompletionSource<FakeOptions>();
                    _result = CreateTaskCompletionSource<bool>();
                }
            }

            public ValidateOptionsResult Validate(string? name, FakeOptions options) =>
                ValidateOptionsResult.Fail("Synchronous validation is not supported.");

            public async Task<ValidateOptionsResult> ValidateAsync(string? name, FakeOptions options, CancellationToken cancellationToken = default)
            {
                TaskCompletionSource<FakeOptions> startedSource;
                Task<bool> resultTask;
                lock (_lock)
                {
                    startedSource = _started;
                    resultTask = _result.Task;
                }

                startedSource.SetResult(options);
                return await resultTask.ConfigureAwait(false)
                    ? ValidateOptionsResult.Success
                    : ValidateOptionsResult.Fail("Async validation failed.");
            }
        }

        private sealed class SequencedAsyncValidator : IAsyncValidateOptions<FakeOptions>
        {
            private readonly object _lock = new object();
            private readonly List<TaskCompletionSource<FakeOptions>> _started;
            private readonly List<TaskCompletionSource<bool>> _results;
            private int _callCount;
            private int _activeCount;

            public SequencedAsyncValidator(int expectedCallCount)
            {
                _started = Enumerable.Range(0, expectedCallCount)
                    .Select(_ => CreateTaskCompletionSource<FakeOptions>())
                    .ToList();
                _results = Enumerable.Range(0, expectedCallCount)
                    .Select(_ => CreateTaskCompletionSource<bool>())
                    .ToList();
            }

            public int MaxActiveCount { get; private set; }

            public Task<FakeOptions> Started(int index)
            {
                lock (_lock)
                {
                    return _started[index].Task;
                }
            }

            public void Complete(int index, bool result)
            {
                TaskCompletionSource<bool> resultSource;
                lock (_lock)
                {
                    resultSource = _results[index];
                }

                resultSource.SetResult(result);
            }

            public ValidateOptionsResult Validate(string? name, FakeOptions options) =>
                ValidateOptionsResult.Fail("Synchronous validation is not supported.");

            public async Task<ValidateOptionsResult> ValidateAsync(string? name, FakeOptions options, CancellationToken cancellationToken = default)
            {
                int index;
                TaskCompletionSource<FakeOptions> startedSource;
                Task<bool> resultTask;
                lock (_lock)
                {
                    index = _callCount++;
                    if ((uint)index >= (uint)_started.Count)
                    {
                        throw new InvalidOperationException("Unexpected validation call.");
                    }

                    _activeCount++;
                    MaxActiveCount = Math.Max(MaxActiveCount, _activeCount);
                    startedSource = _started[index];
                    resultTask = _results[index].Task;
                }

                startedSource.SetResult(options);
                try
                {
                    return await resultTask.ConfigureAwait(false)
                        ? ValidateOptionsResult.Success
                        : ValidateOptionsResult.Fail("Async validation failed.");
                }
                finally
                {
                    lock (_lock)
                    {
                        _activeCount--;
                    }
                }
            }
        }

        private sealed class AsyncValidationDependency : IDisposable
        {
            private readonly AsyncValidationObserver _observer;

            public AsyncValidationDependency(AsyncValidationObserver observer)
            {
                _observer = observer;
            }

            public void Call() => _observer.ValidationCount++;

            public void Dispose() => _observer.DisposeCount++;
        }

        private sealed class AsyncValidationObserver
        {
            public int ValidationCount { get; set; }

            public int DisposeCount { get; set; }
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
        public async Task CanWatchOptionsWithAsyncValidation()
        {
            var services = new ServiceCollection().AddOptions();
            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            var changeToken = new FakeChangeToken();
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(new FakeSource(changeToken));
            var validator = new ControllableAsyncValidator();
            services.AddSingleton<IAsyncValidateOptions<FakeOptions>>(validator);

            var sp = services.BuildServiceProvider();

            var monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            Assert.NotNull(monitor);
            Assert.Equal("1", (await CompleteCurrentValueValidationAsync(monitor, validator)).Message);

            var changed = CreateTaskCompletionSource<string>();
            monitor.OnChange(o => changed.SetResult(o.Message));

            Task reloadTask = Task.Run(changeToken.InvokeChangeCallback);

            FakeOptions pendingOptions = await WaitAsync(validator.Started);
            Assert.Equal("2", pendingOptions.Message);
            Assert.Equal("1", monitor.CurrentValue.Message);
            Assert.False(changed.Task.IsCompleted);

            validator.Complete(true);

            await WaitAsync(reloadTask);
            Assert.Equal("2", await WaitAsync(changed.Task));
            Assert.Equal("2", monitor.CurrentValue.Message);
        }

        [Fact]
        public async Task AsyncValidationFailureIsObservedByGetAndDoesNotNotify()
        {
            var services = new ServiceCollection().AddOptions();
            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            var changeToken = new FakeChangeToken();
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(new FakeSource(changeToken));
            var validator = new ControllableAsyncValidator();
            services.AddSingleton<IAsyncValidateOptions<FakeOptions>>(validator);

            var sp = services.BuildServiceProvider();

            var monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            Assert.NotNull(monitor);
            Assert.Equal("1", (await CompleteCurrentValueValidationAsync(monitor, validator)).Message);

            var changed = CreateTaskCompletionSource<string>();
            monitor.OnChange(o => changed.SetResult(o.Message));

            Task reloadTask = Task.Run(changeToken.InvokeChangeCallback);

            FakeOptions rejectedOptions = await WaitAsync(validator.Started);
            Assert.Equal("2", rejectedOptions.Message);

            validator.Complete(false);

            OptionsValidationException ex = await Assert.ThrowsAsync<OptionsValidationException>(async () => await reloadTask);
            Assert.Contains("Async validation failed.", ex.Failures);

            Assert.False(changed.Task.IsCompleted);
            OptionsValidationException getException = Assert.Throws<OptionsValidationException>(() => monitor.CurrentValue);
            Assert.Contains("Async validation failed.", getException.Failures);

            validator.Reset();
            Task successfulReloadTask = Task.Run(changeToken.InvokeChangeCallback);

            FakeOptions acceptedOptions = await WaitAsync(validator.Started);
            Assert.Equal("3", acceptedOptions.Message);

            validator.Complete(true);

            await WaitAsync(successfulReloadTask);
            Assert.Equal("3", await WaitAsync(changed.Task));
            Assert.Equal("3", monitor.CurrentValue.Message);
        }

        [Fact]
        public async Task AsyncValidationFailureForUncachedOptionsIsObservedByGet()
        {
            var services = new ServiceCollection().AddOptions();
            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            var changeToken = new FakeChangeToken();
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(new FakeSource(changeToken) { Name = "unread" });
            var validator = new ControllableAsyncValidator();
            services.AddSingleton<IAsyncValidateOptions<FakeOptions>>(validator);

            var sp = services.BuildServiceProvider();

            var monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            Assert.NotNull(monitor);

            Task reloadTask = Task.Run(changeToken.InvokeChangeCallback);

            FakeOptions rejectedOptions = await WaitAsync(validator.Started);
            Assert.Equal("1", rejectedOptions.Message);

            validator.Complete(false);

            OptionsValidationException reloadException = await Assert.ThrowsAsync<OptionsValidationException>(async () => await reloadTask);
            Assert.Contains("Async validation failed.", reloadException.Failures);

            OptionsValidationException getException = Assert.Throws<OptionsValidationException>(() => monitor.Get("unread"));
            Assert.Contains("Async validation failed.", getException.Failures);

            validator.Reset();
            Task successfulReloadTask = Task.Run(changeToken.InvokeChangeCallback);

            FakeOptions acceptedOptions = await WaitAsync(validator.Started);
            Assert.Equal("2", acceptedOptions.Message);

            validator.Complete(true);
            await WaitAsync(successfulReloadTask);

            Assert.Equal("2", monitor.Get("unread").Message);
        }

        [Fact]
        public async Task AsyncValidationFailureForUncachedOptionsIsObservedByConcurrentGet()
        {
            var services = new ServiceCollection().AddOptions();
            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            var changeToken = new FakeChangeToken();
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(new FakeSource(changeToken) { Name = "unread" });
            var validator = new ControllableAsyncValidator();
            services.AddSingleton<IAsyncValidateOptions<FakeOptions>>(validator);

            var sp = services.BuildServiceProvider();

            var monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            Assert.NotNull(monitor);

            Task reloadTask = Task.Run(changeToken.InvokeChangeCallback);

            FakeOptions rejectedOptions = await WaitAsync(validator.Started);
            Assert.Equal("1", rejectedOptions.Message);

            Task<FakeOptions> concurrentGetTask = Task.Run(() => monitor.Get("unread"));

            validator.Complete(false);

            OptionsValidationException reloadException = await Assert.ThrowsAsync<OptionsValidationException>(async () => await reloadTask);
            Assert.Contains("Async validation failed.", reloadException.Failures);

            OptionsValidationException getException = await Assert.ThrowsAsync<OptionsValidationException>(async () => await concurrentGetTask);
            Assert.Contains("Async validation failed.", getException.Failures);
        }

        [Fact]
        public async Task ConcurrentInitialAsyncValidationDoesNotBlockReload()
        {
            for (int iteration = 0; iteration < 20; iteration++)
            {
                SetupInvokeCount = 0;
                var services = new ServiceCollection().AddOptions();
                services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
                var changeToken = new FakeChangeToken();
                services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(new FakeSource(changeToken));
                services.AddOptions<FakeOptions>()
                    .Validate(static (FakeOptions _, CancellationToken _) => Task.FromResult(true), "async fail");

                using ServiceProvider sp = services.BuildServiceProvider();

                var monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
                Assert.NotNull(monitor);

                using var start = new ManualResetEventSlim();
                Task<FakeOptions>[] initialReads = Enumerable.Range(0, 32)
                    .Select(_ => Task.Run(() =>
                    {
                        start.Wait();
                        return monitor.CurrentValue;
                    }))
                    .ToArray();

                start.Set();
                FakeOptions[] initialValues = await WaitAsync(Task.WhenAll(initialReads));
                Assert.All(initialValues, options => Assert.Equal("1", options.Message));

                await WaitAsync(Task.Run(changeToken.InvokeChangeCallback));
                Assert.Equal("2", monitor.CurrentValue.Message);
            }
        }

        [Fact]
        public void OptionsValueWithAsyncOnlyValidatorDoesNotObserveMonitorReloadedValue()
        {
            var services = new ServiceCollection().AddOptions();
            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            var changeToken = new FakeChangeToken();
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(new FakeSource(changeToken));
            services.AddOptions<FakeOptions>()
                .Validate(static (FakeOptions _, CancellationToken _) => Task.FromResult(true), "async fail");

            using ServiceProvider sp = services.BuildServiceProvider();

            var monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            var options = sp.GetRequiredService<IOptions<FakeOptions>>();

            FakeOptions initialOptions = monitor.CurrentValue;
            Assert.Equal("1", initialOptions.Message);
            Assert.Throws<OptionsValidationException>(() => options.Value);

            changeToken.InvokeChangeCallback();

            FakeOptions reloadedOptions = monitor.CurrentValue;
            Assert.Equal("3", reloadedOptions.Message);
            Assert.NotSame(initialOptions, reloadedOptions);
            Assert.Throws<OptionsValidationException>(() => options.Value);
        }

        [Fact]
        public async Task AsyncValidationFailureWithCustomCacheIsObservedByGetAndDoesNotNotify()
        {
            var services = new ServiceCollection().AddOptions();
            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            services.AddSingleton<IOptionsMonitorCache<FakeOptions>, TestOptionsCache>();
            var changeToken = new FakeChangeToken();
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(new FakeSource(changeToken));
            var validator = new ControllableAsyncValidator();
            services.AddSingleton<IAsyncValidateOptions<FakeOptions>>(validator);

            var sp = services.BuildServiceProvider();

            var monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            Assert.NotNull(monitor);
            Assert.Equal("1", (await CompleteCurrentValueValidationAsync(monitor, validator)).Message);

            var changed = CreateTaskCompletionSource<string>();
            monitor.OnChange(o => changed.SetResult(o.Message));

            Task reloadTask = Task.Run(changeToken.InvokeChangeCallback);
            FakeOptions rejectedOptions = await WaitAsync(validator.Started);
            Assert.Equal("2", rejectedOptions.Message);

            validator.Complete(false);

            OptionsValidationException reloadException = await Assert.ThrowsAsync<OptionsValidationException>(async () => await reloadTask);
            Assert.Contains("Async validation failed.", reloadException.Failures);

            Assert.False(changed.Task.IsCompleted);
            OptionsValidationException getException = Assert.Throws<OptionsValidationException>(() => monitor.CurrentValue);
            Assert.Contains("Async validation failed.", getException.Failures);
        }

        [Fact]
        public void AsyncValidationOnReloadResolvesValidatorsFromScope()
        {
            var services = new ServiceCollection().AddOptions();
            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            var changeToken = new FakeChangeToken();
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(new FakeSource(changeToken));
            services.AddScoped<AsyncValidationDependency>();
            services.AddSingleton<AsyncValidationObserver>();
            services.AddOptions<FakeOptions>()
                .Validate<AsyncValidationDependency>(
                    static (_, dependency, _) =>
                    {
                        dependency.Call();
                        return Task.FromResult(true);
                    });

            using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

            var monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            Assert.NotNull(monitor);
            Assert.Equal("1", monitor.CurrentValue.Message);

            var observer = sp.GetRequiredService<AsyncValidationObserver>();
            changeToken.InvokeChangeCallback();

            Assert.Equal(2, observer.ValidationCount);
            Assert.Equal(2, observer.DisposeCount);
            Assert.Equal("2", monitor.CurrentValue.Message);
        }

        [Fact]
        public async Task AsyncValidationReloadsForSameNameDoNotRunInParallel()
        {
            var services = new ServiceCollection().AddOptions();
            services.AddSingleton<IConfigureOptions<FakeOptions>>(new CountIncrement(this));
            var changeToken = new FakeChangeToken();
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(new FakeSource(changeToken));
            var changeToken2 = new FakeChangeToken();
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(new FakeSource(changeToken2));
            var validator = new SequencedAsyncValidator(expectedCallCount: 3);
            services.AddSingleton<IAsyncValidateOptions<FakeOptions>>(validator);

            var sp = services.BuildServiceProvider();

            var monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            Assert.NotNull(monitor);
            Task<FakeOptions> initialValueTask = Task.Run(() => monitor.CurrentValue);
            FakeOptions initialOptions = await WaitAsync(validator.Started(0));
            Assert.Equal("1", initialOptions.Message);
            validator.Complete(0, result: true);
            Assert.Equal("1", (await WaitAsync(initialValueTask)).Message);

            Task firstReloadTask = Task.Run(changeToken.InvokeChangeCallback);

            FakeOptions firstOptions = await WaitAsync(validator.Started(1));
            Assert.Equal("2", firstOptions.Message);

            Task secondReloadTask = Task.Run(changeToken2.InvokeChangeCallback);

            validator.Complete(1, result: true);
            await WaitAsync(firstReloadTask);

            FakeOptions secondOptions = await WaitAsync(validator.Started(2));
            Assert.Equal("3", secondOptions.Message);

            validator.Complete(2, result: true);
            await WaitAsync(secondReloadTask);

            Assert.Equal(1, validator.MaxActiveCount);
            Assert.Equal("3", monitor.CurrentValue.Message);
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

        [Fact]
        public void CurrentValueCallsOverriddenGetWhenDefaultValueIsCached()
        {
            var cache = new OptionsCache<FakeOptions>();
            Assert.True(cache.TryAdd(Options.DefaultName, new FakeOptions { Message = "cached" }));

            var monitor = new OverridingOptionsMonitor(
                new OptionsFactory<FakeOptions>(Enumerable.Empty<IConfigureOptions<FakeOptions>>(), Enumerable.Empty<IPostConfigureOptions<FakeOptions>>()),
                Enumerable.Empty<IOptionsChangeTokenSource<FakeOptions>>(),
                cache);

            Assert.Equal("overridden", monitor.CurrentValue.Message);
            Assert.Equal(1, monitor.GetCalls);
        }

        private sealed class OverridingOptionsMonitor : OptionsMonitor<FakeOptions>
        {
            public OverridingOptionsMonitor(
                IOptionsFactory<FakeOptions> factory,
                IEnumerable<IOptionsChangeTokenSource<FakeOptions>> sources,
                IOptionsMonitorCache<FakeOptions> cache)
                : base(factory, sources, cache)
            {
            }

            public int GetCalls { get; private set; }

            public override FakeOptions Get(string? name)
            {
                GetCalls++;
                return new FakeOptions { Message = "overridden" };
            }
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

        private sealed class TestOptionsCache : IOptionsMonitorCache<FakeOptions>
        {
            private readonly Dictionary<string, FakeOptions> _cache = new Dictionary<string, FakeOptions>();

            public void Clear() => _cache.Clear();

            public FakeOptions GetOrAdd(string? name, Func<FakeOptions> createOptions)
            {
                string key = name ?? Options.DefaultName;
                if (!_cache.TryGetValue(key, out FakeOptions? options))
                {
                    options = createOptions();
                    _cache.Add(key, options);
                }

                return options;
            }

            public bool TryAdd(string? name, FakeOptions options)
            {
                string key = name ?? Options.DefaultName;
                if (_cache.ContainsKey(key))
                {
                    return false;
                }

                _cache.Add(key, options);
                return true;
            }

            public bool TryRemove(string? name)
            {
                string key = name ?? Options.DefaultName;
                return _cache.Remove(key);
            }
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
