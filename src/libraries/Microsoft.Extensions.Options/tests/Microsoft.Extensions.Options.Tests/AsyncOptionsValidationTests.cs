// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.Extensions.Options.Tests
{
    public class AsyncOptionsValidationTests
    {
        private static IAsyncStartupValidator GetAsyncStartupValidator(IServiceProvider sp) =>
            Assert.IsAssignableFrom<IAsyncStartupValidator>(sp.GetRequiredService<IStartupValidator>());

        [Fact]
        public async Task AsyncValidateOptions_SkipsWhenNameDoesNotMatch()
        {
            var validator = new AsyncValidateOptions<FakeOptions>(
                "expected",
                (options, ct) => Task.FromResult(false),
                "Should not run");

            ValidateOptionsResult result = await validator.ValidateAsync("other", new FakeOptions(), CancellationToken.None);

            Assert.True(result.Skipped);
        }

        [Fact]
        public async Task AsyncValidateOptions_ValidatesWhenNameMatches()
        {
            var validator = new AsyncValidateOptions<FakeOptions>(
                "expected",
                (options, ct) => Task.FromResult(false),
                "Validation failed");

            ValidateOptionsResult result = await validator.ValidateAsync("expected", new FakeOptions(), CancellationToken.None);

            Assert.True(result.Failed);
            Assert.Contains("Validation failed", result.Failures);
        }

        [Fact]
        public async Task AsyncValidateOptions_ValidatesAllWhenNameIsNull()
        {
            var validator = new AsyncValidateOptions<FakeOptions>(
                null,
                (options, ct) => Task.FromResult(true),
                "fail");

            ValidateOptionsResult result = await validator.ValidateAsync("any-name", new FakeOptions(), CancellationToken.None);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task OptionsBuilder_AsyncValidate_RegistersAndExecutes()
        {
            var services = new ServiceCollection();
            bool asyncRan = false;

            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "test")
                .Validate(async (FakeOptions o, CancellationToken ct) =>
                {
                    asyncRan = true;
                    return await Task.FromResult(true);
                }, "async fail")
                .ValidateOnStart();

            ServiceProvider sp = services.BuildServiceProvider();
            IAsyncStartupValidator validator = GetAsyncStartupValidator(sp);

            await validator.ValidateAsync(CancellationToken.None);

            Assert.True(asyncRan);
        }

        [Fact]
        public async Task StartupValidator_SinglePath_RunsBothSyncAndAsyncValidators()
        {
            var services = new ServiceCollection();
            bool syncRan = false;
            bool asyncRan = false;

            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "test")
                .Validate(o => { syncRan = true; return true; }, "sync fail")
                .Validate(async (FakeOptions o, CancellationToken ct) =>
                {
                    asyncRan = true;
                    return await Task.FromResult(true);
                }, "async fail")
                .ValidateOnStart();

            ServiceProvider sp = services.BuildServiceProvider();

            // Single-path orchestration: one ValidateAsync runs every validator (sync and async) for the type,
            // dispatching each by capability.
            IAsyncStartupValidator validator = GetAsyncStartupValidator(sp);
            await validator.ValidateAsync(CancellationToken.None);

            Assert.True(syncRan);
            Assert.True(asyncRan);
        }

        [Fact]
        public async Task StartupValidator_SinglePath_AggregatesSyncAndAsyncFailures()
        {
            var services = new ServiceCollection();
            bool asyncRan = false;

            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "test")
                .Validate(o => false, "sync validation failed")
                .Validate(async (FakeOptions o, CancellationToken ct) =>
                {
                    asyncRan = true;
                    return await Task.FromResult(false);
                }, "async validation failed")
                .ValidateOnStart();

            ServiceProvider sp = services.BuildServiceProvider();
            IAsyncStartupValidator validator = GetAsyncStartupValidator(sp);

            // The single path does not short-circuit on the first failure: every validator runs and
            // all failures are aggregated into one OptionsValidationException.
            OptionsValidationException ex = await Assert.ThrowsAsync<OptionsValidationException>(
                () => validator.ValidateAsync(CancellationToken.None));

            Assert.True(asyncRan);
            Assert.Contains("sync validation failed", ex.Failures);
            Assert.Contains("async validation failed", ex.Failures);
        }

        [Fact]
        public async Task StartupValidator_ValidateAsync_OnlyAsyncValidators()
        {
            var services = new ServiceCollection();
            bool asyncRan = false;

            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "test")
                .Validate(async (FakeOptions o, CancellationToken ct) =>
                {
                    asyncRan = true;
                    return await Task.FromResult(true);
                }, "async fail")
                .ValidateOnStart();

            ServiceProvider sp = services.BuildServiceProvider();
            IAsyncStartupValidator validator = GetAsyncStartupValidator(sp);

            await validator.ValidateAsync(CancellationToken.None);

            Assert.True(asyncRan);
        }

        [Fact]
        public async Task StartupValidator_ValidateAsync_AsyncFailureThrowsOptionsValidationException()
        {
            var services = new ServiceCollection();

            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "test")
                .Validate(async (FakeOptions o, CancellationToken ct) =>
                {
                    await Task.CompletedTask;
                    return false;
                }, "async validation failed")
                .ValidateOnStart();

            ServiceProvider sp = services.BuildServiceProvider();
            IAsyncStartupValidator validator = GetAsyncStartupValidator(sp);

            OptionsValidationException ex = await Assert.ThrowsAsync<OptionsValidationException>(
                () => validator.ValidateAsync(CancellationToken.None));
            Assert.Contains("async validation failed", ex.Failures);
        }

        [Fact]
        public void ValidateOnStart_CustomSyncOnlyValidator_UsesSyncPath()
        {
            var services = new ServiceCollection();

            // A custom sync-only IStartupValidator registered before ValidateOnStart wins the
            // TryAddTransient, so it is the resolved IStartupValidator.
            services.AddSingleton<IStartupValidator>(new CustomSyncOnlyValidator());

            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "test")
                .Validate(async (FakeOptions o, CancellationToken ct) => await Task.FromResult(true), "async")
                .ValidateOnStart();

            ServiceProvider sp = services.BuildServiceProvider();

            // The custom validator is not async-capable, so the host falls back to the sync path
            // (validator.Validate()) — no InvalidCastException and no async validation.
            IStartupValidator validator = sp.GetRequiredService<IStartupValidator>();
            Assert.IsType<CustomSyncOnlyValidator>(validator);
            Assert.False(validator is IAsyncStartupValidator);
            validator.Validate();
        }

        [Fact]
        public async Task StartupValidator_ValidateAsync_CancellationTokenPropagated()
        {
            var services = new ServiceCollection();
            using var cts = new CancellationTokenSource();

            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "test")
                .Validate(async (FakeOptions o, CancellationToken ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    return await Task.FromResult(true);
                }, "async")
                .ValidateOnStart();

            ServiceProvider sp = services.BuildServiceProvider();
            IAsyncStartupValidator validator = GetAsyncStartupValidator(sp);

            cts.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() => validator.ValidateAsync(cts.Token));
        }

        [Theory]
        [InlineData("named1")]
        [InlineData(null)]
        public async Task AsyncValidateOptions_NameMatching_DefaultAndNamed(string? registeredName)
        {
            var validator = new AsyncValidateOptions<FakeOptions>(
                registeredName,
                (options, ct) => Task.FromResult(false),
                "fail");

            ValidateOptionsResult defaultResult = await validator.ValidateAsync(Options.DefaultName, new FakeOptions(), CancellationToken.None);

            if (registeredName is null)
            {
                Assert.True(defaultResult.Failed);
            }
            else
            {
                Assert.True(defaultResult.Skipped);
            }
        }

        [Fact]
        public async Task StartupValidator_ValidateAsync_MultipleFailures_ThrowsAggregateException()
        {
            var services = new ServiceCollection();

            services.AddOptions<FakeOptions>("instance1")
                .Configure(o => o.Message = "")
                .Validate(async (FakeOptions o, CancellationToken ct) =>
                {
                    await Task.CompletedTask;
                    return o.Message.Length > 0;
                }, "Message required for instance1")
                .ValidateOnStart();

            services.AddOptions<FakeOptions>("instance2")
                .Configure(o => o.Message = "")
                .Validate(async (FakeOptions o, CancellationToken ct) =>
                {
                    await Task.CompletedTask;
                    return o.Message.Length > 0;
                }, "Message required for instance2")
                .ValidateOnStart();

            using ServiceProvider sp = services.BuildServiceProvider();
            IAsyncStartupValidator validator = GetAsyncStartupValidator(sp);

            AggregateException ex = await Assert.ThrowsAsync<AggregateException>(() => validator.ValidateAsync());
            Assert.Equal(2, ex.InnerExceptions.Count);
            Assert.All(ex.InnerExceptions, e => Assert.IsType<OptionsValidationException>(e));
        }

        [Fact]
        public async Task StartupValidator_ValidatorImplementingBoth_DispatchesToAsync()
        {
            var spy = new CapabilitySpyValidator();
            var services = new ServiceCollection();

            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "test")
                .ValidateOnStart();
            services.AddSingleton<IValidateOptions<FakeOptions>>(spy);

            ServiceProvider sp = services.BuildServiceProvider();
            IAsyncStartupValidator validator = GetAsyncStartupValidator(sp);

            await validator.ValidateAsync(CancellationToken.None);

            // A validator that implements both contracts is dispatched through ValidateAsync only.
            Assert.True(spy.AsyncCalled);
            Assert.False(spy.SyncCalled);
        }

        [Fact]
        public void MisregisteredAsyncOnlyValidator_ThrowsClearErrorOnSyncAccess()
        {
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>();
            // Misregistration: registered only as the IAsyncValidateOptions<T> capability interface.
            services.AddSingleton<IAsyncValidateOptions<FakeOptions>>(
                new AsyncValidateOptions<FakeOptions>(null, (o, ct) => Task.FromResult(false), "async fail"));
            using ServiceProvider sp = services.BuildServiceProvider();

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => sp.GetRequiredService<IOptions<FakeOptions>>().Value);

            Assert.Contains(nameof(FakeOptions), ex.Message);
            Assert.Contains(nameof(IValidateOptions<FakeOptions>), ex.Message);
        }

        [Fact]
        public async Task MisregisteredAsyncOnlyValidator_ThrowsClearErrorOnStartup()
        {
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>().ValidateOnStart();
            services.AddSingleton<IAsyncValidateOptions<FakeOptions>>(
                new AsyncValidateOptions<FakeOptions>(null, (o, ct) => Task.FromResult(false), "async fail"));
            using ServiceProvider sp = services.BuildServiceProvider();
            IAsyncStartupValidator validator = GetAsyncStartupValidator(sp);

            await Assert.ThrowsAsync<InvalidOperationException>(() => validator.ValidateAsync(CancellationToken.None));
        }

        [Fact]
        public void CorrectlyRegisteredAsyncValidator_DoesNotThrowMisregistrationError()
        {
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>().Configure(o => o.Message = "ok");
            // Correct: an async-capable validator registered through IValidateOptions<T>.
            services.AddSingleton<IValidateOptions<FakeOptions>>(new CapabilitySpyValidator());
            using ServiceProvider sp = services.BuildServiceProvider();

            FakeOptions value = sp.GetRequiredService<IOptions<FakeOptions>>().Value;

            Assert.Equal("ok", value.Message);
        }

        [Fact]
        public void NoAsyncValidators_DoesNotThrowMisregistrationError()
        {
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>().Configure(o => o.Message = "ok");
            using ServiceProvider sp = services.BuildServiceProvider();

            FakeOptions value = sp.GetRequiredService<IOptions<FakeOptions>>().Value;

            Assert.Equal("ok", value.Message);
        }

        // ---- Phase 9: sync-accessor validated-value caching ----

        [Fact]
        public async Task Phase9_AsyncValidatedType_SyncAccessorsReturnStartupValidatedValue()
        {
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "validated")
                .Validate(async (FakeOptions o, CancellationToken ct) => await Task.FromResult(true), "async fail")
                .ValidateOnStart();
            using ServiceProvider sp = services.BuildServiceProvider();

            // Before startup nothing has been validated, so synchronous access fails fast rather than silently skipping.
            Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptions<FakeOptions>>().Value);

            await GetAsyncStartupValidator(sp).ValidateAsync(CancellationToken.None);

            // After startup seeds the shared cache, synchronous IOptions<T>.Value returns the validated value.
            Assert.Equal("validated", sp.GetRequiredService<IOptions<FakeOptions>>().Value.Message);

            // IOptionsSnapshot<T>.Get (scoped) also serves the startup-validated value instead of re-validating synchronously.
            using IServiceScope scope = sp.CreateScope();
            Assert.Equal("validated", scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Get(null).Message);
        }

        [Fact]
        public void Phase9_SyncOnlyType_SyncAccessorsBehaviorUnchanged()
        {
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "sync")
                .Validate(o => o.Message == "sync", "sync fail");
            using ServiceProvider sp = services.BuildServiceProvider();

            // A sync-only type is not async-capable, so the accessors create and validate synchronously as before.
            Assert.Equal("sync", sp.GetRequiredService<IOptions<FakeOptions>>().Value.Message);
            using IServiceScope scope = sp.CreateScope();
            Assert.Equal("sync", scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Get(null).Message);
        }

        // ---- Phase 8: opt-in async reload revalidation (ValidateOnChange) ----

        [Fact]
        public void Phase8_ValidReload_PublishesNewValidatedValueAndNotifies()
        {
            var source = new ReloadSource();
            var validator = new ReloadTestValidator();
            int configureCount = 0;
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = $"v{Interlocked.Increment(ref configureCount)}")
                .ValidateOnChange();
            services.AddSingleton<IValidateOptions<FakeOptions>>(validator);
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(source);
            using ServiceProvider sp = services.BuildServiceProvider();

            SeedCache(sp, "seed");
            IOptionsMonitor<FakeOptions> monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            Assert.Equal("seed", monitor.CurrentValue.Message);

            using var changed = new ManualResetEventSlim();
            string? notified = null;
            using IDisposable _ = monitor.OnChange((o, n) => { notified = o.Message; changed.Set(); });

            source.Trigger();

            Assert.True(changed.Wait(TimeSpan.FromSeconds(30)), "The change notification was not raised.");
            Assert.NotEqual("seed", monitor.CurrentValue.Message);
            Assert.Equal(monitor.CurrentValue.Message, notified);
        }

        [Fact]
        public void Phase8_InvalidReload_KeepLastGood_ServesLastGoodAndReportsError()
        {
            var source = new ReloadSource();
            var validator = new ReloadTestValidator { Fail = true };
            var services = new ServiceCollection();
            using var errored = new ManualResetEventSlim();
            Exception? reportedError = null;
            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "reloaded")
                .ValidateOnChange(OptionsReloadValidationBehavior.KeepLastGood, (name, ex) => { reportedError = ex; errored.Set(); });
            services.AddSingleton<IValidateOptions<FakeOptions>>(validator);
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(source);
            using ServiceProvider sp = services.BuildServiceProvider();

            SeedCache(sp, "good");
            IOptionsMonitor<FakeOptions> monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();

            bool changeFired = false;
            using IDisposable _ = monitor.OnChange((o, n) => changeFired = true);

            source.Trigger();

            Assert.True(errored.Wait(TimeSpan.FromSeconds(30)), "The error callback was not invoked.");
            Assert.IsType<OptionsValidationException>(reportedError);
            // The last validated value keeps being served, and no change notification is raised for a failed reload.
            Assert.Equal("good", monitor.CurrentValue.Message);
            Assert.False(changeFired);
        }

        [Fact]
        public void Phase8_InvalidReload_FailReads_NextReadThrows()
        {
            var source = new ReloadSource();
            var validator = new ReloadTestValidator { Fail = true };
            var services = new ServiceCollection();
            using var errored = new ManualResetEventSlim();
            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "reloaded")
                .ValidateOnChange(OptionsReloadValidationBehavior.FailReads, (name, ex) => errored.Set());
            services.AddSingleton<IValidateOptions<FakeOptions>>(validator);
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(source);
            using ServiceProvider sp = services.BuildServiceProvider();

            SeedCache(sp, "good");
            IOptionsMonitor<FakeOptions> monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();

            source.Trigger();

            Assert.True(errored.Wait(TimeSpan.FromSeconds(30)), "The error callback was not invoked.");
            // FailReads dropped the cached value, so the next read re-creates and surfaces the failure.
            Assert.Throws<OptionsValidationException>(() => monitor.CurrentValue);
        }

        [Fact]
        public void Phase8_DefaultMonitorWithoutOptIn_UsesLazyRevalidation()
        {
            var source = new ReloadSource();
            int configureCount = 0;
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = $"v{Interlocked.Increment(ref configureCount)}");
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(source);
            using ServiceProvider sp = services.BuildServiceProvider();
            IOptionsMonitor<FakeOptions> monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();

            Assert.Equal("v1", monitor.CurrentValue.Message);

            // Without ValidateOnChange the default lazy behavior clears the cache and re-creates on the next read.
            source.Trigger();
            Assert.Equal("v2", monitor.CurrentValue.Message);
        }

        [Fact]
        public void Phase8_RapidReloads_LatestWins()
        {
            var source = new ReloadSource();
            var validator = new ReloadTestValidator();
            var firstGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            validator.Gate = firstGate.Task;
            int configureCount = 0;
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = $"v{Interlocked.Increment(ref configureCount)}")
                .ValidateOnChange();
            services.AddSingleton<IValidateOptions<FakeOptions>>(validator);
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(source);
            using ServiceProvider sp = services.BuildServiceProvider();

            SeedCache(sp, "seed");
            IOptionsMonitor<FakeOptions> monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();

            using var secondChanged = new ManualResetEventSlim();
            string? lastNotified = null;
            using IDisposable _ = monitor.OnChange((o, n) => { lastNotified = o.Message; secondChanged.Set(); });

            // First reload blocks in ValidateAsync on the gate.
            source.Trigger();
            // Second reload supersedes the first and completes (its gate was cleared before it started).
            validator.Gate = null;
            source.Trigger();

            Assert.True(secondChanged.Wait(TimeSpan.FromSeconds(30)), "The superseding reload did not publish.");
            string published = monitor.CurrentValue.Message;

            // Release the superseded first reload; it must not overwrite the newer published value.
            firstGate.SetResult(true);
            Thread.Sleep(200);

            Assert.Equal(published, monitor.CurrentValue.Message);
            Assert.Equal(published, lastNotified);
        }

        [Fact]
        public void Phase8_DisposeCancelsInFlightReload()
        {
            var source = new ReloadSource();
            var validator = new ReloadTestValidator();
            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            validator.Gate = gate.Task;
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "reloaded")
                .ValidateOnChange();
            services.AddSingleton<IValidateOptions<FakeOptions>>(validator);
            services.AddSingleton<IOptionsChangeTokenSource<FakeOptions>>(source);
            using ServiceProvider sp = services.BuildServiceProvider();

            SeedCache(sp, "seed");
            var monitor = (OptionsMonitor<FakeOptions>)sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();

            source.Trigger();          // starts a background reload blocked on the gate
            monitor.Dispose();         // cancels the in-flight reload
            gate.SetResult(true);      // release; the canceled reload must not publish
            Thread.Sleep(200);

            var cache = (OptionsCache<FakeOptions>)sp.GetRequiredService<IOptionsMonitorCache<FakeOptions>>();
            Assert.True(cache.TryGetValue(Options.DefaultName, out FakeOptions? value));
            Assert.Equal("seed", value!.Message);
        }

        private static void SeedCache(IServiceProvider sp, string message)
        {
            var cache = (OptionsCache<FakeOptions>)sp.GetRequiredService<IOptionsMonitorCache<FakeOptions>>();
            cache.AddOrReplace(Options.DefaultName, new FakeOptions { Message = message });
        }

        private sealed class ReloadSource : IOptionsChangeTokenSource<FakeOptions>
        {
            private readonly FakeChangeToken _token = new FakeChangeToken();
            public string? Name => null;
            public IChangeToken GetChangeToken() => _token;
            public void Trigger() => _token.InvokeChangeCallback();
        }

        private sealed class ReloadTestValidator : IValidateOptions<FakeOptions>, IAsyncValidateOptions<FakeOptions>
        {
            public bool Fail { get; set; }
            public Task? Gate { get; set; }

            public ValidateOptionsResult Validate(string? name, FakeOptions options)
                => ValidateOptionsResult.Fail("Synchronous validation is not supported.");

            public async Task<ValidateOptionsResult> ValidateAsync(string? name, FakeOptions options, CancellationToken cancellationToken)
            {
                Task? gate = Gate;
                if (gate is not null)
                {
                    var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    using (cancellationToken.Register(static s => ((TaskCompletionSource<bool>)s!).TrySetCanceled(), tcs))
                    {
                        await Task.WhenAny(gate, tcs.Task).ConfigureAwait(false);
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                }

                return Fail ? ValidateOptionsResult.Fail("async validation failed") : ValidateOptionsResult.Success;
            }
        }

        private class CustomSyncOnlyValidator : IStartupValidator
        {
            public void Validate() { }
        }

        private sealed class CapabilitySpyValidator : IValidateOptions<FakeOptions>, IAsyncValidateOptions<FakeOptions>
        {
            public bool SyncCalled { get; private set; }
            public bool AsyncCalled { get; private set; }

            public ValidateOptionsResult Validate(string? name, FakeOptions options)
            {
                SyncCalled = true;
                return ValidateOptionsResult.Success;
            }

            public Task<ValidateOptionsResult> ValidateAsync(string? name, FakeOptions options, CancellationToken cancellationToken = default)
            {
                AsyncCalled = true;
                return Task.FromResult(ValidateOptionsResult.Success);
            }
        }
    }
}
