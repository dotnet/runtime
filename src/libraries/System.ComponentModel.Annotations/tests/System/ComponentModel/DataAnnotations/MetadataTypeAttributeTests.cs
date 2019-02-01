// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class MetadataTypeAttributeTests
    {
        [Fact]
        public static void Validate_GetTypeDescriptor_ReturnsMetadataType()
        {
            var provider = new AssociatedMetadataTypeTypeDescriptionProvider(typeof(SomeClassWithMetadataOnAnotherClass));
            ICustomTypeDescriptor typeDescriptor = provider.GetTypeDescriptor(typeof(SomeClassWithMetadataOnAnotherClass), null);
            PropertyDescriptorCollection props = typeDescriptor.GetProperties();
            PropertyDescriptor firstNameProp = props[nameof(SomeClassWithMetadataOnAnotherClass.FirstName)];
            string displayName = firstNameProp.DisplayName;
            Assert.Equal("First name", displayName);
        }

        class MetadataForAnotherClass
        {
            [DisplayName("First name")]
            public string FirstName { get; set; }
        }

        [MetadataType(typeof(MetadataForAnotherClass))]
        class SomeClassWithMetadataOnAnotherClass
        {
            public string FirstName { get; set; }
        }
    }
}
