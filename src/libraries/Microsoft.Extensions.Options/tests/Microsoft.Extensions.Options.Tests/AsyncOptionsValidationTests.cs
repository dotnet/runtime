// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Options.Tests
{
    public class AsyncOptionsValidationTests
    {
        private static IAsyncStartupValidator GetAsyncStartupValidator(IServiceProvider sp) =>
            sp.GetRequiredService<IAsyncStartupValidator>();

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
            IValidateOptions<FakeOptions> registered =
                Assert.Single(sp.GetServices<IValidateOptions<FakeOptions>>());
            IAsyncStartupValidator validator = GetAsyncStartupValidator(sp);

            Assert.IsType<AsyncValidateOptions<FakeOptions>>(registered);
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
#pragma warning disable SYSLIB0066 // Tests the legacy IStartupValidator compatibility contract.
            var services = new ServiceCollection();

            // A custom sync-only IStartupValidator registered before ValidateOnStart wins the compatibility
            // registration, so it remains the resolved IStartupValidator.
            services.AddSingleton<IStartupValidator>(new CustomSyncOnlyValidator());

            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "test")
                .Validate(async (FakeOptions o, CancellationToken ct) => await Task.FromResult(true), "async")
                .ValidateOnStart();

            ServiceProvider sp = services.BuildServiceProvider();

            // The custom validator is not async-capable, so the host uses the legacy synchronous path.
            IStartupValidator validator = sp.GetRequiredService<IStartupValidator>();
            Assert.IsType<CustomSyncOnlyValidator>(validator);
            Assert.False(validator is IAsyncStartupValidator);
            validator.Validate();
#pragma warning restore SYSLIB0066
        }

        [Fact]
        public void ValidateOnStart_RegistersBuiltInValidatorAsBothInterfaces()
        {
#pragma warning disable SYSLIB0066 // Tests the legacy IStartupValidator compatibility contract.
            var services = new ServiceCollection();

            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "test")
                .Validate(o => true)
                .ValidateOnStart();

            ServiceProvider sp = services.BuildServiceProvider();

            IStartupValidator syncValidator = sp.GetRequiredService<IStartupValidator>();
            IAsyncStartupValidator asyncValidator = Assert.Single(sp.GetServices<IAsyncStartupValidator>());
            Assert.Same(syncValidator, asyncValidator);
#pragma warning restore SYSLIB0066
        }

        [Fact]
        public void ValidateOnStart_CalledMultipleTimes_RegistersSingleAsyncStartupValidator()
        {
#pragma warning disable SYSLIB0066 // Tests the legacy IStartupValidator compatibility contract.
            var services = new ServiceCollection();

            services.AddOptions<FakeOptions>("a").Configure(o => o.Message = "a").Validate(o => true).ValidateOnStart();
            services.AddOptions<FakeOptions>("b").Configure(o => o.Message = "b").Validate(o => true).ValidateOnStart();

            ServiceProvider sp = services.BuildServiceProvider();

            IAsyncStartupValidator asyncValidator = Assert.Single(sp.GetServices<IAsyncStartupValidator>());
            Assert.Same(sp.GetRequiredService<IStartupValidator>(), asyncValidator);
#pragma warning restore SYSLIB0066
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
        public async Task AddOptionsWithValidateOnStart_AsyncValidatorRegistrationIsIdempotent()
        {
            var services = new ServiceCollection();

            services.AddOptionsWithValidateOnStart<FakeOptions, CountingAsyncValidator>("one");
            services.AddOptionsWithValidateOnStart<FakeOptions, CountingAsyncValidator>("one");
            services.AddOptionsWithValidateOnStart<FakeOptions, CountingAsyncValidator>("two");

            using ServiceProvider sp = services.BuildServiceProvider();
            CountingAsyncValidator validator = Assert.IsType<CountingAsyncValidator>(
                Assert.Single(sp.GetServices<IValidateOptions<FakeOptions>>()));

            await GetAsyncStartupValidator(sp).ValidateAsync(CancellationToken.None);

            Assert.Equal(0, validator.SyncCalls);
            Assert.Equal(2, validator.AsyncCalls);
        }

        [Fact]
        public async Task AddOptionsWithValidateOnStart_DoesNotDuplicateExistingAsyncValidatorRegistration()
        {
            var validator = new CountingAsyncValidator();
            var services = new ServiceCollection();
            services.AddSingleton<IValidateOptions<FakeOptions>>(validator);
            services.AddOptionsWithValidateOnStart<FakeOptions, CountingAsyncValidator>("named");

            using ServiceProvider sp = services.BuildServiceProvider();

            Assert.Same(validator, Assert.Single(sp.GetServices<IValidateOptions<FakeOptions>>()));
            await GetAsyncStartupValidator(sp).ValidateAsync(CancellationToken.None);

            Assert.Equal(0, validator.SyncCalls);
            Assert.Equal(1, validator.AsyncCalls);
        }

        [Fact]
        public void AddOptionsWithValidateOnStart_AsyncValidatorAppliesToOtherNames()
        {
            var services = new ServiceCollection();
            services.AddOptionsWithValidateOnStart<FakeOptions, RejectingAsyncValidator>();
            services.AddOptions<FakeOptions>("other");

            using ServiceProvider sp = services.BuildServiceProvider();

            Assert.Throws<OptionsValidationException>(
                () => sp.GetRequiredService<IOptionsMonitor<FakeOptions>>().Get("other"));
        }

        [Fact]
        public async Task AddOptionsWithValidateOnStart_PreRegisteredAsyncValidatorRemainsConservative()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IValidateOptions<FakeOptions>>(new CountingAsyncValidator());
            services.AddOptionsWithValidateOnStart<FakeOptions, CountingAsyncValidator>("named");
            services.AddOptions<FakeOptions>().ValidateOnStart();
            services.AddSingleton<IOptions<FakeOptions>>(Options.Create(new FakeOptions()));

            using ServiceProvider sp = services.BuildServiceProvider();

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => GetAsyncStartupValidator(sp).ValidateAsync(CancellationToken.None));

            Assert.Contains(typeof(FakeOptions).ToString(), error.Message);
            Assert.Contains(typeof(OptionsWrapper<FakeOptions>).ToString(), error.Message);
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
        public async Task ValidateWithValidatorType_PreservesAsyncCapability()
        {
            var services = new ServiceCollection();

            services.AddOptions<FakeOptions>()
                .Validate<AsyncValidator>()
                .ValidateOnStart();

            using ServiceProvider sp = services.BuildServiceProvider();

            await GetAsyncStartupValidator(sp).ValidateAsync();
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

            // After startup seeds the singleton slot, IOptions<T>.Value returns the validated value.
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

            // Without ValidateOnStart nothing seeds the singleton slot, so a synchronous read of an async-validated type
            // always fails fast; the value is never silently served unvalidated.
            Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptions<FakeOptions>>().Value);
        }

        [Fact]
        public async Task AsyncOnlyValidation_PoisonedPreStartCacheRemainsFaulted()
        {
            FakeOptions? startupCandidate = null;
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "validated")
                .Validate((FakeOptions o, CancellationToken ct) =>
                {
                    startupCandidate = o;
                    return Task.FromResult(true);
                }, "async fail")
                .ValidateOnStart();
            using ServiceProvider sp = services.BuildServiceProvider();

            IOptions<FakeOptions> options = sp.GetRequiredService<IOptions<FakeOptions>>();
            IOptionsMonitor<FakeOptions> monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();

            Assert.Throws<OptionsValidationException>(() => options.Value);
            Assert.Throws<OptionsValidationException>(() => monitor.Get(Options.DefaultName));

            await Assert.ThrowsAsync<OptionsValidationException>(
                () => GetAsyncStartupValidator(sp).ValidateAsync(CancellationToken.None));

            Assert.NotNull(startupCandidate);
            Assert.Throws<OptionsValidationException>(() => options.Value);
            Assert.Throws<OptionsValidationException>(() => monitor.Get(Options.DefaultName));
        }

        [Fact]
        public async Task AsyncOnlyValidation_IOptionsSnapshotRemainsUnsupportedAfterStartup()
        {
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "validated")
                .Validate((FakeOptions o, CancellationToken ct) => Task.FromResult(true), "async fail")
                .ValidateOnStart();
            using ServiceProvider sp = services.BuildServiceProvider();

            using (IServiceScope scope = sp.CreateScope())
            {
                OptionsValidationException beforeStartupError = Assert.Throws<OptionsValidationException>(
                    () => scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Value);
                AssertAsyncOnlySnapshotFailure(beforeStartupError);
            }

            await GetAsyncStartupValidator(sp).ValidateAsync(CancellationToken.None);

            using IServiceScope newScope = sp.CreateScope();
            OptionsValidationException afterStartupError = Assert.Throws<OptionsValidationException>(
                () => newScope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Value);
            AssertAsyncOnlySnapshotFailure(afterStartupError);
        }

        [Fact]
        public async Task AsyncOnlyValidation_StartupFirstSeedsExactInstance()
        {
            FakeOptions? startupCandidate = null;
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "validated")
                .Validate((FakeOptions o, CancellationToken ct) =>
                {
                    startupCandidate = o;
                    return Task.FromResult(true);
                }, "async fail")
                .ValidateOnStart();
            using ServiceProvider sp = services.BuildServiceProvider();

            await GetAsyncStartupValidator(sp).ValidateAsync(CancellationToken.None);

            Assert.NotNull(startupCandidate);
            Assert.Same(startupCandidate, sp.GetRequiredService<IOptions<FakeOptions>>().Value);
            Assert.Same(startupCandidate, sp.GetRequiredService<IOptionsMonitor<FakeOptions>>().CurrentValue);
        }

        [Fact]
        public async Task BothCapableValidator_PreStartIOptionsValueRemainsWinnerAndAsyncValidationRuns()
        {
            int configureCalls = 0;
            var validator = new CountingAsyncValidator();
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = (++configureCalls).ToString())
                .ValidateOnStart();
            services.AddSingleton<IValidateOptions<FakeOptions>>(validator);
            using ServiceProvider sp = services.BuildServiceProvider();

            IOptions<FakeOptions> options = sp.GetRequiredService<IOptions<FakeOptions>>();
            IOptionsMonitor<FakeOptions> monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            FakeOptions preStartWinner = options.Value;

            await GetAsyncStartupValidator(sp).ValidateAsync(CancellationToken.None);

            using (IServiceScope scope = sp.CreateScope())
            {
                _ = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<FakeOptions>>().Value;
            }

            Assert.Equal(3, configureCalls);
            Assert.Equal(2, validator.SyncCalls);
            Assert.Equal(1, validator.AsyncCalls);
            Assert.Same(preStartWinner, options.Value);
            Assert.Same(preStartWinner, monitor.CurrentValue);
        }

        [Fact]
        public async Task BothCapableValidator_ConcurrentMonitorValueRemainsWinner()
        {
            const string OptionsName = "named";
            var validator = new BlockingAsyncValidator();
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>(OptionsName)
                .Configure(o => o.Message = "configured")
                .ValidateOnStart();
            services.AddSingleton<IValidateOptions<FakeOptions>>(validator);
            using ServiceProvider sp = services.BuildServiceProvider();

            IOptionsMonitor<FakeOptions> monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            Task startupValidation = GetAsyncStartupValidator(sp).ValidateAsync(CancellationToken.None);
            await validator.ValidationStarted;

            FakeOptions concurrentWinner = monitor.Get(OptionsName);
            validator.ContinueValidation();
            await startupValidation;

            Assert.NotSame(validator.AsyncCandidate, concurrentWinner);
            Assert.Same(concurrentWinner, monitor.Get(OptionsName));
        }

        [Fact]
        public async Task BothCapableValidator_PreStartMonitorValueSeedsIOptions()
        {
            int configureCalls = 0;
            var validator = new CountingAsyncValidator();
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>()
                .Configure(_ => configureCalls++)
                .ValidateOnStart();
            services.AddSingleton<IValidateOptions<FakeOptions>>(validator);
            using ServiceProvider sp = services.BuildServiceProvider();

            IOptions<FakeOptions> options = sp.GetRequiredService<IOptions<FakeOptions>>();
            IOptionsMonitor<FakeOptions> monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            FakeOptions preStartWinner = monitor.CurrentValue;

            await GetAsyncStartupValidator(sp).ValidateAsync(CancellationToken.None);

            Assert.Equal(2, configureCalls);
            Assert.Equal(1, validator.SyncCalls);
            Assert.Equal(1, validator.AsyncCalls);
            Assert.Same(preStartWinner, monitor.CurrentValue);
            Assert.Same(preStartWinner, options.Value);
        }

        private static void AssertAsyncOnlySnapshotFailure(OptionsValidationException error)
        {
            string failure = Assert.Single(error.Failures);
            Assert.Contains("IOptionsSnapshot<TOptions>", failure);
            Assert.Contains("cannot execute or await ValidateAsync", failure);
            Assert.Contains("not populated by startup validation", failure);
        }

        [Fact]
        public async Task AsyncStartupValidation_CustomOptionsImplementation_ThrowsInvalidOperationException()
        {
            bool asyncValidationCalled = false;
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>()
                .Validate((FakeOptions o, CancellationToken ct) =>
                {
                    asyncValidationCalled = true;
                    return Task.FromResult(true);
                }, "async fail")
                .ValidateOnStart();
            services.AddSingleton<IOptions<FakeOptions>>(Options.Create(new FakeOptions()));
            using ServiceProvider sp = services.BuildServiceProvider();

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => GetAsyncStartupValidator(sp).ValidateAsync(CancellationToken.None));

            Assert.False(asyncValidationCalled);
            Assert.Contains(typeof(FakeOptions).ToString(), error.Message);
            Assert.Contains(typeof(OptionsWrapper<FakeOptions>).ToString(), error.Message);
        }

        [Fact]
        public async Task StartupValidator_UnexpectedExceptionAfterValidationFailure_Aggregates()
        {
            var sequenceValidator = new SequencedAsyncValidator(cancelSecondCall: false);
            var services = new ServiceCollection();
            services.AddSingleton<IValidateOptions<FakeOptions>>(sequenceValidator);
            services.AddOptions<FakeOptions>("one").ValidateOnStart();
            services.AddOptions<FakeOptions>("two").ValidateOnStart();
            services.AddOptions<FakeOptions>("three").ValidateOnStart();
            using ServiceProvider sp = services.BuildServiceProvider();

            AggregateException error = await Assert.ThrowsAsync<AggregateException>(
                () => GetAsyncStartupValidator(sp).ValidateAsync(CancellationToken.None));

            Assert.Equal(2, error.InnerExceptions.Count);
            Assert.Contains(error.InnerExceptions, exception => exception is OptionsValidationException);
            Assert.Contains(error.InnerExceptions, exception => exception is InvalidOperationException);
            Assert.Equal(2, sequenceValidator.AsyncCalls);
        }

        [Fact]
        public async Task StartupValidator_CancellationAfterValidationFailure_Propagates()
        {
            var sequenceValidator = new SequencedAsyncValidator(cancelSecondCall: true);
            var services = new ServiceCollection();
            services.AddSingleton<IValidateOptions<FakeOptions>>(sequenceValidator);
            services.AddOptions<FakeOptions>("one").ValidateOnStart();
            services.AddOptions<FakeOptions>("two").ValidateOnStart();
            services.AddOptions<FakeOptions>("three").ValidateOnStart();
            using ServiceProvider sp = services.BuildServiceProvider();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => GetAsyncStartupValidator(sp).ValidateAsync(cts.Token));
            Assert.Equal(2, sequenceValidator.AsyncCalls);
        }

        [Theory]
        [InlineData(NamedAsyncRegistration.Delegate)]
        [InlineData(NamedAsyncRegistration.ValidatorType)]
        [InlineData(NamedAsyncRegistration.DataAnnotations)]
        public async Task NamedAsyncValidation_DoesNotRequireBuiltInDefaultOptions(NamedAsyncRegistration registration)
        {
            const string NamedOptions = "named";
            var services = new ServiceCollection();
            services.Configure<FakeOptions>(_ => { });

            OptionsBuilder<FakeOptions> namedBuilder = services.AddOptions<FakeOptions>(NamedOptions);
            switch (registration)
            {
                case NamedAsyncRegistration.Delegate:
                    namedBuilder.Validate((_, _) => Task.FromResult(true));
                    break;
                case NamedAsyncRegistration.ValidatorType:
                    namedBuilder.Validate<AsyncValidator>();
                    break;
                case NamedAsyncRegistration.DataAnnotations:
                    namedBuilder.ValidateDataAnnotations();
                    break;
            }

            services.AddOptions<FakeOptions>().ValidateOnStart();
            services.AddSingleton<IOptions<FakeOptions>>(Options.Create(new FakeOptions()));

            using ServiceProvider sp = services.BuildServiceProvider();

            await GetAsyncStartupValidator(sp).ValidateAsync(CancellationToken.None);
        }

        [Fact]
        public async Task AsyncStartupValidation_DerivedFactoryUsesSynchronousFallback()
        {
            var validator = new CountingAsyncValidator();
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>()
                .ValidateOnStart();
            services.AddSingleton<IValidateOptions<FakeOptions>>(validator);
            services.AddSingleton<IOptionsFactory<FakeOptions>, DerivedOptionsFactory<FakeOptions>>();
            using ServiceProvider sp = services.BuildServiceProvider();

            await GetAsyncStartupValidator(sp).ValidateAsync(CancellationToken.None);

            Assert.Equal(1, validator.SyncCalls);
            Assert.Equal(0, validator.AsyncCalls);
        }

        [Fact]
        public async Task FailedAsyncStartupValidation_DoesNotSeedOptions()
        {
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "invalid")
                .Validate((FakeOptions o, CancellationToken ct) => Task.FromResult(false), "async fail")
                .ValidateOnStart();
            using ServiceProvider sp = services.BuildServiceProvider();

            await Assert.ThrowsAsync<OptionsValidationException>(
                () => GetAsyncStartupValidator(sp).ValidateAsync(CancellationToken.None));

            Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptions<FakeOptions>>().Value);
            Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptionsMonitor<FakeOptions>>().CurrentValue);
        }

        [Fact]
        public async Task IOptionsValue_RemainsStableAfterMonitorCacheEviction()
        {
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "validated")
                .Validate((FakeOptions o, CancellationToken ct) => Task.FromResult(true), "async fail")
                .ValidateOnStart();
            using ServiceProvider sp = services.BuildServiceProvider();

            await GetAsyncStartupValidator(sp).ValidateAsync(CancellationToken.None);

            IOptions<FakeOptions> options = sp.GetRequiredService<IOptions<FakeOptions>>();
            IOptionsMonitor<FakeOptions> monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            IOptionsMonitorCache<FakeOptions> sharedCache = sp.GetRequiredService<IOptionsMonitorCache<FakeOptions>>();
            FakeOptions winner = options.Value;

            Assert.True(sharedCache.TryRemove(Options.DefaultName));
            Assert.Same(winner, options.Value);
            Assert.Throws<OptionsValidationException>(() => monitor.CurrentValue);
        }

        [Fact]
        public async Task ConfigurationReload_WhenAsyncValidatorRequiresAsyncValidation_ThrowsAndLeavesFaultedCache()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [nameof(FakeOptions.Message)] = "startup",
                })
                .Build();
            using IDisposable configurationDisposable = (IDisposable)configuration;

            var services = new ServiceCollection();
            services.Configure<FakeOptions>(configuration);
            services.AddOptions<FakeOptions>()
                .Validate((_, _) => Task.FromResult(true))
                .ValidateOnStart();

            using ServiceProvider sp = services.BuildServiceProvider();
            IOptionsMonitor<FakeOptions> monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            IOptionsMonitorCache<FakeOptions> cache = sp.GetRequiredService<IOptionsMonitorCache<FakeOptions>>();
            int notifications = 0;
            using IDisposable subscription = monitor.OnChange(_ => notifications++);

            await GetAsyncStartupValidator(sp).ValidateAsync(CancellationToken.None);
            Assert.Equal("startup", monitor.CurrentValue.Message);

            configuration[nameof(FakeOptions.Message)] = "reload";
            AggregateException exception = Assert.Throws<AggregateException>(() => configuration.Reload());
            OptionsValidationException validationException =
                Assert.IsType<OptionsValidationException>(Assert.Single(exception.InnerExceptions));

            Assert.Equal(0, notifications);
            Assert.Same(validationException, Assert.Throws<OptionsValidationException>(() => monitor.CurrentValue));
            Assert.False(cache.TryAdd(Options.DefaultName, new FakeOptions()));
        }

        [Fact]
        public async Task NamedAsyncOptions_StartupPublishesExactCandidate()
        {
            var startupCandidates = new Dictionary<string, FakeOptions>();
            var services = new ServiceCollection();
            foreach (string name in new[] { "one", "two" })
            {
                services.AddOptions<FakeOptions>(name)
                    .Configure(o => o.Message = name)
                    .Validate((FakeOptions o, CancellationToken ct) =>
                    {
                        startupCandidates[name] = o;
                        return Task.FromResult(true);
                    }, "async fail")
                    .ValidateOnStart();
            }

            using ServiceProvider sp = services.BuildServiceProvider();

            await GetAsyncStartupValidator(sp).ValidateAsync(CancellationToken.None);

            IOptionsMonitor<FakeOptions> monitor = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>();
            Assert.Equal(2, startupCandidates.Count);
            Assert.Same(startupCandidates["one"], monitor.Get("one"));
            Assert.Same(startupCandidates["two"], monitor.Get("two"));
        }

        [Fact]
        public async Task AsyncStartupValidation_CustomCachePublishesExactCandidate()
        {
            FakeOptions? startupCandidate = null;
            var customCache = new DelegatingOptionsCache<FakeOptions>();
            var services = new ServiceCollection();
            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "validated")
                .Validate((FakeOptions o, CancellationToken ct) =>
                {
                    startupCandidate = o;
                    return Task.FromResult(true);
                }, "async fail")
                .ValidateOnStart();
            services.AddSingleton<IOptionsMonitorCache<FakeOptions>>(customCache);
            using ServiceProvider sp = services.BuildServiceProvider();

            await GetAsyncStartupValidator(sp).ValidateAsync(CancellationToken.None);

            Assert.NotNull(startupCandidate);
            Assert.True(customCache.TryGetValue(Options.DefaultName, out FakeOptions? cached));
            Assert.Same(startupCandidate, cached);
            Assert.Same(startupCandidate, sp.GetRequiredService<IOptions<FakeOptions>>().Value);
            Assert.Same(startupCandidate, sp.GetRequiredService<IOptionsMonitor<FakeOptions>>().CurrentValue);
        }

        public enum NamedAsyncRegistration
        {
            Delegate,
            ValidatorType,
            DataAnnotations,
        }

#pragma warning disable SYSLIB0066 // Tests the legacy IStartupValidator compatibility contract.
        private class CustomSyncOnlyValidator : IStartupValidator
#pragma warning restore SYSLIB0066
        {
            public void Validate() { }
        }

        private sealed class DelegatingOptionsCache<T> : IOptionsMonitorCache<T> where T : class
        {
            private readonly ConcurrentDictionary<string, T> _cache = new(StringComparer.Ordinal);

            public T GetOrAdd(string? name, Func<T> createOptions) =>
                _cache.GetOrAdd(name ?? Options.DefaultName, _ => createOptions());

            public bool TryGetValue(string? name, out T options) =>
                _cache.TryGetValue(name ?? Options.DefaultName, out options!);

            public bool TryAdd(string? name, T options) => _cache.TryAdd(name ?? Options.DefaultName, options);

            public bool TryRemove(string? name) => _cache.TryRemove(name ?? Options.DefaultName, out _);

            public void Clear() => _cache.Clear();
        }

        private sealed class DerivedOptionsFactory<T> : OptionsFactory<T> where T : class
        {
            public DerivedOptionsFactory(
                IEnumerable<IConfigureOptions<T>> setups,
                IEnumerable<IPostConfigureOptions<T>> postConfigures,
                IEnumerable<IValidateOptions<T>> validations)
                : base(setups, postConfigures, validations)
            {
            }
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

        private sealed class CountingAsyncValidator : IAsyncValidateOptions<FakeOptions>
        {
            public int SyncCalls { get; private set; }
            public int AsyncCalls { get; private set; }

            public ValidateOptionsResult Validate(string? name, FakeOptions options)
            {
                SyncCalls++;
                return ValidateOptionsResult.Success;
            }

            public Task<ValidateOptionsResult> ValidateAsync(
                string? name,
                FakeOptions options,
                CancellationToken cancellationToken = default)
            {
                AsyncCalls++;
                return Task.FromResult(ValidateOptionsResult.Success);
            }
        }

        private sealed class BlockingAsyncValidator : IAsyncValidateOptions<FakeOptions>
        {
            private readonly TaskCompletionSource<object?> _continueValidation =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly TaskCompletionSource<object?> _validationStarted =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            public FakeOptions? AsyncCandidate { get; private set; }

            public Task ValidationStarted => _validationStarted.Task;

            public void ContinueValidation() => _continueValidation.SetResult(null);

            public ValidateOptionsResult Validate(string? name, FakeOptions options) =>
                ValidateOptionsResult.Success;

            public async Task<ValidateOptionsResult> ValidateAsync(
                string? name,
                FakeOptions options,
                CancellationToken cancellationToken = default)
            {
                AsyncCandidate = options;
                _validationStarted.SetResult(null);
                await _continueValidation.Task.ConfigureAwait(false);
                return ValidateOptionsResult.Success;
            }
        }

        private sealed class RejectingAsyncValidator : IAsyncValidateOptions<FakeOptions>
        {
            public ValidateOptionsResult Validate(string? name, FakeOptions options) =>
                ValidateOptionsResult.Fail("rejected");

            public Task<ValidateOptionsResult> ValidateAsync(
                string? name,
                FakeOptions options,
                CancellationToken cancellationToken = default) =>
                Task.FromResult(ValidateOptionsResult.Fail("rejected"));
        }

        private sealed class SequencedAsyncValidator : IAsyncValidateOptions<FakeOptions>
        {
            private readonly bool _cancelSecondCall;

            public SequencedAsyncValidator(bool cancelSecondCall) =>
                _cancelSecondCall = cancelSecondCall;

            public int AsyncCalls { get; private set; }

            public ValidateOptionsResult Validate(string? name, FakeOptions options) =>
                ValidateOptionsResult.Success;

            public Task<ValidateOptionsResult> ValidateAsync(
                string? name,
                FakeOptions options,
                CancellationToken cancellationToken = default)
            {
                AsyncCalls++;

                if (AsyncCalls == 1)
                {
                    return Task.FromResult(ValidateOptionsResult.Fail("validation failed"));
                }

                if (AsyncCalls == 2)
                {
                    if (_cancelSecondCall)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    return Task.FromException<ValidateOptionsResult>(
                        new InvalidOperationException("infrastructure failed"));
                }

                return Task.FromResult(ValidateOptionsResult.Success);
            }
        }

        private sealed class AsyncValidator : IAsyncValidateOptions<FakeOptions>
        {
            public ValidateOptionsResult Validate(string? name, FakeOptions options) =>
                throw new InvalidOperationException("Synchronous validation should not run.");

            public Task<ValidateOptionsResult> ValidateAsync(
                string? name,
                FakeOptions options,
                CancellationToken cancellationToken = default) =>
                Task.FromResult(ValidateOptionsResult.Success);
        }
    }
}
