// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Gen.OptionsValidation.Unit.Test
{
    public class OptionsRuntimeTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public void TestValidationSuccessResults()
        {
            MyOptions options = new()
            {
                Name = "T",
                Phone = "P",
                Age = 30,
                Nested = new()
                {
                    Tall = 10,
                    Id = "1",
                    Children1 = new()
                    {
                        new ChildOptions() { Name = "C1-1" },
                        new ChildOptions() { Name = "C1-2" }
                    },
                    Children2 = new List<ChildOptions>()
                    {
                        new ChildOptions() { Name = "C2-1" },
                        new ChildOptions() { Name = "C2-2" }
                    },
                    NestedList = new()
                    {
                        new NestedOptions() { Tall = 5, Id = "1" },
                        new NestedOptions() { Tall = 6, Id = "2" },
                        new NestedOptions() { Tall = 7, Id = "3" }
                    }
                }
            };

            MySourceGenOptionsValidator sourceGenOptionsValidator = new();
            DataAnnotationValidateOptions<MyOptions> dataAnnotationValidateOptions = new("MyOptions");

            ValidateOptionsResult result = sourceGenOptionsValidator.Validate("MyOptions", options);
            Assert.True(result.Succeeded);

            result = dataAnnotationValidateOptions.Validate("MyOptions", options);
            Assert.True(result.Succeeded);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public void TestBasicDataAnnotationFailures()
        {
            MyOptions options = new();

            MySourceGenOptionsValidator sourceGenOptionsValidator = new();
            DataAnnotationValidateOptions<MyOptions> dataAnnotationValidateOptions = new("MyOptions");

            ValidateOptionsResult result1 = sourceGenOptionsValidator.Validate("MyOptions", options);
            Assert.True(result1.Failed);
            Assert.Equal(new List<string>
                        {
                            "Age: The field MyOptions.Age must be between 0 and 100.",
                            "Name: The MyOptions.Name field is required.",
                            "Phone: The MyOptions.Phone field is required."
                        },
                        result1.Failures);

            ValidateOptionsResult result2 = dataAnnotationValidateOptions.Validate("MyOptions", options);
            Assert.True(result2.Failed);
            Assert.Equal(new List<string>
                        {
                            "DataAnnotation validation failed for 'MyOptions' members: 'Age' with the error: 'The field Age must be between 0 and 100.'.",
                            "DataAnnotation validation failed for 'MyOptions' members: 'Name' with the error: 'The Name field is required.'.",
                            "DataAnnotation validation failed for 'MyOptions' members: 'Phone' with the error: 'The Phone field is required.'."
                        },
                        result2.Failures);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public void TestValidationWithNestedTypes()
        {
            MyOptions options = new()
            {
                Name = "T",
                Phone = "P",
                Age = 30,
                Nested = new()
                {
                    Tall = 20,
                }
            };

            MySourceGenOptionsValidator sourceGenOptionsValidator = new();
            DataAnnotationValidateOptions<MyOptions> dataAnnotationValidateOptions = new("MyOptions");

            ValidateOptionsResult result1 = sourceGenOptionsValidator.Validate("MyOptions", options);
            Assert.True(result1.Failed);
            Assert.Equal(new List<string>
                        {
                            "Tall: The field MyOptions.Nested.Tall must be between 0 and 10.",
                            "Id: The MyOptions.Nested.Id field is required.",
                        },
                        result1.Failures);

            ValidateOptionsResult result2 = dataAnnotationValidateOptions.Validate("MyOptions", options);
            Assert.True(result2.Failed);
            Assert.Equal(new List<string>
                        {
                            "DataAnnotation validation failed for 'MyOptions.Nested' members: 'Tall' with the error: 'The field Tall must be between 0 and 10.'.",
                            "DataAnnotation validation failed for 'MyOptions.Nested' members: 'Id' with the error: 'The Id field is required.'.",
                        },
                        result2.Failures);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public void TestValidationWithEnumeration()
        {
            MyOptions options = new()
            {
                Name = "T",
                Phone = "P",
                Age = 30,
                Nested = new()
                {
                    Tall = 10,
                    Id = "1",
                    Children1 = new()
                    {
                        new ChildOptions(),
                        new ChildOptions(),
                        new ChildOptions()
                    },
                    Children2 = new List<ChildOptions>()
                    {
                        new ChildOptions(),
                        new ChildOptions(),
                        new ChildOptions()
                    },

                }
            };

            MySourceGenOptionsValidator sourceGenOptionsValidator = new();
            DataAnnotationValidateOptions<MyOptions> dataAnnotationValidateOptions = new("MyOptions");

            ValidateOptionsResult result1 = sourceGenOptionsValidator.Validate("MyOptions", options);
            Assert.True(result1.Failed);
            Assert.Equal(new List<string>
                        {
                            "Name: The MyOptions.Nested.Children1[0].Name field is required.",
                            "Name: The MyOptions.Nested.Children1[1].Name field is required.",
                            "Name: The MyOptions.Nested.Children1[2].Name field is required.",
                            "Name: The MyOptions.Nested.Children2[0].Name field is required.",
                            "Name: The MyOptions.Nested.Children2[1].Name field is required.",
                            "Name: The MyOptions.Nested.Children2[2].Name field is required.",
                        },
                        result1.Failures);

            ValidateOptionsResult result2 = dataAnnotationValidateOptions.Validate("MyOptions", options);
            Assert.True(result2.Failed);
            Assert.Equal(new List<string>
                        {
                            "DataAnnotation validation failed for 'MyOptions.Nested.Children1[0]' members: 'Name' with the error: 'The Name field is required.'.",
                            "DataAnnotation validation failed for 'MyOptions.Nested.Children1[1]' members: 'Name' with the error: 'The Name field is required.'.",
                            "DataAnnotation validation failed for 'MyOptions.Nested.Children1[2]' members: 'Name' with the error: 'The Name field is required.'.",
                            "DataAnnotation validation failed for 'MyOptions.Nested.Children2[0]' members: 'Name' with the error: 'The Name field is required.'.",
                            "DataAnnotation validation failed for 'MyOptions.Nested.Children2[1]' members: 'Name' with the error: 'The Name field is required.'.",
                            "DataAnnotation validation failed for 'MyOptions.Nested.Children2[2]' members: 'Name' with the error: 'The Name field is required.'.",
                        },
                        result2.Failures);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public void TestValidationWithCyclicReferences()
        {
            NestedOptions nestedOptions = new()
            {
                Tall = 10,
                Id = "2",
            };

            MyOptions options = new()
            {
                Name = "T",
                Phone = "P",
                Age = 30,
                Nested = nestedOptions,
            };

            nestedOptions.NestedList = new()
            {
                new NestedOptions() { Tall = 5, Id = "1" },
                nestedOptions, // Circular reference
                new NestedOptions() { Tall = 7, Id = "3" },
                nestedOptions  // Circular reference
            };

            MySourceGenOptionsValidator sourceGenOptionsValidator = new();
            DataAnnotationValidateOptions<MyOptions> dataAnnotationValidateOptions = new("MyOptions");

            ValidateOptionsResult result1 = sourceGenOptionsValidator.Validate("MyOptions", options);
            Assert.True(result1.Succeeded);

            ValidateOptionsResult result2 = dataAnnotationValidateOptions.Validate("MyOptions", options);
            Assert.True(result1.Succeeded);
        }
    }

    public class MyOptions
    {
        [Range(0, 100)]
        public int Age { get; set; } = 200;

        [Required]
        public string? Name { get; set; }

        [Required]
        public string? Phone { get; set; }

        [ValidateObjectMembers]
        public NestedOptions Nested { get; set; }
    }

    public class NestedOptions
    {
        [Range(0, 10)]
        public double Tall { get; set; }

        [Required]
        public string? Id { get; set; }

        [ValidateEnumeratedItems]
        public List<ChildOptions>? Children1 { get; set; }

        [ValidateEnumeratedItems]
        public IEnumerable<ChildOptions>? Children2 { get; set; }

#pragma warning disable SYSLIB1211 // Source gen does static analysis for circular reference. We need to disable it for this test.
        [ValidateEnumeratedItems]
        public List<NestedOptions> NestedList { get; set; } // To check cycling reference
#pragma warning restore SYSLIB1211
    }

    public class ChildOptions
    {
        [Required]
        public string? Name { get; set; }
    }

    public struct MyOptionsStruct
    {
        [Range(0, 100)]
        public int Age { get; set; }

        [Required]
        public string? Name { get; set; }

        [ValidateObjectMembers]
        public NestedOptions Nested { get; set; }
    }

    [OptionsValidator]
    public partial class MySourceGenOptionsValidator : IValidateOptions<MyOptions>
    {
    }
}