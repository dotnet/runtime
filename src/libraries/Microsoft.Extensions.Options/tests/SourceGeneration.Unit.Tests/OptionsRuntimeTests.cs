// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
        public void TestAsyncOnlyValidatorInheritedSyncValidateRunsSyncValidation()
        {
            IValidateOptions<AsyncParityOptions> validator = new AsyncParityOptionsValidator();

            // Async self-validation alone must not make the generated synchronous path fail because
            // IAsyncValidatableObject requires an IValidatableObject fallback.
            ValidateOptionsResult syncResult = validator.Validate(
                "AsyncParity",
                new AsyncParityOptions { Name = "reserved", Age = 30 });
            Assert.True(syncResult.Succeeded);

            ValidateOptionsResult attributeResult = validator.Validate(
                "AsyncParity",
                new AsyncParityOptions { Name = "ok", Age = 0 });
            Assert.Equal(
                ["Age: The field AsyncParity.Age must be between 1 and 100."],
                attributeResult.Failures);
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

            // AsyncNestedOptions self-validates asynchronously. The async root must run that
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
        public async Task TestExplicitTypeTransitiveAndEnumeratedAsyncValidatorsAreAwaited()
        {
            // Both the explicit-type transitive validator ([ValidateObjectMembers(typeof(...))]) and the explicit-type
            // enumerated-items validator ([ValidateEnumeratedItems(typeof(...))]) implement ValidateAsync explicitly, so
            // the async parent must dispatch through IAsyncValidateOptions<T>. Calling the concrete validator type would
            // fail to compile, while dispatching through its synchronous Validate would silently skip the failure.
            var options = new ExplicitAsyncRootOptions
            {
                Nested = new AsyncNestedOptions { Level = 5, Id = "trigger-async" },
                Items = new() { new AsyncNestedOptions { Level = 5, Id = "trigger-async" } }
            };

            ValidateOptionsResult result = await new ExplicitAsyncRootOptionsValidator().ValidateAsync("Root", options, default);

            Assert.True(result.Failed);
            Assert.Equal(2, result.Failures.Count(f => f.Contains("Explicit async validation failed.")));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public async Task TestExplicitTypeTransitiveAndEnumeratedSyncValidatorsUseSyncFallback()
        {
            var options = new ExplicitSyncRootOptions
            {
                Nested = new AsyncNestedOptions(),
                Items = [new AsyncNestedOptions()]
            };

            ValidateOptionsResult result = await new ExplicitSyncRootOptionsValidator().ValidateAsync("Root", options, default);

            Assert.True(result.Failed);
            Assert.Equal(2, result.Failures.Count(f => f.Contains("Explicit sync validation failed.")));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public async Task TestGeneratedValidateAsyncForwardsCancellationTokenToChildValidatorsAndSelfValidation()
        {
            using var cts = new CancellationTokenSource();
            var nested = new CancellationObservingChildOptions();
            var item = new CancellationObservingChildOptions();
            var options = new CancellationObservingOptions
            {
                Name = "valid",
                Nested = nested,
                Items = [item]
            };

            ValidateOptionsResult result = await new CancellationObservingOptionsValidator().ValidateAsync("opt", options, cts.Token);

            Assert.True(result.Succeeded);
            Assert.Equal(cts.Token, nested.ValidatorCancellationToken);
            Assert.Equal(cts.Token, item.ValidatorCancellationToken);
            Assert.Equal(cts.Token, options.SelfValidationCancellationToken);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public async Task TestGeneratedValidateAsyncForwardsCancellationToAsyncValidationAttribute()
        {
            using var cts = new CancellationTokenSource();
            var options = new AsyncAttributeCancellationOptions { Name = "valid" };
            Task<ValidateOptionsResult> validationTask =
                new AsyncAttributeCancellationOptionsValidator().ValidateAsync("opt", options, cts.Token);

            try
            {
                await options.AttributeStarted.Task.WaitAsync(TimeSpan.FromSeconds(30));
                cts.Cancel();

                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => validationTask.WaitAsync(TimeSpan.FromSeconds(30)));
            }
            finally
            {
                options.AttributeRelease.TrySetResult(true);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public async Task TestGeneratedValidateAsyncNoAwaitPathRunsSynchronously()
        {
            var validator = new SyncSelfValidatingOptionsValidator();

            // The generated ValidateAsync for this model contains no genuine await, so the emitter wraps it in
            // #pragma warning disable/restore CS1998 rather than falling back to a non-async method returning
            // Task.FromResult(...); the method is still declared async so cancellationToken.ThrowIfCancellationRequested()
            // surfaces on the returned Task instead of throwing synchronously (see the dedicated cancellation test below).
            // A synchronously-completing async method still finishes eagerly without yielding, so this still runs synchronously.
            Task<ValidateOptionsResult> successTask = validator.ValidateAsync("opt", new SyncSelfValidatingOptions { Name = "ok" }, default);
            Assert.True(successTask.IsCompletedSuccessfully);
            Assert.True((await successTask).Succeeded);

            ValidateOptionsResult failure = await validator.ValidateAsync("opt", new SyncSelfValidatingOptions { Name = "bad" }, default);
            Assert.True(failure.Failed);
            Assert.Contains(failure.Failures, f => f.Contains("Sync self-validation failed."));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public async Task TestGeneratedValidateAsyncNoAwaitPathDoesNotThrowSynchronously()
        {
            var validator = new SyncSelfValidatingOptionsValidator();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Even though this model's generated ValidateAsync body has no genuine await (see the test above), the
            // method must still be declared async so that cancellationToken.ThrowIfCancellationRequested() surfaces
            // on the returned Task rather than throwing synchronously out of the call. Calling it here, without
            // await, must not throw synchronously even with an already-canceled token.
            Task<ValidateOptionsResult> task = validator.ValidateAsync("opt", new SyncSelfValidatingOptions { Name = "ok" }, cts.Token);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public async Task TestAsyncSelfValidationToleratesNullEnumerable()
        {
            var validator = new NullAsyncEnumerableOptionsValidator();

            // IAsyncValidatableObject.ValidateAsync returns null here despite its non-nullable contract. The
            // generated ValidateAsync must guard against this before "await foreach" (matching the runtime
            // Validator's own null-check) instead of throwing a NullReferenceException.
            ValidateOptionsResult success = await validator.ValidateAsync("opt", new NullAsyncEnumerableOptions { Name = "ok" }, default);
            Assert.True(success.Succeeded);

            ValidateOptionsResult failure = await validator.ValidateAsync("opt", new NullAsyncEnumerableOptions { Name = null }, default);
            Assert.True(failure.Failed);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public async Task TestSynthesizedValidatorNamesDoNotCollide()
        {
            // The synthesized child validator name for an implicitly-typed async transitive member is
            // "__" + modelName + "AsyncValidator__"; for a synchronous one it's "__" + modelName + "Validator__". A
            // model literally named "Foo" reached asynchronously ("__FooAsyncValidator__") and a model literally
            // named "FooAsync" reached synchronously ("__" + "FooAsync" + "Validator__" == "__FooAsyncValidator__")
            // therefore collide unless the generator deterministically de-duplicates. The mere presence of both
            // generated validators below in this compiled assembly proves the collision was resolved (otherwise the
            // assembly would not compile at all); the assertions confirm each root still validates correctly.
            var asyncRootValidator = new SynthesizedNameCollisionAsyncRootValidator();
            ValidateOptionsResult asyncResult = await asyncRootValidator.ValidateAsync(
                "opt", new SynthesizedNameCollisionAsyncRoot { Nested = new Foo { Name = null } }, default);
            Assert.True(asyncResult.Failed);

            var syncRootValidator = new SynthesizedNameCollisionSyncRootValidator();
            ValidateOptionsResult syncResult = syncRootValidator.Validate(
                "opt", new SynthesizedNameCollisionSyncRoot { Nested = new FooAsync { Name = null } });
            Assert.True(syncResult.Failed);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public async Task TestConcurrentMemberValidationRunsMembersConcurrently()
        {
            var gate = new ConcurrencyGate();
            var options = new ConcurrentMembersOptions
            {
                MemberA = new GateOptionsA { Gate = gate },
                MemberB = new GateOptionsB { Gate = gate }
            };

            var validator = new ConcurrentMembersOptionsValidator();

            ValidateOptionsResult result = await validator.ValidateAsync("opt", options, default);

            // Each member's transitive validator signals its own rendezvous slot and waits for the other member.
            // Sequential validation times out; success proves both member validations started before either completed.
            Assert.True(result.Succeeded);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public async Task TestGeneratedValidateAsyncMatchesValidatorParity()
        {
            var validator = new AsyncParityOptionsValidator();

            var valid = new AsyncParityOptions { Name = "ok", Age = 30 };
            var validResults = new List<ValidationResult>();
            bool validatorValid = await Validator.TryValidateObjectAsync(valid, new ValidationContext(valid), validResults, validateAllProperties: true);
            ValidateOptionsResult generatedValid = await validator.ValidateAsync("AsyncParityOptions", valid, default);
            Assert.True(validatorValid);
            Assert.True(generatedValid.Succeeded);
            Assert.Empty(validResults);

            // Attribute-only failure (Age out of range, name is valid and not reserved). Note: Validator.TryValidateObjectAsync
            // skips IAsyncValidatableObject.ValidateAsync once property validation fails, whereas the generated method always
            // runs it; the two agree here only because the self-validation yields nothing for this input, so we deliberately
            // avoid combining an attribute failure with a self-validation failure in one instance.
            var attributeFailure = new AsyncParityOptions { Name = "ok", Age = 0 };
            var attributeResults = new List<ValidationResult>();
            bool validatorAttr = await Validator.TryValidateObjectAsync(attributeFailure, new ValidationContext(attributeFailure), attributeResults, validateAllProperties: true);
            ValidateOptionsResult generatedAttr = await validator.ValidateAsync("AsyncParityOptions", attributeFailure, default);
            Assert.False(validatorAttr);
            Assert.True(generatedAttr.Failed);
            Assert.Equal(attributeResults.Count, generatedAttr.Failures.Count());

            // Self-validation-only failure (attributes pass, reserved name). Validator runs the async self-validation because
            // property validation succeeds, so both report the same single failure.
            var selfFailure = new AsyncParityOptions { Name = "reserved", Age = 30 };
            var selfResults = new List<ValidationResult>();
            bool validatorSelf = await Validator.TryValidateObjectAsync(selfFailure, new ValidationContext(selfFailure), selfResults, validateAllProperties: true);
            ValidateOptionsResult generatedSelf = await validator.ValidateAsync("AsyncParityOptions", selfFailure, default);
            Assert.False(validatorSelf);
            Assert.True(generatedSelf.Failed);
            Assert.Equal(selfResults.Count, generatedSelf.Failures.Count());
            Assert.Contains(generatedSelf.Failures, f => f.Contains("Name is reserved."));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        public async Task TestGeneratedValidatorRunsThroughAsyncStartupValidation()
        {
            var services = new ServiceCollection();
            services.AddOptions<AsyncParityOptions>()
                .Configure(options =>
                {
                    options.Name = "reserved";
                    options.Age = 30;
                })
                .Validate<AsyncParityOptionsValidator>()
                .ValidateOnStart();

            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            IValidateOptions<AsyncParityOptions> registered =
                Assert.Single(serviceProvider.GetServices<IValidateOptions<AsyncParityOptions>>());

            Assert.IsAssignableFrom<IAsyncValidateOptions<AsyncParityOptions>>(registered);

            OptionsValidationException error = await Assert.ThrowsAsync<OptionsValidationException>(
                () => serviceProvider.GetRequiredService<IAsyncStartupValidator>().ValidateAsync());

            Assert.Contains(error.Failures, failure => failure.Contains("Name is reserved."));
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
    public partial class AsyncOptionsValidator : IAsyncValidateOptions<AsyncOptions>
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

    // An explicitly specified transitive/enumerated validator (typeof(...)) that implements
    // IAsyncValidateOptions<T> must be dispatched through ValidateAsync by an async parent. Otherwise the parent calls
    // the child's synchronous Validate and silently skips the child's async-only validation.
    public class ExplicitAsyncRootOptions
    {
        [ValidateObjectMembers(typeof(ExplicitAsyncNestedValidator))]
        public AsyncNestedOptions? Nested { get; set; }

        [ValidateEnumeratedItems(typeof(ExplicitAsyncNestedValidator))]
        public List<AsyncNestedOptions>? Items { get; set; }
    }

    public sealed class ExplicitAsyncNestedValidator : IAsyncValidateOptions<AsyncNestedOptions>
    {
        public ValidateOptionsResult Validate(string? name, AsyncNestedOptions options) => ValidateOptionsResult.Success;

        Task<ValidateOptionsResult> IAsyncValidateOptions<AsyncNestedOptions>.ValidateAsync(
            string? name,
            AsyncNestedOptions options,
            CancellationToken cancellationToken)
            => Task.FromResult(ValidateOptionsResult.Fail("Explicit async validation failed."));
    }

    [OptionsValidator]
    public partial class ExplicitAsyncRootOptionsValidator : IAsyncValidateOptions<ExplicitAsyncRootOptions>
    {
    }

    public class ExplicitSyncRootOptions
    {
        [ValidateObjectMembers(typeof(ExplicitSyncNestedValidator))]
        public AsyncNestedOptions? Nested { get; set; }

        [ValidateEnumeratedItems(typeof(ExplicitSyncNestedValidator))]
        public List<AsyncNestedOptions>? Items { get; set; }
    }

    public sealed class ExplicitSyncNestedValidator : IValidateOptions<AsyncNestedOptions>
    {
        public ValidateOptionsResult Validate(string? name, AsyncNestedOptions options)
            => ValidateOptionsResult.Fail("Explicit sync validation failed.");
    }

    [OptionsValidator]
    public partial class ExplicitSyncRootOptionsValidator : IAsyncValidateOptions<ExplicitSyncRootOptions>
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CancellationObservingAttribute : AsyncValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
            => ValidationResult.Success;

        protected override Task<ValidationResult?> IsValidAsync(
            object? value,
            ValidationContext validationContext,
            CancellationToken cancellationToken)
        {
            AsyncAttributeCancellationOptions options =
                (AsyncAttributeCancellationOptions)validationContext.ObjectInstance;
            options.AttributeStarted.TrySetResult(true);
            return WaitForReleaseAsync(options, cancellationToken);
        }

        private static async Task<ValidationResult?> WaitForReleaseAsync(
            AsyncAttributeCancellationOptions options,
            CancellationToken cancellationToken)
        {
            await options.AttributeRelease.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return ValidationResult.Success;
        }
    }

    public sealed class AsyncAttributeCancellationOptions
    {
        [CancellationObserving]
        public string? Name { get; set; }

        public TaskCompletionSource<bool> AttributeStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> AttributeRelease { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    [OptionsValidator]
    public partial class AsyncAttributeCancellationOptionsValidator :
        IAsyncValidateOptions<AsyncAttributeCancellationOptions>
    {
    }

    public sealed class CancellationObservingChildOptions
    {
        public CancellationToken ValidatorCancellationToken { get; set; }
    }

    public sealed class CancellationObservingChildValidator : IAsyncValidateOptions<CancellationObservingChildOptions>
    {
        public ValidateOptionsResult Validate(string? name, CancellationObservingChildOptions options)
            => ValidateOptionsResult.Success;

        public Task<ValidateOptionsResult> ValidateAsync(
            string? name,
            CancellationObservingChildOptions options,
            CancellationToken cancellationToken = default)
        {
            options.ValidatorCancellationToken = cancellationToken;
            return Task.FromResult(ValidateOptionsResult.Success);
        }
    }

    public class CancellationObservingOptions : IAsyncValidatableObject
    {
        [Required]
        public string? Name { get; set; }

        [ValidateObjectMembers(typeof(CancellationObservingChildValidator))]
        public CancellationObservingChildOptions? Nested { get; set; }

        [ValidateEnumeratedItems(typeof(CancellationObservingChildValidator))]
        public List<CancellationObservingChildOptions>? Items { get; set; }

        public CancellationToken SelfValidationCancellationToken { get; set; }

        public async IAsyncEnumerable<ValidationResult> ValidateAsync(
            ValidationContext validationContext,
            [global::System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            SelfValidationCancellationToken = cancellationToken;
            await Task.Yield();
            yield break;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }

    [OptionsValidator]
    public partial class CancellationObservingOptionsValidator : IAsyncValidateOptions<CancellationObservingOptions>
    {
    }

    // Model that validates only synchronously (IValidatableObject, no attributes, no async children) but is validated by
    // an async validator, so the generated ValidateAsync body contains no genuine await and the emitter must suppress
    // CS1998 rather than dropping the async keyword (dropping it would make ThrowIfCancellationRequested() throw
    // synchronously instead of surfacing on the returned Task).
    public class SyncSelfValidatingOptions : IValidatableObject
    {
        public string? Name { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Name == "bad")
            {
                yield return new ValidationResult("Sync self-validation failed.", new[] { nameof(Name) });
            }
        }
    }

    [OptionsValidator]
    public partial class SyncSelfValidatingOptionsValidator : IAsyncValidateOptions<SyncSelfValidatingOptions>
    {
    }

    // Flat model (attributes + async self-validation, no nested members) so the generated ValidateAsync can be compared
    // for parity against Validator.TryValidateObjectAsync, which does not recurse into nested members.
    public class AsyncParityOptions : IAsyncValidatableObject
    {
        [Required]
        public string? Name { get; set; }

        [Range(1, 100)]
        public int Age { get; set; }

        public async IAsyncEnumerable<ValidationResult> ValidateAsync(
            ValidationContext validationContext,
            [global::System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            if (Name == "reserved")
            {
                yield return new ValidationResult("Name is reserved.", new[] { nameof(Name) });
            }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }

    [OptionsValidator]
    public partial class AsyncParityOptionsValidator : IAsyncValidateOptions<AsyncParityOptions>
    {
    }

    // Self-validation whose IAsyncValidatableObject.ValidateAsync returns null despite the interface's non-nullable
    // contract. The generated ValidateAsync must guard against this (matching the runtime Validator's own
    // null-check) rather than throwing a NullReferenceException from "await foreach".
    public class NullAsyncEnumerableOptions : IAsyncValidatableObject
    {
        [Required]
        public string? Name { get; set; }

        public IAsyncEnumerable<ValidationResult> ValidateAsync(ValidationContext validationContext, CancellationToken cancellationToken = default) => null!;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }

    [OptionsValidator]
    public partial class NullAsyncEnumerableOptionsValidator : IAsyncValidateOptions<NullAsyncEnumerableOptions>
    {
    }

    // Regression: the synthesized child validator name for an implicitly-typed async transitive member is
    // "__" + modelName + "AsyncValidator__", while for a synchronous one it's "__" + modelName + "Validator__". A
    // model literally named "Foo" reached asynchronously synthesizes "__FooAsyncValidator__", and a model literally
    // named "FooAsync" reached synchronously synthesizes the exact same candidate name
    // ("__" + "FooAsync" + "Validator__" == "__FooAsyncValidator__"). Without deterministic de-duplication these two
    // distinct synthesized validator types collide and the assembly fails to compile (duplicate type definition).
    public class Foo
    {
        [Required]
        public string? Name { get; set; }
    }

    public class FooAsync
    {
        [Required]
        public string? Name { get; set; }
    }

    public class SynthesizedNameCollisionAsyncRoot
    {
        [ValidateObjectMembers]
        public Foo? Nested { get; set; }
    }

    [OptionsValidator]
    public partial class SynthesizedNameCollisionAsyncRootValidator : IAsyncValidateOptions<SynthesizedNameCollisionAsyncRoot>
    {
    }

    public class SynthesizedNameCollisionSyncRoot
    {
        [ValidateObjectMembers]
        public FooAsync? Nested { get; set; }
    }

    [OptionsValidator]
    public partial class SynthesizedNameCollisionSyncRootValidator : IValidateOptions<SynthesizedNameCollisionSyncRoot>
    {
    }

    // Shared rendezvous used to prove that two members of ConcurrentMembersOptions are validated concurrently
    // (via Task.WhenAll) rather than sequentially.
    public sealed class ConcurrencyGate
    {
        public TaskCompletionSource<bool> ReachedA { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> ReachedB { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public class GateOptionsA
    {
        public ConcurrencyGate? Gate { get; set; }
    }

    public class GateOptionsB
    {
        public ConcurrencyGate? Gate { get; set; }
    }

    // Hand-written (not source-generated) explicit-type transitive validators: each signals its own rendezvous slot
    // and waits for the other's, so the overall ValidateAsync succeeds only if both run concurrently.
    public sealed class GateOptionsAValidator : IAsyncValidateOptions<GateOptionsA>
    {
        public ValidateOptionsResult Validate(string? name, GateOptionsA options) => ValidateOptionsResult.Success;

        public async Task<ValidateOptionsResult> ValidateAsync(string? name, GateOptionsA options, CancellationToken cancellationToken = default)
        {
            ConcurrencyGate gate = options.Gate!;
            gate.ReachedA.TrySetResult(true);
            await gate.ReachedB.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
            return ValidateOptionsResult.Success;
        }
    }

    public sealed class GateOptionsBValidator : IAsyncValidateOptions<GateOptionsB>
    {
        public ValidateOptionsResult Validate(string? name, GateOptionsB options) => ValidateOptionsResult.Success;

        public async Task<ValidateOptionsResult> ValidateAsync(string? name, GateOptionsB options, CancellationToken cancellationToken = default)
        {
            ConcurrencyGate gate = options.Gate!;
            gate.ReachedB.TrySetResult(true);
            await gate.ReachedA.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
            return ValidateOptionsResult.Success;
        }
    }

    public class ConcurrentMembersOptions
    {
        [ValidateObjectMembers(typeof(GateOptionsAValidator))]
        public GateOptionsA? MemberA { get; set; }

        [ValidateObjectMembers(typeof(GateOptionsBValidator))]
        public GateOptionsB? MemberB { get; set; }
    }

    [OptionsValidator]
    public partial class ConcurrentMembersOptionsValidator : IAsyncValidateOptions<ConcurrentMembersOptions>
    {
    }
#endif // NET
}
