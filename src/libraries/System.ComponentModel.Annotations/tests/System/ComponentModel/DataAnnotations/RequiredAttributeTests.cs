// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class RequiredAttributeTests : ValidationAttributeTestBase
    {
        protected override IEnumerable<TestCase> ValidValues()
        {
            yield return new TestCase(new RequiredAttribute(), "SomeString");
            yield return new TestCase(new RequiredAttribute() { AllowEmptyStrings = true }, string.Empty);
            yield return new TestCase(new RequiredAttribute() { AllowEmptyStrings = true }, " \t \r \n ");
            yield return new TestCase(new RequiredAttribute(), new object());

            // default value types with DisallowAllDefaultValues turned off
            var requiredAttribute = new RequiredAttribute();
            yield return new TestCase(requiredAttribute, false);
            yield return new TestCase(requiredAttribute, 0);
            yield return new TestCase(requiredAttribute, 0d);
            yield return new TestCase(requiredAttribute, default(TimeSpan));
            yield return new TestCase(requiredAttribute, default(DateTime));
            yield return new TestCase(requiredAttribute, default(Guid));

            // non-default value types with DisallowAllDefaultValues turned on
            requiredAttribute = new RequiredAttribute { DisallowAllDefaultValues = true };
            yield return new TestCase(requiredAttribute, true);
            yield return new TestCase(requiredAttribute, 1);
            yield return new TestCase(requiredAttribute, 0.1);
            yield return new TestCase(requiredAttribute, TimeSpan.MaxValue);
            yield return new TestCase(requiredAttribute, DateTime.MaxValue);
            yield return new TestCase(requiredAttribute, Guid.Parse("c3436566-4083-4bbe-8b56-f9c278162c4b"));

            // reference types with DisallowAllDefaultValues turned on
            requiredAttribute = new RequiredAttribute { DisallowAllDefaultValues = true };
            yield return new TestCase(requiredAttribute, "SomeString");
            yield return new TestCase(requiredAttribute, new object());

            // reference types with DisallowAllDefaultValues and AllowEmptyStrings turned on
            requiredAttribute = new RequiredAttribute { DisallowAllDefaultValues = true, AllowEmptyStrings = true };
            yield return new TestCase(requiredAttribute, "SomeString");
            yield return new TestCase(requiredAttribute, string.Empty);
            yield return new TestCase(requiredAttribute, new object());
        }

        protected override IEnumerable<TestCase> InvalidValues()
        {
            yield return new TestCase(new RequiredAttribute(), null);
            yield return new TestCase(new RequiredAttribute() { AllowEmptyStrings = false }, string.Empty);
            yield return new TestCase(new RequiredAttribute() { AllowEmptyStrings = false }, " \t \r \n ");

            // default values with DisallowAllDefaultValues turned on
            var requiredAttribute = new RequiredAttribute { DisallowAllDefaultValues = true };
            yield return new TestCase(requiredAttribute, null);
            yield return new TestCase(requiredAttribute, false);
            yield return new TestCase(requiredAttribute, 0);
            yield return new TestCase(requiredAttribute, 0d);
            yield return new TestCase(requiredAttribute, default(TimeSpan));
            yield return new TestCase(requiredAttribute, default(DateTime));
            yield return new TestCase(requiredAttribute, default(Guid));
            yield return new TestCase(requiredAttribute, default(StructWithTrivialEquality));
            // Structs that are not default but *equal* default should also fail validation.
            yield return new TestCase(requiredAttribute, new StructWithTrivialEquality { Value = 42 });

            // default value properties with DisallowDefaultValues turned on
            requiredAttribute = new RequiredAttribute { DisallowAllDefaultValues = true };
            yield return new TestCase(requiredAttribute, null, CreatePropertyContext<object?>());
            yield return new TestCase(requiredAttribute, null, CreatePropertyContext<int?>());
            yield return new TestCase(requiredAttribute, false, CreatePropertyContext<bool>());
            yield return new TestCase(requiredAttribute, 0, CreatePropertyContext<int>());
            yield return new TestCase(requiredAttribute, 0d, CreatePropertyContext<double>());
            yield return new TestCase(requiredAttribute, default(TimeSpan), CreatePropertyContext<TimeSpan>());
            yield return new TestCase(requiredAttribute, default(DateTime), CreatePropertyContext<DateTime>());
            yield return new TestCase(requiredAttribute, default(Guid), CreatePropertyContext<Guid>());
            yield return new TestCase(requiredAttribute, default(ImmutableArray<int>), CreatePropertyContext<ImmutableArray<int>>());
            yield return new TestCase(requiredAttribute, default(StructWithTrivialEquality), CreatePropertyContext<StructWithTrivialEquality>());
            // Structs that are not default but *equal* default should also fail validation.
            yield return new TestCase(requiredAttribute, new StructWithTrivialEquality { Value = 42 }, CreatePropertyContext<StructWithTrivialEquality>());
        }

        [Theory]
        [MemberData(nameof(GetNonNullDefaultValues))]
        public void DefaultValueTypes_OnPolymorphicProperties_SucceedValidation(object defaultValue)
        {
            var attribute = new RequiredAttribute { DisallowAllDefaultValues = true };
            Assert.False(attribute.IsValid(defaultValue)); // Fails validation when no contexts present

            // Polymorphic contexts should succeed validation
            var polymorphicContext = CreatePropertyContext<object>();
            attribute.Validate(defaultValue, polymorphicContext);
            Assert.Equal(ValidationResult.Success, attribute.GetValidationResult(defaultValue, polymorphicContext));
        }

        public static IEnumerable<object[]> GetNonNullDefaultValues()
        {
            // default value types on polymorphic properties with DisallowDefaultValues turned on
            
            yield return new object[] { false };
            yield return new object[] { 0 };
            yield return new object[] { 0d };
            yield return new object[] { default(TimeSpan) };
            yield return new object[] { default(DateTime) };
            yield return new object[] { default(Guid) };
            yield return new object[] { default(ImmutableArray<int>) };
            yield return new object[] { default(StructWithTrivialEquality) };
            yield return new object[] { new StructWithTrivialEquality { Value = 42 } };
        }

        [Fact]
        public void AllowEmptyStrings_GetSet_ReturnsExpectected()
        {
            var attribute = new RequiredAttribute();
            Assert.False(attribute.AllowEmptyStrings);
            attribute.AllowEmptyStrings = true;
            Assert.True(attribute.AllowEmptyStrings);
            attribute.AllowEmptyStrings = false;
            Assert.False(attribute.AllowEmptyStrings);
        }

        [Fact]
        public void DisallowAllowAllDefaultValues_GetSet_ReturnsExpectected()
        {
            var attribute = new RequiredAttribute();
            Assert.False(attribute.DisallowAllDefaultValues);
            attribute.DisallowAllDefaultValues = true;
            Assert.True(attribute.DisallowAllDefaultValues);
            attribute.DisallowAllDefaultValues = false;
            Assert.False(attribute.DisallowAllDefaultValues);
        }

        private static ValidationContext CreatePropertyContext<T>()
            => new ValidationContext(new GenericPoco<T>()) { MemberName = nameof(GenericPoco<T>.Value) };

        public class GenericPoco<T>
        {
            public T Value { get; set; }
        }

        /// <summary>
        /// Defines a struct where all values are equal.
        /// </summary>
        public readonly struct StructWithTrivialEquality : IEquatable<StructWithTrivialEquality>
        {
            public int Value { get; init; }

            public bool Equals(StructWithTrivialEquality _) => true;
            public override bool Equals(object other) => other is StructWithTrivialEquality;
            public override int GetHashCode() => 0;
        }
    }
}
