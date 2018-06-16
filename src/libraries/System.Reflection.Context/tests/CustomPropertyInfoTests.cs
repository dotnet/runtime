// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Context.Tests
{
    public class CustomPropertyInfoTests
    {
        // Points to a PropertyInfo instance created by reflection. This doesn't work in a reflection-only context.
        private readonly PropertyInfo _customProperty;

        public CustomPropertyInfoTests()
        {
            var customReflectionContext = new TestCustomReflectionContext();
            TypeInfo typeInfo = typeof(TestObject).GetTypeInfo();
            TypeInfo customTypeInfo = customReflectionContext.MapType(typeInfo);
            IEnumerable<PropertyInfo> customProperties = customTypeInfo.DeclaredProperties;
            _customProperty = customProperties.First();
        }

        [Fact]
        public void GetCustomAttributesDataTest()
        {
            // CustomAttributesData operates on custom attributes themselves defined in code 
            // that is loaded in the reflection-only context.
            IList<CustomAttributeData> customAttributesData = _customProperty.GetCustomAttributesData();
            Assert.All(customAttributesData,
                cad => Assert.Equal(ProjectionConstants.ProjectingCustomAttributeData, cad.GetType().FullName));
        }
    }
}
