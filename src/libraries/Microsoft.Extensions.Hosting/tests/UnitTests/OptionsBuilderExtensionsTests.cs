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
                services.AddSingleton<IStartupValidator>(custom);
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
                services.AddSingleton<IStartupValidator>(new ThrowingStartupValidator()));

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

        private sealed class TrackingStartupValidator : IStartupValidator
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

        private sealed class ThrowingStartupValidator : IStartupValidator
        {
            public void Validate() =>
                throw new OptionsValidationException("name", typeof(object), new[] { "sync startup validation failed" });
        }

        private sealed class ThrowingAsyncStartupValidator : IAsyncStartupValidator
        {
            public Task ValidateAsync(CancellationToken cancellationToken = default) =>
                throw new OptionsValidationException("name", typeof(object), new[] { "async startup validation failed" });
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
