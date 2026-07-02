// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Options.Tests
{
    public class AsyncOptionsValidationTests
    {
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
            var validator = sp.GetRequiredService<IAsyncStartupValidator>();

            await validator.ValidateAsync(CancellationToken.None);

            Assert.True(asyncRan);
        }

        [Fact]
        public async Task StartupValidator_TwoStage_RunsBothSyncAndAsyncValidators()
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

            // Two-stage orchestration: Host.cs calls Validate() then ValidateAsync() independently
            var syncValidator = sp.GetRequiredService<IStartupValidator>();
            syncValidator.Validate();
            Assert.True(syncRan);

            var asyncValidator = sp.GetRequiredService<IAsyncStartupValidator>();
            await asyncValidator.ValidateAsync(CancellationToken.None);
            Assert.True(asyncRan);
        }

        [Fact]
        public async Task StartupValidator_TwoStage_SyncFailureSkipsAsyncValidators()
        {
            var services = new ServiceCollection();
            bool asyncRan = false;

            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "test")
                .Validate(o => false, "sync validation failed")
                .Validate(async (FakeOptions o, CancellationToken ct) =>
                {
                    asyncRan = true;
                    return await Task.FromResult(true);
                }, "async")
                .ValidateOnStart();

            ServiceProvider sp = services.BuildServiceProvider();

            // Stage 1: Sync throws — in Host.cs, this prevents reaching Stage 2
            var syncValidator = sp.GetRequiredService<IStartupValidator>();
            Assert.Throws<OptionsValidationException>(() => syncValidator.Validate());

            // Stage 2 is never reached because the exception propagates.
            // Verify async didn't run (simulating Host.cs short-circuit behavior).
            Assert.False(asyncRan);
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
            var validator = sp.GetRequiredService<IAsyncStartupValidator>();

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
            var validator = sp.GetRequiredService<IAsyncStartupValidator>();

            OptionsValidationException ex = await Assert.ThrowsAsync<OptionsValidationException>(
                () => validator.ValidateAsync(CancellationToken.None));
            Assert.Contains("async validation failed", ex.Failures);
        }

        [Fact]
        public async Task ValidateOnStart_CustomSyncOnlyValidator_DoesNotThrowInvalidCast()
        {
            var services = new ServiceCollection();

            // Register a custom IStartupValidator that does NOT implement IAsyncStartupValidator
            services.AddSingleton<IStartupValidator>(new CustomSyncOnlyValidator());

            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "test")
                .Validate(async (FakeOptions o, CancellationToken ct) => await Task.FromResult(true), "async")
                .ValidateOnStart();

            ServiceProvider sp = services.BuildServiceProvider();

            // Should NOT throw InvalidCastException — IAsyncStartupValidator gets its own StartupValidator instance
            var asyncValidator = sp.GetRequiredService<IAsyncStartupValidator>();
            await asyncValidator.ValidateAsync(CancellationToken.None);
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
            var validator = sp.GetRequiredService<IAsyncStartupValidator>();

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
            var validator = sp.GetRequiredService<IAsyncStartupValidator>();

            AggregateException ex = await Assert.ThrowsAsync<AggregateException>(() => validator.ValidateAsync());
            Assert.Equal(2, ex.InnerExceptions.Count);
            Assert.All(ex.InnerExceptions, e => Assert.IsType<OptionsValidationException>(e));
        }

        [Fact]
        public void OptionsValue_WithAsyncOnlyValidatorAndNoValidatedValue_Throws()
        {
            var services = new ServiceCollection();
            bool asyncRan = false;

            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "test")
                .Validate(async (FakeOptions o, CancellationToken ct) =>
                {
                    asyncRan = true;
                    return await Task.FromResult(true);
                }, "async fail");

            using ServiceProvider sp = services.BuildServiceProvider();

            OptionsValidationException ex = Assert.Throws<OptionsValidationException>(
                () => sp.GetRequiredService<IOptions<FakeOptions>>().Value);

            Assert.Contains("Asynchronous validation is registered", ex.Failures.Single());
            Assert.False(asyncRan);
        }

        [Fact]
        public void OptionsValue_ReturnsValueAfterMonitorRunsAsyncValidation()
        {
            var services = new ServiceCollection();
            int asyncRuns = 0;

            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "validated")
                .Validate(async (FakeOptions o, CancellationToken ct) =>
                {
                    asyncRuns++;
                    return await Task.FromResult(true);
                }, "async fail");

            using ServiceProvider sp = services.BuildServiceProvider();

            FakeOptions monitorValue = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>().CurrentValue;
            FakeOptions optionsValue = sp.GetRequiredService<IOptions<FakeOptions>>().Value;

            Assert.Equal("validated", monitorValue.Message);
            Assert.Same(monitorValue, optionsValue);
            Assert.Equal(1, asyncRuns);
        }

        [Fact]
        public void OptionsFactory_WithAsyncOnlyValidator_ThrowsInsteadOfSkipping()
        {
            var services = new ServiceCollection();
            bool asyncRan = false;

            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "test")
                .Validate(async (FakeOptions o, CancellationToken ct) =>
                {
                    asyncRan = true;
                    return await Task.FromResult(true);
                }, "async fail");

            using ServiceProvider sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();

            OptionsValidationException ex = Assert.Throws<OptionsValidationException>(() => factory.Create(Options.DefaultName));

            Assert.Contains("Asynchronous validation is registered", ex.Failures.Single());
            Assert.False(asyncRan);
        }

        [Fact]
        public void OptionsFactory_AsyncValidatorApplicabilityCheckIsCached()
        {
            var services = new ServiceCollection();
            var observer = new AsyncValidationObserver();

            services.AddSingleton(observer);
            services.AddScoped<AsyncValidationDependency>();
            services.AddOptions<FakeOptions>()
                .Validate<AsyncValidationDependency>(
                    static (_, dependency, _) =>
                    {
                        dependency.Call();
                        return Task.FromResult(true);
                    });

            using ServiceProvider sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();

            Assert.Throws<OptionsValidationException>(() => factory.Create(Options.DefaultName));
            Assert.Throws<OptionsValidationException>(() => factory.Create(Options.DefaultName));

            Assert.Equal(0, observer.ValidationCount);
            Assert.Equal(1, observer.DisposeCount);
        }

        [Fact]
        public void OptionsSnapshot_WithAsyncOnlyValidator_ThrowsInsteadOfSkipping()
        {
            var services = new ServiceCollection();
            bool asyncRan = false;

            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "test")
                .Validate(async (FakeOptions o, CancellationToken ct) =>
                {
                    asyncRan = true;
                    return await Task.FromResult(true);
                }, "async fail");

            using ServiceProvider sp = services.BuildServiceProvider();
            using IServiceScope scope = sp.CreateScope();

            OptionsValidationException ex = Assert.Throws<OptionsValidationException>(
                () => scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Value);

            Assert.Contains("Asynchronous validation is registered", ex.Failures.Single());
            Assert.False(asyncRan);
        }

        [Fact]
        public void NamedAsyncOnlyValidator_DoesNotBlockDefaultSyncAccess()
        {
            var services = new ServiceCollection();

            services.Configure<FakeOptions>(o => o.Message = "default");
            services.AddOptions<FakeOptions>("named")
                .Configure(o => o.Message = "named")
                .Validate(async (FakeOptions o, CancellationToken ct) => await Task.FromResult(true), "async fail");

            using ServiceProvider sp = services.BuildServiceProvider();

            Assert.Equal("default", sp.GetRequiredService<IOptions<FakeOptions>>().Value.Message);

            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            OptionsValidationException ex = Assert.Throws<OptionsValidationException>(() => factory.Create("named"));
            Assert.Contains("Asynchronous validation is registered", ex.Failures.Single());
        }

        [Fact]
        public void ValidatorWithSyncPath_DoesNotBlockSyncAccess()
        {
            var services = new ServiceCollection();
            var validator = new SyncAndAsyncValidator();

            services.Configure<FakeOptions>(o => o.Message = "test");
            services.AddSingleton<IValidateOptions<FakeOptions>>(validator);
            services.AddSingleton<IAsyncValidateOptions<FakeOptions>>(validator);
            services.AddOptions<FakeOptions>();

            using ServiceProvider sp = services.BuildServiceProvider();

            Assert.Equal("test", sp.GetRequiredService<IOptions<FakeOptions>>().Value.Message);
            Assert.Equal(1, validator.SyncValidationCount);
            Assert.Equal(0, validator.AsyncValidationCount);
        }

        [Fact]
        public void SyncGuardSuppression_DoesNotSuppressUnrelatedOptionsType()
        {
            var services = new ServiceCollection();

            services.AddOptions<FakeOptions>()
                .Configure<IOptionsFactory<OtherOptions>>((options, factory) => { factory.Create(Options.DefaultName); })
                .Validate(async (FakeOptions options, CancellationToken cancellationToken) => await Task.FromResult(true), "async fail");
            services.AddOptions<OtherOptions>()
                .Validate(async (OtherOptions options, CancellationToken cancellationToken) => await Task.FromResult(true), "async fail");

            using ServiceProvider sp = services.BuildServiceProvider();

            OptionsValidationException ex = Assert.Throws<OptionsValidationException>(
                () => sp.GetRequiredService<IOptionsMonitor<FakeOptions>>().CurrentValue);

            Assert.Contains(nameof(OtherOptions), ex.Failures.Single());
        }

        [Fact]
        public async Task ValidateOnStart_AsyncValidationUsesSyncValidatedInstance()
        {
            var services = new ServiceCollection();
            int configureCount = 0;
            FakeOptions? syncValidatedOptions = null;
            FakeOptions? asyncValidatedOptions = null;

            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = (++configureCount).ToString())
                .Validate(o =>
                {
                    syncValidatedOptions = o;
                    return true;
                }, "sync fail")
                .Validate(async (FakeOptions o, CancellationToken cancellationToken) =>
                {
                    asyncValidatedOptions = o;
                    return await Task.FromResult(true);
                }, "async fail")
                .ValidateOnStart();

            using ServiceProvider sp = services.BuildServiceProvider();

            sp.GetRequiredService<IStartupValidator>().Validate();
            await sp.GetRequiredService<IAsyncStartupValidator>().ValidateAsync();

            Assert.Same(syncValidatedOptions, asyncValidatedOptions);
            Assert.Equal(1, configureCount);
        }

        private class CustomSyncOnlyValidator : IStartupValidator
        {
            public void Validate() { }
        }

        private sealed class SyncAndAsyncValidator : IValidateOptions<FakeOptions>, IAsyncValidateOptions<FakeOptions>
        {
            public int SyncValidationCount { get; private set; }

            public int AsyncValidationCount { get; private set; }

            public ValidateOptionsResult Validate(string? name, FakeOptions options)
            {
                SyncValidationCount++;
                return ValidateOptionsResult.Success;
            }

            public Task<ValidateOptionsResult> ValidateAsync(string? name, FakeOptions options, CancellationToken cancellationToken = default)
            {
                AsyncValidationCount++;
                return Task.FromResult(ValidateOptionsResult.Success);
            }
        }

        private sealed class OtherOptions
        {
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
    }
}
