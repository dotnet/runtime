// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public class OptionsBuilderExtensionsTests
    {
        public static IHostBuilder CreateHostBuilder(Action<IServiceCollection> configure)
        {
            return new HostBuilder().ConfigureServices(configure);
        }

        [Fact]
        public void ValidateOnStart_NullOptionsBuilder_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => OptionsBuilderExtensions.ValidateOnStart<object>(null));
        }

        [Fact]
        public async Task ValidateOnStart_ConfigureAndValidateThenCallValidateOnStart_ValidatesFailure()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddOptions<ComplexOptions>()
                    .Configure(o => o.Boolean = false)
                    .Validate(o => o.Boolean)
                    .ValidateOnStart();
            });

            using (var host = hostBuilder.Build())
            {
                var error = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
                {
                    await host.StartAsync();
                });

                ValidateFailure<ComplexOptions>(error, 1);
            }
        }

        [Fact]
        public async Task ValidateOnStart_CallFirstThenConfigureAndValidate_ValidatesFailure()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddOptions<ComplexOptions>()
                    .ValidateOnStart()
                    .Configure(o => o.Boolean = false)
                    .Validate(o => o.Boolean);
            });

            using (var host = hostBuilder.Build())
            {
                var error = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
                {
                    await host.StartAsync();
                });

                ValidateFailure<ComplexOptions>(error, 1);
            }
        }

        [Fact]
        public async Task ValidateOnStart_ErrorMessageSpecified_FailsWithCustomError()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddOptions<ComplexOptions>()
                    .Configure(o => o.Boolean = false)
                    .Validate(o => o.Boolean, "Boolean must be true.")
                    .ValidateOnStart();
            });

            using (var host = hostBuilder.Build())
            {
                var error = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
                {
                    await host.StartAsync();
                });

                ValidateFailure<ComplexOptions>(error, 1, "Boolean must be true.");
            }
        }

        internal class FakeService { }

        internal class FakeSettings
        {
            public string Name { get; set; }
        }

        [Fact]
        public async Task ValidateOnStart_NamedOptions_ValidatesFailureOnStart()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddOptions().AddSingleton(new FakeService());
                services
                    .AddOptions<FakeSettings>("named")
                    .Configure<FakeService>((o, _) =>
                    {
                        o.Name = "named";
                    })
                    .Validate(o => o.Name == null, "trigger validation failure for named option!")
                    .ValidateOnStart();
            });

            using (var host = hostBuilder.Build())
            {
                var error = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
                {
                    await host.StartAsync();
                });

                ValidateFailure<FakeSettings>(error, 1, "trigger validation failure for named option!");
            }
        }

        [Fact]
        public async Task ValidateOnStart_NamedOptions_ValidatesFailureOnStart_AddOptionsWithValidateOnStart()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddOptions().AddSingleton(new FakeService());
                services
                    .AddOptionsWithValidateOnStart<FakeSettings>("named")
                    .Configure<FakeService>((o, _) =>
                    {
                        o.Name = "named";
                    })
                    .Validate(o => o.Name == null, "trigger validation failure for named option!");
            });

            using (var host = hostBuilder.Build())
            {
                var error = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
                {
                    await host.StartAsync();
                });

                ValidateFailure<FakeSettings>(error, 1, "trigger validation failure for named option!");
            }
        }

        [Fact]
        private async Task ValidateOnStart_AddNamedOptionsMultipleTimesForSameType_BothGetTriggered()
        {
            bool firstOptionsBuilderTriggered = false;
            bool secondOptionsBuilderTriggered = false;
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddOptions<ComplexOptions>("bad_configuration1")
                    .Configure(o => o.Boolean = true)
                    .Validate(o =>
                    {
                        firstOptionsBuilderTriggered = true;
                        return o.Boolean;
                    }, "bad_configuration1")
                    .ValidateOnStart();

                services.AddOptions<ComplexOptions>("bad_configuration2")
                    .Configure(o =>
                    {
                        o.Boolean = false;
                        o.Integer = 11;
                    })
                    .Validate(o =>
                    {
                        secondOptionsBuilderTriggered = true;
                        return o.Boolean;
                    }, "Boolean")
                    .Validate(o => o.Integer > 12, "Integer")
                    .ValidateOnStart();
            });

            using (var host = hostBuilder.Build())
            {
                var error = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
                {
                    await host.StartAsync();
                });

                ValidateFailure<ComplexOptions>(error, 2, "Boolean", "Integer");
            }

            Assert.True(firstOptionsBuilderTriggered);
            Assert.True(secondOptionsBuilderTriggered);
        }

        [Fact]
        private async Task ValidateOnStart_AddEagerValidation_DoesValidationWhenHostStartsWithNoFailure()
        {
            bool validateCalled = false;

            var hostBuilder = CreateHostBuilder(services =>
            {
                // Adds eager validation using ValidateOnStart
                services.AddOptions<ComplexOptions>("correct_configuration")
                    .Configure(o => o.Boolean = true)
                    .Validate(o =>
                    {
                        validateCalled = true;
                        return o.Boolean;
                    }, "correct_configuration")
                    .ValidateOnStart();
            });

            using (var host = hostBuilder.Build())
            {
                await host.StartAsync();
            }

            Assert.True(validateCalled);
        }

        [Fact]
        private async Task ValidateOnStart_AddEagerValidation_DoesValidationWhenHostStartsWithNoFailure_AddOptionsWithValidateOnStart()
        {
            bool validateCalled = false;

            var hostBuilder = CreateHostBuilder(services =>
            {
                // Adds eager validation using ValidateOnStart
                services.AddOptionsWithValidateOnStart<ComplexOptions>("correct_configuration")
                    .Configure(o => o.Boolean = true)
                    .Validate(o =>
                    {
                        validateCalled = true;
                        return o.Boolean;
                    }, "correct_configuration");
            });

            using (var host = hostBuilder.Build())
            {
                await host.StartAsync();
            }

            Assert.True(validateCalled);
        }

        [Fact]
        private async Task CanValidateOptionsEagerly_AddOptionsWithValidateOnStart_IValidateOptions()
        {
            var hostBuilder = CreateHostBuilder(services =>
                services.AddOptionsWithValidateOnStart<ComplexOptions, ComplexOptionsValidator>()
                    .Configure(o => o.Boolean = false));

            using (var host = hostBuilder.Build())
            {
                var error = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
                {
                    await host.StartAsync();
                });

                ValidateFailure<ComplexOptions>(error, 1, "Boolean != true");
            }
        }

        private class ComplexOptionsValidator : IValidateOptions<ComplexOptions>
        {
            public ValidateOptionsResult Validate(string name, ComplexOptions options)
            {
                if (options.Boolean == true)
                {
                    return ValidateOptionsResult.Success;
                }
                return ValidateOptionsResult.Fail("Boolean != true");
            }
        }

        [Fact]
        private async Task ValidateOnStart_AddLazyValidation_SkipsValidationWhenHostStarts()
        {
            bool validateCalled = false;

            var hostBuilder = CreateHostBuilder(services =>
            {
                // Adds eager validation using ValidateOnStart
                services.AddOptions<ComplexOptions>("correct_configuration")
                    .Configure(o => o.Boolean = true)
                    .Validate(o => o.Boolean, "correct_configuration")
                    .ValidateOnStart();

                // Adds lazy validation, skipping validation on start (last options builder for same type gets triggered so above one is skipped)
                services.AddOptions<ComplexOptions>("bad_configuration")
                    .Configure(o => o.Boolean = false)
                    .Validate(o =>
                    {
                        validateCalled = true;
                        return o.Boolean;
                    }, "bad_configuration");
            });

            // For the lazily added "bad_configuration", validation failure does not occur when host starts
            using (var host = hostBuilder.Build())
            {
                await host.StartAsync();
            }

            Assert.False(validateCalled);
        }

        [Fact]
        public async Task ValidateOnStart_AddBothLazyAndEagerValidationOnDifferentTypes_ValidatesWhenHostStartsOnlyForEagerValidations()
        {
            bool validateCalledForNested = false;
            bool validateCalledForComplexOptions = false;

            var hostBuilder = CreateHostBuilder(services =>
            {
                // Lazy validation for NestedOptions
                services.AddOptions<NestedOptions>()
                    .Configure(o => o.Integer = 11)
                    .Validate(o =>
                    {
                        validateCalledForNested = true;
                        return o.Integer > 12;
                    }, "Integer");

                // Eager validation for ComplexOptions
                services.AddOptions<ComplexOptions>()
                    .Configure(o => o.Boolean = false)
                    .Validate(o =>
                    {
                        validateCalledForComplexOptions = true;
                        return o.Boolean;
                    }, "first Boolean must be true.")
                    .ValidateOnStart();
            });

            using (var host = hostBuilder.Build())
            {
                var error = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
                {
                    await host.StartAsync();
                });

                ValidateFailure<ComplexOptions>(error, 1, "first Boolean must be true.");
            }

            Assert.False(validateCalledForNested);
            Assert.True(validateCalledForComplexOptions);
        }

        [Fact]
        public async Task ValidateOnStart_MultipleErrorsInOneValidationCall_ValidatesFailureWithMultipleErrors()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddOptions<ComplexOptions>()
                .Configure(o =>
                {
                    o.Boolean = false;
                    o.Integer = 11;
                })
                .Validate(o => o.Boolean)
                .Validate(o => o.Integer > 12)
                .ValidateOnStart();
            });

            using (var host = hostBuilder.Build())
            {
                var error = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
                {
                    await host.StartAsync();
                });

                ValidateFailure<ComplexOptions>(error, 2);
            }
        }

        [Fact]
        public async Task ValidateOnStart_MultipleErrorsInOneValidationCallUsingCustomErrors_FailuresContainCustomErrors()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddOptions<ComplexOptions>()
                       .Configure(o =>
                       {
                           o.Boolean = false;
                           o.Integer = 11;
                           o.Virtual = "wut";
                       })
                       .Validate(o => o.Boolean)
                       .Validate(o => o.Virtual == null, "Virtual")
                       .Validate(o => o.Integer > 12, "Integer")
                       .ValidateOnStart();
            });
            using (var host = hostBuilder.Build())
            {
                var error = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
                {
                    await host.StartAsync();
                });

                ValidateFailure<ComplexOptions>(error, 3, "Virtual", "Integer");
            }
        }

        [Fact]
        public async Task ValidateOnStart_CustomSyncStartupValidator_OverridesAsyncValidationOnStart()
        {
            var custom = new TrackingStartupValidator();
            var hostBuilder = CreateHostBuilder(services =>
            {
#pragma warning disable SYSLIB0066 // Tests the legacy IStartupValidator compatibility contract.
                services.AddSingleton<IStartupValidator>(custom);
#pragma warning restore SYSLIB0066
                services.AddOptions<ComplexOptions>()
                    .Configure(o => o.Boolean = false)
                    .Validate(o => o.Boolean, "should not run")
                    .ValidateOnStart();
            });

            using (var host = hostBuilder.Build())
            {
                // The custom synchronous validator takes precedence and fully controls startup validation,
                // so the failing ValidateOnStart (async) validation never runs and the host starts.
                await host.StartAsync();
            }

            Assert.True(custom.Validated);
        }

        [Fact]
        public async Task ValidateOnStart_CustomSyncStartupValidatorThatFails_ThrowsOnStart()
        {
            var hostBuilder = CreateHostBuilder(services =>
#pragma warning disable SYSLIB0066 // Tests the legacy IStartupValidator compatibility contract.
                services.AddSingleton<IStartupValidator>(new ThrowingStartupValidator()));
#pragma warning restore SYSLIB0066

            using (var host = hostBuilder.Build())
            {
                await Assert.ThrowsAsync<OptionsValidationException>(async () => await host.StartAsync());
            }
        }

        [Fact]
        public async Task ValidateOnStart_MultipleAsyncStartupValidators_AllRunOnStart()
        {
            var custom = new TrackingAsyncStartupValidator();
            bool validateOnStartRan = false;
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddSingleton<IAsyncStartupValidator>(custom);
                services.AddOptions<ComplexOptions>()
                    .Configure(o => o.Boolean = true)
                    .Validate(o =>
                    {
                        validateOnStartRan = true;
                        return o.Boolean;
                    })
                    .ValidateOnStart();
            });

            using (var host = hostBuilder.Build())
            {
                await host.StartAsync();
            }

            // Both the custom async validator and the built-in ValidateOnStart validator participate.
            Assert.True(custom.Validated);
            Assert.True(validateOnStartRan);
        }

        [Fact]
        public async Task ValidateOnStart_StandaloneAsyncStartupValidator_RunsOnStart()
        {
            var custom = new TrackingAsyncStartupValidator();
            var hostBuilder = CreateHostBuilder(services => services.AddSingleton<IAsyncStartupValidator>(custom));

            using (var host = hostBuilder.Build())
            {
                await host.StartAsync();
            }

            Assert.True(custom.Validated);
        }

        [Fact]
        public async Task ValidateOnStart_AsyncStartupValidatorThatFails_ThrowsOnStart()
        {
            var hostBuilder = CreateHostBuilder(services =>
                services.AddSingleton<IAsyncStartupValidator>(new ThrowingAsyncStartupValidator()));

            using (var host = hostBuilder.Build())
            {
                await Assert.ThrowsAsync<OptionsValidationException>(async () => await host.StartAsync());
            }
        }

        [Fact]
        public async Task ValidateOnStart_EmptyAggregateException_IsNotSwallowed()
        {
            var hostBuilder = CreateHostBuilder(services =>
                services.AddSingleton<IAsyncStartupValidator>(new ThrowingEmptyAggregateAsyncStartupValidator()));

            using var host = hostBuilder.Build();

            AggregateException error = await Assert.ThrowsAsync<AggregateException>(async () => await host.StartAsync());
            Assert.Empty(error.InnerExceptions);
        }

        [Fact]
        public async Task ValidateOnStart_AggregatedValidationFailures_AreFlattenedAndValidationContinues()
        {
            var following = new CountingThrowingAsyncStartupValidator("third failed");
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddSingleton<IAsyncStartupValidator>(new ThrowingAggregateAsyncStartupValidator());
                services.AddSingleton<IAsyncStartupValidator>(following);
            });

            using var host = hostBuilder.Build();

            AggregateException error = await Assert.ThrowsAsync<AggregateException>(async () => await host.StartAsync());

            Assert.Equal(3, error.InnerExceptions.Count);
            Assert.All(error.InnerExceptions, e => Assert.IsType<OptionsValidationException>(e));
            Assert.True(following.Validated);
        }

        [Fact]
        public async Task ValidateOnStart_AsyncSuccessSeedsOptionsBeforeHostedServiceConstruction()
        {
            ComplexOptions startupCandidate = null;
            OptionsReadingHostedService hostedService = null;
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddOptions<ComplexOptions>()
                    .Configure(o => o.Boolean = true)
                    .Validate((ComplexOptions o, CancellationToken ct) =>
                    {
                        startupCandidate = o;
                        return Task.FromResult(true);
                    })
                    .ValidateOnStart();

                services.AddSingleton<IHostedService>(sp =>
                    hostedService = new OptionsReadingHostedService(
                        sp.GetRequiredService<IOptions<ComplexOptions>>()));
            });

            using IHost host = hostBuilder.Build();

            await host.StartAsync();

            Assert.NotNull(startupCandidate);
            Assert.NotNull(hostedService);
            Assert.Same(startupCandidate, hostedService.Options);
            Assert.Same(startupCandidate, host.Services.GetRequiredService<IOptions<ComplexOptions>>().Value);
            Assert.Same(startupCandidate, host.Services.GetRequiredService<IOptionsMonitor<ComplexOptions>>().CurrentValue);
        }

        [Fact]
        public async Task ValidateOnStart_MultipleFailingAsyncStartupValidators_RunAllAndAggregateFailures()
        {
            var first = new CountingThrowingAsyncStartupValidator("first failed");
            var second = new CountingThrowingAsyncStartupValidator("second failed");
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddSingleton<IAsyncStartupValidator>(first);
                services.AddSingleton<IAsyncStartupValidator>(second);
            });

            using (var host = hostBuilder.Build())
            {
                AggregateException error = await Assert.ThrowsAsync<AggregateException>(async () => await host.StartAsync());

                // Every validator runs (no short-circuit on the first failure) and all failures are reported.
                Assert.Equal(2, error.InnerExceptions.Count);
                Assert.All(error.InnerExceptions, e => Assert.IsType<OptionsValidationException>(e));
            }

            Assert.True(first.Validated);
            Assert.True(second.Validated);
        }

        [Fact]
        public async Task ValidateOnStart_DualInterfaceValidatorRegisteredOnlyAsSync_RunsSyncValidate()
        {
            var custom = new TrackingDualStartupValidator();
#pragma warning disable SYSLIB0066 // Tests the legacy IStartupValidator compatibility contract.
            var hostBuilder = CreateHostBuilder(services => services.AddSingleton<IStartupValidator>(custom));
#pragma warning restore SYSLIB0066

            using (var host = hostBuilder.Build())
            {
                await host.StartAsync();
            }

            // Registration under IStartupValidator selects the synchronous path even though the implementation
            // also supports asynchronous validation.
            Assert.True(custom.SyncValidated);
            Assert.False(custom.AsyncValidated);
        }

        [Fact]
        public async Task ValidateOnStart_DualInterfaceValidatorRegisteredOnlyAsAsync_RunsAsyncValidate()
        {
            var custom = new TrackingDualStartupValidator();
            var hostBuilder = CreateHostBuilder(services =>
                services.AddSingleton<IAsyncStartupValidator>(custom));

            using (var host = hostBuilder.Build())
            {
                await host.StartAsync();
            }

            Assert.False(custom.SyncValidated);
            Assert.True(custom.AsyncValidated);
        }

        [Fact]
        public async Task ValidateOnStart_DoesNotConflateDistinctValidatorsOfTheSameType()
        {
            var syncValidator = new TrackingDualStartupValidator();
            var asyncValidator = new TrackingDualStartupValidator();
            var hostBuilder = CreateHostBuilder(services =>
            {
#pragma warning disable SYSLIB0066 // Tests the legacy IStartupValidator compatibility contract.
                services.AddSingleton<IStartupValidator>(syncValidator);
#pragma warning restore SYSLIB0066
                services.AddSingleton<IAsyncStartupValidator>(asyncValidator);
            });

            using (var host = hostBuilder.Build())
            {
                await host.StartAsync();
            }

            Assert.True(syncValidator.SyncValidated);
            Assert.False(syncValidator.AsyncValidated);
            Assert.False(asyncValidator.SyncValidated);
            Assert.False(asyncValidator.AsyncValidated);
        }

        [Fact]
        public async Task ValidateOnStart_DualInterfaceValidatorAliasedUnderBothContracts_RunsAsyncValidate()
        {
            var custom = new TrackingDualStartupValidator();
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddSingleton(custom);
#pragma warning disable SYSLIB0066 // Tests the legacy IStartupValidator compatibility contract.
                services.AddSingleton<IStartupValidator>(
                    sp => sp.GetRequiredService<TrackingDualStartupValidator>());
#pragma warning restore SYSLIB0066
                services.AddSingleton<IAsyncStartupValidator>(
                    sp => sp.GetRequiredService<TrackingDualStartupValidator>());
            });

            using (var host = hostBuilder.Build())
            {
                await host.StartAsync();
            }

            Assert.False(custom.SyncValidated);
            Assert.True(custom.AsyncValidated);
        }

        [Fact]
        public async Task ValidateOnStart_DualInterfaceValidatorRegisteredOnlyAsSync_TakesPrecedenceOverBuiltInAsync()
        {
            var custom = new TrackingDualStartupValidator();
            var hostBuilder = CreateHostBuilder(services =>
            {
#pragma warning disable SYSLIB0066 // Tests the legacy IStartupValidator compatibility contract.
                services.AddSingleton<IStartupValidator>(custom);
#pragma warning restore SYSLIB0066
                services.AddOptions<ComplexOptions>()
                    .Configure(o => o.Boolean = false)
                    .Validate(o => o.Boolean, "should not run")
                    .ValidateOnStart();
            });

            using (var host = hostBuilder.Build())
            {
                // Registration under the obsolete contract takes precedence, so the failing asynchronous
                // ValidateOnStart validation never runs.
                await host.StartAsync();
            }

            Assert.True(custom.SyncValidated);
            Assert.False(custom.AsyncValidated);
        }

        [Fact]
        public async Task ValidateOnStart_SyncOnlyValidatorPresent_DoesNotResolveAsyncValidators()
        {
            bool asyncResolved = false;
            var custom = new TrackingStartupValidator();
            var hostBuilder = CreateHostBuilder(services =>
            {
#pragma warning disable SYSLIB0066 // Tests the legacy IStartupValidator compatibility contract.
                services.AddSingleton<IStartupValidator>(custom);
#pragma warning restore SYSLIB0066
                services.AddSingleton<IAsyncStartupValidator>(_ =>
                {
                    asyncResolved = true;
                    return new TrackingAsyncStartupValidator();
                });
            });

            using (var host = hostBuilder.Build())
            {
                await host.StartAsync();
            }

            // A sync-only IStartupValidator fully controls startup validation, so the async validators are never
            // resolved: their factories/constructors (and any side effects) do not run.
            Assert.True(custom.Validated);
            Assert.False(asyncResolved);
        }

        [Fact]
        public async Task ValidateOnStart_FailedStartupValidation_StopAsyncDoesNotThrow()
        {
            var hostBuilder = CreateHostBuilder(services =>
                services.AddSingleton<IAsyncStartupValidator>(new ThrowingAsyncStartupValidator()));

            using var host = hostBuilder.Build();

            // Startup validation runs before hosted services are resolved, so a failure leaves the host in a
            // "starting" state with no resolved hosted services. StopAsync must handle that without throwing.
            await Assert.ThrowsAsync<OptionsValidationException>(async () => await host.StartAsync());
            await host.StopAsync();
        }

        [Fact]
        public async Task ValidateOnStart_UnexpectedExceptionAfterFailure_RetainsFailuresAndStopsIterating()
        {
            var first = new CountingThrowingAsyncStartupValidator("first failed");
            var second = new ThrowingUnexpectedAsyncStartupValidator();
            var third = new TrackingAsyncStartupValidator();
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddSingleton<IAsyncStartupValidator>(first);
                services.AddSingleton<IAsyncStartupValidator>(second);
                services.AddSingleton<IAsyncStartupValidator>(third);
            });

            using (var host = hostBuilder.Build())
            {
                AggregateException error = await Assert.ThrowsAsync<AggregateException>(async () => await host.StartAsync());

                // The earlier validation failure is retained and reported alongside the unexpected exception.
                Assert.Equal(2, error.InnerExceptions.Count);
                Assert.IsType<OptionsValidationException>(error.InnerExceptions[0]);
                Assert.IsType<InvalidOperationException>(error.InnerExceptions[1]);
            }

            // Iteration stops at the unexpected failure, so validators after it do not run.
            Assert.True(first.Validated);
            Assert.True(second.Validated);
            Assert.False(third.Validated);
        }

        private sealed class OptionsReadingHostedService : IHostedService
        {
            public OptionsReadingHostedService(IOptions<ComplexOptions> options)
            {
                Options = options.Value;
            }

            public ComplexOptions Options { get; }

            public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

#pragma warning disable SYSLIB0066 // Tests the legacy IStartupValidator compatibility contract.
        private sealed class TrackingStartupValidator : IStartupValidator
#pragma warning restore SYSLIB0066
        {
            public bool Validated { get; private set; }

            public void Validate() => Validated = true;
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

#pragma warning disable SYSLIB0066 // Tests the legacy IStartupValidator compatibility contract.
        private sealed class TrackingDualStartupValidator : IStartupValidator, IAsyncStartupValidator
#pragma warning restore SYSLIB0066
        {
            public bool SyncValidated { get; private set; }
            public bool AsyncValidated { get; private set; }

            public void Validate() => SyncValidated = true;

            public Task ValidateAsync(CancellationToken cancellationToken = default)
            {
                AsyncValidated = true;
                return Task.CompletedTask;
            }
        }

#pragma warning disable SYSLIB0066 // Tests the legacy IStartupValidator compatibility contract.
        private sealed class ThrowingStartupValidator : IStartupValidator
#pragma warning restore SYSLIB0066
        {
            public void Validate() =>
                throw new OptionsValidationException("name", typeof(object), new[] { "sync startup validation failed" });
        }

        private sealed class ThrowingAsyncStartupValidator : IAsyncStartupValidator
        {
            public Task ValidateAsync(CancellationToken cancellationToken = default) =>
                throw new OptionsValidationException("name", typeof(object), new[] { "async startup validation failed" });
        }

        private sealed class ThrowingEmptyAggregateAsyncStartupValidator : IAsyncStartupValidator
        {
            public Task ValidateAsync(CancellationToken cancellationToken = default) =>
                throw new AggregateException();
        }

        private sealed class ThrowingAggregateAsyncStartupValidator : IAsyncStartupValidator
        {
            public Task ValidateAsync(CancellationToken cancellationToken = default) =>
                throw new AggregateException(
                    new OptionsValidationException("first", typeof(object), new[] { "first failed" }),
                    new OptionsValidationException("second", typeof(object), new[] { "second failed" }));
        }

        private sealed class ThrowingUnexpectedAsyncStartupValidator : IAsyncStartupValidator
        {
            public bool Validated { get; private set; }

            public Task ValidateAsync(CancellationToken cancellationToken = default)
            {
                Validated = true;
                throw new InvalidOperationException("unexpected failure");
            }
        }

        private sealed class CountingThrowingAsyncStartupValidator : IAsyncStartupValidator
        {
            private readonly string _failure;

            public CountingThrowingAsyncStartupValidator(string failure) => _failure = failure;

            public bool Validated { get; private set; }

            public Task ValidateAsync(CancellationToken cancellationToken = default)
            {
                Validated = true;
                throw new OptionsValidationException("name", typeof(object), new[] { _failure });
            }
        }

        private static void ValidateFailure(Type type, OptionsValidationException e, int count = 1, params string[] errorsToMatch)
        {
            Assert.Equal(type, e.OptionsType);

            Assert.Equal(count, e.Failures.Count());

            // Check for the error in any of the failures
            foreach (var error in errorsToMatch)
            {
#if NET
                Assert.True(e.Failures.FirstOrDefault(predicate: f => f.Contains(error, StringComparison.CurrentCulture)) != null, "Did not find: " + error + " " + e.Failures.First());
#else
                Assert.True(e.Failures.FirstOrDefault(predicate: f => f.IndexOf(error, StringComparison.CurrentCulture) >= 0) != null, "Did not find: " + error + " " + e.Failures.First());
#endif
            }
        }

        private static void ValidateFailure<TOptions>(OptionsValidationException e, int count = 1, params string[] errorsToMatch)
        {
            ValidateFailure(typeof(TOptions), e, count, errorsToMatch);
        }
    }
}
