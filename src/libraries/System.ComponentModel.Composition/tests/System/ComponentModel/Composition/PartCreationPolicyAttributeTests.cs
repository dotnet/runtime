// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Composition
{
    public class PartCreationPolicyAttributeTests
    {
        [Fact]
        public void Constructor_ShouldSetCreationPolicyToGivenValue()
        {
            var expectations = Expectations.GetEnumValues<CreationPolicy>();

            foreach (var e in expectations)
            {
                var attribute = new PartCreationPolicyAttribute(e);

                Assert.Equal(e, attribute.CreationPolicy);
            }
        }

        [Fact]
        public void Constructor_OutOfRangeValueAsCreationPolicyArgument_ShouldSetCreationPolicy()
        {   // Attributes should not throw exceptions

            var expectations = Expectations.GetInvalidEnumValues<CreationPolicy>();

            foreach (var e in expectations)
            {
                var attribute = new PartCreationPolicyAttribute(e);

                Assert.Equal(e, attribute.CreationPolicy);
            }
        }
    }
}
