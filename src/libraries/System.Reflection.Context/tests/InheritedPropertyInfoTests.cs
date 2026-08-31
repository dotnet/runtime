// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Reflection.Context.Tests
{
    public class InheritedPropertyInfoTests
    {
        private readonly CustomReflectionContext _customReflectionContext = new VirtualPropertyInfoCustomReflectionContext();
        private TypeInfo _customTypeInfo;
        private TypeInfo _customTypeInfoToCheckForEquality;
        private PropertyInfo[] _propertyInfos;
        private PropertyInfo _propertyInfo;

        public InheritedPropertyInfoTests()
        {
            TypeInfo typeInfo = typeof(SecondTestObject).GetTypeInfo();
            _customTypeInfo = _customReflectionContext.MapType(typeInfo);
            _customTypeInfoToCheckForEquality = _customReflectionContext.MapType(typeInfo);
            _propertyInfos = _customTypeInfo.BaseType.GetTypeInfo().DeclaredProperties.ToArray();
            _propertyInfo = _customTypeInfo.GetProperty("number");
        }

        [Fact]
        public void GetCustomAttributes_WithType_ReturnsVirtualAttribute()
        {
            object[] attributes = _propertyInfos.FirstOrDefault(x => x.Name == "number").GetCustomAttributes(typeof(TestPropertyAttribute), true);
            Assert.Single(attributes);
            Assert.IsType<TestPropertyAttribute>(attributes[0]);
        }

        [Fact]
        public void GetProperty_NullGetter_ReturnsNull()
        {
            PropertyInfo nullGetter = _customTypeInfo.GetProperty("number2");
            Assert.Null(nullGetter.GetGetMethod());
        }

        [Fact]
        public void GetProperty_NonNullGetter_ReturnsInheritedMethodInfo()
        {
            PropertyInfo property = _customTypeInfo.GetProperty("number2");
            Assert.Equal("InheritedMethodInfo", property.GetSetMethod().GetType().Name);
        }

        [Fact]
        public void GetProperty_NullSetter_ReturnsNull()
        {
            PropertyInfo nullSetter = _customTypeInfo.GetProperty("number3");
            Assert.Null(nullSetter.GetSetMethod());
        }

        [Fact]
        public void GetHashCode_DifferentObjectSameType_AreEqual()
        {
            PropertyInfo differentObjectSameType = _customTypeInfoToCheckForEquality.GetProperty("number");
            Assert.Equal(differentObjectSameType.GetHashCode(), _propertyInfo.GetHashCode());
        }

        [Fact]
        public void Equals_DifferentObjectSameType_ReturnsTrue()
        {
            PropertyInfo differentObjectSameType = _customTypeInfoToCheckForEquality.GetProperty("number");
            Assert.True(_propertyInfo.Equals(differentObjectSameType));
        }

        [Fact]
        public void Equals_DifferentObjectDifferentType_ReturnsFalse()
        {
            TestObject differentObjectDifferentType = new TestObject("a");
            Assert.False(_propertyInfo.Equals(differentObjectDifferentType));
        }

        [Fact]
        public void Equals_Null_ReturnsFalse()
        {
            Assert.False(_propertyInfo.Equals(null));
        }
    }
}
