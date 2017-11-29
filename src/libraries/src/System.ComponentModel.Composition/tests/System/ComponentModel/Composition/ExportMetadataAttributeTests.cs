// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Composition
{
    public class ExportMetadataAttributeTests
    {
        [Fact]
        public void Constructor_NullAsNameArgument_ShouldSetNamePropertyToEmptyString()
        {
            var attribute = new ExportMetadataAttribute((string)null, "Value");

            Assert.Equal(string.Empty, attribute.Name);
        }

        [Fact]
        public void Constructor_ShouldSetIsMultiplePropertyToFalse()
        {
            var attribute = new ExportMetadataAttribute("Name", "Value");

            Assert.False(attribute.IsMultiple);
        }

        [Fact]
        public void Constructor_ValueAsNameArgument_ShouldSetNameProperty()
        {
            var expectations = Expectations.GetMetadataNames();
            
            foreach (var e in expectations)
            {
                var attribute = new ExportMetadataAttribute(e, "Value");

                Assert.Equal(e, attribute.Name);                
            }
        }

        [Fact]
        public void Constructor_ValueAsValueArgument_ShouldSetValueProperty()
        {
            var expectations = Expectations.GetMetadataValues();
            
            foreach (var e in expectations)
            {
                var attribute = new ExportMetadataAttribute("Name", e);

                Assert.Equal(e, attribute.Value);
            }
        }

        [Fact]
        public void IsMultiple_ValueAsValueArgument_ShouldSetPropert()
        {
            var expectations = Expectations.GetBooleans();

            var attribute = new ExportMetadataAttribute("Name", "Value");

            foreach (var e in expectations)
            {
                attribute.IsMultiple = e;
                Assert.Equal(e, attribute.IsMultiple);
            }
        }
    }
}
