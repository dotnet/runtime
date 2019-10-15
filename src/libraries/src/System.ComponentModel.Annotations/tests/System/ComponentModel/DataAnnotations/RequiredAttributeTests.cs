// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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
        }

        protected override IEnumerable<TestCase> InvalidValues()
        {
            yield return new TestCase(new RequiredAttribute(), null);
            yield return new TestCase(new RequiredAttribute() { AllowEmptyStrings = false }, string.Empty);
            yield return new TestCase(new RequiredAttribute() { AllowEmptyStrings = false }, " \t \r \n ");
        }

        [Fact]
        public static void AllowEmptyStrings_GetSet_ReturnsExpectected()
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
