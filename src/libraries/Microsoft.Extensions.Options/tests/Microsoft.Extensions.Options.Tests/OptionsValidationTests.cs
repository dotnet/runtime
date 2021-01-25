// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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

    }
}
