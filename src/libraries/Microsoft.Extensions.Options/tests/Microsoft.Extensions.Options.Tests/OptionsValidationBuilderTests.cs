// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;

namespace Microsoft.Extensions.Options.Tests
{
    public class OptionsValidationBuilderTests
    {
        [Fact]
        public void ValidateEmptyBuilder()
        {
            ValidateOptionsResultBuilder builder = new();
            Assert.True(EqualResults(ValidateOptionsResult.Success, builder.Build()));

            builder.AddResult((ValidationResult)null);
            Assert.True(EqualResults(ValidateOptionsResult.Success, builder.Build()));

            builder.AddResult(ValidationResult.Success);
            Assert.True(EqualResults(ValidateOptionsResult.Success, builder.Build()));

            builder.AddResult(new ValidationResult(null));
            Assert.True(EqualResults(ValidateOptionsResult.Success, builder.Build()));

            builder.AddResult(new ValidationResult(null, null));
            Assert.True(EqualResults(ValidateOptionsResult.Success, builder.Build()));

            builder.AddResult(ValidateOptionsResult.Skip);
            Assert.True(EqualResults(ValidateOptionsResult.Success, builder.Build()));
        }

        [Fact]
        public void ValidateBuilderThrows()
        {
            ValidateOptionsResultBuilder builder = new();
            Assert.Throws<ArgumentNullException>(() => builder.AddError(null));
            Assert.Throws<ArgumentNullException>(() => builder.AddResult((ValidateOptionsResult)null));
        }

        [Fact]
        public void ValidateAddErrors()
        {
            ValidateOptionsResultBuilder builder = new();

            string errors = "Failure 1";
            builder.AddError(errors);
            ValidateOptionsResult r = builder.Build();
            Assert.False(EqualResults(ValidateOptionsResult.Success, r), $"{r.FailureMessage}");
            Assert.True(EqualResults(ValidateOptionsResult.Fail(errors), r), $"{r.FailureMessage} != {ValidateOptionsResult.Fail(errors).FailureMessage}");

            errors += "; Failure 2";
            builder.AddError("Failure 2");
            r = builder.Build();
            Assert.True(EqualResults(ValidateOptionsResult.Fail(errors), r), $"{r.FailureMessage} != {ValidateOptionsResult.Fail(errors).FailureMessage}");

            errors += "; Property Prop1: Failure 3";
            builder.AddError("Failure 3", "Prop1");
            r = builder.Build();
            Assert.True(EqualResults(ValidateOptionsResult.Fail(errors), r), $"{r.FailureMessage} != {ValidateOptionsResult.Fail(errors).FailureMessage}");
        }

        [Fact]
        public void ValidateAddValidationResult()
        {
            ValidateOptionsResultBuilder builder = new();

            string errors = "Failure 4";
            builder.AddResult(new ValidationResult("Failure 4"));
            ValidateOptionsResult r = builder.Build();
            Assert.True(EqualResults(ValidateOptionsResult.Fail(errors), r), $"{r.FailureMessage} != {ValidateOptionsResult.Fail(errors).FailureMessage}");

            errors += "; member1, member2: Failure 5";
            builder.AddResult(new ValidationResult("Failure 5", new List<string>() { "member1", "member2" }));
            r = builder.Build();
            Assert.True(EqualResults(ValidateOptionsResult.Fail(errors), r), $"{r.FailureMessage} != {ValidateOptionsResult.Fail(errors).FailureMessage}");

            builder.AddResults((IEnumerable<ValidationResult?>?) null);
            r = builder.Build();
            Assert.True(EqualResults(ValidateOptionsResult.Fail(errors), r), $"{r.FailureMessage} != {ValidateOptionsResult.Fail(errors).FailureMessage}");

            errors += "; Failure 6; Failure 7";
            builder.AddResults(
                new List<ValidationResult?>()
                {
                    new ValidationResult("Failure 6"),
                    null,
                    new ValidationResult("Failure 7"),
                    null
                });

            r = builder.Build();
            Assert.True(EqualResults(ValidateOptionsResult.Fail(errors), r), $"{r.FailureMessage} != {ValidateOptionsResult.Fail(errors).FailureMessage}");
        }

        [Fact]
        public void ValidateAddValidateOptionResult()
        {
            ValidateOptionsResultBuilder builder = new();

            string errors = "Failure 8";
            builder.AddResult(ValidateOptionsResult.Fail("Failure 8"));
            ValidateOptionsResult r = builder.Build();
            Assert.True(EqualResults(ValidateOptionsResult.Fail(errors), r), $"{r.FailureMessage} != {ValidateOptionsResult.Fail(errors).FailureMessage}");

            errors += "; Failure 9; Failure 10";
            builder.AddResult(ValidateOptionsResult.Fail(new List<string>() { "Failure 9", null, null, "Failure 10" }));
            r = builder.Build();
            Assert.True(EqualResults(ValidateOptionsResult.Fail(errors), r), $"{r.FailureMessage} != {ValidateOptionsResult.Fail(errors).FailureMessage}");
        }

        [Fact]
        public void ValidateClear()
        {
            ValidateOptionsResultBuilder builder = new();
            string errors = "Failure 10";

            builder.AddError(errors);
            ValidateOptionsResult r = builder.Build();
            Assert.True(EqualResults(ValidateOptionsResult.Fail(errors), r), $"{r.FailureMessage} != {ValidateOptionsResult.Fail(errors).FailureMessage}");

            builder.Clear();
            Assert.True(EqualResults(ValidateOptionsResult.Success, builder.Build()));

            errors = "Failure 11";
            builder.AddError(errors);
            r = builder.Build();
            Assert.True(EqualResults(ValidateOptionsResult.Fail(errors), r), $"{r.FailureMessage} != {ValidateOptionsResult.Fail(errors).FailureMessage}");
        }

        [Fact]
        public void ValidateAddingMixedErrors()
        {
            ValidateOptionsResultBuilder builder = new();
            string errors = "Failure 12";
            builder.AddError(errors);
            ValidateOptionsResult r = builder.Build();
            Assert.True(EqualResults(ValidateOptionsResult.Fail(errors), r), $"{r.FailureMessage} != {ValidateOptionsResult.Fail(errors).FailureMessage}");

            errors += "; Property Prop: Failure 13";
            builder.AddError("Failure 13", "Prop");
            r = builder.Build();
            Assert.True(EqualResults(ValidateOptionsResult.Fail(errors), r), $"{r.FailureMessage} != {ValidateOptionsResult.Fail(errors).FailureMessage}");

            errors += "; Failure 14";
            builder.AddResult(new ValidationResult("Failure 14"));
            r = builder.Build();
            Assert.True(EqualResults(ValidateOptionsResult.Fail(errors), r), $"{r.FailureMessage} != {ValidateOptionsResult.Fail(errors).FailureMessage}");

            errors += "; member1, member2: Failure 15";
            builder.AddResult(new ValidationResult("Failure 15", new List<string>() { "member1", "member2" }));
            r = builder.Build();
            Assert.True(EqualResults(ValidateOptionsResult.Fail(errors), r), $"{r.FailureMessage} != {ValidateOptionsResult.Fail(errors).FailureMessage}");

            errors += "; Failure 16; Failure 17";
            builder.AddResults(
                new List<ValidationResult?>()
                {
                    new ValidationResult("Failure 16"),
                    null,
                    new ValidationResult("Failure 17"),
                    null
                });

            r = builder.Build();
            Assert.True(EqualResults(ValidateOptionsResult.Fail(errors), r), $"{r.FailureMessage} != {ValidateOptionsResult.Fail(errors).FailureMessage}");

            errors += "; Failure 18";
            builder.AddResult(ValidateOptionsResult.Fail("Failure 18"));
            r = builder.Build();
            Assert.True(EqualResults(ValidateOptionsResult.Fail(errors), r), $"{r.FailureMessage} != {ValidateOptionsResult.Fail(errors).FailureMessage}");

            errors += "; Failure 19; Failure 20";
            builder.AddResult(ValidateOptionsResult.Fail(new List<string>() { "Failure 19", null, null, "Failure 20" }));
            r = builder.Build();
            Assert.True(EqualResults(ValidateOptionsResult.Fail(errors), r), $"{r.FailureMessage} != {ValidateOptionsResult.Fail(errors).FailureMessage}");

            builder.Clear();
            Assert.True(EqualResults(ValidateOptionsResult.Success, builder.Build()));
        }

        private static bool EqualResults(ValidateOptionsResult r1, ValidateOptionsResult r2) =>
            r1.Succeeded == r2.Succeeded &&
            r1.Skipped == r2.Skipped &&
            r1.Failed == r2.Failed &&
            r1.FailureMessage == r2.FailureMessage &&
            (r1.Failures == r1.Failures || (r1.Failures != null && r1.Failures != null && Enumerable.SequenceEqual(r1.Failures, r2.Failures)));
    }
}
