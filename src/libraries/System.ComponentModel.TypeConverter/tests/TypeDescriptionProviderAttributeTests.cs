// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class TypeDescriptionProviderAttributeTests
    {
        [Fact]
        public void GetTypeName_FromString()
        {
            var name = "name";
            var attribute = new TypeDescriptionProviderAttribute(name);

            Assert.Equal(name, attribute.TypeName);
        }

        [Fact]
        public void GetTypeName_FromAttribute()
        {
            var type = typeof(TypeDescriptionProviderAttribute);
            var attribute = new TypeDescriptionProviderAttribute(type);

            Assert.Equal(type.AssemblyQualifiedName, attribute.TypeName);
        }
    }
}
