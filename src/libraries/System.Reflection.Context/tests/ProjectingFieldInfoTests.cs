// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Context.Tests
{
    public class ProjectingFieldInfoTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new VirtualPropertyInfoCustomReflectionContext();
        private TypeInfo _customTypeInfo;
        private FieldInfo _field;
        public ProjectingFieldInfoTests()
        {
            TypeInfo typeInfo = typeof(SecondTestObject).GetTypeInfo();
            _customTypeInfo = _customReflectionContext.MapType(typeInfo);
            _field = _customTypeInfo.GetField("field");
        }

        [Fact]
        public void DeclaringType_ReturnsCustomType()
        {
            Assert.Equal(ProjectionConstants.SecondTestObject, _field.DeclaringType.FullName);
        }

        [Fact]
        public void FieldType_ReturnsIntType()
        {
            Assert.Equal("Int32", _field.FieldType.Name);
        }

        [Fact]
        public void Module()
        {
            Assert.Equal(typeof(SecondTestObject).Module.Name, _field.Module.Name);
        }

        [Fact]
        public void ReflectedType()
        {
            Assert.Equal(typeof(SecondTestObject).Name, _field.ReflectedType.Name);
        }

        [Fact]
        public void GetCustomAttributesData()
        {
            IList<CustomAttributeData> attributes = _field.GetCustomAttributesData();
            Assert.Single(attributes);
        }
    }
}
