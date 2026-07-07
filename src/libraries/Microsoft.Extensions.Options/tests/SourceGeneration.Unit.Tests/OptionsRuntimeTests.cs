// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading;
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

#if NET
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
#endif // NET

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public void TestCustomGeneratedAttributes()
        {
            OptionsUsingGeneratedAttributes noFailures = new OptionsUsingGeneratedAttributes()
            {
#if NET
                P0 = "123",
                P11 = new DateTime(2023, 2, 1),
                P12 = 6,
                P13 = 9,
                P14 = new List<string>() { "1", "2" },
                P15 = new FakeCount(5),
                P16 = new FakeCountChild(5),
                P17 = new int[] { 1, 2 },
                P18 = new List<string>() { "1", "2", "3" },
                P19 = new FakeCount(3),
                P20 = new FakeCountChild(3),
                P23 = new List<string>() { "1", "2", "3", "4" },
                P24 = new FakeCount(4),
                P25 = new FakeCountChild(4),
                P27 = new List<string> { "1", "2" },
                P28 = new HashSet<string> { "1", "2" },
                P29 = new List<string> { "1", "2", "3" },
                P30 = new HashSet<string> { "1", "2", "3" },
                P31 = new List<int> { 1, 2, 3, 4 },
                P32 = new HashSet<int> { 1, 2, 3, 4 },
#endif // NET
                P1 = 2,
                P2 = "12345",
                P3 = "12345",
                P4 = "12345",
                P5 = 4,
                P6 = 4,
                P7 = 15,
                P8 = 15,
                P9 = 2.5m,
                P10 = 14.0,
                P21 = new int[] { 1, 2, 3 },
                P22 = new int[] { 1, 2, 3, 4 },
                P26 = 14.0,
            };
            List<ValidationResult> results = new();
            Assert.True(Validator.TryValidateObject(noFailures, new ValidationContext(noFailures), results, true));

            OptionsUsingGeneratedAttributesValidator validator = new();
            Assert.True(validator.Validate("OptionsUsingGeneratedAttributes", noFailures).Succeeded);

            OptionsUsingGeneratedAttributes failing = new OptionsUsingGeneratedAttributes()
            {
#if NET
                P0 = "",
                P11 = new DateTime(2023, 1, 1),
                P12 = 5,
                P13 = 10,
                P14 = new List<string>() { "1" },
                P15 = new FakeCount(1),
                P16 = new FakeCountChild(11),
                P17 = new int[] { 1 },
                P18 = new List<string>() { "1", "2" },
                P19 = new FakeCount(2),
                P20 = new FakeCountChild(1),
                P23 = new List<string>() { "1", "2", "3", "4", "5" },
                P24 = new FakeCount(5),
                P25 = new FakeCountChild(5),
                P27 = new List<string> { "1" },
                P28 = new HashSet<string> { "1" },
                P29 = new List<string> { "1", "2" },
                P30 = new HashSet<string> { "1", "2" },
                P31 = new List<int> { 1, 2, 3, 4, 5 },
                P32 = new HashSet<int> { 1, 2, 3, 4, 5 },
#endif // NET
                P1 = 4,
                P2 = "1234",
                P3 = "123456",
                P4 = "12345",
                P5 = 10,
                P6 = 10,
                P7 = 5,
                P8 = 5,
                P9 = 4.0m,
                P10 = 20.0,
                P21 = new int[] { 1, 2 },
                P22 = new int[] { 1, 2, 3, 4, 5 },
                P26 = 20.0,
            };

            Assert.False(Validator.TryValidateObject(failing, new ValidationContext(failing), results, true));

            ValidateOptionsResult generatorResult = validator.Validate("OptionsUsingGeneratedAttributes", failing);
            Assert.True(generatorResult.Failed);

            Assert.Equal(new [] {
#if NET
                "P0: The field OptionsUsingGeneratedAttributes.P0 must be a string or collection type with a minimum length of '1' and maximum length of '3'.",
                string.Format(CultureInfo.CurrentCulture, "P11: The field OptionsUsingGeneratedAttributes.P11 must be between {0} and {1}.", new DateTime(2023, 1, 30), new DateTime(2023, 12, 30)),
                "P12: The field OptionsUsingGeneratedAttributes.P12 must be between 5 exclusive and 10.",
                "P13: The field OptionsUsingGeneratedAttributes.P13 must be between 5 and 10 exclusive.",
                "P14: The field OptionsUsingGeneratedAttributes.P14 must be a string or collection type with a minimum length of '2' and maximum length of '10'.",
                "P15: The field OptionsUsingGeneratedAttributes.P15 must be a string or collection type with a minimum length of '2' and maximum length of '10'.",
                "P16: The field OptionsUsingGeneratedAttributes.P16 must be a string or collection type with a minimum length of '2' and maximum length of '10'.",
                "P17: The field OptionsUsingGeneratedAttributes.P17 must be a string or collection type with a minimum length of '2' and maximum length of '10'.",
                "P18: The field OptionsUsingGeneratedAttributes.P18 must be a string or array type with a minimum length of '3'.",
                "P19: The field OptionsUsingGeneratedAttributes.P19 must be a string or array type with a minimum length of '3'.",
                "P20: The field OptionsUsingGeneratedAttributes.P20 must be a string or array type with a minimum length of '3'.",
                "P23: The field OptionsUsingGeneratedAttributes.P23 must be a string or array type with a maximum length of '4'.",
                "P24: The field OptionsUsingGeneratedAttributes.P24 must be a string or array type with a maximum length of '4'.",
                "P25: The field OptionsUsingGeneratedAttributes.P25 must be a string or array type with a maximum length of '4'.",
                "P27: The field OptionsUsingGeneratedAttributes.P27 must be a string or collection type with a minimum length of '2' and maximum length of '10'.",
                "P28: The field OptionsUsingGeneratedAttributes.P28 must be a string or collection type with a minimum length of '2' and maximum length of '10'.",
                "P29: The field OptionsUsingGeneratedAttributes.P29 must be a string or array type with a minimum length of '3'.",
                "P30: The field OptionsUsingGeneratedAttributes.P30 must be a string or array type with a minimum length of '3'.",
                "P31: The field OptionsUsingGeneratedAttributes.P31 must be a string or array type with a maximum length of '4'.",
                "P32: The field OptionsUsingGeneratedAttributes.P32 must be a string or array type with a maximum length of '4'.",
#endif // NET
                "P1: The field OptionsUsingGeneratedAttributes.P1 must be between 1 and 3.",
                "P2: The field OptionsUsingGeneratedAttributes.P2 must be a string or array type with a minimum length of '5'.",
                "P3: The field OptionsUsingGeneratedAttributes.P3 must be a string or array type with a maximum length of '5'.",
                "P4: 'OptionsUsingGeneratedAttributes.P4' and 'P2' do not match.",
                "P5: The field OptionsUsingGeneratedAttributes.P5 must be between 2 and 8.",
                "P6: The field OptionsUsingGeneratedAttributes.P6 must be between 2 and 8.",
                "P7: The field OptionsUsingGeneratedAttributes.P7 must be between 10 and 20.",
                "P8: The field OptionsUsingGeneratedAttributes.P8 must be between 10 and 20.",
                "P9: The field OptionsUsingGeneratedAttributes.P9 must be between 1.5 and 3.14.",
                "P10: The field OptionsUsingGeneratedAttributes.P10 must be between 12.4 and 16.5.",
                "P21: The field OptionsUsingGeneratedAttributes.P21 must be a string or array type with a minimum length of '3'.",
                "P22: The field OptionsUsingGeneratedAttributes.P22 must be a string or array type with a maximum length of '4'.",
                "P26: The field OptionsUsingGeneratedAttributes.P26 must be between 12.4 and 16.5.",
            }, generatorResult.Failures);

            Assert.Equal(results.Count(), generatorResult.Failures.Count());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public void TestGeneratedRangeAttributeThreadSafety()
        {
            OptionsWithTimeSpanRangeAttribute options = new OptionsWithTimeSpanRangeAttribute() { Name = "T1", Period = TimeSpan.FromHours(1) };
            TimeSpanRangeAttributeValidator validator = new TimeSpanRangeAttributeValidator();

            var barrier = new Barrier(8);
            Task.WaitAll(
                (from i in Enumerable.Range(0, barrier.ParticipantCount)
                select Task.Factory.StartNew(() =>
                {
                    barrier.SignalAndWait();
                    ValidateOptionsResult result = validator.Validate("T1", options);
                    Assert.True(result.Succeeded);
                }, TaskCreationOptions.LongRunning)).ToArray());
        }

#if NET
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public async Task TestAsyncValidationSucceeds()
        {
            AsyncOptions options = new()
            {
                Name = "Valid",
                Age = 30,
                Nested = new()
                {
                    Level = 5,
                    Id = "1",
                    Children = new() { new AsyncChildOptions { Name = "C1" } }
                }
            };

            AsyncOptionsValidator validator = new();

            ValidateOptionsResult asyncResult = await validator.ValidateAsync("AsyncOptions", options, default);
            Assert.True(asyncResult.Succeeded);

            // The generated ValidateAsync must agree with the synchronous Validate for the same input.
            ValidateOptionsResult syncResult = validator.Validate("AsyncOptions", options);
            Assert.True(syncResult.Succeeded);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public async Task TestAsyncValidationFailures()
        {
            AsyncOptions options = new()
            {
                Name = "Invalid", // trips self-validation
                Age = 0,          // out of [Range(1, 100)]
                Nested = new()
                {
                    Level = 50,   // out of [Range(0, 10)]
                    Id = null,    // [Required]
                    Children = new() { new AsyncChildOptions { Name = null } } // [Required] on enumerated item
                }
            };

            AsyncOptionsValidator validator = new();

            ValidateOptionsResult asyncResult = await validator.ValidateAsync("AsyncOptions", options, default);
            Assert.True(asyncResult.Failed);

            // Attribute, nested object-member, enumerated-item failures, plus the async self-validation failure,
            // are all surfaced. The self-validation entry ("Async self-validation failed.") proves the generated
            // ValidateAsync dispatches to IAsyncValidatableObject.ValidateAsync (await foreach) rather than the
            // synchronous IValidatableObject.Validate path.
            Assert.Equal(new List<string>
                        {
                            "Age: The field AsyncOptions.Age must be between 1 and 100.",
                            "Level: The field AsyncOptions.Nested.Level must be between 0 and 10.",
                            "Id: The AsyncOptions.Nested.Id field is required.",
                            "Name: The AsyncOptions.Nested.Children[0].Name field is required.",
                            "Async self-validation failed.",
                        },
                        asyncResult.Failures);

            // The synchronous Validate path uses the synchronous self-validation instead, confirming the two code
            // paths are genuinely distinct and the async test would not pass if ValidateAsync silently ran the sync path.
            ValidateOptionsResult syncResult = validator.Validate("AsyncOptions", options);
            Assert.True(syncResult.Failed);
            Assert.Contains("Sync self-validation failed.", syncResult.Failures);
            Assert.DoesNotContain("Async self-validation failed.", syncResult.Failures);
            Assert.DoesNotContain("Sync self-validation failed.", asyncResult.Failures);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public async Task TestAsyncValidationWithSharedSynthesizedValidator()
        {
            // The async AsyncOptionsValidator and the synchronous SyncRootReusingNestedOptionsValidator both nest
            // AsyncNestedOptions, so they share a single synthesized child validator. Exercise both roots to confirm the
            // shared child validates correctly from an async caller and a sync caller alike.
            AsyncOptions asyncOptions = new()
            {
                Name = "Valid",
                Age = 30,
                Nested = new() { Level = 50, Id = null, Children = new() { new AsyncChildOptions { Name = "C1" } } }
            };

            ValidateOptionsResult asyncResult = await new AsyncOptionsValidator().ValidateAsync("AsyncOptions", asyncOptions, default);
            Assert.True(asyncResult.Failed);
            Assert.Contains("Level: The field AsyncOptions.Nested.Level must be between 0 and 10.", asyncResult.Failures);
            Assert.Contains("Id: The AsyncOptions.Nested.Id field is required.", asyncResult.Failures);

            SyncRootReusingNestedOptions syncOptions = new()
            {
                Nested = new() { Level = 5, Id = "1", Children = new() { new AsyncChildOptions { Name = "C1" } } }
            };

            ValidateOptionsResult syncResult = new SyncRootReusingNestedOptionsValidator().Validate("SyncRoot", syncOptions);
            Assert.True(syncResult.Succeeded);

            // Finding 3 regression: AsyncNestedOptions self-validates asynchronously. The async root must run that
            // nested async self-validation, while the synchronous root sharing the same nested model type must not.
            // Keying synthesized validators by model type alone would let the async root reuse a synchronous-only
            // child (depending on discovery order) and silently skip nested async self-validation.
            AsyncOptions asyncSelfValidating = new()
            {
                Name = "Valid",
                Age = 30,
                Nested = new() { Level = 5, Id = "trigger-async", Children = new() { new AsyncChildOptions { Name = "C1" } } }
            };

            ValidateOptionsResult nestedAsyncResult = await new AsyncOptionsValidator().ValidateAsync("AsyncOptions", asyncSelfValidating, default);
            Assert.True(nestedAsyncResult.Failed);
            Assert.Contains("Nested async self-validation failed.", nestedAsyncResult.Failures);

            SyncRootReusingNestedOptions syncSelfValidating = new()
            {
                Nested = new() { Level = 5, Id = "trigger-async", Children = new() { new AsyncChildOptions { Name = "C1" } } }
            };

            ValidateOptionsResult syncNestedResult = new SyncRootReusingNestedOptionsValidator().Validate("SyncRoot", syncSelfValidating);
            Assert.True(syncNestedResult.Succeeded);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public async Task TestManualValidateAsyncIsNotOverwritten()
        {
            // The generator must not emit a ValidateAsync for ManualAsyncOptionsValidator (that would be a duplicate
            // member); the hand-written implementation is the one invoked.
            ValidateOptionsResult result = await new ManualAsyncOptionsValidator().ValidateAsync("ManualAsyncOptions", new ManualAsyncOptions { Name = "Valid" }, default);
            Assert.True(result.Failed);
            Assert.Contains("Manual ValidateAsync invoked.", result.Failures);
        }
#endif // NET
    }

    public class FakeCount(int count) { public int Count { get { return count; } } }
    public class FakeCountChild(int count) : FakeCount(count) { }

    public class OptionsUsingGeneratedAttributes
    {
#if NET
        [LengthAttribute(1, 3)]
        public string? P0 { get; set; }

        [RangeAttribute(typeof(DateTime), "01/30/2023", "12/30/2023", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
        public DateTime P11 { get; set; }

        [RangeAttribute(5, 10, MinimumIsExclusive = true)]
        public int P12 { get; set; }

        [RangeAttribute(5, 10, MaximumIsExclusive = true)]
        public int P13 { get; set; }

        [LengthAttribute(2, 10)]
        public List<string> P14 { get; set; }

        [LengthAttribute(2, 10)]
        public FakeCount P15 { get; set; }

        [LengthAttribute(2, 10)]
        public FakeCountChild P16 { get; set; }

        [LengthAttribute(2, 10)]
        public int[] P17 { get; set; }

        // Although MinLength and MaxLength attributes defined in NETFX but the implementation there has a bug which can produce exception like the following when using types like List<string>:
        // System.InvalidCastException : Unable to cast object of type 'System.Collections.Generic.List`1[System.String]' to type 'System.Array'.

        [MinLengthAttribute(3)]
        public List<string> P18 { get; set; }

        [MinLengthAttribute(3)]
        public FakeCount P19 { get; set; }

        [MinLengthAttribute(3)]
        public FakeCountChild P20 { get; set; }

        [MaxLengthAttribute(4)]
        public List<string> P23 { get; set; }

        [MaxLengthAttribute(4)]
        public FakeCount P24 { get; set; }

        [MaxLengthAttribute(4)]
        public FakeCountChild P25 { get; set; }

        [LengthAttribute(2, 10)]
        public IList<string> P27 { get; set; }

        [LengthAttribute(2, 10)]
        public ICollection<string> P28 { get; set; }

        [MinLengthAttribute(3)]
        public IList<string> P29 { get; set; }

        [MinLengthAttribute(3)]
        public ICollection<string> P30 { get; set; }

        [MaxLengthAttribute(4)]
        public IList<int> P31 { get; set; }

        [MaxLengthAttribute(4)]
        public ICollection<int> P32 { get; set; }
#endif // NET

        [RangeAttribute(1, 3)]
        public int P1 { get; set; }

        [MinLengthAttribute(5)]
        public string? P2 { get; set; }

        [MaxLengthAttribute(5)]
        public string? P3 { get; set; }

        [CompareAttribute("P2")]
        public string? P4 { get; set; }

        [RangeAttribute(typeof(byte), "2", "8")]
        public byte P5 { get; set; }

        [RangeAttribute(typeof(sbyte), "2", "8")]
        public sbyte P6 { get; set; }

        [RangeAttribute(typeof(short), "10", "20")]
        public short P7 { get; set; }

        [RangeAttribute(typeof(ulong), "10", "20")]
        public ulong P8 { get; set; }

        [RangeAttribute(typeof(decimal), "1.5", "3.14")]
        public decimal P9 { get; set; }

        [RangeAttribute(typeof(double), "12.40", "16.50")]
        public double P10 { get; set; }

        [MinLengthAttribute(3)]
        public int[] P21 { get; set; }

        [MaxLengthAttribute(4)]
        public int[] P22 { get; set; }

        [RangeAttribute(typeof(double), "12.40", "16.50")]
        public double? P26 { get; set; }
    }

    [OptionsValidator]
    public partial class OptionsUsingGeneratedAttributesValidator : IValidateOptions<OptionsUsingGeneratedAttributes>
    {
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

#if NET
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
#endif // NET


    public class OptionsWithTimeSpanRangeAttribute
    {
        [Required]
        public string Name { get; set; }

        [RangeAttribute(typeof(TimeSpan), "01:00:00", "23:59:59")]
        public TimeSpan Period { get; set; }
    }

    [OptionsValidator]
    public partial class TimeSpanRangeAttributeValidator : IValidateOptions<OptionsWithTimeSpanRangeAttribute>
    {
    }

#if NET
    public class AsyncChildOptions
    {
        [Required]
        public string? Name { get; set; }
    }

    public class AsyncNestedOptions : IAsyncValidatableObject
    {
        [Range(0, 10)]
        public int Level { get; set; }

        [Required]
        public string? Id { get; set; }

        [ValidateEnumeratedItems]
        public List<AsyncChildOptions>? Children { get; set; }

        public async IAsyncEnumerable<ValidationResult> ValidateAsync(
            ValidationContext validationContext,
            [global::System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            if (Id == "trigger-async")
            {
                yield return new ValidationResult("Nested async self-validation failed.");
            }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }

    public class AsyncOptions : IAsyncValidatableObject
    {
        [Required]
        public string? Name { get; set; }

        [Range(1, 100)]
        public int Age { get; set; }

        [ValidateObjectMembers]
        public AsyncNestedOptions? Nested { get; set; }

        public async IAsyncEnumerable<ValidationResult> ValidateAsync(
            ValidationContext validationContext,
            [global::System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            if (Name == "Invalid")
            {
                yield return new ValidationResult("Async self-validation failed.");
            }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Name == "Invalid")
            {
                yield return new ValidationResult("Sync self-validation failed.");
            }
        }
    }

    [OptionsValidator]
    public partial class AsyncOptionsValidator : IValidateOptions<AsyncOptions>, IAsyncValidateOptions<AsyncOptions>
    {
    }

    // Regression: a synchronous validator that nests the same type (AsyncNestedOptions) as the async
    // AsyncOptionsValidator. Synthesized child validators are cached per model type and per capability
    // (synchronous vs asynchronous), so the async root gets an async-capable child and the synchronous root gets a
    // synchronous one regardless of discovery order. This guards against the async root emitting
    // "await child.ValidateAsync(...)" against a synthesized child generated without a ValidateAsync method, and
    // against the async root silently falling back to the synchronous child's Validate path.
    public class SyncRootReusingNestedOptions
    {
        [ValidateObjectMembers]
        public AsyncNestedOptions? Nested { get; set; }
    }

    [OptionsValidator]
    public partial class SyncRootReusingNestedOptionsValidator : IValidateOptions<SyncRootReusingNestedOptions>
    {
    }

    public class ManualAsyncOptions
    {
        [Required]
        public string? Name { get; set; }
    }

    // Regression: the validator implements IAsyncValidateOptions<T> but supplies its own ValidateAsync. The generator
    // must skip emitting ValidateAsync to avoid a duplicate-member compile error, and the hand-written method must win.
    [OptionsValidator]
    public partial class ManualAsyncOptionsValidator : IValidateOptions<ManualAsyncOptions>, IAsyncValidateOptions<ManualAsyncOptions>
    {
        public Task<ValidateOptionsResult> ValidateAsync(string? name, ManualAsyncOptions options, CancellationToken cancellationToken = default)
            => Task.FromResult(ValidateOptionsResult.Fail("Manual ValidateAsync invoked."));
    }
#endif // NET
}
