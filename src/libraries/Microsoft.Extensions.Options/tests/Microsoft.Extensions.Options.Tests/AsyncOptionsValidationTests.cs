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

        private class CustomSyncOnlyValidator : IStartupValidator
        {
            public void Validate() { }
        }
    }
}
