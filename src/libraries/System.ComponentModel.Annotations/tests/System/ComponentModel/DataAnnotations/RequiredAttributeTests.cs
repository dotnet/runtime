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

            // default value types are always valid
            var requiredAttribute = new RequiredAttribute();
            yield return new TestCase(requiredAttribute, false);
            yield return new TestCase(requiredAttribute, 0);
            yield return new TestCase(requiredAttribute, 0d);
            yield return new TestCase(requiredAttribute, default(TimeSpan));
            yield return new TestCase(requiredAttribute, default(DateTime));
            yield return new TestCase(requiredAttribute, default(Guid));

            // non-default value types are always valid
            yield return new TestCase(requiredAttribute, true);
            yield return new TestCase(requiredAttribute, 1);
            yield return new TestCase(requiredAttribute, 0.1);
            yield return new TestCase(requiredAttribute, TimeSpan.MaxValue);
            yield return new TestCase(requiredAttribute, DateTime.MaxValue);
            yield return new TestCase(requiredAttribute, Guid.Parse("c3436566-4083-4bbe-8b56-f9c278162c4b"));

            // Populated System.Nullable values are always valid
            yield return new TestCase(new RequiredAttribute(), (bool?)false);
            yield return new TestCase(new RequiredAttribute(), (int?)0);
            yield return new TestCase(new RequiredAttribute(), (Guid?)Guid.Empty);
            yield return new TestCase(new RequiredAttribute(), (DateTime?)default(DateTime));
            yield return new TestCase(new RequiredAttribute(), (TimeSpan?)default(TimeSpan));
        }

        protected override IEnumerable<TestCase> InvalidValues()
        {
            yield return new TestCase(new RequiredAttribute(), null);
            yield return new TestCase(new RequiredAttribute() { AllowEmptyStrings = false }, string.Empty);
            yield return new TestCase(new RequiredAttribute() { AllowEmptyStrings = false }, " \t \r \n ");
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
    }
}
