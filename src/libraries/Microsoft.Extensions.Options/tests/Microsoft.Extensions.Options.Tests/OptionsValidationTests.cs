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

            var validator = sp.GetService<IStartupValidator>();
            Assert.Null(validator);
        }

        [Fact]
        public void ValidateOnStart_Called()
        {
            var services = new ServiceCollection();
            services.AddOptions<ComplexOptions>()
                .Validate(o => o.Integer > 12)
                .ValidateOnStart();

            var sp = services.BuildServiceProvider();

            var validator = sp.GetService<IStartupValidator>();
            Assert.NotNull(validator);
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

            var validator = sp.GetService<IStartupValidator>();
            Assert.NotNull(validator);
            OptionsValidationException ex = Assert.Throws<OptionsValidationException>(validator.Validate);
            Assert.Equal(2, ex.Failures.Count());
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
    }
}
