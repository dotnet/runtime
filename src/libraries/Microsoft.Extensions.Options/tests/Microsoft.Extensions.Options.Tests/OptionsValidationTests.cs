// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Options.Tests
{
    public class OptionsValidationTest
    {
        [Fact]
        public void ValidationResultSuccessIfNameMatched()
        {
            var services = new ServiceCollection();
            services.AddOptions<ComplexOptions>()
                .Validate(o => o.Boolean)
                .Validate(o => o.Integer > 12);

            var sp = services.BuildServiceProvider();

            var validations = sp.GetService<IEnumerable<IValidateOptions<ComplexOptions>>>();
            var options = new ComplexOptions
            {
                Boolean = true,
                Integer = 13
            };
            foreach (var v in validations)
            {
                Assert.True(v.Validate(Options.DefaultName, options).Succeeded);
                Assert.True(v.Validate("Something", options).Skipped);
            }
        }

        [Fact]
        public void ValidateOnStart_NotCalled()
        {
            var services = new ServiceCollection();
            services.AddOptions<ComplexOptions>()
                .Validate(o => o.Integer > 12);

            var sp = services.BuildServiceProvider();

#pragma warning disable SYSLIB0066 // Tests the legacy IStartupValidator compatibility contract.
            Assert.Null(sp.GetService<IStartupValidator>());
#pragma warning restore SYSLIB0066
            Assert.Null(sp.GetService<IAsyncStartupValidator>());
        }

        [Fact]
        public void ValidateOnStart_Called()
        {
            var services = new ServiceCollection();
            services.AddOptions<ComplexOptions>()
                .Validate(o => o.Integer > 12)
                .ValidateOnStart();

            var sp = services.BuildServiceProvider();

#pragma warning disable SYSLIB0066 // Tests the legacy IStartupValidator compatibility contract.
            IStartupValidator validator = sp.GetRequiredService<IStartupValidator>();
#pragma warning restore SYSLIB0066
            OptionsValidationException ex = Assert.Throws<OptionsValidationException>(validator.Validate);
            Assert.Equal(1, ex.Failures.Count());
        }

        [Fact]
        public void ValidateOnStart_CalledMultiple()
        {
            var services = new ServiceCollection();
            services.AddOptions<ComplexOptions>()
                .Validate(o => o.Boolean)
                .Validate(o => o.Integer > 12)
                .ValidateOnStart();

            var sp = services.BuildServiceProvider();

#pragma warning disable SYSLIB0066 // Tests the legacy IStartupValidator compatibility contract.
            IStartupValidator validator = sp.GetRequiredService<IStartupValidator>();
#pragma warning restore SYSLIB0066
            OptionsValidationException ex = Assert.Throws<OptionsValidationException>(validator.Validate);
            Assert.Equal(2, ex.Failures.Count());
        }

        [Fact]
        public void StartupValidator_UnexpectedExceptionAfterValidationFailure_Aggregates()
        {
            var sequenceValidator = new SequencedValidateOptions();
            var services = new ServiceCollection();
            services.AddSingleton<IValidateOptions<ComplexOptions>>(sequenceValidator);
            services.AddOptions<ComplexOptions>("one").ValidateOnStart();
            services.AddOptions<ComplexOptions>("two").ValidateOnStart();
            services.AddOptions<ComplexOptions>("three").ValidateOnStart();
            using ServiceProvider sp = services.BuildServiceProvider();

#pragma warning disable SYSLIB0066 // Tests the legacy IStartupValidator compatibility contract.
            IStartupValidator validator = sp.GetRequiredService<IStartupValidator>();
#pragma warning restore SYSLIB0066
            AggregateException error = Assert.Throws<AggregateException>(validator.Validate);

            Assert.Equal(2, error.InnerExceptions.Count);
            Assert.Contains(error.InnerExceptions, exception => exception is OptionsValidationException);
            Assert.Contains(error.InnerExceptions, exception => exception is InvalidOperationException);
            Assert.Equal(2, sequenceValidator.Calls);
        }

        [Fact]
        public void ValidationResultSkippedIfNameNotMatched()
        {
            var services = new ServiceCollection();
            services.AddOptions<ComplexOptions>("Name")
                .Validate(o => o.Boolean);

            var sp = services.BuildServiceProvider();

            var validations = sp.GetService<IEnumerable<IValidateOptions<ComplexOptions>>>();
            var options = new ComplexOptions
            {
                Boolean = true,
            };
            foreach (var v in validations)
            {
                Assert.True(v.Validate(Options.DefaultName, options).Skipped);
                Assert.True(v.Validate("Name", options).Succeeded);
            }
        }

        [Fact]
        public void ValidationResultFailedOrSkipped()
        {
            var services = new ServiceCollection();
            services.AddOptions<ComplexOptions>("Name")
                .Validate(o => o.Boolean);

            var sp = services.BuildServiceProvider();

            var validations = sp.GetService<IEnumerable<IValidateOptions<ComplexOptions>>>();
            var options = new ComplexOptions
            {
                Boolean = false,
            };
            foreach (var v in validations)
            {
                Assert.True(v.Validate(Options.DefaultName, options).Skipped);
                Assert.True(v.Validate("Name", options).Failed);
            }
        }

        [Fact]
        public void ValidationCannotBeNull()
        {
            string validName = "Name";
            string validFailureMessage = "Something's wrong";
            object validDependency = new();

            Assert.Throws<ArgumentNullException>(() => new ValidateOptions<object>(validName, null, validFailureMessage));
            Assert.Throws<ArgumentNullException>(() => new ValidateOptions<object, object>(validName, validDependency, null, validFailureMessage));
            Assert.Throws<ArgumentNullException>(() => new ValidateOptions<object, object, object>(validName, validDependency, validDependency, null, validFailureMessage));
            Assert.Throws<ArgumentNullException>(() => new ValidateOptions<object, object, object, object>(validName, validDependency, validDependency, validDependency, null, validFailureMessage));
            Assert.Throws<ArgumentNullException>(() => new ValidateOptions<object, object, object, object, object>(validName, validDependency, validDependency, validDependency, validDependency, null, validFailureMessage));
            Assert.Throws<ArgumentNullException>(() => new ValidateOptions<object, object, object, object, object, object>(validName, validDependency, validDependency, validDependency, validDependency, validDependency, null, validFailureMessage));
        }

        private sealed class SequencedValidateOptions : IValidateOptions<ComplexOptions>
        {
            public int Calls { get; private set; }

            public ValidateOptionsResult Validate(string? name, ComplexOptions options)
            {
                Calls++;

                return Calls switch
                {
                    1 => ValidateOptionsResult.Fail("validation failed"),
                    2 => throw new InvalidOperationException("infrastructure failed"),
                    _ => ValidateOptionsResult.Success,
                };
            }
        }
    }
}
