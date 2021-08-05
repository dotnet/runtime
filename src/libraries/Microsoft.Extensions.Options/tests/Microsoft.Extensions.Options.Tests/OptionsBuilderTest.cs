// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Options.Tests
{
    public class OptionsBuilderTest
    {
        [Fact]
        public void CanSupportDefaultName()
        {
            var services = new ServiceCollection();
            var dic = new Dictionary<string, string>
            {
                { "Message", "!" },
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(dic).Build();

            var builder = services.AddOptions<FakeOptions>();
            builder
                .Configure(options => options.Message += "Igetstomped")
                .Bind(config)
                .PostConfigure(options => options.Message += "]")
                .Configure(options => options.Message += "[")
                .Configure(options => options.Message += "Default");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("![Default]", factory.Create(Options.DefaultName).Message);
        }

        [Fact]
        public void CanSupportNamedOptions()
        {
            var services = new ServiceCollection();
            var dic = new Dictionary<string, string>
            {
                { "Message", "!" },
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(dic).Build();

            var builder1 = services.AddOptions<FakeOptions>("1");
            var builder2 = services.AddOptions<FakeOptions>("2");
            builder1
                .Bind(config)
                .PostConfigure(options => options.Message += "]")
                .Configure(options => options.Message += "[")
                .Configure(options => options.Message += "one");
            builder2
                .Bind(config)
                .PostConfigure(options => options.Message += ">")
                .Configure(options => options.Message += "<")
                .Configure(options => options.Message += "two");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("![one]", factory.Create("1").Message);
            Assert.Equal("!<two>", factory.Create("2").Message);
        }

        [Fact]
        public void CanMixConfigureCallsOutsideBuilderInOrder()
        {
            var services = new ServiceCollection();
            var builder = services.AddOptions<FakeOptions>("1");

            services.ConfigureAll<FakeOptions>(o => o.Message += "A");
            builder.PostConfigure(o => o.Message += "D");
            services.PostConfigure<FakeOptions>("1", o => o.Message += "E");
            builder.Configure(o => o.Message += "B");
            services.Configure<FakeOptions>("1", o => o.Message += "C");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("ABCDE", factory.Create("1").Message);
        }


        public class SomeService
        {
            public SomeService(string stuff) => Stuff = stuff;

            public string Stuff { get; set; }
        }

        public class Counter
        {
            public int Count { get; set; }

            public void Increment() => Count++;
        }

        public class SomeCounterConsumer
        {
            public SomeCounterConsumer(Counter c)
            {
                Current = c.Count;
                c.Increment();
            }

            public int Current { get; }
        }

        [Fact]
        public void ConfigureOptionsWithSingletonDepWillUpdate()
        {
            var someService = new SomeService("Something");
            var services = new ServiceCollection().AddOptions().AddSingleton(someService);
            services.AddOptions<FakeOptions>().Configure<SomeService>((o, s) => o.Message = s.Stuff);
            services.AddOptions<FakeOptions>("named").Configure<SomeService>((o, s) => o.Message = "named " + s.Stuff);

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();

            Assert.Equal("named Something", factory.Create("named").Message);
            Assert.Equal("Something", factory.Create(Options.DefaultName).Message);

            someService.Stuff = "Else";

            Assert.Equal("named Else", factory.Create("named").Message);
            Assert.Equal("Else", factory.Create(Options.DefaultName).Message);
        }

        [Fact]
        public void ConfigureOptionsWithTransientDep()
        {
            var services = new ServiceCollection()
                .AddSingleton<Counter>()
                .AddTransient<SomeCounterConsumer>();
            services.AddOptions<FakeOptions>().Configure(o => o.Message = "none");
            services.AddOptions<FakeOptions>("1dep").Configure<SomeCounterConsumer>((o, s) => o.Message = s.Current + "");
            services.AddOptions<FakeOptions>("2dep").Configure<SomeCounterConsumer, SomeCounterConsumer>((o, s, s2) => o.Message = s.Current + " " + s2.Current);
            services.AddOptions<FakeOptions>("3dep").Configure<SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer>((o, s, s2, s3) => o.Message = s.Current + " " + s2.Current + " " + s3.Current);
            services.AddOptions<FakeOptions>("4dep").Configure<SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer>((o, s, s2, s3, s4) => o.Message = s.Current + " " + s2.Current + " " + s3.Current + " " + s4.Current);
            services.AddOptions<FakeOptions>("5dep").Configure<SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer>((o, s, s2, s3, s4, s5) => o.Message = s.Current + " " + s2.Current + " " + s3.Current + " " + s4.Current + " " + s5.Current);

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();

            Assert.Equal("none", factory.Create(Options.DefaultName).Message);
            Assert.Equal("0", factory.Create("1dep").Message);
            Assert.Equal("1 2", factory.Create("2dep").Message);
            Assert.Equal("3 4 5", factory.Create("3dep").Message);
            Assert.Equal("6 7 8 9", factory.Create("4dep").Message);
            Assert.Equal("10 11 12 13 14", factory.Create("5dep").Message);

            // Factory caches configures
            Assert.Equal("0", factory.Create("1dep").Message);

            // New factory will reexecute
            Assert.Equal("15", sp.GetRequiredService<IOptionsFactory<FakeOptions>>().Create("1dep").Message);
        }

        [Fact]
        public void PostConfigureOptionsWithTransientDep()
        {
            var services = new ServiceCollection()
                .AddSingleton<Counter>()
                .AddTransient<SomeCounterConsumer>();
            services.ConfigureAll<FakeOptions>(o => o.Message = "Override");
            services.AddOptions<FakeOptions>().PostConfigure(o => o.Message = "none");
            services.AddOptions<FakeOptions>("1dep").PostConfigure<SomeCounterConsumer>((o, s) => o.Message = s.Current + "");
            services.AddOptions<FakeOptions>("2dep").PostConfigure<SomeCounterConsumer, SomeCounterConsumer>((o, s, s2) => o.Message = s.Current + " " + s2.Current);
            services.AddOptions<FakeOptions>("3dep").PostConfigure<SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer>((o, s, s2, s3) => o.Message = s.Current + " " + s2.Current + " " + s3.Current);
            services.AddOptions<FakeOptions>("4dep").PostConfigure<SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer>((o, s, s2, s3, s4) => o.Message = s.Current + " " + s2.Current + " " + s3.Current + " " + s4.Current);
            services.AddOptions<FakeOptions>("5dep").PostConfigure<SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer>((o, s, s2, s3, s4, s5) => o.Message = s.Current + " " + s2.Current + " " + s3.Current + " " + s4.Current + " " + s5.Current);

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();

            Assert.Equal("none", factory.Create(Options.DefaultName).Message);
            Assert.Equal("0", factory.Create("1dep").Message);
            Assert.Equal("1 2", factory.Create("2dep").Message);
            Assert.Equal("3 4 5", factory.Create("3dep").Message);
            Assert.Equal("6 7 8 9", factory.Create("4dep").Message);
            Assert.Equal("10 11 12 13 14", factory.Create("5dep").Message);

            // Factory caches configures
            Assert.Equal("0", factory.Create("1dep").Message);

            // New factory will reexecute
            Assert.Equal("15", sp.GetRequiredService<IOptionsFactory<FakeOptions>>().Create("1dep").Message);
        }

        [Fact]
        public void CanConfigureWithServiceProvider()
        {
            var someService = new SomeService("Something");
            var services = new ServiceCollection().AddSingleton(someService);
            services.AddOptions<FakeOptions>().Configure<IServiceProvider>((o, s) => o.Message = s.GetRequiredService<SomeService>().Stuff);

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();
            Assert.Equal("Something", factory.Create(Options.DefaultName).Message);
        }

        [Theory]
        [InlineData("PrivateSetter")]
        [InlineData("ProtectedSetter")]
        [InlineData("InternalSetter")]
        public void CanBindToNonPublicProperties(string property)
        {
            var dic = new Dictionary<string, string>
            {
                {property, "stuff"},
            };
            var services = new ServiceCollection();
            services.AddOptions<ComplexOptions>().Bind(new ConfigurationBuilder().AddInMemoryCollection(dic).Build(), o => o.BindNonPublicProperties = true);
            var sp = services.BuildServiceProvider();
            var options = sp.GetRequiredService<IOptions<ComplexOptions>>().Value;
            Assert.Equal("stuff", options.GetType().GetProperty(property).GetValue(options));
        }

        [Theory]
        [InlineData("PrivateSetter")]
        [InlineData("ProtectedSetter")]
        [InlineData("InternalSetter")]
        public void CanNamedBindToNonPublicProperties(string property)
        {
            var dic = new Dictionary<string, string>
            {
                {property, "stuff"},
            };
            var services = new ServiceCollection();
            services.AddOptions<ComplexOptions>("named").Bind(new ConfigurationBuilder().AddInMemoryCollection(dic).Build(), o => o.BindNonPublicProperties = true);
            var sp = services.BuildServiceProvider();
            var options = sp.GetRequiredService<IOptionsMonitor<ComplexOptions>>().Get("named");
            Assert.Equal("stuff", options.GetType().GetProperty(property).GetValue(options));
        }

        [Fact]
        public void CanValidateOptionsWithCustomError()
        {
            var services = new ServiceCollection();
            services.AddOptions<ComplexOptions>()
                .Configure(o => o.Boolean = false)
                .Validate(o => o.Boolean, "Boolean must be true.");
            services.AddOptions<ComplexOptions>("named")
                .Configure(o => o.Boolean = true)
                .Validate(o => !o.Boolean, "named Boolean must be false.");
            var sp = services.BuildServiceProvider();
            var error = Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptions<ComplexOptions>>().Value);
            ValidateFailure<ComplexOptions>(error, Options.DefaultName, 1, "Boolean must be true.");
            error = Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptionsMonitor<ComplexOptions>>().Get("named"));
            ValidateFailure<ComplexOptions>(error, "named", 1, "named Boolean must be false.");
        }


        [Fact]
        public void CanValidateOptionsWithDefaultError()
        {
            var services = new ServiceCollection();
            services.AddOptions<ComplexOptions>()
                .Configure(o => o.Boolean = false)
                .Validate(o => o.Boolean);
            var sp = services.BuildServiceProvider();
            var error = Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptions<ComplexOptions>>().Value);
            ValidateFailure<ComplexOptions>(error);
        }

        [Fact]
        public void CanValidateOptionsWithMultipleDefaultErrors()
        {
            var services = new ServiceCollection();
            services.AddOptions<ComplexOptions>()
                .Configure(o =>
                {
                    o.Boolean = false;
                    o.Integer = 11;
                })
                .Validate(o => o.Boolean)
                .Validate(o => o.Integer > 12);

            var sp = services.BuildServiceProvider();
            var error = Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptions<ComplexOptions>>().Value);
            ValidateFailure<ComplexOptions>(error, Options.DefaultName, 2, "A validation error has occurred.");
        }

        [Fact]
        public void CanValidateOptionsWithMixedOverloads()
        {
            var services = new ServiceCollection();
            services.AddOptions<ComplexOptions>()
                .Configure(o =>
                {
                    o.Boolean = false;
                    o.Integer = 11;
                    o.Virtual = "wut";
                })
                .Validate(o => o.Boolean)
                .Validate(o => o.Virtual == null, "Virtual")
                .Validate(o => o.Integer > 12, "Integer");

            var sp = services.BuildServiceProvider();
            var error = Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptions<ComplexOptions>>().Value);
            ValidateFailure<ComplexOptions>(error, Options.DefaultName, 3, "A validation error has occurred.", "Virtual", "Integer");
        }

        public class BadValidator : IValidateOptions<FakeOptions>
        {
            public ValidateOptionsResult Validate(string name, FakeOptions options)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void BadValidatorFailsGracefully()
        {
            var services = new ServiceCollection().AddOptions();
            services.AddSingleton<IValidateOptions<FakeOptions>, BadValidator>();
            var sp = services.BuildServiceProvider();
            var error = Assert.Throws<NotImplementedException>(() => sp.GetRequiredService<IOptions<FakeOptions>>().Value);
        }

        private class MultiOptionValidator : IValidateOptions<ComplexOptions>, IValidateOptions<FakeOptions>
        {
            private readonly string _allowed;
            public MultiOptionValidator(string allowed) => _allowed = allowed;

            public ValidateOptionsResult Validate(string name, ComplexOptions options)
            {
                if (options.Virtual == _allowed)
                {
                    return ValidateOptionsResult.Success;
                }
                return ValidateOptionsResult.Fail("Virtual != " + _allowed);
            }

            public ValidateOptionsResult Validate(string name, FakeOptions options)
            {
                if (options.Message == _allowed)
                {
                    return ValidateOptionsResult.Success;
                }
                return ValidateOptionsResult.Fail("Message != " + _allowed);
            }
        }

        [Fact]
        public void CanValidateMultipleOptionsWithOneValidator()
        {
            var services = new ServiceCollection();
            services.AddOptions<ComplexOptions>()
                .Configure(o => o.Virtual = "wut");
            services.AddOptions<FakeOptions>("fake")
                .Configure(o => o.Message = "real");

            var validator = new MultiOptionValidator("real");
            services.AddSingleton<IValidateOptions<ComplexOptions>>(validator);
            services.AddSingleton<IValidateOptions<FakeOptions>>(validator);

            var sp = services.BuildServiceProvider();
            var error = Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptions<ComplexOptions>>().Value);
            ValidateFailure<ComplexOptions>(error, Options.DefaultName, 1, "Virtual != real");

            error = Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptions<FakeOptions>>().Value);
            ValidateFailure<FakeOptions>(error, Options.DefaultName, 1, "Message != real");

            var fake = sp.GetRequiredService<IOptionsMonitor<FakeOptions>>().Get("fake");
            Assert.Equal("real", fake.Message);
        }

        private class DependencyValidator : IValidateOptions<ComplexOptions>
        {
            private readonly string _allowed;
            public DependencyValidator(IOptions<FakeOptions> _fake)
            {
                _allowed = _fake.Value.Message;
            }

            public ValidateOptionsResult Validate(string name, ComplexOptions options)
            {
                if (options.Virtual == _allowed)
                {
                    return ValidateOptionsResult.Success;
                }
                return ValidateOptionsResult.Fail("Virtual != " + _allowed);
            }
        }

        private void ValidateFailure<TOptions>(OptionsValidationException e, string name = "", int count = 1, params string[] errorsToMatch)
        {
            Assert.Equal(typeof(TOptions), e.OptionsType);
            Assert.Equal(name, e.OptionsName);
            if (errorsToMatch.Length == 0)
            {
                errorsToMatch = new string[] { "A validation error has occurred." };
            }
            Assert.Equal(count, e.Failures.Count());
            // Check for the error in any of the failures
            foreach (var error in errorsToMatch)
            {
                Assert.True(e.Failures.FirstOrDefault(f => f.Contains(error)) != null, "Did not find: " + error);
            }
            Assert.Equal(e.Message, String.Join("; ", e.Failures));
        }

        [Fact]
        public void CanValidateOptionsThatDependOnOptions()
        {
            var services = new ServiceCollection();
            services.AddOptions<ComplexOptions>()
                .Configure(o => o.Virtual = "default");
            services.AddOptions<ComplexOptions>("yes")
                .Configure(o => o.Virtual = "target");
            services.AddOptions<ComplexOptions>("no")
                .Configure(o => o.Virtual = "no");
            services.AddOptions<FakeOptions>()
                .Configure(o => o.Message = "target");
            services.AddSingleton<IValidateOptions<ComplexOptions>, DependencyValidator>();

            var sp = services.BuildServiceProvider();

            var error = Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptions<ComplexOptions>>().Value);
            ValidateFailure<ComplexOptions>(error, Options.DefaultName, 1, "Virtual != target");

            error = Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptionsMonitor<ComplexOptions>>().Get(Options.DefaultName));
            ValidateFailure<ComplexOptions>(error, Options.DefaultName, 1, "Virtual != target");

            error = Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptionsMonitor<ComplexOptions>>().Get("no"));
            ValidateFailure<ComplexOptions>(error, "no", 1, "Virtual != target");

            var op = sp.GetRequiredService<IOptionsMonitor<ComplexOptions>>().Get("yes");
            Assert.Equal("target", op.Virtual);
        }

        [Fact]
        public void ValidateWithDependencies()
        {
            var services = new ServiceCollection()
                .AddSingleton<Counter>()
                .AddTransient<SomeCounterConsumer>();
            services.AddOptions<FakeOptions>().Configure(o => o.Message = "default");
            services.AddOptions<FakeOptions>("0dep").Configure(o => o.Message = "Foo")
                .Validate(o => o.Message == "Foo");
            services.AddOptions<FakeOptions>("1dep").Configure(o => o.Message = "Foo 0")
                .Validate<SomeCounterConsumer>((o, s1) => o.Message == $"Foo {s1.Current}", "Custom failure message");
            services.AddOptions<FakeOptions>("2dep").Configure(o => o.Message = "Foo 1 2")
                .Validate<SomeCounterConsumer, SomeCounterConsumer>((o, s1, s2) => o.Message == $"Foo {s1.Current} {s2.Current}");
            services.AddOptions<FakeOptions>("3dep").Configure(o => o.Message = "Foo 3 4 5")
                .Validate<SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer>((o, s1, s2, s3) => o.Message == $"Foo {s1.Current} {s2.Current} {s3.Current}");
            services.AddOptions<FakeOptions>("4dep").Configure(o => o.Message = "Foo 6 7 8 9")
                .Validate<SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer>((o, s1, s2, s3, s4) => o.Message == $"Foo {s1.Current} {s2.Current} {s3.Current} {s4.Current}");
            services.AddOptions<FakeOptions>("5dep").Configure(o => o.Message = "Foo 10 11 12 13 14")
                .Validate<SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer, SomeCounterConsumer>((o, s1, s2, s3, s4, s5) => o.Message == $"Foo {s1.Current} {s2.Current} {s3.Current} {s4.Current} {s5.Current}");

            var sp = services.BuildServiceProvider();
            var factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();

            Assert.Equal("default", factory.Create(Options.DefaultName).Message);
            Assert.Equal("Foo", factory.Create("0dep").Message);
            Assert.Equal("Foo 0", factory.Create("1dep").Message);
            Assert.Equal("Foo 1 2", factory.Create("2dep").Message);
            Assert.Equal("Foo 3 4 5", factory.Create("3dep").Message);
            Assert.Equal("Foo 6 7 8 9", factory.Create("4dep").Message);
            Assert.Equal("Foo 10 11 12 13 14", factory.Create("5dep").Message);

            // factory caches configures
            Assert.Equal("Foo 1 2", factory.Create("2dep").Message);

            // A new factory will recreate validators which will resolve new SomeCounterConsumer
            // dependencies. That means that the counters will be incremented, causing validation failures.
            factory = sp.GetRequiredService<IOptionsFactory<FakeOptions>>();

            var error1 = Assert.Throws<OptionsValidationException>(() => factory.Create("1dep"));
            ValidateFailure<FakeOptions>(error1, "1dep", 1, "Custom failure message");

            var error2 = Assert.Throws<OptionsValidationException>(() => factory.Create("2dep"));
            ValidateFailure<FakeOptions>(error2, "2dep", 1);
        }

        // Prototype of startup validation

        public interface IStartupValidator
        {
            void Validate();
        }

        public class StartupValidationOptions
        {
            private Dictionary<Type, IList<string>> _targets = new Dictionary<Type, IList<string>>();

            public IDictionary<Type, IList<string>> ValidationTargets { get => _targets; }

            public void Validate<TOptions>(string name) where TOptions : class
            {
                if (!_targets.ContainsKey(typeof(TOptions)))
                {
                    _targets[typeof(TOptions)] = new List<string>();
                }
                _targets[typeof(TOptions)].Add(name ?? Options.DefaultName);
            }

            public void Validate<TOptions>() where TOptions : class => Validate<TOptions>(Options.DefaultName);
        }

        public class OptionsStartupValidator : IStartupValidator
        {
            private IServiceProvider _services;
            private StartupValidationOptions _options;

            public OptionsStartupValidator(IOptions<StartupValidationOptions> options, IServiceProvider services)
            {
                _services = services;
                _options = options.Value;
            }

            public void Validate()
            {
                var errors = new List<string>();
                foreach (var targetType in _options.ValidationTargets.Keys)
                {
                    var optionsType = typeof(IOptionsMonitor<>).MakeGenericType(targetType);
                    var monitor = _services.GetRequiredService(optionsType);
                    var getMethod = optionsType.GetMethod("Get");
                    foreach (var namedInstance in _options.ValidationTargets[targetType])
                    {
                        // TODO: maybe aggregate and catch all options instead of one at a time?
                        try
                        {
                            getMethod.Invoke(monitor, new object[] { namedInstance });
                        }
                        catch (Exception e)
                        {
                            if (e.InnerException is OptionsValidationException)
                            {
                                throw e.InnerException;
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public void CanValidateOptionsEagerly()
        {
            var services = new ServiceCollection();
            services.AddOptions<ComplexOptions>()
                .Configure(o =>
                {
                    o.Boolean = false;
                    o.Integer = 11;
                    o.Virtual = "wut";
                })
                .Validate(o => o.Boolean)
                .Validate(o => o.Virtual == null, "Virtual")
                .Validate(o => o.Integer > 12, "Integer");

            services.Configure<StartupValidationOptions>(o => o.Validate<ComplexOptions>());
            services.AddSingleton<IStartupValidator, OptionsStartupValidator>();

            var sp = services.BuildServiceProvider();

            var startupValidator = sp.GetRequiredService<IStartupValidator>();

            var error = Assert.Throws<OptionsValidationException>(() => startupValidator.Validate());
            ValidateFailure<ComplexOptions>(error, Options.DefaultName, 3, "A validation error has occurred.", "Virtual", "Integer");
        }

        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
        public class FromAttribute : ValidationAttribute
        {
            public string Accepted { get; set; }

            public override bool IsValid(object value)
                => value == null || value.ToString() == Accepted;
        }

        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
        public class DepValidator : ValidationAttribute
        {
            public string Target { get; set; }

            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                object instance = validationContext.ObjectInstance;
                Type type = instance.GetType();
                var dep1 = type.GetProperty("Dep1")?.GetValue(instance);
                var dep2 = type.GetProperty(Target)?.GetValue(instance);
                if (dep1 == dep2)
                {
                    return ValidationResult.Success;
                }
                return new ValidationResult("Dep1 != " + Target, new string[] { "Dep1", Target });
            }
        }

        private class AnnotatedOptions
        {
            [Required]
            public string Required { get; set; }

            [StringLength(5, ErrorMessage = "Too long.")]
            public string StringLength { get; set; }

            [Range(-5, 5, ErrorMessage = "Out of range.")]
            public int IntRange { get; set; }

            [From(Accepted = "USA")]
            public string Custom { get; set; }

            [DepValidator(Target = "Dep2")]
            public string Dep1 { get; set; }
            public string Dep2 { get; set; }
        }

        [Fact]
        public void CanValidateDataAnnotations()
        {
            var services = new ServiceCollection();
            services.AddOptions<AnnotatedOptions>()
                .Configure(o =>
                {
                    o.StringLength = "111111";
                    o.IntRange = 10;
                    o.Custom = "nowhere";
                    o.Dep1 = "Not dep2";
                })
                .ValidateDataAnnotations();

            var sp = services.BuildServiceProvider();

            var error = Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptions<AnnotatedOptions>>().Value);
            ValidateFailure<AnnotatedOptions>(error, Options.DefaultName, 5,
                "DataAnnotation validation failed for 'AnnotatedOptions' members: 'Required' with the error: 'The Required field is required.'.",
                "DataAnnotation validation failed for 'AnnotatedOptions' members: 'StringLength' with the error: 'Too long.'.",
                "DataAnnotation validation failed for 'AnnotatedOptions' members: 'IntRange' with the error: 'Out of range.'.",
                "DataAnnotation validation failed for 'AnnotatedOptions' members: 'Custom' with the error: 'The field Custom is invalid.'.",
                "DataAnnotation validation failed for 'AnnotatedOptions' members: 'Dep1,Dep2' with the error: 'Dep1 != Dep2'.");
        }

        [Fact]
        public void CanValidateMixDataAnnotations()
        {
            var services = new ServiceCollection();
            services.AddOptions<AnnotatedOptions>()
                .Configure(o =>
                {
                    o.StringLength = "111111";
                    o.IntRange = 10;
                    o.Custom = "nowhere";
                    o.Dep1 = "Not dep2";
                })
                .ValidateDataAnnotations()
                .Validate(o => o.Custom != "nowhere", "I don't want to go to nowhere!");

            var sp = services.BuildServiceProvider();

            var error = Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptions<AnnotatedOptions>>().Value);
            ValidateFailure<AnnotatedOptions>(error, Options.DefaultName, 6,
                "DataAnnotation validation failed for 'AnnotatedOptions' members: 'Required' with the error: 'The Required field is required.'.",
                "DataAnnotation validation failed for 'AnnotatedOptions' members: 'StringLength' with the error: 'Too long.'.",
                "DataAnnotation validation failed for 'AnnotatedOptions' members: 'IntRange' with the error: 'Out of range.'.",
                "DataAnnotation validation failed for 'AnnotatedOptions' members: 'Custom' with the error: 'The field Custom is invalid.'.",
                "DataAnnotation validation failed for 'AnnotatedOptions' members: 'Dep1,Dep2' with the error: 'Dep1 != Dep2'.",
                "I don't want to go to nowhere!");
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void ValidateOnStart_CallValidateDataAnnotations_ValidationSuccessful()
        {
            var services = new ServiceCollection();
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

            var sp = services.BuildServiceProvider();

            var error = Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptions<AnnotatedOptions>>().Value);
            ValidateFailure<AnnotatedOptions>(error, Options.DefaultName, 5,
                    "DataAnnotation validation failed for 'AnnotatedOptions' members: 'Required' with the error: 'The Required field is required.'.",
                    "DataAnnotation validation failed for 'AnnotatedOptions' members: 'StringLength' with the error: 'Too long.'.",
                    "DataAnnotation validation failed for 'AnnotatedOptions' members: 'IntRange' with the error: 'Out of range.'.",
                    "DataAnnotation validation failed for 'AnnotatedOptions' members: 'Custom' with the error: 'The field Custom is invalid.'.",
                    "DataAnnotation validation failed for 'AnnotatedOptions' members: 'Dep1,Dep2' with the error: 'Dep1 != Dep2'.");
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void ValidateOnStart_CallValidateAndValidateDataAnnotations_FailuresCaughtFromBothValidateAndValidateDataAnnotations()
        {
            var services = new ServiceCollection();

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

            var sp = services.BuildServiceProvider();

            var error = Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptions<AnnotatedOptions>>().Value);
            ValidateFailure<AnnotatedOptions>(error, Options.DefaultName, 6,
                    "DataAnnotation validation failed for 'AnnotatedOptions' members: 'Required' with the error: 'The Required field is required.'.",
                    "DataAnnotation validation failed for 'AnnotatedOptions' members: 'StringLength' with the error: 'Too long.'.",
                    "DataAnnotation validation failed for 'AnnotatedOptions' members: 'IntRange' with the error: 'Out of range.'.",
                    "DataAnnotation validation failed for 'AnnotatedOptions' members: 'Custom' with the error: 'The field Custom is invalid.'.",
                    "DataAnnotation validation failed for 'AnnotatedOptions' members: 'Dep1,Dep2' with the error: 'Dep1 != Dep2'.",
                    "I don't want to go to nowhere!");
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void ValidateOnStart_CallValidateOnStartFirst_ValidatesFailuresCorrectly()
        {
            var services = new ServiceCollection();

            services.AddOptions<AnnotatedOptions>()
                    .ValidateOnStart()
                    .Configure(o =>
                    {
                        o.StringLength = "111111";
                        o.IntRange = 10;
                        o.Custom = "nowhere";
                        o.Dep1 = "Not dep2";
                    })
                    .ValidateDataAnnotations()
                    .Validate(o => o.Custom != "nowhere", "I don't want to go to nowhere!");

            var sp = services.BuildServiceProvider();

            var error = Assert.Throws<OptionsValidationException>(() => sp.GetRequiredService<IOptions<AnnotatedOptions>>().Value);
            ValidateFailure<AnnotatedOptions>(error, Options.DefaultName, 6,
                    "DataAnnotation validation failed for 'AnnotatedOptions' members: 'Required' with the error: 'The Required field is required.'.",
                    "DataAnnotation validation failed for 'AnnotatedOptions' members: 'StringLength' with the error: 'Too long.'.",
                    "DataAnnotation validation failed for 'AnnotatedOptions' members: 'IntRange' with the error: 'Out of range.'.",
                    "DataAnnotation validation failed for 'AnnotatedOptions' members: 'Custom' with the error: 'The field Custom is invalid.'.",
                    "DataAnnotation validation failed for 'AnnotatedOptions' members: 'Dep1,Dep2' with the error: 'Dep1 != Dep2'.",
                    "I don't want to go to nowhere!");
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void ValidateOnStart_ConfigureBasedOnDataAnnotationRestrictions_ValidationSuccessful()
        {
            var services = new ServiceCollection();
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

            var sp = services.BuildServiceProvider();

            var value = sp.GetRequiredService<IOptions<AnnotatedOptions>>().Value;

            Assert.NotNull(value);
        }
    }
}
