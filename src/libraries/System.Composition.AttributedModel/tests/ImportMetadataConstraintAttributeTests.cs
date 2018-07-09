// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Composition.AttributeModel.Tests
{
    public class ImportMetadataConstraintAttributeTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("Name", "Value")]
        public void Ctor_Name_Value(string name, string value)
        {
            var attribute = new ImportMetadataConstraintAttribute(name, value);
            Assert.Equal(name, attribute.Name);
            Assert.Equal(value, attribute.Value);
        }
    }
}
