// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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

            // The custom validator is not async-capable, so the host falls back to the sync path (validator.Validate())
            // This means no InvalidCastException and no async validation.
            IStartupValidator validator = sp.GetRequiredService<IStartupValidator>();
            Assert.IsType<CustomSyncOnlyValidator>(validator);
            Assert.False(validator is IAsyncStartupValidator);
            validator.Validate();
        }

        [Fact]
        public void ValidateOnStart_RegistersBuiltInValidatorAsBothInterfaces()
        {
            var services = new ServiceCollection();

            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "test")
                .Validate(o => true)
                .ValidateOnStart();

            ServiceProvider sp = services.BuildServiceProvider();

            IStartupValidator sync = sp.GetRequiredService<IStartupValidator>();
            Assert.IsType<IAsyncStartupValidator>(sync, exactMatch: false);
            Assert.Single(sp.GetServices<IAsyncStartupValidator>());
        }

        [Fact]
        public void ValidateOnStart_CalledMultipleTimes_RegistersSingleAsyncStartupValidator()
        {
            var services = new ServiceCollection();

            services.AddOptions<FakeOptions>("a").Configure(o => o.Message = "a").Validate(o => true).ValidateOnStart();
            services.AddOptions<FakeOptions>("b").Configure(o => o.Message = "b").Validate(o => true).ValidateOnStart();

            ServiceProvider sp = services.BuildServiceProvider();

            Assert.Single(sp.GetServices<IAsyncStartupValidator>());
        }

        [Fact]
        public void ValidateOnStart_CustomAsyncStartupValidator_CoexistsWithBuiltInInEnumerable()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IAsyncStartupValidator>(new TrackingAsyncStartupValidator());
            services.AddOptions<FakeOptions>().Configure(o => o.Message = "test").Validate(o => true).ValidateOnStart();

            ServiceProvider sp = services.BuildServiceProvider();

            // A custom async startup validator (a different implementation type) coexists with the built-in one.
            Assert.Equal(2, sp.GetServices<IAsyncStartupValidator>().Count());
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
        public void SyncOnlyValidatedOptions_SyncAccessorsBehaviorUnchanged()
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

        [Fact]
        public async Task AsyncValidatedOptions_IOptionsValue_ThrowsBeforeStartupAndServesSeededValueAfter()
        {
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "validated")
                .Validate(async (FakeOptions o, CancellationToken ct) => await Task.FromResult(true), "async fail")
                .ValidateOnStart();
            using ServiceProvider sp = services.BuildServiceProvider();

            // Before startup nothing has been validated, so synchronous access fails fast (the async validator's
            // synchronous Validate is unsupported) rather than silently returning an unvalidated value.
            Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptions<FakeOptions>>().Value);

            await GetAsyncStartupValidator(sp).ValidateAsync(CancellationToken.None);

            // After startup seeds the shared cache, the singleton IOptions<T>.Value returns the validated value.
            Assert.Equal("validated", sp.GetRequiredService<IOptions<FakeOptions>>().Value.Message);
        }

        [Fact]
        public void AsyncValidatedOptions_IOptionsValue_WithoutValidateOnStart_Throws()
        {
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "validated")
                .Validate(async (FakeOptions o, CancellationToken ct) => await Task.FromResult(true), "async fail");
            using ServiceProvider sp = services.BuildServiceProvider();

            // Without ValidateOnStart nothing seeds the cache, so a synchronous read of an async-validated type
            // always fails fast; the value is never silently served unvalidated.
            Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptions<FakeOptions>>().Value);
        }

        [Fact]
        public async Task AsyncValidatedOptions_IOptionsValue_ServedFromSharedCache()
        {
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "validated")
                .Validate(async (FakeOptions o, CancellationToken ct) => await Task.FromResult(true), "async fail")
                .ValidateOnStart();
            using ServiceProvider sp = services.BuildServiceProvider();

            await GetAsyncStartupValidator(sp).ValidateAsync(CancellationToken.None);

            // IOptions<T>.Value serves the exact instance seeded into the shared monitor cache during startup,
            // rather than re-creating (and re-running the throwing synchronous Validate).
            FakeOptions fromOptions = sp.GetRequiredService<IOptions<FakeOptions>>().Value;
            FakeOptions fromMonitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>().CurrentValue;
            Assert.Same(fromMonitor, fromOptions);
        }

        private class CustomSyncOnlyValidator : IStartupValidator
        {
            public void Validate() { }
        }

        private sealed class TrackingAsyncStartupValidator : IAsyncStartupValidator
        {
            public bool Validated { get; private set; }

            public Task ValidateAsync(CancellationToken cancellationToken = default)
            {
                Validated = true;
                return Task.CompletedTask;
            }
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
