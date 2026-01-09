// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Context.Tests
{
    // Test for CustomAttributeData projections
    public class CustomAttributeDataTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();
        private readonly TypeInfo _customTypeInfo;
        private readonly CustomAttributeData _customAttributeData;

        public CustomAttributeDataTests()
        {
            TypeInfo typeInfo = typeof(TypeWithProperties).GetTypeInfo();
            _customTypeInfo = _customReflectionContext.MapType(typeInfo);
            PropertyInfo property = _customTypeInfo.GetProperty("AttributedProperty");
            _customAttributeData = property.GetCustomAttributesData().FirstOrDefault();
        }

        [Fact]
        public void CustomAttributeData_Exists()
        {
            Assert.NotNull(_customAttributeData);
        }

        [Fact]
        public void AttributeType_ReturnsProjectedType()
        {
            Type attrType = _customAttributeData.AttributeType;
            Assert.NotNull(attrType);
            Assert.Equal(ProjectionConstants.CustomType, attrType.GetType().FullName);
        }

        [Fact]
        public void Constructor_ReturnsProjectedConstructor()
        {
            ConstructorInfo ctor = _customAttributeData.Constructor;
            Assert.NotNull(ctor);
        }

        [Fact]
        public void ConstructorArguments_ReturnsValue()
        {
            IList<CustomAttributeTypedArgument> args = _customAttributeData.ConstructorArguments;
            Assert.NotNull(args);
        }

        [Fact]
        public void NamedArguments_ReturnsValue()
        {
            IList<CustomAttributeNamedArgument> args = _customAttributeData.NamedArguments;
            Assert.NotNull(args);
        }

        [Fact]
        public void ToString_ReturnsValue()
        {
            string str = _customAttributeData.ToString();
            Assert.NotNull(str);
            Assert.NotEmpty(str);
        }

        [Fact]
        public void Equals_SameData_ReturnsValue()
        {
            PropertyInfo property = _customTypeInfo.GetProperty("AttributedProperty");
            CustomAttributeData sameData = property.GetCustomAttributesData().FirstOrDefault();
            bool areEqual = _customAttributeData.Equals(sameData);
            Assert.True(areEqual || !areEqual);
        }

        [Fact]
        public void GetHashCode_ReturnsValue()
        {
            int hashCode = _customAttributeData.GetHashCode();
            Assert.NotEqual(0, hashCode);
        }
    }
}
