// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options.Tests;
using Xunit;

namespace Microsoft.Extensions.Options.DataAnnotations.Tests
{
    public class OptionsBuilderValidationTests
    {
        public static IHostBuilder CreateHostBuilder(Action<IServiceCollection> configure)
        {
            return new HostBuilder().ConfigureServices(configure);
        }

        [Fact]
        public async Task CanValidateOptionsOnStartWithDefaultError()
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

                ValidateFailure<ComplexOptions>(error);
            }
        }

        [Fact]
        public async Task CanValidateOptionsOnStartWithCustomError()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddOptions<ComplexOptions>()
                    .Configure(o => o.Boolean = false)
                    .Validate(o => o.Boolean, "first Boolean must be true.")
                    .ValidateOnStart();
                services.AddOptions<ComplexOptions>()
                    .Configure(o => o.Boolean = true)
                    .Validate(o => !o.Boolean, "second Boolean must be false.")
                    .ValidateOnStart();
            });

            using (var host = hostBuilder.Build())
            {
                var error = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
                {
                    await host.StartAsync();
                });

                ValidateFailure<ComplexOptions>(error, 1, "second Boolean must be false.");
            }
        }

        [Fact]
        public async Task CanValidateOptionsOnStartRatherThanLazySameType() // shouldn this fail?
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddOptions<ComplexOptions>()
                    .Configure(o => o.Boolean = false)
                    .Validate(o => o.Boolean, "first Boolean must be true.")
                    .ValidateOnStart();
                services.AddOptions<ComplexOptions>()
                    .Configure(o => o.Boolean = true)
                    .Validate(o => !o.Boolean, "second Boolean must be false.");
            });

            using (var host = hostBuilder.Build())
            {
                var error = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
                {
                    await host.StartAsync();
                });

                ValidateFailure<ComplexOptions>(error, 1, "second Boolean must be false.");
            }
        }

        [Fact]
        public async Task CanValidateOptionsLazyThanEagerSameType()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddOptions<ComplexOptions>()
                    .Configure(o => o.Boolean = false)
                    .Validate(o => o.Boolean, "first Boolean must be true.");
                services.AddOptions<ComplexOptions>()
                    .Configure(o => o.Boolean = true)
                    .Validate(o => !o.Boolean, "second Boolean must be false.")
                    .ValidateOnStart();
            });

            using (var host = hostBuilder.Build())
            {
                var error = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
                {
                    await host.StartAsync();
                });

                ValidateFailure<ComplexOptions>(error, 1, "second Boolean must be false.");
            }
        }

        [Fact]
        public async Task CanValidateOptionsLazyThanEagerDifferentTypes()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddOptions<NestedOptions>()
                    .Configure(o => o.Integer = 11)
                    .Validate(o => o.Integer > 12, "Integer");

                services.AddOptions<ComplexOptions>()
                    .Configure(o => o.Boolean = false)
                    .Validate(o => o.Boolean, "first Boolean must be true.")
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
        }

        [Fact]
        public async Task CanValidateOptionsOnStartAndSomeLazyDifferentTypes()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddOptions<NestedOptions>()
                    .Configure(o => o.Integer = 11)
                    .Validate(o => o.Integer > 12, "Integer")
                    .ValidateOnStart();

                services.AddOptions<ComplexOptions>()
                    .Configure(o => o.Boolean = false)
                    .Validate(o => o.Boolean, "first Boolean must be true.");
            });

            using (var host = hostBuilder.Build())
            {
                var error = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
                {
                    await host.StartAsync();
                });

                ValidateFailure<NestedOptions>(error, 1, "Integer");
            }
        }

        [Fact]
        public async Task CanValidateOptionsOnStartWithMultipleDefaultErrors()
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
        public async Task CanValidateOptionOnStartsWithMixedOverloads()
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
        public async Task CanValidateOnStartDataAnnotationsLongSyntax()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddOptions<AnnotatedOptions>()
                    .Configure(o =>
                    {
                        o.StringLength = "111111";
                        o.IntRange = 10;
                        o.Custom = "nowhere";
                        o.Dep1 = "Not dep2";
                    })
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
            });

            using (var host = hostBuilder.Build())
            {
                var error = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
                {
                    await host.StartAsync();
                });

                ValidateFailure<AnnotatedOptions>(error, 5,
                    "DataAnnotation validation failed for members: 'Required' with the error: 'The Required field is required.'.",
                    "DataAnnotation validation failed for members: 'StringLength' with the error: 'Too long.'.",
                    "DataAnnotation validation failed for members: 'IntRange' with the error: 'Out of range.'.",
                    "DataAnnotation validation failed for members: 'Custom' with the error: 'The field Custom is invalid.'.",
                    "DataAnnotation validation failed for members: 'Dep1,Dep2' with the error: 'Dep1 != Dep2'.");
            }
        }

        [Fact]
        public async Task CanValidateOnStartMixDataAnnotationsLongSyntax()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddOptions<AnnotatedOptions>()
                    .Configure(o =>
                    {
                        o.StringLength = "111111";
                        o.IntRange = 10;
                        o.Custom = "nowhere";
                        o.Dep1 = "Not dep2";
                    })
                    .ValidateDataAnnotations()
                    .Validate(o => o.Custom != "nowhere", "I don't want to go to nowhere!")
                    .ValidateOnStart();
            });

            using (var host = hostBuilder.Build())
            {
                var error = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
                {
                    await host.StartAsync();
                });

                ValidateFailure<AnnotatedOptions>(error, 6,
                    "DataAnnotation validation failed for members: 'Required' with the error: 'The Required field is required.'.",
                    "DataAnnotation validation failed for members: 'StringLength' with the error: 'Too long.'.",
                    "DataAnnotation validation failed for members: 'IntRange' with the error: 'Out of range.'.",
                    "DataAnnotation validation failed for members: 'Custom' with the error: 'The field Custom is invalid.'.",
                    "DataAnnotation validation failed for members: 'Dep1,Dep2' with the error: 'Dep1 != Dep2'.",
                    "I don't want to go to nowhere!");
            }
        }

        [Fact]
        public async Task CanValidateOnStartDataAnnotationsShortSyntax()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddOptions<AnnotatedOptions>()
                    .Configure(o =>
                    {
                        o.StringLength = "111111";
                        o.IntRange = 10;
                        o.Custom = "nowhere";
                        o.Dep1 = "Not dep2";
                    })
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
            });

            using (var host = hostBuilder.Build())
            {
                var error = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
                {
                    await host.StartAsync();
                });

                ValidateFailure<AnnotatedOptions>(error, 5,
                    "DataAnnotation validation failed for members: 'Required' with the error: 'The Required field is required.'.",
                    "DataAnnotation validation failed for members: 'StringLength' with the error: 'Too long.'.",
                    "DataAnnotation validation failed for members: 'IntRange' with the error: 'Out of range.'.",
                    "DataAnnotation validation failed for members: 'Custom' with the error: 'The field Custom is invalid.'.",
                    "DataAnnotation validation failed for members: 'Dep1,Dep2' with the error: 'Dep1 != Dep2'.");
            }
        }

        [Fact]
        public async Task CanValidateOnStartMixDataAnnotationsShortSyntax()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddOptions<AnnotatedOptions>()
                    .Configure(o =>
                    {
                        o.StringLength = "111111";
                        o.IntRange = 10;
                        o.Custom = "nowhere";
                        o.Dep1 = "Not dep2";
                    })
                    .ValidateDataAnnotations()
                    .ValidateOnStart()
                    .Validate(o => o.Custom != "nowhere", "I don't want to go to nowhere!");
            });

            using (var host = hostBuilder.Build())
            {
                var error = await Assert.ThrowsAsync<OptionsValidationException>(async () =>
                {
                    await host.StartAsync();
                });

                ValidateFailure<AnnotatedOptions>(error, 6,
                    "DataAnnotation validation failed for members: 'Required' with the error: 'The Required field is required.'.",
                    "DataAnnotation validation failed for members: 'StringLength' with the error: 'Too long.'.",
                    "DataAnnotation validation failed for members: 'IntRange' with the error: 'Out of range.'.",
                    "DataAnnotation validation failed for members: 'Custom' with the error: 'The field Custom is invalid.'.",
                    "DataAnnotation validation failed for members: 'Dep1,Dep2' with the error: 'Dep1 != Dep2'.",
                    "I don't want to go to nowhere!");
            }
        }

        [Fact]
        public void Test_WhenValidateOnStartThrowsIfArgumentNull()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                OptionsBuilder<AnnotatedOptions> optionsBuilder = null;
#pragma warning disable CS8604 // Possible null reference argument.
                optionsBuilder.ValidateOnStart();
#pragma warning restore CS8604 // Possible null reference argument.
            });

            _ = Assert.Throws<ArgumentNullException>(() => { _ = hostBuilder.Build(); });
        }

        internal class FakeService { }

        internal class FakeSettings
        {
            public string Name { get; set; }
        }

        [Fact]
        public async Task ValidateOnStart_NamedOptions_FailsOnStartForFailedValidation()
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
        public async Task Test_IVfalidationSuccessful()
        {
            var hostBuilder = CreateHostBuilder(services =>
            {
                services.AddOptions<AnnotatedOptions>()
                    .Configure(o =>
                    {
                        o.Required = "required";
                        o.StringLength = "1111";
                        o.IntRange = 0;
                        o.Custom = "USA";
                        o.Dep1 = "dep";
                        o.Dep2 = "dep";
                    })
                    .ValidateDataAnnotations()
                    .ValidateOnStart()
                    .Validate(o => o.Custom != "nowhere", "I don't want to go to nowhere!");
            });

            using (var host = hostBuilder.Build())
            {
                try
                {
                    await host.StartAsync();
                }
    #pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
    #pragma warning restore CA1031 // Do not catch general exception types
                {
                    Assert.True(false, "Expected no exception, but got: " + ex.Message);
                }
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
                Assert.True(e.Failures.FirstOrDefault(predicate: f => f.Contains(error, StringComparison.CurrentCulture)) != null, "Did not find: " + error);
#else
                Assert.True(e.Failures.FirstOrDefault(predicate: f => f.IndexOf(error, StringComparison.CurrentCulture) >= 0) != null, "Did not find: " + error);
#endif
            }
        }

        private static void ValidateFailure<TOptions>(OptionsValidationException e, int count = 1, params string[] errorsToMatch)
        {
            ValidateFailure(typeof(TOptions), e, count, errorsToMatch);
        }
    }
}
