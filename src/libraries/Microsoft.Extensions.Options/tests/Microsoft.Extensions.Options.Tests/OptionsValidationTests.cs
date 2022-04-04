// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Options.Tests
{
    public class AnnotatedOptions
    {
        [Required]
        public string Required { get; set; }

        [StringLength(5, ErrorMessage = "Too long.")]
        public string StringLength { get; set; }

        [Range(-5, 5, ErrorMessage = "Out of range.")]
        public int IntRange { get; set; }

        public AnnotatedOptionsSubsection UnattributedAnnotatedOptionSubsection { get; set; }

        [Required]
        public AnnotatedOptionsSubsection AnnotatedOptionSubsection { get; set; }
    }

    public class AnnotatedOptionsSubsection
    {
        [Range(-5, 5, ErrorMessage = "Really out of range.")]
        public int IntRange2 { get; set; }
    }
    
    public class OptionsValidationTest
    {
        [Fact]
        public void ValidationResultFailedIfPropertyInUnattributedNestedValueFails()
        {
            var services = new ServiceCollection();
            services.AddOptions<AnnotatedOptions>().Configure(o => { }).ValidateDataAnnotations();

            var sp = services.BuildServiceProvider();

            var validations = sp.GetService<IEnumerable<IValidateOptions<AnnotatedOptions>>>();
            var options = new AnnotatedOptions
            {
                IntRange = 4,
                Required = "aaa",
                StringLength = "222",
                AnnotatedOptionSubsection = new AnnotatedOptionsSubsection
                {
                    IntRange2 = 3 // in range
                },
                UnattributedAnnotatedOptionSubsection = new AnnotatedOptionsSubsection
                {
                    IntRange2 = 100 // out of range
                }
            };

            foreach (var v in validations)
            {
                ValidateOptionsResult result = v.Validate(Options.DefaultName, options);
                Assert.True(result.Failed);
                Assert.Equal(1, result.Failures.Count());
                Assert.Equal("DataAnnotation validation failed for 'AnnotatedOptionsSubsection' members: 'IntRange2' with the error: 'Really out of range.'.", result.Failures.Single());
            }
        }

        [Fact]
        public void ValidationResultFailedIfPropertyInUnattributedNestedValueInNamedInstanceFails()
        {
            var services = new ServiceCollection();
            services.AddOptions<AnnotatedOptions>("MyOptions!").Configure(o => { }).ValidateDataAnnotations();

            var sp = services.BuildServiceProvider();

            var validations = sp.GetService<IEnumerable<IValidateOptions<AnnotatedOptions>>>();
            var options = new AnnotatedOptions
            {
                IntRange = 4,
                Required = "aaa",
                StringLength = "222",
                AnnotatedOptionSubsection = new AnnotatedOptionsSubsection
                {
                    IntRange2 = 3 // in range
                },
                UnattributedAnnotatedOptionSubsection = new AnnotatedOptionsSubsection
                {
                    IntRange2 = 100 // out of range
                }
            };

            foreach (var v in validations)
            {
                ValidateOptionsResult result = v.Validate("MyOptions!", options);
                Assert.True(result.Failed);
                Assert.Equal(1, result.Failures.Count());
                Assert.Equal("DataAnnotation validation failed for 'AnnotatedOptionsSubsection' members: 'IntRange2' with the error: 'Really out of range.'.", result.Failures.Single());
            }
        }

        [Fact]
        public void ValidationResultFailedIfPropertyInNestedValueFails()
        {
            var services = new ServiceCollection();
            services.AddOptions<AnnotatedOptions>().Configure(o => { }).ValidateDataAnnotations();

            var sp = services.BuildServiceProvider();

            var validations = sp.GetService<IEnumerable<IValidateOptions<AnnotatedOptions>>>();
            var options = new AnnotatedOptions
            {
                IntRange = 4,
                Required = "aaa",
                StringLength = "222",
                AnnotatedOptionSubsection = new AnnotatedOptionsSubsection
                {
                    IntRange2 = 100 // out of range
                }
            };

            foreach (var v in validations)
            {
                ValidateOptionsResult result = v.Validate(Options.DefaultName, options);
                Assert.True(result.Failed);
                Assert.Equal(1, result.Failures.Count());
                Assert.Equal("DataAnnotation validation failed for 'AnnotatedOptionsSubsection' members: 'IntRange2' with the error: 'Really out of range.'.", result.Failures.Single());
            }
        }

        [Fact]
        public void HandlesNullNestedValue()
        {
            var services = new ServiceCollection();
            services.AddOptions<AnnotatedOptions>().Configure(o => { }).ValidateDataAnnotations();

            var sp = services.BuildServiceProvider();

            var validations = sp.GetService<IEnumerable<IValidateOptions<AnnotatedOptions>>>();
            var options = new AnnotatedOptions
            {
                IntRange = 4,
                Required = "aaa",
                StringLength = "222",
                AnnotatedOptionSubsection = new AnnotatedOptionsSubsection(),
                UnattributedAnnotatedOptionSubsection = null!
            };

            foreach (var v in validations)
            {
                ValidateOptionsResult result = v.Validate(Options.DefaultName, options);
                Assert.True(result.Succeeded);
            }
        }

        [Fact]
        public void ReportsValidationErrorsInTopLevelAndSubLevelOptions()
        {
            var services = new ServiceCollection();
            services.AddOptions<AnnotatedOptions>().Configure(o => { }).ValidateDataAnnotations();

            var sp = services.BuildServiceProvider();

            var validations = sp.GetService<IEnumerable<IValidateOptions<AnnotatedOptions>>>();
            var options = new AnnotatedOptions
            {
                IntRange = 200, // out of range
                Required = "aaa",
                StringLength = "222",
                AnnotatedOptionSubsection = new AnnotatedOptionsSubsection
                {
                    IntRange2 = 100 // out of range
                }
            };

            foreach (var v in validations)
            {
                ValidateOptionsResult result = v.Validate(Options.DefaultName, options);
                Assert.True(result.Failed);
                Assert.Equal(2, result.Failures.Count());
                Assert.Equal("DataAnnotation validation failed for 'AnnotatedOptions' members: 'IntRange' with the error: 'Out of range.'.", result.Failures.ElementAt(0));
                Assert.Equal("DataAnnotation validation failed for 'AnnotatedOptionsSubsection' members: 'IntRange2' with the error: 'Really out of range.'.", result.Failures.ElementAt(1));
            }
        }

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
