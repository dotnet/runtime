// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        private async Task ValidateOnStart_AddOptionsMultipleTimesForSameType_LastOneGetsTriggered()
        {
            bool firstOptionsBuilderTriggered = false;
            bool secondOptionsBuilderTriggered = false;
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddOptions<ComplexOptions>("bad_configuration1")
                    .Configure(o => o.Boolean = false)
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

            Assert.False(firstOptionsBuilderTriggered);
            Assert.True(secondOptionsBuilderTriggered);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
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

        private static void ValidateFailure(Type type, OptionsValidationException e, int count = 1, params string[] errorsToMatch)
        {
            Assert.Equal(type, e.OptionsType);

            Assert.Equal(count, e.Failures.Count());

            // Check for the error in any of the failures
            foreach (var error in errorsToMatch)
            {
#if NETCOREAPP
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
