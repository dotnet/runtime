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
        public void TestObjectsWithIndexerProperties()
        {
            DataAnnotationValidateOptions<MyDictionaryOptions> dataAnnotationValidateOptions1 = new("MyDictionaryOptions");
            MyDictionaryOptionsOptionsValidator sourceGenOptionsValidator1 = new();

            var options1 = new MyDictionaryOptions();
            ValidateOptionsResult result1 = sourceGenOptionsValidator1.Validate("MyDictionaryOptions", options1);
            ValidateOptionsResult result2 = dataAnnotationValidateOptions1.Validate("MyDictionaryOptions", options1);

            Assert.True(result1.Succeeded);
            Assert.True(result2.Succeeded);

            DataAnnotationValidateOptions<MyListOptions<string>> dataAnnotationValidateOptions2 = new("MyListOptions");
            MyListOptionsOptionsValidator sourceGenOptionsValidator2 = new();

            var options2 = new MyListOptions<string>() { Prop = "test" };
            result1 = sourceGenOptionsValidator2.Validate("MyListOptions", options2);
            result2 = dataAnnotationValidateOptions2.Validate("MyListOptions", options2);

            Assert.True(result1.Succeeded);
            Assert.True(result2.Succeeded);
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

#if NET8_0_OR_GREATER
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public void TestNewDataAnnotationFailures()
        {
            NewAttributesValidator sourceGenValidator = new();

            OptionsUsingNewAttributes validOptions = new()
            {
                P1 = "123456", P2 = 2, P3 = 4, P4 = "c", P5 = "d"
            };

            ValidateOptionsResult result = sourceGenValidator.Validate("OptionsUsingNewAttributes", validOptions);
            Assert.True(result.Succeeded);

            OptionsUsingNewAttributes invalidOptions = new()
            {
                P1 = "123", P2 = 4, P3 = 1, P4 = "e", P5 = "c"
            };

            result = sourceGenValidator.Validate("OptionsUsingNewAttributes", invalidOptions);

            Assert.Equal(new []{
                "P1: The field OptionsUsingNewAttributes.P1 must be a string or collection type with a minimum length of '5' and maximum length of '10'.",
                "P2: The OptionsUsingNewAttributes.P2 field does not equal any of the values specified in AllowedValuesAttribute.",
                "P3: The OptionsUsingNewAttributes.P3 field equals one of the values specified in DeniedValuesAttribute.",
                "P4: The OptionsUsingNewAttributes.P4 field does not equal any of the values specified in AllowedValuesAttribute.",
                "P5: The OptionsUsingNewAttributes.P5 field equals one of the values specified in DeniedValuesAttribute."
            }, result.Failures);
        }
#endif // NET8_0_OR_GREATER
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

    public class MyDictionaryOptions : Dictionary<string, string> { [Required] public string Prop { get; set; } = "test"; }
    [OptionsValidator] public partial class MyDictionaryOptionsOptionsValidator : IValidateOptions<MyDictionaryOptions> { }

    public class MyListOptions<T> : List<T> { [Required] public T Prop { get; set; } = default; }
    [OptionsValidator] public partial class MyListOptionsOptionsValidator : IValidateOptions<MyListOptions<string>> { }

#if NET8_0_OR_GREATER
    public class OptionsUsingNewAttributes
    {
        [Length(5, 10)]
        public string P1 { get; set; }

        [AllowedValues(1, 2, 3)]
        public int P2 { get; set; }

        [DeniedValues(1, 2, 3)]
        public int P3 { get; set; }

        [AllowedValues(new object?[] { "a", "b", "c" })]
        public string P4 { get; set; }

        [DeniedValues(new object?[] { "a", "b", "c" })]
        public string P5 { get; set; }
    }

    [OptionsValidator]
    public partial class NewAttributesValidator : IValidateOptions<OptionsUsingNewAttributes>
    {
    }
#endif // NET8_0_OR_GREATER
}