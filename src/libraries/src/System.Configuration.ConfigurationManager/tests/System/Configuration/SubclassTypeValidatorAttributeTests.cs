// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;
using Xunit;

namespace System.ConfigurationTests
{
    public class SubclassTypeValidatorAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData(typeof(string))]
        public void GetBaseClass_FlowFromConstructor_Equals(Type expected)
        {
            SubclassTypeValidatorAttribute attr = new SubclassTypeValidatorAttribute(expected);
            Assert.Equal(expected, attr.BaseClass);
        }
    }
}